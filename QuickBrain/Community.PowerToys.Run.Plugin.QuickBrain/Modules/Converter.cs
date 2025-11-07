using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuickBrain.Modules;

public class Converter
{
    private readonly Settings _settings;
    private readonly Dictionary<string, ConversionInfo> _lengthConversions;
    private readonly Dictionary<string, ConversionInfo> _weightConversions;
    private readonly Dictionary<string, ConversionInfo> _temperatureConversions;
    private readonly Dictionary<string, ConversionInfo> _volumeConversions;
    private readonly Dictionary<string, ConversionInfo> _areaConversions;
    private readonly Dictionary<string, ConversionInfo> _timeConversions;
    private readonly Dictionary<string, ConversionInfo> _speedConversions;
    private readonly Dictionary<string, ConversionInfo> _dataConversions;

    public Converter(Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _lengthConversions = InitializeLengthConversions();
        _weightConversions = InitializeWeightConversions();
        _temperatureConversions = InitializeTemperatureConversions();
        _volumeConversions = InitializeVolumeConversions();
        _areaConversions = InitializeAreaConversions();
        _timeConversions = InitializeTimeConversions();
        _speedConversions = InitializeSpeedConversions();
        _dataConversions = InitializeDataConversions();
    }

    private Dictionary<string, ConversionInfo> InitializeLengthConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["meter"] = new ConversionInfo(1.0, "m", false),
            ["meters"] = new ConversionInfo(1.0, "m", false),
            ["m"] = new ConversionInfo(1.0, "m", false),
            ["kilometer"] = new ConversionInfo(1000.0, "km", false),
            ["kilometers"] = new ConversionInfo(1000.0, "km", false),
            ["km"] = new ConversionInfo(1000.0, "km", false),
            ["mile"] = new ConversionInfo(1609.344, "mi", false),
            ["miles"] = new ConversionInfo(1609.344, "mi", false),
            ["mi"] = new ConversionInfo(1609.344, "mi", false),
            ["foot"] = new ConversionInfo(0.3048, "ft", false),
            ["feet"] = new ConversionInfo(0.3048, "ft", false),
            ["ft"] = new ConversionInfo(0.3048, "ft", false),
            ["inch"] = new ConversionInfo(0.0254, "in", false),
            ["inches"] = new ConversionInfo(0.0254, "in", false),
            ["in"] = new ConversionInfo(0.0254, "in", false),
            ["yard"] = new ConversionInfo(0.9144, "yd", false),
            ["yards"] = new ConversionInfo(0.9144, "yd", false),
            ["yd"] = new ConversionInfo(0.9144, "yd", false),
            ["centimeter"] = new ConversionInfo(0.01, "cm", false),
            ["centimeters"] = new ConversionInfo(0.01, "cm", false),
            ["cm"] = new ConversionInfo(0.01, "cm", false),
            ["millimeter"] = new ConversionInfo(0.001, "mm", false),
            ["millimeters"] = new ConversionInfo(0.001, "mm", false),
            ["mm"] = new ConversionInfo(0.001, "mm", false)
        };
    }

    private Dictionary<string, ConversionInfo> InitializeWeightConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["kilogram"] = new ConversionInfo(1.0, "kg", false),
            ["kilograms"] = new ConversionInfo(1.0, "kg", false),
            ["kg"] = new ConversionInfo(1.0, "kg", false),
            ["gram"] = new ConversionInfo(0.001, "g", false),
            ["grams"] = new ConversionInfo(0.001, "g", false),
            ["g"] = new ConversionInfo(0.001, "g", false),
            ["pound"] = new ConversionInfo(0.45359237, "lb", false),
            ["pounds"] = new ConversionInfo(0.45359237, "lb", false),
            ["lb"] = new ConversionInfo(0.45359237, "lb", false),
            ["ounce"] = new ConversionInfo(0.0283495231, "oz", false),
            ["ounces"] = new ConversionInfo(0.0283495231, "oz", false),
            ["oz"] = new ConversionInfo(0.0283495231, "oz", false),
            ["ton"] = new ConversionInfo(907.18474, "ton", false),
            ["tons"] = new ConversionInfo(907.18474, "ton", false),
            ["milligram"] = new ConversionInfo(0.000001, "mg", false),
            ["milligrams"] = new ConversionInfo(0.000001, "mg", false),
            ["mg"] = new ConversionInfo(0.000001, "mg", false)
        };
    }

    private Dictionary<string, ConversionInfo> InitializeTemperatureConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["celsius"] = new ConversionInfo(0.0, "°C", true),
            ["c"] = new ConversionInfo(0.0, "°C", true),
            ["fahrenheit"] = new ConversionInfo(0.0, "°F", true),
            ["f"] = new ConversionInfo(0.0, "°F", true),
            ["kelvin"] = new ConversionInfo(0.0, "K", true),
            ["k"] = new ConversionInfo(0.0, "K", true)
        };
    }

    private Dictionary<string, ConversionInfo> InitializeVolumeConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["liter"] = new ConversionInfo(1.0, "L", false),
            ["liters"] = new ConversionInfo(1.0, "L", false),
            ["l"] = new ConversionInfo(1.0, "L", false),
            ["milliliter"] = new ConversionInfo(0.001, "mL", false),
            ["milliliters"] = new ConversionInfo(0.001, "mL", false),
            ["ml"] = new ConversionInfo(0.001, "mL", false),
            ["gallon"] = new ConversionInfo(3.785411784, "gal", false),
            ["gallons"] = new ConversionInfo(3.785411784, "gal", false),
            ["gal"] = new ConversionInfo(3.785411784, "gal", false),
            ["quart"] = new ConversionInfo(0.946352946, "qt", false),
            ["quarts"] = new ConversionInfo(0.946352946, "qt", false),
            ["qt"] = new ConversionInfo(0.946352946, "qt", false),
            ["cup"] = new ConversionInfo(0.2365882365, "cup", false),
            ["cups"] = new ConversionInfo(0.2365882365, "cup", false),
            ["pint"] = new ConversionInfo(0.473176473, "pt", false),
            ["pints"] = new ConversionInfo(0.473176473, "pt", false),
            ["pt"] = new ConversionInfo(0.473176473, "pt", false),
            ["fluid ounce"] = new ConversionInfo(0.0295735296, "fl oz", false),
            ["fluid ounces"] = new ConversionInfo(0.0295735296, "fl oz", false),
            ["floz"] = new ConversionInfo(0.0295735296, "fl oz", false)
        };
    }

    private Dictionary<string, ConversionInfo> InitializeAreaConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["square meter"] = new ConversionInfo(1.0, "m²", false),
            ["square meters"] = new ConversionInfo(1.0, "m²", false),
            ["sq meter"] = new ConversionInfo(1.0, "m²", false),
            ["sq meters"] = new ConversionInfo(1.0, "m²", false),
            ["kilometer²"] = new ConversionInfo(1000000.0, "km²", false),
            ["square kilometer"] = new ConversionInfo(1000000.0, "km²", false),
            ["square kilometers"] = new ConversionInfo(1000000.0, "km²", false),
            ["acre"] = new ConversionInfo(4046.8564224, "acre", false),
            ["acres"] = new ConversionInfo(4046.8564224, "acre", false),
            ["hectare"] = new ConversionInfo(10000.0, "ha", false),
            ["hectares"] = new ConversionInfo(10000.0, "ha", false),
            ["sq foot"] = new ConversionInfo(0.09290304, "ft²", false),
            ["sq feet"] = new ConversionInfo(0.09290304, "ft²", false),
            ["square foot"] = new ConversionInfo(0.09290304, "ft²", false),
            ["square feet"] = new ConversionInfo(0.09290304, "ft²", false)
        };
    }

    private Dictionary<string, ConversionInfo> InitializeTimeConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["second"] = new ConversionInfo(1.0, "s", false),
            ["seconds"] = new ConversionInfo(1.0, "s", false),
            ["s"] = new ConversionInfo(1.0, "s", false),
            ["minute"] = new ConversionInfo(60.0, "min", false),
            ["minutes"] = new ConversionInfo(60.0, "min", false),
            ["min"] = new ConversionInfo(60.0, "min", false),
            ["hour"] = new ConversionInfo(3600.0, "h", false),
            ["hours"] = new ConversionInfo(3600.0, "h", false),
            ["h"] = new ConversionInfo(3600.0, "h", false),
            ["day"] = new ConversionInfo(86400.0, "day", false),
            ["days"] = new ConversionInfo(86400.0, "day", false),
            ["week"] = new ConversionInfo(604800.0, "week", false),
            ["weeks"] = new ConversionInfo(604800.0, "week", false),
            ["month"] = new ConversionInfo(2629746.0, "month", false),
            ["months"] = new ConversionInfo(2629746.0, "month", false),
            ["year"] = new ConversionInfo(31556952.0, "year", false),
            ["years"] = new ConversionInfo(31556952.0, "year", false)
        };
    }

    private Dictionary<string, ConversionInfo> InitializeSpeedConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["m/s"] = new ConversionInfo(1.0, "m/s", false),
            ["mps"] = new ConversionInfo(1.0, "m/s", false),
            ["meter per second"] = new ConversionInfo(1.0, "m/s", false),
            ["km/h"] = new ConversionInfo(0.2777777778, "km/h", false),
            ["kmh"] = new ConversionInfo(0.2777777778, "km/h", false),
            ["kilometer per hour"] = new ConversionInfo(0.2777777778, "km/h", false),
            ["mph"] = new ConversionInfo(0.44704, "mph", false),
            ["mile per hour"] = new ConversionInfo(0.44704, "mph", false),
            ["knot"] = new ConversionInfo(0.5144444444, "knot", false),
            ["knots"] = new ConversionInfo(0.5144444444, "knot", false)
        };
    }

    private Dictionary<string, ConversionInfo> InitializeDataConversions()
    {
        return new Dictionary<string, ConversionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["byte"] = new ConversionInfo(1.0, "B", false),
            ["bytes"] = new ConversionInfo(1.0, "B", false),
            ["kilobyte"] = new ConversionInfo(1024.0, "KB", false),
            ["kilobytes"] = new ConversionInfo(1024.0, "KB", false),
            ["kb"] = new ConversionInfo(1024.0, "KB", false),
            ["megabyte"] = new ConversionInfo(1048576.0, "MB", false),
            ["megabytes"] = new ConversionInfo(1048576.0, "MB", false),
            ["mb"] = new ConversionInfo(1048576.0, "MB", false),
            ["gigabyte"] = new ConversionInfo(1073741824.0, "GB", false),
            ["gigabytes"] = new ConversionInfo(1073741824.0, "GB", false),
            ["gb"] = new ConversionInfo(1073741824.0, "GB", false),
            ["terabyte"] = new ConversionInfo(1099511627776.0, "TB", false),
            ["terabytes"] = new ConversionInfo(1099511627776.0, "TB", false),
            ["tb"] = new ConversionInfo(1099511627776.0, "TB", false),
            ["petabyte"] = new ConversionInfo(1125899906842624.0, "PB", false),
            ["petabytes"] = new ConversionInfo(1125899906842624.0, "PB", false),
            ["pb"] = new ConversionInfo(1125899906842624.0, "PB", false)
        };
    }

    public CalculationResult? Convert(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return CalculationResult.Error("Expression cannot be empty", expression);

        try
        {
            var match = Regex.Match(expression.ToLower().Trim(), 
                @"^([\d.]+)\s+([a-z²/]+)\s+(?:to|in)\s+([a-z²/]+)$");
            
            if (!match.Success)
            {
                // Try alternative pattern: "10km miles" or "10 km miles"
                match = Regex.Match(expression.ToLower().Trim(), 
                    @"^([\d.]+)\s*([a-z²/]+)\s+([a-z²/]+)$");
            }

            if (!match.Success)
                return CalculationResult.Error("Invalid conversion format. Use: '10 km to miles'", expression);

            var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var fromUnit = match.Groups[2].Value;
            var toUnit = match.Groups[3].Value;

            var result = PerformConversion(value, fromUnit, toUnit);
            if (result == null)
                return CalculationResult.Error($"Cannot convert from '{fromUnit}' to '{toUnit}'", expression);

            var formattedResult = FormatResult(result.Value);
            
            return new CalculationResult
            {
                Title = expression,
                SubTitle = $"{value} {fromUnit} = {formattedResult} {toUnit}",
                Result = formattedResult,
                RawExpression = expression,
                Type = CalculationType.UnitConversion,
                NumericValue = result.Value,
                Unit = toUnit,
                IsError = false
            };
        }
        catch (Exception ex)
        {
            return CalculationResult.Error(ex.Message, expression);
        }
    }

    private double? PerformConversion(double value, string fromUnit, string toUnit)
    {
        // Try each conversion type
        var conversions = new[]
        {
            _lengthConversions, _weightConversions, _temperatureConversions,
            _volumeConversions, _areaConversions, _timeConversions,
            _speedConversions, _dataConversions
        };

        foreach (var conversionDict in conversions)
        {
            if (conversionDict.TryGetValue(fromUnit, out var fromInfo) && 
                conversionDict.TryGetValue(toUnit, out var toInfo))
            {
                return ConvertValue(value, fromInfo, toInfo);
            }
        }

        return null;
    }

    private double ConvertValue(double value, ConversionInfo from, ConversionInfo to)
    {
        if (from.IsSpecialConversion && to.IsSpecialConversion)
        {
            // Handle temperature conversions
            return ConvertTemperature(value, from.Symbol, to.Symbol);
        }
        else if (!from.IsSpecialConversion && !to.IsSpecialConversion)
        {
            // Handle regular unit conversions
            var baseValue = value * from.ToBaseUnit;
            return baseValue / to.ToBaseUnit;
        }
        else
        {
            throw new ArgumentException("Cannot convert between special and regular units");
        }
    }

    private double ConvertTemperature(double value, string fromUnit, string toUnit)
    {
        // Convert to Celsius first, then to target
        var celsius = fromUnit switch
        {
            "°C" or "C" => value,
            "°F" or "F" => (value - 32) * 5 / 9,
            "K" => value - 273.15,
            _ => throw new ArgumentException($"Unknown temperature unit: {fromUnit}")
        };

        return toUnit switch
        {
            "°C" or "C" => celsius,
            "°F" or "F" => celsius * 9 / 5 + 32,
            "K" => celsius + 273.15,
            _ => throw new ArgumentException($"Unknown temperature unit: {toUnit}")
        };
    }

    private string FormatResult(double result)
    {
        if (double.IsInfinity(result))
            return "Infinity";
        if (double.IsNaN(result))
            return "NaN";
        
        if (Math.Abs(result) > 0 && (Math.Abs(result) < 0.0001 || Math.Abs(result) > 1e10))
        {
            return result.ToString("0.####E+0", CultureInfo.InvariantCulture);
        }
        
        int decimals = Math.Max(0, _settings.Precision);
        var rounded = Math.Round(result, decimals, MidpointRounding.AwayFromZero);
        var format = decimals == 0 ? "0" : "0." + new string('#', decimals);
        return rounded.ToString(format, CultureInfo.InvariantCulture);
    }
}

public class ConversionInfo
{
    public double ToBaseUnit { get; }
    public string Symbol { get; }
    public bool IsSpecialConversion { get; }

    public ConversionInfo(double toBaseUnit, string symbol, bool isSpecialConversion)
    {
        ToBaseUnit = toBaseUnit;
        Symbol = symbol;
        IsSpecialConversion = isSpecialConversion;
    }
}
