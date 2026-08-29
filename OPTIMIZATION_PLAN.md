# 🚀 План оптимізації плагіна QuickBrain

**Версія:** 1.0
**Дата:** 2025-11-07
**Поточна версія плагіна:** v1.0.0

---

## 📊 Аналіз поточного стану

### Сильні сторони
- ✅ Модульна архітектура з окремими движками (Math, Converter, DateCalc, Logic)
- ✅ Підтримка AI провайдерів (OpenAI, Anthropic, HuggingFace, OpenRouter)
- ✅ Історія обчислень з пошуком та фільтрацією
- ✅ Тематичні іконки з підтримкою темної/світлої теми
- ✅ Комплексна валідація налаштувань
- ✅ Thread-safe операції з історією (ReaderWriterLockSlim)

### Критичні проблеми

#### 🔴 Продуктивність
1. **Main.cs надто великий** (1262 рядки) - порушення Single Responsibility Principle
2. **Синхронна обробка запитів** - блокує UI thread
3. **Ініціалізація всіх модулів** при старті, навіть якщо не використовуються
4. **Відсутність кешування** результатів обчислень
5. **AI таймаути** можуть сповільнювати роботу (2-10 секунд)
6. **Повторна серіалізація** історії при кожному збереженні

#### 🟡 User Experience
1. **Немає індикації завантаження** для AI запитів
2. **Помилки не завжди зрозумілі** користувачу
3. **Відсутність автодоповнення** та підказок
4. **Немає швидких дій** для популярних операцій
5. **Дублікати в історії** (незважаючи на спробу їх видалення)
6. **Немає візуальної ієрархії** результатів

#### 🟠 Логіка та User Flow
1. **Неоптимальна послідовність** модулів (Math → Converter → Date → Logic → NLP)
2. **AI запити обробляються окремо** від основного pipeline
3. **Відсутність "smart suggestions"** на базі історії та контексту
4. **Немає адаптивної пріоритизації** результатів
5. **Складний доступ** до історії та налаштувань

---

## 🎯 Цілі оптимізації

### Метрики успіху
- ⚡ **Швидкість відповіді**: < 50ms для простих обчислень, < 200ms для складних
- 📦 **Використання пам'яті**: зменшення на 30-40%
- 🎨 **UX метрики**: зменшення кількості кліків на 50%
- 📈 **Точність результатів**: 95%+ релевантність першого результату

---

## 📋 План оптимізації

### Фаза 1: Архітектурний рефакторинг 🏗️

#### 1.1 Розділення Main.cs на окремі компоненти
**Пріоритет:** 🔴 CRITICAL
**Складність:** High
**Час виконання:** 8-12 годин

**Що робити:**
```
Main.cs (1262 рядки) → Розділити на:
├── Main.cs (150-200 рядків) - точка входу, координація
├── QueryProcessor.cs - обробка запитів
├── ResultBuilder.cs - побудова результатів
├── ContextMenuBuilder.cs - контекстне меню
├── HistoryService.cs - сервіс історії
├── IconResolver.cs - резолвер іконок
└── SettingsManager.cs - менеджер налаштувань
```

**Переваги:**
- Легше тестувати окремі компоненти
- Краща читабельність та підтримка коду
- Можливість паралельної роботи над різними частинами
- Зменшення когнітивного навантаження

**Реалізація:**
```csharp
// Main.cs - тільки координація
public class Main : IPlugin, IContextMenu, ISettingProvider
{
    private QueryProcessor _queryProcessor;
    private ResultBuilder _resultBuilder;
    private ContextMenuBuilder _menuBuilder;

    public List<Result> Query(Query query)
        => _queryProcessor.Process(query);
}

// QueryProcessor.cs - логіка обробки
public class QueryProcessor
{
    private readonly ModulePipeline _pipeline;
    private readonly ResultCache _cache;

    public List<Result> Process(Query query)
    {
        if (_cache.TryGet(query.Search, out var cached))
            return cached;

        var result = _pipeline.Execute(query);
        _cache.Set(query.Search, result);
        return result;
    }
}
```

---

#### 1.2 Впровадження модульного pipeline з пріоритетами
**Пріоритет:** 🔴 CRITICAL
**Складність:** Medium
**Час виконання:** 4-6 годин

**Поточна проблема:**
```csharp
// Послідовно виконуються всі модулі, навіть якщо результат вже знайдено
var math = _mathEngine?.Evaluate(input);
var conv = _converter?.Convert(input);
var date = _dateCalc?.Calculate(input);
var logic = _logicEval?.Evaluate(input);
```

**Нова реалізація:**
```csharp
// Модулі з пріоритетами та ранньою зупинкою
public interface ICalculationModule
{
    int Priority { get; }
    bool CanHandle(string input);
    CalculationResult? TryCalculate(string input);
}

public class ModulePipeline
{
    private readonly List<ICalculationModule> _modules;

    public ModulePipeline()
    {
        _modules = new()
        {
            new QuickMathModule { Priority = 100 },      // "2+2" - найшвидше
            new ConverterModule { Priority = 90 },       // "10 km to miles"
            new DateCalcModule { Priority = 80 },        // розпізнає дати
            new LogicModule { Priority = 70 },           // логічні вирази
            new AdvancedMathModule { Priority = 60 },    // складна математика
            new NLPModule { Priority = 10 }              // останнє - NLP
        };
    }

    public CalculationResult Execute(string input)
    {
        foreach (var module in _modules.OrderByDescending(m => m.Priority))
        {
            if (module.CanHandle(input))
            {
                var result = module.TryCalculate(input);
                if (result != null && !result.IsError)
                    return result;
            }
        }
        return CalculationResult.Error("Unable to process query", input);
    }
}
```

