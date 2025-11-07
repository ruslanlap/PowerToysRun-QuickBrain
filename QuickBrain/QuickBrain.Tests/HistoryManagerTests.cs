namespace QuickBrain.Tests;

public class HistoryManagerTests : IDisposable
{
    private readonly HistoryManager _historyManager;

    public HistoryManagerTests()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        _historyManager = new HistoryManager(settings);
        _historyManager.ClearHistory();
    }

    [Fact]
    public void HistoryManager_Add_AddsResultToHistory()
    {
        var result = CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic);

        _historyManager.Add(result);
        var history = _historyManager.GetHistory();

        Assert.Single(history);
        Assert.Equal("2 + 2", history[0].Title);
    }

    [Fact]
    public void HistoryManager_Add_DoesNotAddErrors()
    {
        var error = CalculationResult.Error("Test error");

        _historyManager.Add(error);
        var history = _historyManager.GetHistory();

        Assert.Empty(history);
    }

    [Fact]
    public void HistoryManager_GetHistory_ReturnsNewestFirst()
    {
        var result1 = CalculationResult.Success("First", "1", CalculationType.Arithmetic);
        var result2 = CalculationResult.Success("Second", "2", CalculationType.Arithmetic);

        _historyManager.Add(result1);
        _historyManager.Add(result2);
        var history = _historyManager.GetHistory();

        Assert.Equal(2, history.Count);
        Assert.Equal("Second", history[0].Title);
        Assert.Equal("First", history[1].Title);
    }

    [Fact]
    public void HistoryManager_ClearHistory_RemovesAllEntries()
    {
        _historyManager.Add(CalculationResult.Success("Test", "1", CalculationType.Arithmetic));
        _historyManager.ClearHistory();
        var history = _historyManager.GetHistory();

        Assert.Empty(history);
    }

    [Fact]
    public void HistoryManager_RespectsFifoLimit()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 3 };
        var historyManager = new HistoryManager(settings);
        historyManager.ClearHistory();

        for (int i = 1; i <= 5; i++)
        {
            historyManager.Add(CalculationResult.Success($"Result {i}", i.ToString(), CalculationType.Arithmetic));
        }

        var history = historyManager.GetHistory(10);

        Assert.Equal(3, history.Count);
        Assert.Equal("Result 5", history[0].Title);
        Assert.Equal("Result 4", history[1].Title);
        Assert.Equal("Result 3", history[2].Title);

        historyManager.Dispose();
    }

    [Fact]
    public void HistoryManager_DisabledHistorySetting_DoesNotAdd()
    {
        var settings = new Settings { EnableHistory = false };
        var historyManager = new HistoryManager(settings);
        historyManager.ClearHistory();

        historyManager.Add(CalculationResult.Success("Test", "1", CalculationType.Arithmetic));
        var history = historyManager.GetHistory();

        Assert.Empty(history);
        historyManager.Dispose();
    }

    [Fact]
    public void HistoryManager_Load_HandlesCorruptedJson()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        var historyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerToys",
            "QuickBrain",
            "history.json"
        );

        var directory = Path.GetDirectoryName(historyFilePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            File.WriteAllText(historyFilePath, "{ invalid json content");
            
            var historyManager = new HistoryManager(settings);
            var history = historyManager.GetHistory();

            Assert.Empty(history);
            
            historyManager.Dispose();
        }
        finally
        {
            if (File.Exists(historyFilePath))
            {
                File.Delete(historyFilePath);
            }
        }
    }

    [Fact]
    public void HistoryManager_Save_CreatesDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "QuickBrainHistoryTest", Guid.NewGuid().ToString());
        var historyFilePath = Path.Combine(tempDir, "PowerToys", "QuickBrain", "history.json");

        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA");
        
        try
        {
            Environment.SetEnvironmentVariable("APPDATA", tempDir);
            
            var historyManager = new HistoryManager(settings);
            historyManager.Add(CalculationResult.Success("Test", "1", CalculationType.Arithmetic));
            historyManager.Dispose();

            var expectedPath = Path.Combine(tempDir, "PowerToys", "QuickBrain", "history.json");
            Assert.True(File.Exists(expectedPath));
            
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(originalAppData))
            {
                Environment.SetEnvironmentVariable("APPDATA", originalAppData);
            }
        }
    }

    [Fact]
    public void HistoryManager_Search_FindsByExpression()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));
        _historyManager.Add(CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic));

        var results = _historyManager.Search("2 + 2");

        Assert.Single(results);
        Assert.Equal("2 + 2", results[0].Title);
    }

    [Fact]
    public void HistoryManager_Search_FindsByResult()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));
        _historyManager.Add(CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic));

        var results = _historyManager.Search("15");

        Assert.Single(results);
        Assert.Equal("5 * 3", results[0].Title);
    }

    [Fact]
    public void HistoryManager_Search_CaseInsensitive()
    {
        _historyManager.Add(CalculationResult.Success("10 + 2", "12", CalculationType.Arithmetic));

        var results = _historyManager.Search("ADD");

        Assert.Single(results);
    }

    [Fact]
    public void HistoryManager_Search_EmptyQuery_ReturnsEmpty()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));

        var results = _historyManager.Search("");

        Assert.Empty(results);
    }

    [Fact]
    public void HistoryManager_Filter_ByType()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));
        _historyManager.Add(CalculationResult.Success("10 km to miles", "6.2", CalculationType.UnitConversion));
        _historyManager.Add(CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic));

        var arithmeticResults = _historyManager.Filter(CalculationType.Arithmetic);

        Assert.Equal(2, arithmeticResults.Count);
        Assert.All(arithmeticResults, r => Assert.Equal(CalculationType.Arithmetic, r.Type));
    }

    [Fact]
    public void HistoryManager_Export_ToJson()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));
        _historyManager.Add(CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic));

        var json = _historyManager.Export("json");

        Assert.NotEmpty(json);
        Assert.Contains("[", json);
        Assert.Contains("]", json);
    }

    [Fact]
    public void HistoryManager_Export_ToCsv()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));

        var csv = _historyManager.Export("csv");

        Assert.NotEmpty(csv);
        Assert.Contains("Title,Expression,Result", csv);
        Assert.Contains("2 + 2", csv);
    }

    [Fact]
    public void HistoryManager_Export_InvalidFormat_ThrowsException()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));

        Assert.Throws<ArgumentException>(() => _historyManager.Export("xml"));
    }

    [Fact]
    public void HistoryManager_GetAll_ReturnsAllEntries()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));
        _historyManager.Add(CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic));

        var all = _historyManager.GetAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void HistoryManager_SetMaxSize_ReducesHistorySize()
    {
        for (int i = 1; i <= 5; i++)
        {
            _historyManager.Add(CalculationResult.Success($"Result {i}", i.ToString(), CalculationType.Arithmetic));
        }

        _historyManager.SetMaxSize(2);
        var history = _historyManager.GetAll();

        Assert.Equal(2, history.Count);
        Assert.Equal("Result 5", history[0].Title);
        Assert.Equal("Result 4", history[1].Title);
    }

    [Fact]
    public void HistoryManager_SetMaxSize_InvalidLimit_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => _historyManager.SetMaxSize(0));
        Assert.Throws<ArgumentException>(() => _historyManager.SetMaxSize(-1));
    }

    [Fact]
    public void HistoryManager_GetStats_ReturnsStatistics()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic).WithNumericValue(4));
        _historyManager.Add(CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic).WithNumericValue(15));

        var stats = _historyManager.GetStats();

        Assert.Equal(2, stats.TotalCount);
        Assert.Equal(10, stats.MaxSize);
        Assert.NotNull(stats.MostRecentEntry);
        Assert.NotNull(stats.OldestEntry);
        Assert.Contains(CalculationType.Arithmetic.ToString(), stats.CountByType.Keys);
    }

    [Fact]
    public void HistoryManager_GetStats_IncludesNumericStats()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic).WithNumericValue(4));
        _historyManager.Add(CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic).WithNumericValue(15));

        var stats = _historyManager.GetStats();

        Assert.Equal(2, stats.NumericValuesCount);
        Assert.Equal(9.5, stats.AverageNumericValue);
    }

    [Fact]
    public void HistoryManager_ThreadSafety_ConcurrentAdds()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 100 };
        var historyManager = new HistoryManager(settings);
        historyManager.ClearHistory();

        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int threadNum = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    historyManager.Add(CalculationResult.Success($"Result {threadNum}-{j}", (threadNum * 10 + j).ToString(), CalculationType.Arithmetic));
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        var history = historyManager.GetAll();
        Assert.Equal(100, history.Count);

        historyManager.Dispose();
    }

    [Fact]
    public void HistoryManager_HistoryEntry_HasTimestamps()
    {
        _historyManager.Add(CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic));

        var history = _historyManager.GetAll();
        var entry = history[0];

        Assert.NotEqual(DateTime.MinValue, entry.Added);
        Assert.NotNull(entry.Executed);
    }

    public void Dispose()
    {
        _historyManager?.Dispose();
        GC.SuppressFinalize(this);
    }
}
