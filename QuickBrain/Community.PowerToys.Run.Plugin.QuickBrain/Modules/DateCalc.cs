using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuickBrain.Modules;

public class DateCalc
{
    private readonly Settings _settings;
    private readonly Dictionary<string, Func<DateTime, int, DateTime>> _dateOperations;
    private readonly Dictionary<string, Func<DateTime, DateTime, int>> _dateDifferences;

    public DateCalc(Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dateOperations = InitializeDateOperations();
        _dateDifferences = InitializeDateDifferences();
    }

    private Dictionary<string, Func<DateTime, int, DateTime>> InitializeDateOperations()
    {
        return new Dictionary<string, Func<DateTime, int, DateTime>>(StringComparer.OrdinalIgnoreCase)
        {
            ["day"] = (date, amount) => date.AddDays(amount),
            ["days"] = (date, amount) => date.AddDays(amount),
            ["week"] = (date, amount) => date.AddDays(amount * 7),
            ["weeks"] = (date, amount) => date.AddDays(amount * 7),
            ["month"] = (date, amount) => date.AddMonths(amount),
            ["months"] = (date, amount) => date.AddMonths(amount),
            ["year"] = (date, amount) => date.AddYears(amount),
            ["years"] = (date, amount) => date.AddYears(amount),
            ["hour"] = (date, amount) => date.AddHours(amount),
            ["hours"] = (date, amount) => date.AddHours(amount),
            ["minute"] = (date, amount) => date.AddMinutes(amount),
            ["minutes"] = (date, amount) => date.AddMinutes(amount),
            ["second"] = (date, amount) => date.AddSeconds(amount),
            ["seconds"] = (date, amount) => date.AddSeconds(amount)
        };
    }

    private Dictionary<string, Func<DateTime, DateTime, int>> InitializeDateDifferences()
    {
        return new Dictionary<string, Func<DateTime, DateTime, int>>(StringComparer.OrdinalIgnoreCase)
        {
            ["days"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalDays),
            ["day"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalDays),
            ["weeks"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalDays) / 7,
            ["week"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalDays) / 7,
            ["months"] = (date1, date2) => Math.Abs((date2.Year - date1.Year) * 12 + date2.Month - date1.Month),
            ["month"] = (date1, date2) => Math.Abs((date2.Year - date1.Year) * 12 + date2.Month - date1.Month),
            ["years"] = (date1, date2) => Math.Abs(date2.Year - date1.Year),
            ["year"] = (date1, date2) => Math.Abs(date2.Year - date1.Year),
            ["hours"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalHours),
            ["hour"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalHours),
            ["minutes"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalMinutes),
            ["minute"] = (date1, date2) => (int)Math.Abs((date2 - date1).TotalMinutes),
            ["business days"] = CalculateBusinessDays,
            ["business day"] = CalculateBusinessDays,
            ["workdays"] = CalculateBusinessDays,
            ["workday"] = CalculateBusinessDays
        };
    }

