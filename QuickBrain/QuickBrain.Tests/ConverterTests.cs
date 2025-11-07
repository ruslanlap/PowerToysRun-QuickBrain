using System.Globalization;
using QuickBrain;
using QuickBrain.Modules;
using Xunit;

namespace QuickBrain.Tests;

public class ConverterTests
{
    private readonly Converter _converter;

    public ConverterTests()
    {
        var settings = new Settings { Precision = 4 };
        _converter = new Converter(settings);
    }

    [Fact]
    public void LengthConversion_KilometersToMiles_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "10 km to miles";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("6.2137", result.Result);
        Assert.Equal("mi", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 6.2137) < 0.001);
    }

    [Fact]
    public void LengthConversion_FeetToMeters_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "10 feet to meters";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("3.0480", result.Result);
        Assert.Equal("m", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 3.048) < 0.001);
    }

    [Fact]
    public void WeightConversion_PoundsToKilograms_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "10 lb to kg";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("4.5359", result.Result);
        Assert.Equal("kg", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 4.5359) < 0.001);
    }

    [Fact]
    public void TemperatureConversion_CelsiusToFahrenheit_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "100 celsius to fahrenheit";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("212.0000", result.Result);
        Assert.Equal("°F", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 212.0) < 0.001);
    }

    [Fact]
    public void TemperatureConversion_FahrenheitToCelsius_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "32 f to c";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("0.0000", result.Result);
        Assert.Equal("°C", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 0.0) < 0.001);
    }

    [Fact]
    public void VolumeConversion_GallonsToLiters_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "1 gallon to liters";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("3.7854", result.Result);
        Assert.Equal("L", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 3.7854) < 0.001);
    }

    [Fact]
    public void TimeConversion_HoursToMinutes_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2.5 hours to minutes";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("150.0000", result.Result);
        Assert.Equal("min", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 150.0) < 0.001);
    }

    [Fact]
    public void SpeedConversion_MphToKmh_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "60 mph to km/h";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("96.5606", result.Result);
        Assert.Equal("km/h", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 96.5606) < 0.001);
    }

    [Fact]
    public void DataConversion_GigabytesToMegabytes_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "2 GB to MB";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("2048.0000", result.Result);
        Assert.Equal("MB", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 2048.0) < 0.001);
    }

    [Fact]
    public void AreaConversion_SquareFeetToSquareMeters_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "100 sq feet to square meters";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("9.2903", result.Result);
        Assert.Equal("m²", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 9.2903) < 0.001);
    }

    [Fact]
    public void ConversionWithAlternativeFormat_ShortForm_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "5km miles";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("3.1069", result.Result);
        Assert.Equal("miles", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 3.1069) < 0.001);
    }

    [Fact]
    public void InvalidConversionFormat_ReturnsError()
    {
        // Arrange
        var expression = "invalid conversion";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Invalid conversion format. Use: '10 km to miles'", result.ErrorMessage);
    }

    [Fact]
    public void UnsupportedConversion_ReturnsError()
    {
        // Arrange
        var expression = "10 apples to oranges";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Cannot convert from 'apples' to 'oranges'", result.ErrorMessage);
    }

    [Fact]
    public void EmptyExpression_ReturnsError()
    {
        // Arrange
        var expression = "";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("Expression cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void TemperatureConversion_KelvinToCelsius_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "273.15 K to C";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("0.0000", result.Result);
        Assert.Equal("C", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 0.0) < 0.001);
    }

    [Fact]
    public void VolumeConversion_MillilitersToCups_ReturnsCorrectResult()
    {
        // Arrange
        var expression = "236.588 ml to cups";
        
        // Act
        var result = _converter.Convert(expression);
        
        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("1.0000", result.Result);
        Assert.Equal("cups", result.Unit);
        Assert.True(Math.Abs(result.NumericValue!.Value - 1.0) < 0.001);
    }
}