namespace QuickBrain.Tests;

public class CalculationResultTests
{
    [Fact]
    public void CalculationResult_Success_CreatesValidResult()
    {
        var result = CalculationResult.Success("2 + 2", "4", CalculationType.Arithmetic);

        Assert.Equal("2 + 2", result.Title);
        Assert.Equal("4", result.Result);
        Assert.Equal("Result: 4", result.SubTitle);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
        Assert.False(result.IsError);
    }

    [Fact]
    public void CalculationResult_Error_CreatesErrorResult()
    {
        var result = CalculationResult.Error("Division by zero", "5 / 0");

        Assert.Equal("Error", result.Title);
        Assert.Equal("Division by zero", result.SubTitle);
        Assert.Equal("5 / 0", result.RawExpression);
        Assert.Equal(CalculationType.Error, result.Type);
        Assert.True(result.IsError);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void CalculationResult_FluentApi_WorksCorrectly()
    {
        var result = CalculationResult.Success("Test", "100", CalculationType.Arithmetic)
            .WithSubTitle("Custom subtitle")
            .WithNumericValue(100.5)
            .WithUnit("meters")
            .WithScore(95)
            .WithIconPath("icon.png");

        Assert.Equal("Custom subtitle", result.SubTitle);
        Assert.Equal(100.5, result.NumericValue);
        Assert.Equal("meters", result.Unit);
        Assert.Equal(95, result.Score);
        Assert.Equal("icon.png", result.IconPath);
    }

    [Fact]
    public void CalculationResult_HasTimestamp()
    {
        var before = DateTime.UtcNow;
        var result = CalculationResult.Success("Test", "Result", CalculationType.Arithmetic);
        var after = DateTime.UtcNow;

        Assert.InRange(result.Timestamp, before, after);
    }
}
