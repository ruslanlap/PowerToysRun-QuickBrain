using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using QuickBrain.Modules;

namespace Community.PowerToys.Run.Plugin.QuickBrain
{
    /// <summary>
    /// Intelligently classifies queries to route them to the correct calculation module.
    /// Improves accuracy and performance by selecting the right module on first try.
    /// </summary>
    public class QueryClassifier
    {
        private readonly Dictionary<CalculationType, List<QueryPattern>> _patterns;

        public QueryClassifier()
        {
            _patterns = InitializePatterns();
        }

        /// <summary>
        /// Classify a query to determine its type and confidence level.
        /// </summary>
        /// <param name="query">The user's input query</param>
        /// <returns>Tuple of (CalculationType, confidence score 0-100)</returns>
        public (CalculationType type, int confidence) Classify(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (CalculationType.Error, 0);
            }

            var normalized = query.Trim().ToLowerInvariant();
            var scores = new Dictionary<CalculationType, int>();

            // Check each pattern category
            foreach (var (type, patterns) in _patterns)
            {
                foreach (var pattern in patterns)
                {
                    if (Regex.IsMatch(normalized, pattern.Pattern, RegexOptions.IgnoreCase))
                    {
                        // Take highest confidence for each type
                        if (!scores.ContainsKey(type) || scores[type] < pattern.Confidence)
                        {
                            scores[type] = pattern.Confidence;
                        }
                    }
                }
            }

            // Return best match
            if (scores.Any())
            {
                var best = scores.OrderByDescending(s => s.Value).First();
                return (best.Key, best.Value);
            }

            // Default: assume arithmetic if it looks like math
            if (Regex.IsMatch(normalized, @"[\d\+\-\*/\^\(\)\.]+"))
            {
                return (CalculationType.Arithmetic, 50);
            }

            return (CalculationType.Error, 0);
        }

        /// <summary>
        /// Get suggested modules to try in priority order.
        /// </summary>
        /// <param name="query">The user's input query</param>
        /// <returns>List of CalculationType in priority order</returns>
        public List<CalculationType> GetModulePriority(string query)
        {
            var (primaryType, confidence) = Classify(query);
            var priority = new List<CalculationType>();

            // Add primary type if confidence is good
            if (confidence >= 50)
            {
                priority.Add(primaryType);
            }

            // Add related types based on query characteristics
            var normalized = query.Trim().ToLowerInvariant();

            // If contains numbers, try math-related modules
            if (Regex.IsMatch(normalized, @"\d"))
            {
                if (!priority.Contains(CalculationType.Arithmetic))
                    priority.Add(CalculationType.Arithmetic);
            }

            // If contains units, try converter
            if (Regex.IsMatch(normalized, @"(km|kg|lb|celsius|fahrenheit|miles|meters|pounds|to|in)"))
            {
                if (!priority.Contains(CalculationType.UnitConversion))
                    priority.Add(CalculationType.UnitConversion);
            }

            // If contains dates, try date calc
            if (Regex.IsMatch(normalized, @"(\d{4}-\d{2}-\d{2}|today|tomorrow|yesterday|days?|weeks?|months?|years?)"))
            {
                if (!priority.Contains(CalculationType.DateCalculation))
                    priority.Add(CalculationType.DateCalculation);
            }

            // If contains boolean keywords, try logic
            if (Regex.IsMatch(normalized, @"\b(and|or|not|xor|true|false)\b"))
            {
                if (!priority.Contains(CalculationType.LogicEvaluation))
                    priority.Add(CalculationType.LogicEvaluation);
            }

            // Default fallback order
            var defaultOrder = new[]
            {
                CalculationType.Arithmetic,
                CalculationType.UnitConversion,
                CalculationType.DateCalculation,
                CalculationType.LogicEvaluation
            };

            foreach (var type in defaultOrder)
            {
                if (!priority.Contains(type))
                {
                    priority.Add(type);
                }
            }

            return priority;
        }

        /// <summary>
        /// Check if a query is likely to be a specific type.
        /// </summary>
        public bool IsLikelyType(string query, CalculationType type)
        {
            var (classified, confidence) = Classify(query);
            return classified == type && confidence >= 70;
        }

