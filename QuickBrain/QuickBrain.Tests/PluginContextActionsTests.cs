namespace QuickBrain.Tests;

public class PluginContextActionsTests
{
    [Fact]
    public void Plugin_ResultAction_ExecutesSuccessfully()
    {
        var result = new Wox.Plugin.Result
        {
            Title = "Test",
            SubTitle = "Test Result",
            Action = _ => true
        };

        Assert.NotNull(result.Action);
        var executed = result.Action(null);
        Assert.True(executed);
    }

    [Fact]
    public void CalculationResult_SuccessCreatesValidResult()
    {
        var result = CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic);

        Assert.NotNull(result);
        Assert.Equal("2 + 2", result.Title);
        Assert.Equal("4", result.Result);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
        Assert.False(result.IsError);
    }

    [Fact]
    public void CalculationResult_ErrorCreatesValidError()
    {
        var result = CalculationResult.Error("Test error");

        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Test error", result.ErrorMessage);
    }

    [Fact]
    public void HistoryEntry_FromCalculationResult_CreatesValidEntry()
    {
        var calcResult = CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic);
        var entry = HistoryEntry.FromCalculationResult(calcResult);

        Assert.NotNull(entry);
        Assert.Equal("2 + 2", entry.Title);
        Assert.Equal("4", entry.Result);
        Assert.Equal(CalculationType.Arithmetic, entry.Type);
    }

    [Fact]
    public void HistoryManager_CanBeCreatedAndDisposed()
    {
        var settings = new Settings { EnableHistory = true };
        using (var manager = new HistoryManager(settings))
        {
            Assert.NotNull(manager);
        }
    }

    [Fact]
    public void HistoryManager_AddAndRetrieve()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        using (var manager = new HistoryManager(settings))
        {
            manager.ClearHistory();

            var result = CalculationResult.Success("Test", "123", CalculationType.Arithmetic);
            manager.Add(result);

            var history = manager.GetAll();
            Assert.Single(history);
        }
    }

    [Fact]
    public void HistoryManager_Search()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        using (var manager = new HistoryManager(settings))
        {
            manager.ClearHistory();

            var result1 = CalculationResult.Success("10 + 2", "12", CalculationType.Arithmetic);
            var result2 = CalculationResult.Success("5 * 3", "15", CalculationType.Arithmetic);

            manager.Add(result1);
            manager.Add(result2);

            var search = manager.Search("10");
            Assert.Single(search);
        }
    }

    [Fact]
    public void HistoryManager_Filter()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        using (var manager = new HistoryManager(settings))
        {
            manager.ClearHistory();

            var result1 = CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic);
            var result2 = CalculationResult.Success("10 km to miles", "6.2", CalculationType.UnitConversion);

            manager.Add(result1);
            manager.Add(result2);

            var arithmetic = manager.Filter(CalculationType.Arithmetic);
            Assert.Single(arithmetic);

            var conversion = manager.Filter(CalculationType.UnitConversion);
            Assert.Single(conversion);
        }
    }

    [Fact]
    public void HistoryManager_Export()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        using (var manager = new HistoryManager(settings))
        {
            manager.ClearHistory();

            var result = CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic);
            manager.Add(result);

            var json = manager.Export("json");
            Assert.NotEmpty(json);
            Assert.Contains("\"Title\"", json);
            Assert.Contains("\"Result\"", json);

            var csv = manager.Export("csv");
            Assert.NotEmpty(csv);
            Assert.Contains("Title,Expression,Result", csv);
        }
    }

    [Fact]
    public void HistoryManager_Statistics()
    {
        var settings = new Settings { EnableHistory = true, HistoryLimit = 10 };
        using (var manager = new HistoryManager(settings))
        {
            manager.ClearHistory();

            var result = CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic).WithNumericValue(4);
            manager.Add(result);

            var stats = manager.GetStats();
            Assert.Equal(1, stats.TotalCount);
            Assert.Equal(10, stats.MaxSize);
        }
    }
}
