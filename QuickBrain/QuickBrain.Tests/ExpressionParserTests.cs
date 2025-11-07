using System.Globalization;
using QuickBrain;
using QuickBrain.Modules;
using Xunit;

namespace QuickBrain.Tests;

public class ExpressionParserTests
{
    private readonly ExpressionParser _expressionParser;

    public ExpressionParserTests()
    {
        var settings = new Settings();
        _expressionParser = new ExpressionParser(settings);
    }

    [Fact]
    public void Parse_ArithmeticExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "2 + 3 * 4";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.Arithmetic, result.Type);
        Assert.Equal("2 + 3 * 4", result.OriginalInput);
        Assert.Equal("2 + 3 * 4", result.NormalizedInput);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Parse_UnitConversionExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "10 km to miles";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.UnitConversion, result.Type);
        Assert.Equal("10 km to miles", result.OriginalInput);
        Assert.Equal("10 km to miles", result.NormalizedInput);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Parse_DateCalculationExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "today + 5 days";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.DateCalculation, result.Type);
        Assert.Equal("today + 5 days", result.OriginalInput);
        Assert.Equal("today + 5 days", result.NormalizedInput);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Parse_LogicExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "true and false";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.Logic, result.Type);
        Assert.Equal("true and false", result.OriginalInput);
        Assert.Equal("true and false", result.NormalizedInput);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Parse_NaturalLanguageExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "what is the meaning of life";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.NaturalLanguage, result.Type);
        Assert.Equal("what is the meaning of life", result.OriginalInput);
        Assert.Equal("what is the meaning of life", result.NormalizedInput);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsError()
    {
        // Arrange
        var input = "";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal(ExpressionType.Unknown, result.Type);
        Assert.Equal("Input cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void Parse_WhitespaceInput_ReturnsError()
    {
        // Arrange
        var input = "   ";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal(ExpressionType.Unknown, result.Type);
        Assert.Equal("Input cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void Parse_ArithmeticExpression_GeneratesCorrectTokens()
    {
        // Arrange
        var input = "2 + 3 * 4";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(5, result.Tokens.Count);
        
        Assert.Equal(TokenType.Number, result.Tokens[0].Type);
        Assert.Equal("2", result.Tokens[0].Value);
        
        Assert.Equal(TokenType.Operator, result.Tokens[1].Type);
        Assert.Equal("+", result.Tokens[1].Value);
        
        Assert.Equal(TokenType.Number, result.Tokens[2].Type);
        Assert.Equal("3", result.Tokens[2].Value);
        
        Assert.Equal(TokenType.Operator, result.Tokens[3].Type);
        Assert.Equal("*", result.Tokens[3].Value);
        
        Assert.Equal(TokenType.Number, result.Tokens[4].Type);
        Assert.Equal("4", result.Tokens[4].Value);
    }

    [Fact]
    public void Parse_FunctionCallExpression_GeneratesCorrectTokens()
    {
        // Arrange
        var input = "sin(45) + cos(30)";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.True(result.Tokens.Any(t => t.Type == TokenType.Function && t.Value == "sin"));
        Assert.True(result.Tokens.Any(t => t.Type == TokenType.Function && t.Value == "cos"));
        Assert.True(result.Tokens.Any(t => t.Type == TokenType.Parenthesis && t.Value == "("));
        Assert.True(result.Tokens.Any(t => t.Type == TokenType.Parenthesis && t.Value == ")"));
    }

    [Fact]
    public void Parse_ConstantsExpression_GeneratesCorrectTokens()
    {
        // Arrange
        var input = "pi * 2 + e";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.True(result.Tokens.Any(t => t.Type == TokenType.Variable && t.Value == "pi"));
        Assert.True(result.Tokens.Any(t => t.Type == TokenType.Variable && t.Value == "e"));
    }

    [Fact]
    public void Parse_UnitConversionAlternativeFormat_ReturnsCorrectType()
    {
        // Arrange
        var input = "5km miles";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.UnitConversion, result.Type);
    }

    [Fact]
    public void Parse_DateDifferenceExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "days between 2024-01-01 and 2024-01-31";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.DateCalculation, result.Type);
    }

    [Fact]
    public void Parse_RelativeDateExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "next monday";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.DateCalculation, result.Type);
    }

    [Fact]
    public void Parse_BitwiseExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "0b1010 & 0b1100";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.Logic, result.Type);
    }

    [Fact]
    public void Parse_ComparisonExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "5 > 3";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.Logic, result.Type);
    }

    [Fact]
    public void EvaluateExpression_Arithmetic_ReturnsCorrectResult()
    {
        // Arrange
        var input = "2 + 3 * 4";
        
        // Act
        var result = _expressionParser.EvaluateExpression(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("14.0000000000", result.Result);
        Assert.Equal(14.0, result.NumericValue);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
    }

    [Fact]
    public void EvaluateExpression_UnitConversion_ReturnsCorrectResult()
    {
        // Arrange
        var input = "10 km to miles";
        
        // Act
        var result = _expressionParser.EvaluateExpression(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("6.2137119224", result.Result);
        Assert.Equal("miles", result.Unit);
        Assert.Equal(CalculationType.UnitConversion, result.Type);
    }

    [Fact]
    public void EvaluateExpression_DateCalculation_ReturnsCorrectResult()
    {
        // Arrange
        var input = "today + 5 days";
        
        // Act
        var result = _expressionParser.EvaluateExpression(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("today + 5 days =", result.SubTitle);
        Assert.Equal(CalculationType.DateCalculation, result.Type);
    }

    [Fact]
    public void EvaluateExpression_Logic_ReturnsCorrectResult()
    {
        // Arrange
        var input = "true and false";
        
        // Act
        var result = _expressionParser.EvaluateExpression(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("False", result.Result);
        Assert.Equal(0.0, result.NumericValue);
        Assert.Equal(CalculationType.LogicEvaluation, result.Type);
    }

    [Fact]
    public void EvaluateExpression_NaturalLanguage_ReturnsError()
    {
        // Arrange
        var input = "what is the meaning of life";
        
        // Act
        var result = _expressionParser.EvaluateExpression(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Natural language processing requires AI integration", result.ErrorMessage);
    }

    [Fact]
    public void EvaluateExpression_InvalidExpression_ReturnsError()
    {
        // Arrange
        var input = "2 + * 3";
        
        // Act
        var result = _expressionParser.EvaluateExpression(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_AgeCalculationExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "age of 1990-01-01";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.DateCalculation, result.Type);
    }

    [Fact]
    public void Parse_WeekdayDetectionExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "what day is 2024-01-01";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.DateCalculation, result.Type);
    }

    [Fact]
    public void Parse_BusinessDaysExpression_ReturnsCorrectType()
    {
        // Arrange
        var input = "business days between 2024-01-01 and 2024-01-07";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.DateCalculation, result.Type);
    }

    [Fact]
    public void Parse_ComplexArithmeticWithParentheses_ReturnsCorrectTokens()
    {
        // Arrange
        var input = "(2 + 3) * (4 - 1)";
        
        // Act
        var result = _expressionParser.Parse(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Equal(ExpressionType.Arithmetic, result.Type);
        
        var openParenCount = result.Tokens.Count(t => t.Type == TokenType.Parenthesis && t.Value == "(");
        var closeParenCount = result.Tokens.Count(t => t.Type == TokenType.Parenthesis && t.Value == ")");
        Assert.Equal(2, openParenCount);
        Assert.Equal(2, closeParenCount);
    }
}