        private Dictionary<CalculationType, List<QueryPattern>> InitializePatterns()
        {
            return new Dictionary<CalculationType, List<QueryPattern>>
            {
                [CalculationType.UnitConversion] = new List<QueryPattern>
                {
                    // Explicit conversion syntax
                    new QueryPattern(@"\d+\.?\d*\s*(km|kilometers?|kilometres?)\s+to\s+", 95),
                    new QueryPattern(@"\d+\.?\d*\s*(kg|kilograms?)\s+to\s+", 95),
                    new QueryPattern(@"\d+\.?\d*\s*(lb|lbs|pounds?)\s+to\s+", 95),
                    new QueryPattern(@"\d+\.?\d*\s*(celsius|fahrenheit|kelvin|[cf])\s+to\s+", 95),
                    new QueryPattern(@"\d+\.?\d*\s*(miles?|meters?|metres?|feet|ft|inches?|in)\s+to\s+", 95),
                    new QueryPattern(@"\d+\.?\d*\s*(gallons?|liters?|litres?|ml|cups?)\s+to\s+", 95),
                    new QueryPattern(@"\d+\.?\d*\s*(gb|mb|kb|tb|bytes?)\s+to\s+", 95),

                    // "Convert X" syntax
                    new QueryPattern(@"^convert\s+\d+", 90),
                    new QueryPattern(@"^how\s+many\s+\w+\s+in\s+\d+", 85),

                    // Unit mentions
                    new QueryPattern(@"\d+\s*(km|kg|lb|miles|meters|pounds|celsius|fahrenheit)", 70),
                },

                [CalculationType.DateCalculation] = new List<QueryPattern>
                {
                    // ISO date format
                    new QueryPattern(@"\d{4}-\d{2}-\d{2}", 95),

                    // Date arithmetic
                    new QueryPattern(@"(today|now|tomorrow|yesterday)\s*[\+\-]\s*\d+\s*(days?|weeks?|months?|years?)", 95),
                    new QueryPattern(@"\d+\s*(days?|weeks?|months?|years?)\s+(from|after|before)", 90),

                    // Date differences
                    new QueryPattern(@"(days?|weeks?|months?|years?)\s+between", 95),
                    new QueryPattern(@"(days?|weeks?|months?|years?)\s+(from|until|to)", 85),

                    // Relative dates
                    new QueryPattern(@"\b(today|tomorrow|yesterday|now)\b", 80),
                    new QueryPattern(@"(next|last)\s+(week|month|year)", 85),
                },

                [CalculationType.Arithmetic] = new List<QueryPattern>
                {
                    // Simple arithmetic
                    new QueryPattern(@"^\d+\s*[\+\-\*/]\s*\d+$", 100),
                    new QueryPattern(@"^\d+\.?\d*\s*[\+\-\*/\^]\s*\d+\.?\d*$", 100),

                    // Percentage
                    new QueryPattern(@"\d+%\s+of\s+\d+", 95),
                    new QueryPattern(@"what\s+is\s+\d+%", 90),

                    // Multiple operations
                    new QueryPattern(@"\d+\s*[\+\-\*/]\s*\d+\s*[\+\-\*/]\s*\d+", 95),
                },

                [CalculationType.Trigonometric] = new List<QueryPattern>
                {
                    new QueryPattern(@"\b(sin|cos|tan|asin|acos|atan)\s*\(", 95),
                    new QueryPattern(@"\b(sinh|cosh|tanh)\s*\(", 95),
                },

                [CalculationType.Logarithmic] = new List<QueryPattern>
                {
                    new QueryPattern(@"\b(log|ln|log10|log2)\s*\(", 95),
                    new QueryPattern(@"\b(exp|pow|power)\s*\(", 90),
                },

                [CalculationType.Statistical] = new List<QueryPattern>
                {
                    new QueryPattern(@"\b(mean|median|mode|average|avg)\s*[\(\[]", 95),
                    new QueryPattern(@"\b(stddev|variance|stdev|std)\s*[\(\[]", 95),
                    new QueryPattern(@"\b(sum|min|max|count)\s*[\(\[]", 90),
                },

                [CalculationType.Algebraic] = new List<QueryPattern>
                {
                    new QueryPattern(@"\b(sqrt|cbrt|root)\s*\(", 95),
                    new QueryPattern(@"\b(abs|absolute)\s*\(", 90),
                    new QueryPattern(@"\b(floor|ceil|ceiling|round)\s*\(", 90),
                    new QueryPattern(@"\b(factorial|fact)\s*\(", 90),
                },

                [CalculationType.LogicEvaluation] = new List<QueryPattern>
                {
                    // Boolean operators
                    new QueryPattern(@"\b(true|false)\b.*\b(and|or|not|xor)\b", 95),
                    new QueryPattern(@"\b(and|or|not|xor)\b.*\b(true|false)\b", 95),

                    // Comparison operators
                    new QueryPattern(@"\d+\s*(<|>|<=|>=|==|!=)\s*\d+", 90),
                    new QueryPattern(@"\b(greater|less|equal)\s+(than|to)\b", 85),

                    // Bitwise
                    new QueryPattern(@"\d+\s*(&|\||\^|<<|>>)\s*\d+", 85),
                },

                [CalculationType.Health] = new List<QueryPattern>
                {
                    new QueryPattern(@"\bbmi\b", 95),
                    new QueryPattern(@"\bbody\s+mass\s+index\b", 95),
                    new QueryPattern(@"\b(height|weight)\b.*\b(height|weight)\b", 70),
                },

                [CalculationType.Money] = new List<QueryPattern>
                {
                    new QueryPattern(@"\btip\s+\d+%", 95),
                    new QueryPattern(@"\b(discount|tax|interest)\s+\d+", 85),
                    new QueryPattern(@"\$\d+", 70),
                    new QueryPattern(@"\b(price|cost|total)\b", 60),
                },

                [CalculationType.AiAssisted] = new List<QueryPattern>
                {
                    new QueryPattern(@"^ai\s+", 100),
                    new QueryPattern(@"^ask\s+", 100),
                    new QueryPattern(@"^(explain|what|why|how)\s+", 70),
                },
            };
        }
    }

    /// <summary>
    /// Represents a pattern for matching queries with confidence score.
    /// </summary>
    public record QueryPattern(string Pattern, int Confidence);
}
