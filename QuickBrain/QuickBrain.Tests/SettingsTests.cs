namespace QuickBrain.Tests;

public class SettingsTests
{
    [Fact]
    public void Settings_DefaultValues_AreCorrect()
    {
        var settings = new Settings();

        Assert.Equal(5, settings.MaxResults);
        Assert.Equal(10, settings.Precision);
        Assert.Equal(AngleUnit.Radians, settings.AngleUnit);
        Assert.Equal("Auto", settings.Theme);
        Assert.True(settings.EnableHistory);
        Assert.Equal(100, settings.HistoryLimit);
        Assert.False(settings.EnableNaturalLanguage);
        Assert.False(settings.EnableAiCalculations);
        Assert.Equal("OpenAI", settings.AiProvider);
        Assert.True(settings.OfflineFirst);
    }

    [Fact]
    public void Settings_Validate_AcceptsValidSettings()
    {
        var settings = new Settings
        {
            MaxResults = 10,
            Precision = 5,
            HistoryLimit = 50,
            AiTimeout = 5000
        };

        var isValid = settings.Validate(out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Settings_Validate_RejectsInvalidMaxResults()
    {
        var settings = new Settings { MaxResults = 0 };

        var isValid = settings.Validate(out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("MaxResults"));
    }

    [Fact]
    public void Settings_Validate_RejectsInvalidPrecision()
    {
        var settings = new Settings { Precision = 30 };

        var isValid = settings.Validate(out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Precision"));
    }

    [Fact]
    public void Settings_Validate_RequiresApiKeyForAiCalculations()
    {
        var settings = new Settings
        {
            EnableAiCalculations = true,
            AiProvider = "OpenAI",
            OpenAiApiKey = null
        };

        var isValid = settings.Validate(out var errors);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("OpenAI API key"));
    }

    [Fact]
    public void Settings_ToJsonForLogging_MasksApiKeys()
    {
        var settings = new Settings
        {
            OpenAiApiKey = "sk-secret-key",
            AnthropicApiKey = "claude-secret-key"
        };

        var json = settings.ToJsonForLogging();

        Assert.DoesNotContain("sk-secret-key", json);
        Assert.DoesNotContain("claude-secret-key", json);
        Assert.Contains("***", json);
    }

    [Fact]
    public void Settings_Clone_CreatesIndependentCopy()
    {
        var original = new Settings
        {
            MaxResults = 10,
            OpenAiApiKey = "test-key"
        };

        var clone = original.Clone();
        clone.MaxResults = 20;
        clone.OpenAiApiKey = "different-key";

        Assert.Equal(10, original.MaxResults);
        Assert.Equal("test-key", original.OpenAiApiKey);
        Assert.Equal(20, clone.MaxResults);
        Assert.Equal("different-key", clone.OpenAiApiKey);
    }
}