**Оптимізація порядку:**
1. **QuickMath** (2+2, простa арифметика) - 1-5ms
2. **Converter** (100 km to miles) - 5-10ms
3. **DateCalc** (today + 7 days) - 10-20ms
4. **Logic** (true AND false) - 5-10ms
5. **AdvancedMath** (sin(pi/4)) - 10-30ms
6. **NLP/AI** (якщо enabled) - 100-3000ms

---

#### 1.3 Lazy initialization модулів
**Пріоритет:** 🟡 HIGH
**Складність:** Low
**Час виконання:** 2-3 години

**Поточна проблема:**
```csharp
// Всі модулі ініціалізуються при старті
private void InitializeModules()
{
    _mathEngine = new MathEngine(_settings);
    _converter = new Converter(_settings);
    _dateCalc = new DateCalc(_settings);
    _logicEval = new LogicEval(_settings);
    _nlpProcessor = new NaturalLanguageProcessor(_settings);
    // ... тощо
}
```

**Нова реалізація:**
```csharp
// Lazy initialization
private Lazy<MathEngine> _mathEngine;
private Lazy<Converter> _converter;
private Lazy<DateCalc> _dateCalc;

private void InitializeModules()
{
    _mathEngine = new Lazy<MathEngine>(() => new MathEngine(_settings));
    _converter = new Lazy<Converter>(() => new Converter(_settings));
    _dateCalc = new Lazy<DateCalc>(() => new DateCalc(_settings));
    // Ініціалізація тільки при першому використанні
}

// Використання
var result = _mathEngine.Value.Evaluate(input);
```

**Переваги:**
- Швидший старт плагіна (50-100ms → 10-20ms)
- Менше пам'яті при старті
- Модулі створюються тільки при потребі

---

### Фаза 2: Оптимізація продуктивності ⚡

#### 2.1 Кешування результатів обчислень
**Пріоритет:** 🔴 CRITICAL
**Складність:** Medium
**Час виконання:** 4-6 годин

**Реалізація:**
```csharp
public class ResultCache
{
    private readonly LruCache<string, List<Result>> _cache;
    private readonly int _maxSize = 100;

    public ResultCache(int maxSize = 100)
    {
        _cache = new LruCache<string, List<Result>>(maxSize);
    }

    public bool TryGet(string query, out List<Result> results)
    {
        var normalizedQuery = NormalizeQuery(query);
        return _cache.TryGetValue(normalizedQuery, out results);
    }

    public void Set(string query, List<Result> results)
    {
        var normalizedQuery = NormalizeQuery(query);
        _cache.AddOrUpdate(normalizedQuery, results);
    }

    private string NormalizeQuery(string query)
        => query.Trim().ToLowerInvariant();
}

// LRU Cache implementation
public class LruCache<TKey, TValue>
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;

    // ... implementation
}
```

**Очікувані результати:**
- Повторні запити: 50ms → 1-2ms
- Зменшення навантаження на CPU на 60-70%
- Кращий UX при виправленні помилок

---

#### 2.2 Async/Await для AI модулів
**Пріоритет:** 🔴 CRITICAL
**Складність:** Medium
**Час виконання:** 5-7 годин

**Поточна проблема:**
```csharp
// Блокування UI thread
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout));
var nlpTask = Task.Run(() => _nlpProcessor?.ProcessAsync(input, cts.Token), cts.Token);
if (nlpTask.Wait(Math.Min(2000, _settings.AiTimeout))) // БЛОКУВАННЯ!
{
    var nlp = nlpTask.Result;
}
```

**Нова реалізація:**
```csharp
// Async query processing
public async Task<List<Result>> QueryAsync(Query query, CancellationToken ct = default)
{
    var results = new List<Result>();

    // Швидкі модулі - синхронно
    var quickResult = TryQuickModules(query.Search);
    if (quickResult != null)
    {
        results.Add(quickResult);
        return results;
    }

    // Повільні модулі - асинхронно з timeout
    try
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMilliseconds(_settings.AiTimeout));

        var nlpResult = await _nlpProcessor.ProcessAsync(query.Search, cts.Token);
        if (nlpResult != null && !nlpResult.IsError)
        {
            results.Add(ConvertToResult(nlpResult));
        }
    }
    catch (OperationCanceledException)
    {
        // Timeout - показати placeholder
        results.Add(CreateTimeoutResult(query.Search));
    }

    return results;
}
```

**Переваги:**
- Не блокується UI thread
- Можливість показати проміжні результати
- Краща responsiveness

---

#### 2.3 Батчингова серіалізація історії
**Пріоритет:** 🟡 HIGH
**Складність:** Low
**Час виконання:** 2-3 години

**Поточна проблема:**
```csharp
// Збереження після кожної операції
public void Add(CalculationResult result)
{
    _history.Insert(0, entry);
    SaveHistory(); // Запис на диск КОЖНОГО РАЗУ!
}
```

**Нова реалізація:**
```csharp
public class HistoryManager
{
    private readonly Timer _saveTimer;
    private volatile bool _isDirty;

    public HistoryManager(Settings settings)
    {
        // Збереження кожні 30 секунд або при закритті
        _saveTimer = new Timer(SaveIfDirty, null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    public void Add(CalculationResult result)
    {
        _lock.EnterWriteLock();
        try
        {
            _history.Insert(0, entry);
            _isDirty = true; // Позначити як змінено
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void SaveIfDirty(object? state)
    {
        if (_isDirty)
        {
            SaveHistory();
            _isDirty = false;
        }
    }

    public void Dispose()
    {
        _saveTimer?.Dispose();
        if (_isDirty) SaveHistory(); // Останнє збереження
    }
}
```

