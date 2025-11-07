using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace QuickBrain.Modules;

public class HistoryStatistics
{
    public int TotalCount { get; set; }
    public int MaxSize { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? MostRecentEntry { get; set; }
    public Dictionary<string, int> CountByType { get; set; } = new();
    public int NumericValuesCount { get; set; }
    public double AverageNumericValue { get; set; }
}
