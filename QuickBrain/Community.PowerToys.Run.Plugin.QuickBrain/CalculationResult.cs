using System;
namespace QuickBrain.Modules;

public class CalculationResult
{
    public string Title { get; set; } = string.Empty;
    public string SubTitle { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? RawExpression { get; set; }
    public CalculationType Type { get; set; }
    public double? NumericValue { get; set; }
    public string? Unit { get; set; }
    public string? IconPath { get; set; }
    public int Score { get; set; } = 100;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }

    public static CalculationResult Success(string title, string result, CalculationType type)
    {
        return new CalculationResult
        {
            Title = title,
            Result = result,
            SubTitle = $"Result: {result}",
            Type = type,
            IsError = false
        };
    }

    public static CalculationResult Error(string message, string? expression = null)
    {
        return new CalculationResult
        {
            Title = "Error",
            SubTitle = message,
            Result = string.Empty,
            RawExpression = expression,
            Type = CalculationType.Error,
            IsError = true,
            ErrorMessage = message,
            Score = 0
        };
    }

    public CalculationResult WithSubTitle(string subTitle)
    {
        SubTitle = subTitle;
        return this;
    }

    public CalculationResult WithNumericValue(double value)
    {
        NumericValue = value;
        return this;
    }

    public CalculationResult WithUnit(string unit)
    {
        Unit = unit;
        return this;
    }

    public CalculationResult WithScore(int score)
    {
        Score = score;
        return this;
    }

    public CalculationResult WithIconPath(string iconPath)
    {
        IconPath = iconPath;
        return this;
    }
}

public enum CalculationType
{
    Arithmetic,
    Algebraic,
    Trigonometric,
    Logarithmic,
    Statistical,
    UnitConversion,
    DateCalculation,
    LogicEvaluation,
    Health,
    Money,
    NaturalLanguage,
    AiAssisted,
    Error
}
