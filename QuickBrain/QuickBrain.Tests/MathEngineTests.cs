using System.Globalization;
using QuickBrain;
using QuickBrain.Modules;
using Xunit;

namespace QuickBrain.Tests;

public class MathEngineTests
{
    private readonly MathEngine _mathEngine;

    public MathEngineTests()
    {
        var settings = new Settings { Precision = 10 };
        _mathEngine = new MathEngine(settings);
    }

    [Fact]
    public void BasicArithmetic_Addition_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2 + 3";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("5.0000000000", result.Result);
        Assert.Equal(5.0, result.NumericValue);
    }

    [Fact]
    public void BasicArithmetic_Subtraction_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "10 - 4";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("6.0000000000", result.Result);
        Assert.Equal(6.0, result.NumericValue);
    }

    [Fact]
    public void BasicArithmetic_Multiplication_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "7 * 8";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("56.0000000000", result.Result);
        Assert.Equal(56.0, result.NumericValue);
    }

    [Fact]
    public void BasicArithmetic_Division_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "15 / 3";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("5.0000000000", result.Result);
        Assert.Equal(5.0, result.NumericValue);
    }

    [Fact]
    public void OperatorPrecedence_ComplexExpression_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2 + 3 * 4 - 6 / 2";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("11.0000000000", result.Result);
        Assert.Equal(11.0, result.NumericValue);
    }

    [Fact]
    public void Parentheses_Grouping_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "(2 + 3) * (4 - 1)";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("15.0000000000", result.Result);
        Assert.Equal(15.0, result.NumericValue);
    }

    [Fact]
    public void TrigonometricFunctions_SinDegrees_ReturnsCorrectResult()
    {
        // Arrange
        var settings = new Settings { Precision = 10, AngleUnit = AngleUnit.Degrees };
        var mathEngine = new MathEngine(settings);
        var expression = "sin(30)";
        
        // Act
        var result = mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("0.5000000000", result.Result);
        Assert.True(Math.Abs(result.NumericValue!.Value - 0.5) < 0.0001);
    }

    [Fact]
    public void TrigonometricFunctions_CosRadians_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "cos(0)";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("1.0000000000", result.Result);
        Assert.True(Math.Abs(result.NumericValue!.Value - 1.0) < 0.0001);
    }

    [Fact]
    public void MathematicalConstants_Pi_ReturnsCorrectValue()
    {
        // Arrange
        var expression = "pi";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("3.1415926536", result.Result);
        Assert.True(Math.Abs(result.NumericValue!.Value - Math.PI) < 0.0001);
    }

    [Fact]
    public void MathematicalConstants_E_ReturnsCorrectValue()
    {
        // Arrange
        var expression = "e";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2.7182818285", result.Result);
        Assert.True(Math.Abs(result.NumericValue!.Value - Math.E) < 0.0001);
    }

    [Fact]
    public void ImplicitMultiplication_ConstantWithNumber_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2pi";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("6.2831853072", result.Result);
        Assert.True(Math.Abs(result.NumericValue!.Value - 2 * Math.PI) < 0.0001);
    }

    [Fact]
    public void DivisionByZero_ReturnsError()
    {
        // Arrange
        var expression = "5 / 0";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Contains("Division by zero", result.ErrorMessage);
    }

    [Fact]
    public void InvalidExpression_ReturnsError()
    {
        // Arrange
        var expression = "2 + * 3";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
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
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Expression cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void Exponentiation_PowerFunction_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "pow(2, 8)";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("256.0000000000", result.Result);
        Assert.Equal(256.0, result.NumericValue);
    }

    [Fact]
    public void SquareRoot_SqrtFunction_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "sqrt(16)";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("4.0000000000", result.Result);
        Assert.Equal(4.0, result.NumericValue);
    }

    [Fact]
    public void Factorial_FactorialFunction_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "factorial(5)";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("120.0000000000", result.Result);
        Assert.Equal(120.0, result.NumericValue);
    }

    [Fact]
    public void Logarithm_Log10Function_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "log(100)";
        
        // Act
        var result = _mathEngine.Evaluate(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2.0000000000", result.Result);
        Assert.Equal(2.0, result.NumericValue);
    }
}