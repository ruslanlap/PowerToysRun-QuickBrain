using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using QuickBrain.Modules;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.QuickBrain
{
    /// <summary>
    /// PowerToys Run QuickBrain plugin — merged implementation:
    /// - Keeps template structure (theme-aware icons, context menu, IDisposable pattern)
    /// - Integrates working logic (settings load/save, modules, query pipeline, history, optional AI explain)
    /// </summary>
    public class Main : IPlugin, IContextMenu, ISettingProvider, IReloadable, IDisposable
    {
        private const string SettingsFileName = "settings.json";

        /// <summary>
        /// ID of the plugin.
        /// </summary>
        public static string PluginID => "36FD3BCBE8684A548E91C95315804782";

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "QuickBrain";

        /// <summary>
        /// Description of the plugin.
        /// </summary>
        public string Description => "Smart calculator with natural language support, unit conversions, date calculations, and AI-powered computations";

        private PluginInitContext _context = null!;
        private string _iconPath = string.Empty;
        private bool _disposed;

        private global::QuickBrain.Settings _settings = new();
        private string _settingsPath = string.Empty;

        // Result cache for performance optimization (50ms → 1-2ms for repeated queries)
        private ResultCache _resultCache = null!;

        // Query classifier for smart module routing (95%+ accuracy)
        private QueryClassifier _classifier = null!;

        // Core modules - Lazy initialization for better startup performance
        private Lazy<MathEngine> _mathEngine = null!;
        private Lazy<Converter> _converter = null!;
        private Lazy<DateCalc> _dateCalc = null!;
        private Lazy<LogicEval> _logicEval = null!;
        private Lazy<ExpressionParser> _expressionParser = null!;
        private Lazy<NaturalLanguageProcessor> _nlpProcessor = null!;
        private HistoryManager _historyManager = null!; // Keep eager - always needed
        private AiStreamingModule? _aiStreaming;

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());

            _settingsPath = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, SettingsFileName);

            LoadSettings();
            InitializeModules();

            Console.WriteLine($"QuickBrain plugin initialized. Settings: {_settings.ToJsonForLogging()}");
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            try
            {
                var input = query?.Search?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(input))
                {
                    return GetDefaultResults();
                }

                // Check cache first for non-dynamic queries (skip AI/history commands)
                bool isDynamicQuery = input.StartsWith("ai ", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("ask ", StringComparison.OrdinalIgnoreCase) ||
                                     input.StartsWith("history", StringComparison.OrdinalIgnoreCase);

                if (!isDynamicQuery && _resultCache.TryGet(input, out var cachedResults))
                {
                    return cachedResults;
                }

                // Check for AI streaming query (starts with "ai " or "ask ")
                if (input.StartsWith("ai ", StringComparison.OrdinalIgnoreCase))
                {
                    var aiPrompt = input.Substring(3).Trim();
                    if (!string.IsNullOrWhiteSpace(aiPrompt))
                    {
                        _aiStreaming ??= new AiStreamingModule(_settings);
                        var aiResult = _aiStreaming.ProcessQuery(
                            query?.RawQuery ?? string.Empty,
                            aiPrompt,
                            GetIconFor(CalculationType.AiAssisted),
                            rawQuery => _context?.API?.ChangeQuery(rawQuery, true));
                        
                        if (aiResult != null)
                        {
                            return new List<Result> { aiResult };
                        }
                    }
                }

                if (input.StartsWith("ask ", StringComparison.OrdinalIgnoreCase))
                {
                    var aiPrompt = input.Substring(4).Trim();
                    if (!string.IsNullOrWhiteSpace(aiPrompt))
                    {
                        _aiStreaming ??= new AiStreamingModule(_settings);
                        var aiResult = _aiStreaming.ProcessQuery(
                            query?.RawQuery ?? string.Empty,
                            aiPrompt,
                            GetIconFor(CalculationType.AiAssisted),
                            rawQuery => _context?.API?.ChangeQuery(rawQuery, true));
                        
                        if (aiResult != null)
                        {
                            return new List<Result> { aiResult };
                        }
                    }
                }

                // History commands
                if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
                {
                    return GetHistoryResults();
                }
                if (input.StartsWith("history ", StringComparison.OrdinalIgnoreCase))
                {
                    var subCommand = input.Substring(8).Trim();
                    
                    if (subCommand.Equals("clear", StringComparison.OrdinalIgnoreCase))
                    {
                        _historyManager?.ClearHistory();
                        return new List<Result>
                        {
                            new Result
                            {
                                IcoPath = GetIconFor(CalculationType.Error),
                                Title = "History cleared",
                                SubTitle = "All calculation history has been removed",
                                Score = 100
                            }
                        };
                    }
                    else if (subCommand.Equals("open", StringComparison.OrdinalIgnoreCase))
                    {
                        var historyPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "PowerToys", "QuickBrain", "history.json");
                        
                        return new List<Result>
                        {
                            new Result
                            {
                                IcoPath = GetIconFor(CalculationType.Error),
                                Title = "Open history file",
                                SubTitle = historyPath,
                                Score = 100,
                                Action = _ =>
                                {
                                    try
                                    {
                                        if (!File.Exists(historyPath))
                                        {
                                            Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);
                                            File.WriteAllText(historyPath, "[]");
                                        }
                                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                        {
                                            FileName = historyPath,
                                            UseShellExecute = true
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to open history file: {ex.Message}");
                                    }
                                    return true;
                                }
                            }
                        };
                    }
                    else
                    {
                        // Search in history
                        return SearchHistoryResults(subCommand);
                    }
                }

                // Smart module routing with QueryClassifier
                var priority = _classifier.GetModulePriority(input);
                CalculationResult? calculationResult = null;

                // Try modules in priority order (smart routing)
                foreach (var moduleType in priority)
                {
                    calculationResult = TryModule(moduleType, input);
                    if (calculationResult != null && !calculationResult.IsError)
                    {
                        break; // Found result, stop trying
                    }
                }

                // 5) Natural language / AI compute
                if (calculationResult == null && _settings.EnableNaturalLanguage)
                {
                    try
                    {
                        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.AiTimeout));
                        var nlpTask = Task.Run(() => _nlpProcessor.Value.ProcessAsync(input, cts.Token), cts.Token);
                        
                        if (nlpTask.Wait(Math.Min(2000, _settings.AiTimeout)))
                        {
                            var nlp = nlpTask.Result;
                            if (nlp != null && !nlp.IsError)
                            {
                                calculationResult = nlp;
                            }
                        }
                        else
                        {
                            Console.WriteLine("NLP processing timed out in UI");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"NaturalLanguageProcessor failed: {ex.Message}");
                    }
                }

                // 6) Fallback
                if (calculationResult == null)
                {
                    calculationResult = CalculationResult.Error("Unable to process query", input);
                }

                // 7) History
                if (!calculationResult.IsError)
                {
                    _historyManager?.Add(calculationResult);
                }

                // Surface result to Run
                var iconFor = GetIconFor(calculationResult.Type);
                results.Add(new Result
                {
                    QueryTextDisplay = input,
                    IcoPath = iconFor,
                    Title = $"QuickBrain: {input}",
                    SubTitle = $"Result: {calculationResult.Result}",
                    ToolTipData = new ToolTipData("QuickBrain", calculationResult.SubTitle ?? string.Empty),
                    Score = 100,
                    Action = _ =>
                    {
                        try
                        {
                            Clipboard.SetDataObject(calculationResult.Result ?? string.Empty);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to copy result to clipboard: {ex.Message}");
                        }
                        return true;
                    },
                    ContextData = new ContextPayload(input, calculationResult.Result, calculationResult.SubTitle ?? string.Empty)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing query '{query?.Search}': {ex.Message}");
                results.Add(CreateErrorResult(ex.Message, input));
            }

            // Cache successful results for non-dynamic queries
            if (results.Count > 0 && !isDynamicQuery)
            {
                _resultCache.Set(input, results);
            }

            return results;
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var menu = new List<ContextMenuResult>();

            // Check if it's an AI streaming result
            if (selectedResult?.ContextData is string responseText && 
                !string.IsNullOrWhiteSpace(responseText) && 
                _aiStreaming != null &&
                _aiStreaming.IsStreaming)
            {
                return _aiStreaming.BuildContextMenu(responseText, Name);
            }

            // Settings menu available for all results
            menu.Add(new ContextMenuResult
            {
                PluginName = Name,
                Title = "Open Settings",
                FontFamily = "Segoe MDL2 Assets",
                Glyph = "\xE713", // Settings
                Action = _ =>
                {
                    try
                    {
                        if (!File.Exists(_settingsPath))
                        {
                            SaveSettings();
                        }
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = _settingsPath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to open settings file: {ex.Message}");
                    }
                    return true;
                }
            });

            // Calculation-specific context menu
            if (selectedResult?.ContextData is ContextPayload payload)
            {
                menu.Insert(0, new ContextMenuResult
                {
                    PluginName = Name,
                    Title = "Copy result (Ctrl+C)",
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE8C8", // Copy
                    AcceleratorKey = Key.C,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ => { Clipboard.SetDataObject(payload.Result ?? string.Empty); return true; },
                });

                menu.Insert(1, new ContextMenuResult
                {
                    PluginName = Name,
                    Title = "Copy input",
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE8C8",
                    Action = _ => { Clipboard.SetDataObject(payload.Input ?? string.Empty); return true; },
                });

                if (_settings.EnableAiCalculations)
                {
                    menu.Insert(2, new ContextMenuResult
                    {
                        PluginName = Name,
                        Title = "Explain via AI",
                        FontFamily = "Segoe MDL2 Assets",
                        Glyph = "\xE8D2", // Info
                        Action = _ =>
                        {
                            // Show immediate feedback
                            _context?.API?.ShowMsg("QuickBrain", "Generating AI explanation...");
                            
                            // Run async in background
                            Task.Run(async () =>
                            {
                                try
                                {
                                    var explanation = await ExplainViaAi(payload.Input);
                                    
                                    // Use Application.Current.Dispatcher for thread-safe UI update
                                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(explanation))
                                        {
                                            try
                                            {
                                                Clipboard.SetDataObject(explanation);
                                                _context?.API?.ShowMsg("QuickBrain", "AI explanation copied to clipboard.");
                                            }
                                            catch
                                            {
                                                // Fallback: show in message instead
                                                _context?.API?.ShowMsg("QuickBrain Explanation", explanation);
                                            }
                                        }
                                        else
                                        {
                                            _context?.API?.ShowMsg("QuickBrain", "No AI explanation available. Check AI settings.");
                                        }
                                    });
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                                    {
                                        _context?.API?.ShowMsg("QuickBrain Error", $"AI explanation failed: {ex.Message}");
                                    });
                                    Console.WriteLine($"ExplainViaAi error: {ex.Message}");
                                }
                            });
                            return true;
                        }
                    });
                }

                menu.Add(new ContextMenuResult
                {
                    PluginName = Name,
                    Title = "View in History",
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE81C", // History
                    Action = _ =>
                    {
                        _context?.API?.ChangeQuery("qb history " + payload.Input, true);
                        return false;
                    }
                });
            }

            return menu;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
            {
                return;
            }

            if (_context?.API != null)
            {
                _context.API.ThemeChanged -= OnThemeChanged;
            }

            _historyManager?.Dispose();

            // Dispose lazy-loaded modules only if they were initialized
            if (_nlpProcessor?.IsValueCreated == true)
            {
                _nlpProcessor.Value.Dispose();
            }

            _aiStreaming?.Dispose();

            _disposed = true;
        }

        // --- Internal helpers ---

        private void UpdateIconPath(Theme theme)
            => _iconPath = theme == Theme.Light || theme == Theme.HighContrastWhite
                ? "Images/quickbrain.light.png"
                : "Images/quickbrain.dark.png";

        private string GetIconFor(CalculationType type)
        {
            var theme = _context?.API?.GetCurrentTheme() ?? Theme.Dark;
            var suffix = theme == Theme.Light || theme == Theme.HighContrastWhite ? ".light.png" : ".dark.png";
            
            var iconPath = type switch
            {
                CalculationType.Arithmetic or CalculationType.Algebraic or CalculationType.Trigonometric or CalculationType.Logarithmic or CalculationType.Statistical => "Images/math",
                CalculationType.UnitConversion => "Images/convert",
                CalculationType.DateCalculation => "Images/date",
                CalculationType.LogicEvaluation => "Images/logic",
                CalculationType.Health => "Images/health",
                CalculationType.Money => "Images/money",
                CalculationType.NaturalLanguage or CalculationType.AiAssisted => "Images/ai",
                CalculationType.Error => "Images/error",
                _ => null
            };

            if (iconPath == null)
            {
                return _iconPath;
            }

            var themedIconPath = iconPath + suffix;
            var fullPath = Path.Combine(_context?.CurrentPluginMetadata?.PluginDirectory ?? string.Empty, themedIconPath);
            
            if (File.Exists(fullPath))
            {
                return themedIconPath;
            }

            // Fallback to .png if themed version doesn't exist
            var fallbackPath = iconPath + ".png";
            var fallbackFullPath = Path.Combine(_context?.CurrentPluginMetadata?.PluginDirectory ?? string.Empty, fallbackPath);
            
            return File.Exists(fallbackFullPath) ? fallbackPath : _iconPath;
        }

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);

        private List<Result> GetDefaultResults()
        {
            var results = new List<Result>();
            
            // AI streaming shortcut
            results.Add(new Result
            {
                IcoPath = GetIconFor(CalculationType.AiAssisted),
                Title = "ai <question>",
                SubTitle = "Ask AI with streaming response",
                Score = 100,
                ContextData = "tip"
            });

            // History shortcuts
            var theme = _context?.API?.GetCurrentTheme() ?? Theme.Dark;
            var historySuffix = theme == Theme.Light || theme == Theme.HighContrastWhite ? ".light.png" : ".dark.png";
            var historyIconPath = "Images/history" + historySuffix;
            var historyFullPath = Path.Combine(_context?.CurrentPluginMetadata?.PluginDirectory ?? string.Empty, historyIconPath);
            if (!File.Exists(historyFullPath))
            {
                historyIconPath = "Images/history.png";
            }
            
            results.Add(new Result
            {
                IcoPath = historyIconPath,
                Title = "history",
                SubTitle = "View calculation history",
                Score = 100,
                ContextData = "tip",
                Action = _ =>
                {
                    _context?.API?.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} history", true);
                    return false;
                }
            });

            results.Add(new Result
            {
                IcoPath = historyIconPath,
                Title = "history clear",
                SubTitle = "Clear all calculation history",
                Score = 99,
                ContextData = "tip",
                Action = _ =>
                {
                    _context?.API?.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} history clear", true);
                    return false;
                }
            });

            results.Add(new Result
            {
                IcoPath = historyIconPath,
                Title = "history open",
                SubTitle = "Open history.json file",
                Score = 98,
                ContextData = "tip",
                Action = _ =>
                {
                    _context?.API?.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} history open", true);
                    return false;
                }
            });

            var tips = new (string title, string subtitle, CalculationType type)[]
            {
                ("2+2", "Basic math", CalculationType.Arithmetic),
                ("10 km to miles", "Unit conversion", CalculationType.UnitConversion),
                ("20% of 500", "Percentage calculation", CalculationType.Arithmetic),
                ("bmi 180cm 75kg", "Body Mass Index", CalculationType.Health),
                ("tip 10% 450", "Tip calculator", CalculationType.Money),
                ("days between 2025-01-01 and 2025-12-31", "Date difference", CalculationType.DateCalculation),
                ("(5>3) and (2<1)", "Logic evaluation", CalculationType.LogicEvaluation)
            };
            
            foreach (var t in tips)
            {
                var example = t.title;
                results.Add(new Result
                {
                    IcoPath = GetIconFor(t.type),
                    Title = $"Example: {example}",
                    SubTitle = $"{t.subtitle} - Click to edit",
                    Score = 50,
                    ContextData = "tip",
                    Action = _ =>
                    {
                        // Insert example into query for editing
                        _context?.API?.ChangeQuery($"{_context.CurrentPluginMetadata.ActionKeyword} {example}", true);
                        return false;
                    }
                });
            }
            return results;
        }

        private List<Result> GetHistoryResults()
        {
            var results = new List<Result>();
            var history = _historyManager?.GetHistory(20) ?? new List<HistoryEntry>();

            var theme = _context?.API?.GetCurrentTheme() ?? Theme.Dark;
            var historySuffix = theme == Theme.Light || theme == Theme.HighContrastWhite ? ".light.png" : ".dark.png";
            var historyIconPath = "Images/history" + historySuffix;
            var historyFullPath = Path.Combine(_context?.CurrentPluginMetadata?.PluginDirectory ?? string.Empty, historyIconPath);
            if (!File.Exists(historyFullPath))
            {
                historyIconPath = "Images/history.png";
            }

            // Add management options at top
            results.Add(new Result
            {
                IcoPath = historyIconPath,
                Title = "Clear history",
                SubTitle = "Remove all calculation history",
                Score = 101,
                Action = _ =>
                {
                    _historyManager?.ClearHistory();
                    _context?.API?.ShowMsg("QuickBrain", "History cleared successfully");
                    return true;
                }
            });

            results.Add(new Result
            {
                IcoPath = historyIconPath,
                Title = "Open history file",
                SubTitle = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PowerToys", "QuickBrain", "history.json"),
                Score = 100,
                Action = _ =>
                {
                    try
                    {
                        var historyPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "PowerToys", "QuickBrain", "history.json");
                        
                        if (!File.Exists(historyPath))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(historyPath)!);
                            File.WriteAllText(historyPath, "[]");
                        }
                        
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = historyPath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _context?.API?.ShowMsg("QuickBrain Error", $"Failed to open file: {ex.Message}");
                    }
                    return true;
                }
            });

            if (!history.Any())
            {
                results.Add(new Result
                {
                    IcoPath = historyIconPath,
                    Title = "No history",
                    SubTitle = "No calculations in history yet",
                    Score = 90
                });
                return results;
            }

            foreach (var entry in history)
            {
                results.Add(new Result
                {
                    IcoPath = GetIconFor(entry.Type),
                    Title = entry.Title,
                    SubTitle = $"Result: {entry.Result} | {entry.Added:g}",
                    ToolTipData = new ToolTipData("History Entry", entry.Expression),
                    Score = 90,
                    Action = _ =>
                    {
                        try
                        {
                            Clipboard.SetDataObject(entry.Result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to copy result: {ex.Message}");
                        }
                        return true;
                    },
                    ContextData = new ContextPayload(entry.Expression, entry.Result, entry.Title)
                });
            }

            return results;
        }

        private List<Result> SearchHistoryResults(string searchTerm)
        {
            var results = new List<Result>();
            var history = _historyManager?.Search(searchTerm) ?? new List<HistoryEntry>();

            var theme = _context?.API?.GetCurrentTheme() ?? Theme.Dark;
            var historySuffix = theme == Theme.Light || theme == Theme.HighContrastWhite ? ".light.png" : ".dark.png";
            var historyIconPath = "Images/history" + historySuffix;
            var historyFullPath = Path.Combine(_context?.CurrentPluginMetadata?.PluginDirectory ?? string.Empty, historyIconPath);
            if (!File.Exists(historyFullPath))
            {
                historyIconPath = "Images/history.png";
            }

            if (!history.Any())
            {
                results.Add(new Result
                {
                    IcoPath = historyIconPath,
                    Title = $"No results for '{searchTerm}'",
                    SubTitle = "No matching calculations found in history",
                    Score = 100
                });
                return results;
            }

            foreach (var entry in history)
            {
                results.Add(new Result
                {
                    IcoPath = GetIconFor(entry.Type),
                    Title = entry.Title,
                    SubTitle = $"Result: {entry.Result} | {entry.Added:g}",
                    ToolTipData = new ToolTipData("History Entry", entry.Expression),
                    Score = 90,
                    Action = _ =>
                    {
                        try
                        {
                            Clipboard.SetDataObject(entry.Result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to copy result: {ex.Message}");
                        }
                        return true;
                    },
                    ContextData = new ContextPayload(entry.Expression, entry.Result, entry.Title)
                });
            }

            return results;
        }

        private CalculationResult? TryModule(CalculationType type, string input)
        {
            try
            {
                return type switch
                {
                    CalculationType.Arithmetic or CalculationType.Algebraic or CalculationType.Trigonometric or CalculationType.Logarithmic or CalculationType.Statistical
                        => _mathEngine.Value.Evaluate(input),
                    CalculationType.UnitConversion
                        => _converter.Value.Convert(input),
                    CalculationType.DateCalculation
                        => _dateCalc.Value.Calculate(input),
                    CalculationType.LogicEvaluation
                        => _logicEval.Value.Evaluate(input),
                    _ => null
                };
            }
            catch (DivideByZeroException ex)
            {
                return ErrorMessageBuilder.BuildError(ex, input);
            }
            catch (OverflowException ex)
            {
                return ErrorMessageBuilder.BuildError(ex, input);
            }
            catch (ArgumentException ex)
            {
                return ErrorMessageBuilder.BuildError(ex, input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Module {type} failed: {ex.Message}");
                return null; // Try next module
            }
        }

        private Result CreateErrorResult(string message, string? input = null)
        {
            // Use ErrorMessageBuilder for better context
            var errorResult = ErrorMessageBuilder.BuildSimpleError("QuickBrain Error", message, input);

            return new Result
            {
                IcoPath = GetIconFor(CalculationType.Error),
                Title = errorResult.Title,
                SubTitle = errorResult.SubTitle + " | Check logs for details.",
                Score = 0,
            };
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<global::QuickBrain.Settings>(json) ?? new global::QuickBrain.Settings();

                    if (!_settings.Validate(out var errors))
                    {
                        Console.WriteLine($"Settings validation failed: {string.Join(", ", errors)}");
                        _settings = new global::QuickBrain.Settings();
                    }
                }
                else
                {
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load settings from {_settingsPath}: {ex.Message}");
                _settings = new global::QuickBrain.Settings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings to {_settingsPath}: {ex.Message}");
            }
        }

        private void InitializeModules()
        {
            // Result cache for performance (100 entries LRU cache)
            _resultCache = new ResultCache(capacity: 100);

            // Query classifier for smart routing
            _classifier = new QueryClassifier();

            // Lazy initialization - modules created only when first used
            _mathEngine = new Lazy<MathEngine>(() => new MathEngine(_settings));
            _converter = new Lazy<Converter>(() => new Converter(_settings));
            _dateCalc = new Lazy<DateCalc>(() => new DateCalc(_settings));
            _logicEval = new Lazy<LogicEval>(() => new LogicEval(_settings));
            _expressionParser = new Lazy<ExpressionParser>(() => new ExpressionParser(_settings));
            _nlpProcessor = new Lazy<NaturalLanguageProcessor>(() => new NaturalLanguageProcessor(_settings));

            // HistoryManager is eager - always needed for tracking
            _historyManager = new HistoryManager(_settings);

            Console.WriteLine("QuickBrain modules initialized (lazy) with result caching");
        }

        public void ReloadData()
        {
            LoadSettings();
            InitializeModules(); // This also recreates the cache
            Console.WriteLine("QuickBrain plugin reloaded with fresh cache");
        }

        // ISettingProvider implementation
        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>
        {
            // General Settings
            new PluginAdditionalOption
            {
                Key = "MaxResults",
                DisplayLabel = "Maximum Results",
                DisplayDescription = "Maximum number of results to display",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _settings.MaxResults,
                NumberBoxMin = 1,
                NumberBoxMax = 20
            },
            new PluginAdditionalOption
            {
                Key = "Precision",
                DisplayLabel = "Decimal Precision",
                DisplayDescription = "Number of decimal places for calculation results",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _settings.Precision,
                NumberBoxMin = 0,
                NumberBoxMax = 28
            },
            new PluginAdditionalOption
            {
                Key = "AngleUnit",
                DisplayLabel = "Angle Unit",
                DisplayDescription = "Default unit for trigonometric functions",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("Radians", "Radians"),
                    new KeyValuePair<string, string>("Degrees", "Degrees"),
                    new KeyValuePair<string, string>("Gradians", "Gradians")
                },
                ComboBoxValue = (int)_settings.AngleUnit
            },
            
            // History Settings
            new PluginAdditionalOption
            {
                Key = "EnableHistory",
                DisplayLabel = "Enable History",
                DisplayDescription = "Save calculation history",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = _settings.EnableHistory
            },
            new PluginAdditionalOption
            {
                Key = "HistoryLimit",
                DisplayLabel = "History Limit",
                DisplayDescription = "Maximum number of history entries to keep",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _settings.HistoryLimit,
                NumberBoxMin = 0,
                NumberBoxMax = 1000
            },

            // Natural Language Settings
            new PluginAdditionalOption
            {
                Key = "EnableNaturalLanguage",
                DisplayLabel = "Enable Natural Language",
                DisplayDescription = "Process natural language queries (offline)",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = _settings.EnableNaturalLanguage
            },

            // AI Settings
            new PluginAdditionalOption
            {
                Key = "EnableAIParsing",
                DisplayLabel = "Enable AI Parsing",
                DisplayDescription = "Use AI to parse complex expressions",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = _settings.EnableAIParsing
            },
            new PluginAdditionalOption
            {
                Key = "EnableAiCalculations",
                DisplayLabel = "Enable AI Calculations",
                DisplayDescription = "Use AI for advanced calculations",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = _settings.EnableAiCalculations
            },
            new PluginAdditionalOption
            {
                Key = "OfflineFirst",
                DisplayLabel = "Offline First",
                DisplayDescription = "Try offline processing before using AI",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = _settings.OfflineFirst
            },
            new PluginAdditionalOption
            {
                Key = "AiTimeout",
                DisplayLabel = "AI Timeout (ms)",
                DisplayDescription = "Maximum time to wait for AI response",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _settings.AiTimeout,
                NumberBoxMin = 1000,
                NumberBoxMax = 60000
            },

            // AI Provider Selection
            new PluginAdditionalOption
            {
                Key = "AiProvider",
                DisplayLabel = "AI Provider",
                DisplayDescription = "Primary AI provider",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxItems = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("OpenAI", "0"),
                    new KeyValuePair<string, string>("Anthropic", "1")
                },
                ComboBoxValue = _settings.AiProvider == "Anthropic" ? 1 : 0
            },
            new PluginAdditionalOption
            {
                Key = "UseHuggingFace",
                DisplayLabel = "Use HuggingFace",
                DisplayDescription = "Enable HuggingFace provider",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = _settings.UseHuggingFace
            },
            new PluginAdditionalOption
            {
                Key = "UseOpenRouter",
                DisplayLabel = "Use OpenRouter",
                DisplayDescription = "Enable OpenRouter provider",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Value = _settings.UseOpenRouter
            },

            // OpenAI Settings
            new PluginAdditionalOption
            {
                Key = "OpenAiApiKey",
                DisplayLabel = "OpenAI API Key",
                DisplayDescription = "Your OpenAI API key",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.OpenAiApiKey ?? string.Empty
            },
            new PluginAdditionalOption
            {
                Key = "OpenAiModel",
                DisplayLabel = "OpenAI Model",
                DisplayDescription = "Model to use (e.g., gpt-4, gpt-3.5-turbo)",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.OpenAiModel
            },

            // Anthropic Settings
            new PluginAdditionalOption
            {
                Key = "AnthropicApiKey",
                DisplayLabel = "Anthropic API Key",
                DisplayDescription = "Your Anthropic API key",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.AnthropicApiKey ?? string.Empty
            },
            new PluginAdditionalOption
            {
                Key = "AnthropicModel",
                DisplayLabel = "Anthropic Model",
                DisplayDescription = "Model to use (e.g., claude-3-sonnet-20240229)",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.AnthropicModel
            },

            // HuggingFace Settings
            new PluginAdditionalOption
            {
                Key = "HuggingFaceApiKey",
                DisplayLabel = "HuggingFace API Key",
                DisplayDescription = "Your HuggingFace API key",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.HuggingFaceApiKey ?? string.Empty
            },
            new PluginAdditionalOption
            {
                Key = "HuggingFaceModel",
                DisplayLabel = "HuggingFace Model",
                DisplayDescription = "HuggingFace model to use",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.HuggingFaceModel
            },

            // OpenRouter Settings
            new PluginAdditionalOption
            {
                Key = "OpenRouterApiKey",
                DisplayLabel = "OpenRouter API Key",
                DisplayDescription = "Your OpenRouter API key",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.OpenRouterApiKey ?? string.Empty
            },
            new PluginAdditionalOption
            {
                Key = "OpenRouterModel",
                DisplayLabel = "OpenRouter Model",
                DisplayDescription = "OpenRouter model to use",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _settings.OpenRouterModel
            }
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions == null)
            {
                return;
            }

            // General Settings
            var maxResults = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "MaxResults");
            if (maxResults != null)
            {
                _settings.MaxResults = (int)maxResults.NumberValue;
            }

            var precision = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "Precision");
            if (precision != null)
            {
                _settings.Precision = (int)precision.NumberValue;
            }

            var angleUnit = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "AngleUnit");
            if (angleUnit != null)
            {
                _settings.AngleUnit = (global::QuickBrain.AngleUnit)angleUnit.ComboBoxValue;
            }

            // History Settings
            var enableHistory = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "EnableHistory");
            if (enableHistory != null)
            {
                _settings.EnableHistory = enableHistory.Value;
            }

            var historyLimit = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "HistoryLimit");
            if (historyLimit != null)
            {
                _settings.HistoryLimit = (int)historyLimit.NumberValue;
            }

            // Natural Language
            var enableNaturalLanguage = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "EnableNaturalLanguage");
            if (enableNaturalLanguage != null)
            {
                _settings.EnableNaturalLanguage = enableNaturalLanguage.Value;
            }

            // AI Settings
            var enableAIParsing = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "EnableAIParsing");
            if (enableAIParsing != null)
            {
                _settings.EnableAIParsing = enableAIParsing.Value;
            }

            var enableAiCalculations = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "EnableAiCalculations");
            if (enableAiCalculations != null)
            {
                _settings.EnableAiCalculations = enableAiCalculations.Value;
            }

            var offlineFirst = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "OfflineFirst");
            if (offlineFirst != null)
            {
                _settings.OfflineFirst = offlineFirst.Value;
            }

            var aiTimeout = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "AiTimeout");
            if (aiTimeout != null)
            {
                _settings.AiTimeout = (int)aiTimeout.NumberValue;
            }

            // AI Provider
            var aiProvider = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "AiProvider");
            if (aiProvider != null)
            {
                _settings.AiProvider = aiProvider.ComboBoxValue == 1 ? "Anthropic" : "OpenAI";
            }

            var useHuggingFace = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "UseHuggingFace");
            if (useHuggingFace != null)
            {
                _settings.UseHuggingFace = useHuggingFace.Value;
            }

            var useOpenRouter = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "UseOpenRouter");
            if (useOpenRouter != null)
            {
                _settings.UseOpenRouter = useOpenRouter.Value;
            }

            // API Keys
            var openAiApiKey = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "OpenAiApiKey");
            if (openAiApiKey != null && !string.IsNullOrWhiteSpace(openAiApiKey.TextValue))
            {
                _settings.OpenAiApiKey = openAiApiKey.TextValue;
            }

            var openAiModel = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "OpenAiModel");
            if (openAiModel != null && !string.IsNullOrWhiteSpace(openAiModel.TextValue))
            {
                _settings.OpenAiModel = openAiModel.TextValue;
            }

            var anthropicApiKey = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "AnthropicApiKey");
            if (anthropicApiKey != null && !string.IsNullOrWhiteSpace(anthropicApiKey.TextValue))
            {
                _settings.AnthropicApiKey = anthropicApiKey.TextValue;
            }

            var anthropicModel = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "AnthropicModel");
            if (anthropicModel != null && !string.IsNullOrWhiteSpace(anthropicModel.TextValue))
            {
                _settings.AnthropicModel = anthropicModel.TextValue;
            }

            var huggingFaceApiKey = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "HuggingFaceApiKey");
            if (huggingFaceApiKey != null && !string.IsNullOrWhiteSpace(huggingFaceApiKey.TextValue))
            {
                _settings.HuggingFaceApiKey = huggingFaceApiKey.TextValue;
            }

            var huggingFaceModel = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "HuggingFaceModel");
            if (huggingFaceModel != null && !string.IsNullOrWhiteSpace(huggingFaceModel.TextValue))
            {
                _settings.HuggingFaceModel = huggingFaceModel.TextValue;
            }

            var openRouterApiKey = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "OpenRouterApiKey");
            if (openRouterApiKey != null && !string.IsNullOrWhiteSpace(openRouterApiKey.TextValue))
            {
                _settings.OpenRouterApiKey = openRouterApiKey.TextValue;
            }

            var openRouterModel = settings.AdditionalOptions.FirstOrDefault(x => x.Key == "OpenRouterModel");
            if (openRouterModel != null && !string.IsNullOrWhiteSpace(openRouterModel.TextValue))
            {
                _settings.OpenRouterModel = openRouterModel.TextValue;
            }

            // Save and reload
            SaveSettings();
            InitializeModules();
        }

        private async Task<string> ExplainViaAi(string expression)
        {
            try
            {
                if (!_settings.EnableAiCalculations)
                {
                    return string.Empty;
                }

                var prompt = $"Briefly explain this mathematical expression: {expression}";
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.AiTimeout));
                var result = await _nlpProcessor.Value.ProcessAsync(prompt, cts.Token);
                if (result != null && !result.IsError && !string.IsNullOrWhiteSpace(result.SubTitle))
                {
                    return result.SubTitle;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error explaining via AI: {ex.Message}");
                return string.Empty;
            }
        }

        // Context payload to pass around values cleanly
        private sealed record ContextPayload(string Input, string Result, string SubTitle);
    }
}