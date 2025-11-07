using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace QuickBrain.Modules;

public class HistoryManager : IDisposable
{
    private readonly QuickBrain.Settings _settings;
    private readonly List<HistoryEntry> _history = new();
    private readonly string _historyFilePath;
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;
    private int _maxSize;

    public HistoryManager(QuickBrain.Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _maxSize = _settings.HistoryLimit;
        _historyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerToys",
            "QuickBrain",
            "history.json"
        );

        LoadHistory();
    }

    public void Add(CalculationResult result)
    {
        if (!_settings.EnableHistory || result.IsError)
        {
            return;
        }

        _lock.EnterWriteLock();
        try
        {
            var entry = HistoryEntry.FromCalculationResult(result);
            
            // Remove duplicate if exists (same expression and result)
            var existingIndex = _history.FindIndex(e => 
                string.Equals(e.Expression, entry.Expression, StringComparison.Ordinal) &&
                string.Equals(e.Result, entry.Result, StringComparison.Ordinal));
            
            if (existingIndex >= 0)
            {
                _history.RemoveAt(existingIndex);
            }
            
            _history.Insert(0, entry);

            while (_history.Count > _maxSize)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            SaveHistory();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public IReadOnlyList<HistoryEntry> GetAll()
    {
        _lock.EnterReadLock();
        try
        {
            return _history.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IReadOnlyList<HistoryEntry> GetHistory(int count = 10)
    {
        _lock.EnterReadLock();
        try
        {
            return _history.Take(count).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IReadOnlyList<HistoryEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<HistoryEntry>();
        }

        var lowerQuery = query.ToLower();
        _lock.EnterReadLock();
        try
        {
            return _history
                .Where(e => e.Title.ToLower().Contains(lowerQuery) ||
                           e.Result.ToLower().Contains(lowerQuery) ||
                           e.Expression.ToLower().Contains(lowerQuery))
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IReadOnlyList<HistoryEntry> Filter(CalculationType type)
    {
        _lock.EnterReadLock();
        try
        {
            return _history.Where(e => e.Type == type).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public string Export(string format = "json")
    {
        _lock.EnterReadLock();
        try
        {
            return format.ToLower() switch
            {
                "csv" => ExportToCsv(),
                "json" => ExportToJson(),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private string ExportToCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Added,Calculated,Executed,Title,Expression,Result,Type,NumericValue,Unit");

        foreach (var entry in _history)
        {
            var numericValue = entry.NumericValue?.ToString() ?? string.Empty;
            sb.AppendLine($"\"{entry.Added:o}\",\"{entry.Calculated?.ToString("o") ?? ""}\",\"{entry.Executed?.ToString("o") ?? ""}\",\"{EscapeCsv(entry.Title)}\",\"{EscapeCsv(entry.Expression)}\",\"{EscapeCsv(entry.Result)}\",\"{entry.Type}\",\"{numericValue}\",\"{EscapeCsv(entry.Unit ?? "")}\"");
        }

        return sb.ToString();
    }

    private string ExportToJson()
    {
        return JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    public void ClearHistory()
    {
        _lock.EnterWriteLock();
        try
        {
            _history.Clear();
            SaveHistory();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void SetMaxSize(int limit)
    {
        if (limit <= 0)
        {
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));
        }

        _lock.EnterWriteLock();
        try
        {
            _maxSize = limit;
            while (_history.Count > _maxSize)
            {
                _history.RemoveAt(_history.Count - 1);
            }
            SaveHistory();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public HistoryStatistics GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            var stats = new HistoryStatistics
            {
                TotalCount = _history.Count,
                MaxSize = _maxSize,
                OldestEntry = _history.LastOrDefault()?.Added,
                MostRecentEntry = _history.FirstOrDefault()?.Added
            };

            var typeGroups = _history.GroupBy(e => e.Type).ToList();
            stats.CountByType = typeGroups.ToDictionary(g => g.Key.ToString(), g => g.Count());

            if (_history.Any(e => e.NumericValue.HasValue))
            {
                stats.NumericValuesCount = _history.Count(e => e.NumericValue.HasValue);
                stats.AverageNumericValue = _history
                    .Where(e => e.NumericValue.HasValue)
                    .Average(e => e.NumericValue!.Value);
            }

            return stats;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var json = File.ReadAllText(_historyFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                List<HistoryEntry>? history = null;
                try
                {
                    history = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
                }
                catch (Exception deserializeEx)
                {
                    Console.WriteLine($"Failed to deserialize history JSON: {deserializeEx.Message}");
                    try
                    {
                        File.Delete(_historyFilePath);
                    }
                    catch
                    {
                        // Ignore errors during file deletion
                    }
                    return;
                }

                if (history != null)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _history.AddRange(history.Take(_maxSize));
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load history: {ex.Message}");
        }
    }

    private void SaveHistory()
    {
        try
        {
            var directory = Path.GetDirectoryName(_historyFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save history: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                SaveHistory();
            }
            finally
            {
                _lock?.Dispose();
                _disposed = true;
            }
        }

        GC.SuppressFinalize(this);
    }
}
