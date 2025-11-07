using System.Globalization;
using QuickBrain;
using QuickBrain.Modules;
using Xunit;

namespace QuickBrain.Tests;

public class DateCalcTests
{
    private readonly DateCalc _dateCalc;

    public DateCalcTests()
    {
        var settings = new Settings();
        _dateCalc = new DateCalc(settings);
    }

    [Fact]
    public void DateArithmetic_AddDays_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2024-01-01 + 5 days";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2024-01-06 00:00:00", result.Result);
        Assert.Contains("2024-01-01 + 5 days = 2024-01-06", result.SubTitle);
    }

    [Fact]
    public void DateArithmetic_SubtractWeeks_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2024-01-15 - 2 weeks";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2024-01-01 00:00:00", result.Result);
        Assert.Contains("2024-01-15 - 2 weeks = 2024-01-01", result.SubTitle);
    }

    [Fact]
    public void DateArithmetic_AddMonths_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2024-01-15 + 2 months";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2024-03-15 00:00:00", result.Result);
        Assert.Contains("2024-01-15 + 2 months = 2024-03-15", result.SubTitle);
    }

    [Fact]
    public void DateDifference_DaysBetween_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "days between 2024-01-01 and 2024-01-31";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("30", result.Result);
        Assert.Equal(30.0, result.NumericValue);
        Assert.Contains("30 days between 2024-01-01 and 2024-01-31", result.SubTitle);
    }

    [Fact]
    public void DateDifference_BusinessDays_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "business days between 2024-01-01 and 2024-01-07";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("5", result.Result);
        Assert.Equal(5.0, result.NumericValue);
        Assert.Contains("5 business days between 2024-01-01 and 2024-01-07", result.SubTitle);
    }

    [Fact]
    public void RelativeDate_NextMonday_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "next monday";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("Date:", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void RelativeDate_LastFriday_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "last friday";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("Date:", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void RelativeDate_DaysAgo_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "3 days ago";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("Date:", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void AgeCalculation_ValidBirthDate_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "age of 1990-01-01";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("Age:", result.SubTitle);
        Assert.Contains("years", result.Result);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void WeekdayDetection_ValidDate_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "what day is 2024-01-01";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("2024-01-01 is a", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void DateArithmetic_TodayPlusDays_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "today + 7 days";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("today + 7 days =", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void DateArithmetic_YesterdayMinusDays_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "yesterday - 2 days";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("yesterday - 2 days =", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void DateDifference_WeeksBetween_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "weeks between 2024-01-01 and 2024-01-29";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("4", result.Result);
        Assert.Equal(4.0, result.NumericValue);
        Assert.Contains("4 weeks between 2024-01-01 and 2024-01-29", result.SubTitle);
    }

    [Fact]
    public void DateDifference_MonthsBetween_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "months between 2024-01-01 and 2024-06-01";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("5", result.Result);
        Assert.Equal(5.0, result.NumericValue);
        Assert.Contains("5 months between 2024-01-01 and 2024-06-01", result.SubTitle);
    }

    [Fact]
    public void DateArithmetic_AddHours_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2024-01-01 + 24 hours";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2024-01-02 00:00:00", result.Result);
        Assert.Contains("2024-01-01 + 24 hours = 2024-01-02", result.SubTitle);
    }

    [Fact]
    public void SimpleDateParsing_ValidDate_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2024-12-25";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2024-12-25 00:00:00", result.Result);
        Assert.Contains("Date: 2024-12-25 00:00:00", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void InvalidDateExpression_ReturnsError()
    {
        // Arrange
        var expression = "invalid date expression";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Invalid date expression format", result.ErrorMessage);
    }

    [Fact]
    public void EmptyExpression_ReturnsError()
    {
        // Arrange
        var expression = "";
        
        // Act
        var result = _dateCalc.Calculate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Expression cannot be empty", result.ErrorMessage);
    }
}