**Переваги:**
- Зменшення I/O операцій на 90-95%
- Швидша робота з історією
- Менше зносу SSD

---

#### 2.4 Оптимізація MathEngine
**Пріоритет:** 🟡 HIGH
**Складність:** Medium
**Час виконання:** 4-5 годин

**Проблеми:**
1. Regex викликається багато разів для кожного виразу
2. Створення великих проміжних колекцій
3. Відсутність кешування для констант та функцій

**Оптимізації:**
```csharp
public class MathEngine
{
    // Compiled regex для продуктивності
    private static readonly Regex NumberPattern = new(@"(\d+\.?\d*|\.?\d+)",
        RegexOptions.Compiled);
    private static readonly Regex TokenPattern = new(@"(\d+\.?\d*|\.?\d+)|([+\-*/^%(),])|([a-z]+)",
        RegexOptions.Compiled);

    // Pre-computed values cache
    private readonly Dictionary<string, double> _expressionCache = new();

    public CalculationResult? Evaluate(string expression)
    {
        // Перевірка кешу
        if (_expressionCache.TryGetValue(expression, out var cachedResult))
        {
            return BuildResult(expression, cachedResult);
        }

        // ... обчислення

        // Кешування результату
        if (!result.IsError)
        {
            _expressionCache[expression] = result.NumericValue ?? 0;
        }

        return result;
    }

    // Оптимізація tokenization
    private List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>(expression.Length / 2); // Pre-allocate

        foreach (Match match in TokenPattern.Matches(expression))
        {
            // ... tokenization
        }

        return tokens;
    }
}
```

---

### Фаза 3: Покращення User Experience 🎨

#### 3.1 Інтелектуальні підказки (Smart Suggestions)
**Пріоритет:** 🟡 HIGH
**Складність:** Medium
**Час виконання:** 6-8 годин

**Реалізація:**
```csharp
public class SmartSuggestionEngine
{
    private readonly HistoryManager _history;
    private readonly Settings _settings;

    public List<Suggestion> GetSuggestions(string partialInput)
    {
        var suggestions = new List<Suggestion>();

        // 1. Історія (найчастіші запити)
        var frequentQueries = _history.GetMostFrequent(5)
            .Where(e => e.Expression.StartsWith(partialInput,
                StringComparison.OrdinalIgnoreCase));
        suggestions.AddRange(frequentQueries.Select(e => new Suggestion
        {
            Text = e.Expression,
            Type = SuggestionType.History,
            Icon = "history",
            Score = 100
        }));

        // 2. Контекстні підказки
        if (partialInput.Contains("to ", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.AddRange(GetConversionSuggestions(partialInput));
        }

        // 3. Функції та константи
        if (partialInput.EndsWith("("))
        {
            var funcName = ExtractFunctionName(partialInput);
            suggestions.Add(GetFunctionHelp(funcName));
        }

        return suggestions.OrderByDescending(s => s.Score).ToList();
    }

    private List<Suggestion> GetConversionSuggestions(string input)
    {
        // "100 km to " → підказати "miles", "meters", "feet"
        var commonConversions = new[]
        {
            "miles", "meters", "feet", "inches",
            "pounds", "kilograms", "celsius", "fahrenheit"
        };

        return commonConversions.Select(unit => new Suggestion
        {
            Text = input + unit,
            Type = SuggestionType.Conversion,
            Icon = "convert",
            Score = 80
        }).ToList();
    }
}
```

**UX покращення:**
- Автодоповнення на базі історії
- Контекстні підказки
- Документація функцій inline

---

#### 3.2 Покращені повідомлення про помилки
**Пріоритет:** 🟡 HIGH
**Складність:** Low
**Час виконання:** 3-4 години

**Поточні помилки:**
```
"Unable to process query"
"Invalid expression"
"Error"
```

**Нові помилки з контекстом:**
```csharp
public class ErrorMessageBuilder
{
    public static CalculationResult BuildError(Exception ex, string input)
    {
        return ex switch
        {
            DivideByZeroException => new CalculationResult
            {
                Title = "Помилка:Ділення на нуль",
                SubTitle = "Вираз міститьділення на нуль. Перевірте дані.",
                ErrorMessage = "Division by zero is undefined",
                Type = CalculationType.Error,
                IsError = true
            },

            OverflowException => new CalculationResult
            {
                Title = "Помилка: Переповнення",
                SubTitle = "Результат занадто великий для обчислення. Спробуйте менші числа.",
                ErrorMessage = ex.Message,
                Type = CalculationType.Error,
                IsError = true
            },

            FormatException when input.Contains("to") => new CalculationResult
            {
                Title = "Помилка конвертації",
                SubTitle = $"Не вдалося розпізнати одиниці виміру. Приклад: '100 km to miles'",
                ErrorMessage = ex.Message,
                Type = CalculationType.Error,
                IsError = true
            },

            ArgumentException => new CalculationResult
            {
                Title = "Невірний синтаксис",
                SubTitle = $"Помилка: {ex.Message}. Спробуйте '2+2' або 'sin(45)'",
                ErrorMessage = ex.Message,
                Type = CalculationType.Error,
                IsError = true
            },

            _ => new CalculationResult
            {
                Title = "Помилка обчислення",
                SubTitle = $"Не вдалося обробити '{input}'. Спробуйте інший формат.",
                ErrorMessage = ex.Message,
                Type = CalculationType.Error,
                IsError = true
            }
        };
    }
}
```

---

#### 3.3 Візуальна індикація для AI запитів
**Пріоритет:** 🟡 HIGH
**Складність:** Medium
**Час виконання:** 4-5 годин

