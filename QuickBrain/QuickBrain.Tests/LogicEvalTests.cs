using System.Globalization;
using QuickBrain;
using QuickBrain.Modules;
using Xunit;

namespace QuickBrain.Tests;

public class LogicEvalTests
{
    private readonly LogicEval _logicEval;

    public LogicEvalTests()
    {
        var settings = new Settings();
        _logicEval = new LogicEval(settings);
    }

    [Fact]
    public void BooleanLogic_AndOperation_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "true and true";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void BooleanLogic_OrOperation_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "false or true";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void BooleanLogic_NotOperation_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "not true";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("False", result.Result);
        Assert.Equal(0.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void BooleanLogic_XorOperation_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "true xor false";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void BooleanLogic_ComplexExpression_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "(true and false) or (not false)";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void ComparisonOperations_Equal_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "5 == 5";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void ComparisonOperations_NotEqual_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "5 != 3";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void ComparisonOperations_LessThan_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "3 < 5";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void ComparisonOperations_GreaterThanOrEqual_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "10 >= 5";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void BitwiseOperations_And_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "0b1010 & 0b1100";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("8", result.Result);
        Assert.Equal(8.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0x8", result.SubTitle);
    }

    [Fact]
    public void BitwiseOperations_Or_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "0b1010 | 0b1100";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("14", result.Result);
        Assert.Equal(14.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0xE", result.SubTitle);
    }

    [Fact]
    public void BitwiseOperations_Xor_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "0b1010 ^ 0b1100";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("6", result.Result);
        Assert.Equal(6.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0x6", result.SubTitle);
    }

    [Fact]
    public void BitwiseOperations_LeftShift_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "0b1010 << 2";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("40", result.Result);
        Assert.Equal(40.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0x28", result.SubTitle);
    }

    [Fact]
    public void BitwiseOperations_RightShift_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "0b10100000 >> 3";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("20", result.Result);
        Assert.Equal(20.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0x14", result.SubTitle);
    }

    [Fact]
    public void BitwiseOperations_Not_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "~0b1010";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("-11", result.Result);
        Assert.Equal(-11.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void HexadecimalNumbers_BitwiseOperation_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "0xFF & 0x0F";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("15", result.Result);
        Assert.Equal(15.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0xF", result.SubTitle);
    }

    [Fact]
    public void OctalNumbers_BitwiseOperation_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "0o12 | 0o15";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("15", result.Result);
        Assert.Equal(15.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0xF", result.SubTitle);
    }

    [Fact]
    public void SymbolicOperators_AndOr_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "1 && 0 || 1";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void Parentheses_Grouping_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "(1 && 0) || 1";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("True", result.Result);
        Assert.Equal(1.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void InvalidExpression_ReturnsError()
    {
        // Arrange
        var expression = "invalid logic expression";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void EmptyExpression_ReturnsError()
    {
        // Arrange
        var expression = "";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Expression cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void ComplexBitwiseExpression_WithParentheses_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "(0b1010 & 0b1100) | 0b0001";
        
        // Act
        var result = _logicEval.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("9", result.Result);
        Assert.Equal(9.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
        Assert.Contains("0x9", result.SubTitle);
    }
}