    private static int CalculateBusinessDays(DateTime start, DateTime end)
    {
        if (start > end)
            (start, end) = (end, start);

        int businessDays = 0;
        var current = start.Date;
        while (current <= end.Date)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                businessDays++;
            current = current.AddDays(1);
        }
        return businessDays;
    }

    public CalculationResult? Calculate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return CalculationResult.Error("Expression cannot be empty", expression);

        try
        {
            expression = expression.ToLower().Trim();

            // Handle date arithmetic: "today + 5 days", "2024-01-01 - 2 weeks"
            var arithmeticMatch = Regex.Match(expression, 
                @"(.+?)\s*([+-])\s*(\d+)\s+(day|days|week|weeks|month|months|year|years|hour|hours|minute|minutes|second|seconds)$");
            
            if (arithmeticMatch.Success)
            {
                var dateStr = arithmeticMatch.Groups[1].Value.Trim();
                var operation = arithmeticMatch.Groups[2].Value;
                var amount = int.Parse(arithmeticMatch.Groups[3].Value);
                var unit = arithmeticMatch.Groups[4].Value;

                var baseDate = ParseDate(dateStr);
                if (baseDate == null)
                    return CalculationResult.Error($"Cannot parse date: {dateStr}", expression);

                var adjustedAmount = operation == "-" ? -amount : amount;
                var resultDate = _dateOperations[unit](baseDate.Value, adjustedAmount);

                return new CalculationResult
                {
                    Title = expression,
                    SubTitle = $"{baseDate:yyyy-MM-dd} {operation} {amount} {unit} = {resultDate:yyyy-MM-dd}",
                    Result = resultDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    RawExpression = expression,
                    Type = CalculationType.DateCalculation,
                    NumericValue = resultDate.Ticks,
                    IsError = false
                };
            }

            // Handle date difference: "days between 2024-01-01 and 2024-01-31"
            var differenceMatch = Regex.Match(expression, 
                @"(day|days|week|weeks|month|months|year|years|hour|hours|minute|minutes|second|seconds|business days|business day|workdays|workday)\s+between\s+(.+?)\s+and\s+(.+)$");
            
            if (differenceMatch.Success)
            {
                var unit = differenceMatch.Groups[1].Value;
                var date1Str = differenceMatch.Groups[2].Value.Trim();
                var date2Str = differenceMatch.Groups[3].Value.Trim();

                var date1 = ParseDate(date1Str);
                var date2 = ParseDate(date2Str);

                if (date1 == null)
                    return CalculationResult.Error($"Cannot parse date: {date1Str}", expression);
                if (date2 == null)
                    return CalculationResult.Error($"Cannot parse date: {date2Str}", expression);

                var difference = _dateDifferences[unit](date1.Value, date2.Value);
                var unitText = difference == 1 ? unit.TrimEnd('s') : unit;

                return new CalculationResult
                {
                    Title = expression,
                    SubTitle = $"{difference} {unitText} between {date1:yyyy-MM-dd} and {date2:yyyy-MM-dd}",
                    Result = difference.ToString(),
                    RawExpression = expression,
                    Type = CalculationType.DateCalculation,
                    NumericValue = difference,
                    IsError = false
                };
            }

            // Handle relative dates: "next monday", "3 days ago", "last friday"
            var relativeMatch = Regex.Match(expression, 
                @"^(next|last|in|(\d+)\s+(day|days|week|weeks|month|months|year|years)\s+(ago|from now))\s+(monday|tuesday|wednesday|thursday|friday|saturday|sunday)$");
            
            if (relativeMatch.Success)
            {
                var resultDate = CalculateRelativeDate(expression);
                if (resultDate != null)
                {
                    return new CalculationResult
                    {
                        Title = expression,
                        SubTitle = $"Date: {resultDate.Value:yyyy-MM-dd}",
                        Result = resultDate.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                        RawExpression = expression,
                        Type = CalculationType.DateCalculation,
                        NumericValue = resultDate.Value.Ticks,
                        IsError = false
                    };
                }
            }

            // Handle age calculation: "age of 1990-01-01"
            var ageMatch = Regex.Match(expression, @"^age\s+of\s+(.+)$");
            if (ageMatch.Success)
            {
                var birthDateStr = ageMatch.Groups[1].Value.Trim();
                var birthDate = ParseDate(birthDateStr);
                
                if (birthDate == null)
                    return CalculationResult.Error($"Cannot parse birth date: {birthDateStr}", expression);

                var today = DateTime.Today;
                var age = today.Year - birthDate.Value.Year;
                if (birthDate.Value.Date > today.AddYears(-age))
                    age--;

                return new CalculationResult
                {
                    Title = expression,
                    SubTitle = $"Age: {age} years",
                    Result = $"{age} years",
                    RawExpression = expression,
                    Type = CalculationType.DateCalculation,
                    NumericValue = age,
                    IsError = false
                };
            }

            // Handle weekday detection: "what day is 2024-01-01"
            var weekdayMatch = Regex.Match(expression, @"^(what|which)\s+day\s+is\s+(.+)$");
            if (weekdayMatch.Success)
            {
                var dateStr = weekdayMatch.Groups[2].Value.Trim();
                var date = ParseDate(dateStr);
                
                if (date == null)
                    return CalculationResult.Error($"Cannot parse date: {dateStr}", expression);

                var dayOfWeek = date.Value.DayOfWeek.ToString();

                return new CalculationResult
                {
                    Title = expression,
                    SubTitle = $"{date:yyyy-MM-dd} is a {dayOfWeek}",
                    Result = dayOfWeek,
                    RawExpression = expression,
                    Type = CalculationType.DateCalculation,
                    IsError = false
                };
            }

            // Handle simple date parsing: just return the parsed date
            var simpleDate = ParseDate(expression);
            if (simpleDate != null)
            {
                return new CalculationResult
                {
                    Title = expression,
                    SubTitle = $"Date: {simpleDate.Value:yyyy-MM-dd HH:mm:ss}",
                    Result = simpleDate.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    RawExpression = expression,
                    Type = CalculationType.DateCalculation,
                    NumericValue = simpleDate.Value.Ticks,
                    IsError = false
                };
            }

            return CalculationResult.Error("Invalid date expression format", expression);
        }
        catch (Exception ex)
        {
            return CalculationResult.Error(ex.Message, expression);
        }
    }

    private DateTime? ParseDate(string dateStr)
    {
        dateStr = dateStr.ToLower().Trim();

        // Handle common date keywords
        if (dateStr == "today" || dateStr == "now")
            return DateTime.Now;
        if (dateStr == "yesterday")
            return DateTime.Today.AddDays(-1);
        if (dateStr == "tomorrow")
            return DateTime.Today.AddDays(1);

        // Try various date formats
        var formats = new[]
        {
            "yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "dd/MM/yyyy",
            "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss",
            "MMM dd, yyyy", "MMMM dd, yyyy",
            "dd MMM yyyy", "dd MMMM yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;
        }

        // Try natural parsing
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var naturalResult))
            return naturalResult;

        return null;
    }

    private DateTime? CalculateRelativeDate(string expression)
    {
        var today = DateTime.Today;

        // Handle "next monday", "last friday"
        var nextLastMatch = Regex.Match(expression, @"^(next|last)\s+(monday|tuesday|wednesday|thursday|friday|saturday|sunday)$");
        if (nextLastMatch.Success)
        {
            var direction = nextLastMatch.Groups[1].Value;
            var targetDay = Enum.Parse<DayOfWeek>(nextLastMatch.Groups[2].Value, true);
            
            var daysToAdd = (targetDay - today.DayOfWeek + 7) % 7;
            if (direction == "last")
                daysToAdd = daysToAdd == 0 ? -7 : daysToAdd - 7;
            else if (daysToAdd == 0)
                daysToAdd = 7;

            return today.AddDays(daysToAdd);
        }

        // Handle "3 days ago", "2 weeks from now"
        var agoFromMatch = Regex.Match(expression, @"^(\d+)\s+(day|days|week|weeks|month|months|year|years)\s+(ago|from now)$");
        if (agoFromMatch.Success)
        {
            var amount = int.Parse(agoFromMatch.Groups[1].Value);
            var unit = agoFromMatch.Groups[2].Value;
            var direction = agoFromMatch.Groups[3].Value;

            var adjustedAmount = direction == "ago" ? -amount : amount;
            return _dateOperations[unit](today, adjustedAmount);
        }

        return null;
    }
}