**Реалізація:**
```csharp
public class AiProgressIndicator
{
    public Result CreateProgressResult(string query)
    {
        return new Result
        {
            Title = "🤔 Обробка AI запиту...",
            SubTitle = $"Запит: {query}",
            IcoPath = GetIconFor(CalculationType.AiAssisted),
            Score = 100,
            Action = _ =>
            {
                // Скасувати запит
                _cancellationTokenSource?.Cancel();
                return true;
            }
        };
    }

    public Result CreateStreamingResult(string query, string partialResponse)
    {
        return new Result
        {
            Title = $"💬 {query}",
            SubTitle = partialResponse + " ▌", // Cursor animation
            IcoPath = GetIconFor(CalculationType.AiAssisted),
            Score = 100
        };
    }
}
```

**UX покращення:**
- Показувати індикатор завантаження для AI
- Streaming відповідей (текст з'являється поступово)
- Можливість скасувати запит

---

#### 3.4 Швидкі дії (Quick Actions)
**Пріоритет:** 🟠 MEDIUM
**Складність:** Low
**Час виконання:** 2-3 години

**Реалізація:**
```csharp
public class QuickActionsBuilder
{
    public List<ContextMenuResult> BuildQuickActions(CalculationResult result)
    {
        var actions = new List<ContextMenuResult>();

        // 1. Копіювати тільки число
        if (result.NumericValue.HasValue)
        {
            actions.Add(new ContextMenuResult
            {
                Title = "Copy number only (Ctrl+Shift+C)",
                AcceleratorKey = Key.C,
                AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Glyph = "\xE8C8",
                Action = _ =>
                {
                    Clipboard.SetText(result.NumericValue.Value.ToString());
                    return true;
                }
            });
        }

        // 2. Конвертувати в інші одиниці
        if (result.Type == CalculationType.UnitConversion)
        {
            actions.Add(new ContextMenuResult
            {
                Title = "Convert to other units",
                Glyph = "\xE8AB",
                Action = _ =>
                {
                    _context.API.ChangeQuery($"qb {result.NumericValue} ", true);
                    return false;
                }
            });
        }

        // 3. Додати до виразу
        if (result.Type == CalculationType.Arithmetic)
        {
            actions.Add(new ContextMenuResult
            {
                Title = "Use in new calculation",
                Glyph = "\xE710",
                Action = _ =>
                {
                    _context.API.ChangeQuery($"qb {result.Result} + ", true);
                    return false;
                }
            });
        }

        return actions;
    }
}
```

---

#### 3.5 Категоризація та візуальна ієрархія результатів
**Пріоритет:** 🟠 MEDIUM
**Складність:** Medium
**Час виконання:** 4-5 годин

**Реалізація:**
```csharp
public class ResultRanker
{
    public List<Result> RankResults(List<CalculationResult> results, string query)
    {
        var rankedResults = new List<(Result result, int score)>();

        foreach (var calcResult in results)
        {
            var score = CalculateRelevanceScore(calcResult, query);
            var result = BuildResult(calcResult);
            result.Score = score;
            rankedResults.Add((result, score));
        }

        return rankedResults
            .OrderByDescending(r => r.score)
            .Select(r => r.result)
            .ToList();
    }

    private int CalculateRelevanceScore(CalculationResult result, string query)
    {
        var score = 100; // Base score

        // 1. Точна відповідність
        if (result.Type == GetExpectedType(query))
            score += 50;

        // 2. Швидкість обчислення (пріоритет швидшим)
        if (result.Type == CalculationType.Arithmetic)
            score += 30;

        // 3. Історичні дані (частота використання)
        var historyFrequency = _history.GetFrequency(result.RawExpression);
        score += historyFrequency * 5;

        // 4. Confidence (для AI)
        if (result.Type == CalculationType.AiAssisted)
            score -= 20; // AI має нижчий пріоритет

        return score;
    }

    private CalculationType GetExpectedType(string query)
    {
        if (query.Contains("to ")) return CalculationType.UnitConversion;
        if (Regex.IsMatch(query, @"\d{4}-\d{2}-\d{2}")) return CalculationType.DateCalculation;
        if (query.Contains("AND") || query.Contains("OR")) return CalculationType.LogicEvaluation;
        return CalculationType.Arithmetic;
    }
}
```

---

### Фаза 4: Логічні Use Cases та User Flow 🔄

#### 4.1 Інтелектуальне розпізнавання типу запиту
**Пріоритет:** 🔴 CRITICAL
**Складність:** High
**Час виконання:** 6-8 годин

**Реалізація:**
```csharp
public class QueryClassifier
{
    private readonly Dictionary<CalculationType, List<QueryPattern>> _patterns;

    public QueryClassifier()
    {
        _patterns = new()
        {
            [CalculationType.UnitConversion] = new()
            {
                new QueryPattern(@"\d+\s*(km|kg|lb|celsius|fahrenheit)\s+to\s+", 95),
                new QueryPattern(@"convert\s+\d+", 90),
                new QueryPattern(@"\d+\s+(miles|pounds|meters)\s+in\s+", 85)
            },

            [CalculationType.DateCalculation] = new()
            {
                new QueryPattern(@"\d{4}-\d{2}-\d{2}", 95),
                new QueryPattern(@"(today|tomorrow|yesterday)", 90),
                new QueryPattern(@"(days|weeks|months|years)\s+(between|from|after)", 85)
            },

            [CalculationType.Arithmetic] = new()
            {
                new QueryPattern(@"^\d+\s*[\+\-\*/]\s*\d+$", 100),
                new QueryPattern(@"\d+%\s+of\s+\d+", 90),
                new QueryPattern(@"(sin|cos|tan|sqrt|log)\(", 95)
            },

            [CalculationType.LogicEvaluation] = new()
            {
                new QueryPattern(@"\b(true|false)\b.*\b(AND|OR|NOT|XOR)\b", 95),
                new QueryPattern(@"\d+\s*(<|>|<=|>=|==|!=)\s*\d+", 85)
            },

            [CalculationType.Health] = new()
            {
                new QueryPattern(@"bmi\s+\d+", 95),
                new QueryPattern(@"body\s+mass\s+index", 90)
            },

            [CalculationType.Money] = new()
            {
                new QueryPattern(@"tip\s+\d+%", 95),
                new QueryPattern(@"(discount|tax|interest)\s+\d+", 85)
            }
        };
    }

    public (CalculationType type, int confidence) Classify(string query)
    {
        var scores = new Dictionary<CalculationType, int>();

        foreach (var (type, patterns) in _patterns)
        {
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(query, pattern.Pattern, RegexOptions.IgnoreCase))
                {
                    if (!scores.ContainsKey(type) || scores[type] < pattern.Confidence)
                    {
                        scores[type] = pattern.Confidence;
                    }
                }
            }
        }

        if (scores.Any())
        {
            var best = scores.OrderByDescending(s => s.Value).First();
            return (best.Key, best.Value);
        }

        return (CalculationType.Arithmetic, 0); // Default fallback
    }
}

public record QueryPattern(string Pattern, int Confidence);
```

**Переваги:**
- Швидше визначення правильного модуля
- Краща точність результатів
- Можливість адаптації на базі історії

---

#### 4.2 Контекстно-залежні підказки
**Пріоритет:** 🟡 HIGH
**Складність:** Medium
**Час виконання:** 5-6 годин

**Use Cases:**

**UC-1: Конвертація одиниць**
```
Користувач вводить: "100 km"
Система показує:
  → 100 km to miles (62.137 miles)
  → 100 km to meters (100,000 m)
  → 100 km to feet (328,084 ft)
```

**UC-2: Математичні функції**
```
Користувач вводить: "sin("
Система показує:
  → sin(x) - Calculate sine of x
  → Current angle unit: Radians
  → Example: sin(pi/2) = 1
```

**UC-3: Дати**
```
Користувач вводить: "2025"
Система показує:
  → Days between 2025-01-01 and today
  → Weeks until 2025-12-31
  → Is 2025 a leap year? No
```

**Реалізація:**
```csharp
public class ContextualHintsProvider
{
    public List<Result> GetHints(string partialInput, CalculationType expectedType)
    {
        return expectedType switch
        {
            CalculationType.UnitConversion => GetConversionHints(partialInput),
            CalculationType.DateCalculation => GetDateHints(partialInput),
            CalculationType.Arithmetic => GetMathHints(partialInput),
            _ => new List<Result>()
        };
    }

    private List<Result> GetConversionHints(string input)
    {
        var hints = new List<Result>();
        var match = Regex.Match(input, @"(\d+\.?\d*)\s*([a-z]+)");

        if (match.Success)
        {
            var value = double.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;

            var suggestions = GetSuggestedConversions(unit);
            foreach (var targetUnit in suggestions.Take(3))
            {
                var converted = ConvertUnits(value, unit, targetUnit);
                hints.Add(new Result
                {
                    Title = $"{value} {unit} to {targetUnit}",
                    SubTitle = $"= {converted:F2} {targetUnit}",
                    Score = 90,
                    Action = _ =>
                    {
                        _context.API.ChangeQuery($"qb {value} {unit} to {targetUnit}", true);
                        return false;
                    }
                });
            }
        }

        return hints;
    }
}
```

---

#### 4.3 Історія з інтелектуальними рекомендаціями
**Пріоритет:** 🟡 HIGH
**Складність:** Medium
**Час виконання:** 5-6 годин

**Нові можливості:**

1. **Частота використання**
```csharp
public class HistoryStatistics
{
    public Dictionary<string, int> ExpressionFrequency { get; set; }
    public Dictionary<CalculationType, int> TypeFrequency { get; set; }
    public List<string> MostFrequentExpressions { get; set; }
    public TimeSpan AverageCalculationTime { get; set; }
}

public class HistoryManager
{
    public List<string> GetFrequentExpressions(int count = 5)
    {
        return _history
            .GroupBy(e => e.Expression)
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.Key)
            .ToList();
    }

    public CalculationType GetMostUsedType()
    {
        return _history
            .GroupBy(e => e.Type)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
    }
}
```

2. **Смарт-рекомендації на базі історії**
```csharp
public class SmartRecommendations
{
    public List<Result> GetRecommendations(string currentQuery)
    {
        var recommendations = new List<Result>();

        // 1. Схожі запити з історії
        var similarQueries = _history.FindSimilar(currentQuery, 3);
        foreach (var query in similarQueries)
        {
            recommendations.Add(new Result
            {
                Title = $"📜 {query.Expression}",
                SubTitle = $"Result: {query.Result} | Used {query.UsageCount} times",
                Score = 85,
                Action = _ =>
                {
                    _context.API.ChangeQuery($"qb {query.Expression}", true);
                    return false;
                }
            });
        }

        // 2. Рекомендації на базі типу
        var currentType = _classifier.Classify(currentQuery).type;
        var relatedQueries = _history
            .Filter(currentType)
            .OrderByDescending(e => e.Added)
            .Take(2);

        foreach (var query in relatedQueries)
        {
            recommendations.Add(new Result
            {
                Title = $"🔍 Similar: {query.Expression}",
                SubTitle = query.Result,
                Score = 75
            });
        }

        return recommendations;
    }
}
```

3. **Експорт та аналітика історії**
```csharp
public class HistoryAnalytics
{
    public HistoryReport GenerateReport()
    {
        return new HistoryReport
        {
            TotalCalculations = _history.Count,
            MostUsedType = GetMostUsedType(),
            MostUsedFunction = GetMostUsedFunction(),
            AveragePerDay = CalculateAveragePerDay(),
            TopExpressions = GetTopExpressions(10),
            TypeDistribution = GetTypeDistribution(),
            Timeline = GetUsageTimeline()
        };
    }

    private Dictionary<string, int> GetTypeDistribution()
    {
        return _history
            .GroupBy(e => e.Type.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private List<TimelinePoint> GetUsageTimeline()
    {
        return _history
            .GroupBy(e => e.Added.Date)
            .Select(g => new TimelinePoint
            {
                Date = g.Key,
                Count = g.Count(),
                Types = g.GroupBy(e => e.Type).ToDictionary(t => t.Key.ToString(), t => t.Count())
            })
            .OrderBy(p => p.Date)
            .ToList();
    }
}
```

---

#### 4.4 Покращений AI workflow
**Пріоритет:** 🟠 MEDIUM
**Складність:** High
**Час виконання:** 8-10 годин

**Проблеми поточного підходу:**
- AI запити обробляються окремо від основного pipeline
- Відсутність fallback стратегії
- Немає контролю якості AI відповідей

**Новий підхід:**
```csharp
public class AiWorkflowManager
{
    private readonly List<IAiProvider> _providers;
    private readonly QueryClassifier _classifier;
    private readonly ResultValidator _validator;

    public async Task<CalculationResult> ProcessWithAi(
        string query,
        CalculationType expectedType,
        CancellationToken ct)
    {
        // 1. Спробувати offline спочатку (якщо OfflineFirst = true)
        if (_settings.OfflineFirst)
        {
            var offlineResult = TryOfflineProcessing(query, expectedType);
            if (offlineResult != null && _validator.IsValid(offlineResult, expectedType))
            {
                return offlineResult;
            }
        }

        // 2. AI processing з fallback
        CalculationResult? aiResult = null;
        Exception? lastException = null;

        foreach (var provider in _providers)
        {
            try
            {
                aiResult = await provider.ProcessAsync(query, expectedType, ct);

                // Валідація результату
                if (_validator.IsValid(aiResult, expectedType))
                {
                    aiResult.SubTitle += $" (via {provider.Name})";
                    return aiResult;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"AI provider {provider.Name} failed: {ex.Message}");
                continue; // Try next provider
            }
        }

        // 3. Fallback до offline якщо всі AI провайдери failed
        var fallbackResult = TryOfflineProcessing(query, expectedType);
        if (fallbackResult != null)
        {
            fallbackResult.SubTitle += " (offline fallback)";
            return fallbackResult;
        }

        // 4. Помилка
        return CalculationResult.Error(
            $"Unable to process query. {lastException?.Message ?? "No AI providers available"}",
            query);
    }
}

public class ResultValidator
{
    public bool IsValid(CalculationResult result, CalculationType expectedType)
    {
        if (result.IsError) return false;

        // Перевірка відповідності типу
        if (result.Type != expectedType && expectedType != CalculationType.Error)
        {
            // Допустимо якщо близькі типи
            if (!AreRelatedTypes(result.Type, expectedType))
                return false;
        }

        // Перевірка на некоректні значення
        if (result.NumericValue.HasValue)
        {
            if (double.IsNaN(result.NumericValue.Value) ||
                double.IsInfinity(result.NumericValue.Value))
                return false;
        }

        // Перевірка на порожній результат
        if (string.IsNullOrWhiteSpace(result.Result))
            return false;

        return true;
    }

    private bool AreRelatedTypes(CalculationType actual, CalculationType expected)
    {
        var relatedGroups = new[]
        {
            new[] { CalculationType.Arithmetic, CalculationType.Algebraic, CalculationType.Trigonometric },
            new[] { CalculationType.NaturalLanguage, CalculationType.AiAssisted }
        };

        return relatedGroups.Any(group => group.Contains(actual) && group.Contains(expected));
    }
}
```

---

### Фаза 5: Безпека та Privacy 🔒

#### 5.1 Шифрування API ключів
**Пріоритет:** 🟡 HIGH
**Складність:** Medium
**Час виконання:** 4-6 годин

**Реалізація:**
```csharp
public class SecureSettingsManager
{
    private readonly string _settingsPath;

    public void SaveSettings(Settings settings)
    {
        var secureSettings = settings.Clone();

        // Шифрування API ключів
        secureSettings.OpenAiApiKey = EncryptApiKey(settings.OpenAiApiKey);
        secureSettings.AnthropicApiKey = EncryptApiKey(settings.AnthropicApiKey);
        secureSettings.HuggingFaceApiKey = EncryptApiKey(settings.HuggingFaceApiKey);
        secureSettings.OpenRouterApiKey = EncryptApiKey(settings.OpenRouterApiKey);

        var json = JsonSerializer.Serialize(secureSettings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsPath, json);
    }

    private string? EncryptApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        // Використання Windows Data Protection API
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var encrypted = ProtectedData.Protect(
            bytes,
            entropy: null,
            scope: DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(encrypted);
    }

    private string? DecryptApiKey(string? encryptedKey)
    {
        if (string.IsNullOrWhiteSpace(encryptedKey))
            return null;

        try
        {
            var encrypted = Convert.FromBase64String(encryptedKey);
            var decrypted = ProtectedData.Unprotect(
                encrypted,
                entropy: null,
                scope: DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null; // Invalid or corrupted key
        }
    }
}
```

**Переваги:**
- API ключі не зберігаються в plain text
- Доступ тільки для поточного користувача Windows
- Захист від випадкового витоку через git/backup

---

#### 5.2 Privacy режим для історії
**Пріоритет:** 🟠 MEDIUM
**Складність:** Low
**Час виконання:** 2-3 години

**Реалізація:**
```csharp
public class Settings
{
    [JsonPropertyName("privacyMode")]
    public PrivacyMode PrivacyMode { get; set; } = PrivacyMode.Normal;

    [JsonPropertyName("excludeFromHistory")]
    public List<string> ExcludeFromHistory { get; set; } = new();
}

public enum PrivacyMode
{
    Normal,          // Зберігати все
    SensitiveOnly,   // Не зберігати персональні дані (дати народження, фін. дані)
    NoHistory        // Взагалі не зберігати історію
}

public class HistoryManager
{
    public void Add(CalculationResult result)
    {
        if (!_settings.EnableHistory || _settings.PrivacyMode == PrivacyMode.NoHistory)
            return;

        if (_settings.PrivacyMode == PrivacyMode.SensitiveOnly)
        {
            if (IsSensitiveQuery(result.RawExpression))
                return;
        }

        // Check exclude patterns
        if (_settings.ExcludeFromHistory.Any(pattern =>
            Regex.IsMatch(result.RawExpression ?? "", pattern)))
        {
            return;
        }

        // ... add to history
    }

    private bool IsSensitiveQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        // Персональні дані
        var sensitivePatterns = new[]
        {
            @"\b\d{4}-\d{2}-\d{2}\b",  // Дати (можуть бути датами народження)
            @"\b\d{3}-\d{2}-\d{4}\b",  // SSN-like
            @"\$\d+",                   // Фінансові суми
            @"salary|income|debt|loan", // Фінансові терміни
        };

        return sensitivePatterns.Any(pattern =>
            Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase));
    }
}
```

---

### Фаза 6: Тестування та якість 🧪

#### 6.1 Unit тести для всіх модулів
**Пріоритет:** 🔴 CRITICAL
**Складність:** High
**Час виконання:** 10-12 годин

**Покриття:**
- MathEngine: 100+ тестів для всіх функцій
- Converter: 50+ тестів для всіх одиниць
- DateCalc: 30+ тестів для дат
- LogicEval: 20+ тестів для логіки
- HistoryManager: 25+ тестів
- QueryClassifier: 40+ тестів

**Приклад:**
```csharp
public class MathEngineTests
{
    [Theory]
    [InlineData("2+2", 4)]
    [InlineData("10-3", 7)]
    [InlineData("5*6", 30)]
    [InlineData("15/3", 5)]
    [InlineData("2^8", 256)]
    [InlineData("sqrt(25)", 5)]
    [InlineData("sin(0)", 0)]
    public void Evaluate_BasicOperations_ReturnsCorrectResult(string expression, double expected)
    {
        var engine = new MathEngine(new Settings());
        var result = engine.Evaluate(expression);

        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal(expected, result.NumericValue.Value, precision: 4);
    }

    [Fact]
    public void Evaluate_DivisionByZero_ReturnsError()
    {
        var engine = new MathEngine(new Settings());
        var result = engine.Evaluate("10/0");

        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Contains("zero", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
```

---

#### 6.2 Performance benchmarks
**Пріоритет:** 🟡 HIGH
**Складність:** Medium
**Час виконання:** 4-5 годин

**Реалізація:**
```csharp
public class PerformanceBenchmarks
{
    [Benchmark]
    public void SimpleArithmetic()
    {
        var engine = new MathEngine(_settings);
        engine.Evaluate("2+2");
    }

    [Benchmark]
    public void ComplexExpression()
    {
        var engine = new MathEngine(_settings);
        engine.Evaluate("sin(pi/4) * sqrt(2) + log10(100)");
    }

    [Benchmark]
    public void UnitConversion()
    {
        var converter = new Converter(_settings);
        converter.Convert("100 km to miles");
    }

    [Benchmark]
    public void HistorySave()
    {
        var history = new HistoryManager(_settings);
        var result = CalculationResult.Success("2+2", "4", CalculationType.Arithmetic);
        history.Add(result);
    }
}
```

**Цільові метрики:**
- SimpleArithmetic: < 1ms
- ComplexExpression: < 5ms
- UnitConversion: < 3ms
- HistorySave (batched): < 0.5ms

---

## 📈 Метрики успіху

### Продуктивність
- ✅ **Швидкість відповіді:**
  - Прості обчислення: < 50ms
  - Складні обчислення: < 200ms
  - AI запити: < 3s

- ✅ **Використання пам'яті:**
  - Базове споживання: < 50MB
  - З історією (100 записів): < 60MB
  - Зменшення на 30-40% порівняно з поточною версією

- ✅ **CPU навантаження:**
  - Idle: < 0.1%
  - Under load: < 5%

### User Experience
- ✅ **Точність результатів:**
  - Перший результат релевантний: 95%+
  - Помилки зрозумілі та корисні: 100%

- ✅ **Зручність:**
  - Зменшення кліків на 50%
  - Автодоповнення працює: 80%+ випадків
  - Підказки корисні: 90%+ користувачів

### Надійність
- ✅ **Тестове покриття:** 80%+
- ✅ **Crash rate:** < 0.1%
- ✅ **Error recovery:** 100% (graceful degradation)

---

## 🗓️ Графік реалізації

### Тиждень 1-2: Фаза 1 (Архітектурний рефакторинг)
- День 1-3: Розділення Main.cs
- День 4-5: Модульний pipeline
- День 6-7: Lazy initialization + code review

### Тиждень 3-4: Фаза 2 (Оптимізація продуктивності)
- День 1-2: Кешування результатів
- День 3-4: Async/Await для AI
- День 5: Батчингова серіалізація
- День 6-7: Оптимізація MathEngine + benchmarks

### Тиждень 5-6: Фаза 3 (UX покращення)
- День 1-2: Smart suggestions
- День 3: Покращені помилки
- День 4-5: AI індикація
- День 6-7: Quick actions + візуальна ієрархія

### Тиждень 7-8: Фаза 4 (Use Cases та Flow)
- День 1-3: Query classifier
- День 4-5: Контекстні підказки
- День 6-7: Історія з рекомендаціями

### Тиждень 9: Фаза 5 (Безпека)
- День 1-3: Шифрування API ключів
- День 4-5: Privacy режим

### Тиждень 10: Фаза 6 (Тестування)
- День 1-5: Unit тести
- День 6-7: Integration тести та benchmarks

### Тиждень 11-12: Фінал
- День 1-3: Bug fixes
- День 4-5: Документація
- День 6-7: Підготовка релізу v2.0.0

---

## 🎯 Пріоритизація для швидкого старту

### Must Have (версія 1.1)
1. ✅ Кешування результатів (2.1)
2. ✅ Розділення Main.cs (1.1)
3. ✅ Query classifier (4.1)
4. ✅ Покращені помилки (3.2)
5. ✅ Lazy initialization (1.3)

**Очікуваний результат:** +50% продуктивність, +30% UX

### Should Have (версія 1.5)
1. ✅ Модульний pipeline (1.2)
2. ✅ Smart suggestions (3.1)
3. ✅ Async/Await AI (2.2)
4. ✅ Контекстні підказки (4.2)
5. ✅ Батчингова серіалізація (2.3)

**Очікуваний результат:** +70% продуктивність, +60% UX

### Nice to Have (версія 2.0)
1. ✅ AI workflow improvements (4.4)
2. ✅ Історія з рекомендаціями (4.3)
3. ✅ Quick actions (3.4)
4. ✅ Шифрування API ключів (5.1)
5. ✅ Privacy режим (5.2)

**Очікуваний результат:** Повна реалізація плану, +100% продуктивність, +100% UX

---

## 📊 Ризики та мітігація

### Технічні ризики

| Ризик | Імовірність | Вплив | Мітігація |
|-------|-------------|-------|-----------|
| Breaking changes в API | Medium | High | Comprehensive testing, backward compatibility |
| Performance regression | Low | High | Benchmarks before/after, continuous monitoring |
| Memory leaks | Medium | Medium | Profiling, stress testing, proper disposal |
| AI provider changes | High | Medium | Abstraction layer, multiple providers support |

### Бізнес ризики

| Ризик | Імовірність | Вплив | Мітігація |
|-------|-------------|-------|-----------|
| Користувачі не приймуть зміни | Low | High | Beta testing, gradual rollout, documentation |
| Складність міграції | Medium | Medium | Migration tool, backward compatibility |
| Збільшення складності коду | Medium | Low | Code reviews, documentation, tests |

---

## ✅ Критерії прийняття

### Фаза 1
- [ ] Main.cs < 250 рядків
- [ ] Всі компоненти мають окремі класи
- [ ] 100% існуючих тестів проходять
- [ ] Модулі ініціалізуються lazy

### Фаза 2
- [ ] LRU cache реалізовано та протестовано
- [ ] Benchmarks показують покращення на 50%+
- [ ] Async AI не блокує UI
- [ ] Пам'ять зменшена на 30%+

### Фаза 3
- [ ] Smart suggestions працюють для 5+ сценаріїв
- [ ] Помилки включають контекст та приклади
- [ ] AI індикація показується для всіх AI запитів
- [ ] Quick actions доступні в контекстному меню

### Фаза 4
- [ ] Query classifier має точність 90%+
- [ ] Контекстні підказки працюють для всіх типів
- [ ] Історія показує рекомендації
- [ ] AI workflow підтримує fallback

### Фаза 5
- [ ] API ключі зашифровані
- [ ] Privacy режим працює
- [ ] Sensitive data не зберігається

### Фаза 6
- [ ] Test coverage > 80%
- [ ] Всі benchmarks в межах цільових метрик
- [ ] Zero critical bugs
- [ ] Documentation complete

---

## 📚 Додаткові ресурси

### Документація
- [PowerToys Run API](https://github.com/microsoft/PowerToys/wiki/Launcher-API)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)

### Інструменти
- BenchmarkDotNet - performance testing
- dotMemory - memory profiling
- ReSharper - code quality
- SonarQube - code analysis

---

## 🎓 Висновки

Цей план оптимізації покриває всі аспекти покращення плагіна QuickBrain:

1. **Продуктивність**: Кешування, async операції, lazy loading, оптимізація алгоритмів
2. **User Experience**: Підказки, автодоповнення, краще UX для AI, візуальна ієрархія
3. **Логіка**: Інтелектуальне розпізнавання запитів, адаптивний workflow, контекстні підказки
4. **Архітектура**: Модульність, тестованість, підтримуваність
5. **Безпека**: Шифрування ключів, privacy режим

**Очікуваний результат:**
- ⚡ **2x-10x швидше** для більшості операцій
- 🎨 **Значно краще UX** з підказками та автодоповненням
- 🏗️ **Більш підтримуваний код** з чіткою архітектурою
- 🔒 **Безпечніше зберігання** чутливих даних

**Наступний крок:** Обрати пріоритетні задачі та почати з Фази 1!
