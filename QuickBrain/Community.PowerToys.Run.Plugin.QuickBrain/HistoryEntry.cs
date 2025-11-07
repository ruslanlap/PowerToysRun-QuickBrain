using System;

namespace QuickBrain.Modules;

public class HistoryEntry
{
    public string Title { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public CalculationType Type { get; set; }
    public double? NumericValue { get; set; }
    public string? Unit { get; set; }
    public DateTime Added { get; set; } = DateTime.UtcNow;
    public DateTime? Calculated { get; set; }
    public DateTime? Executed { get; set; }

    public static HistoryEntry FromCalculationResult(CalculationResult result)
    {
        return new HistoryEntry
        {
            Title = result.Title,
            Result = result.Result,
            Expression = result.RawExpression ?? result.Title,
            Type = result.Type,
            NumericValue = result.NumericValue,
            Unit = result.Unit,
            Added = DateTime.UtcNow,
            Calculated = result.Timestamp,
            Executed = DateTime.UtcNow
        };
    }
}
