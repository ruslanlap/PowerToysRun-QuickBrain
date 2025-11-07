using Xunit;
using System.Collections.Generic;

namespace QuickBrain.Tests;

public class SettingsAiProviderTests
{
    [Fact]
    public void Validate_WithHuggingFaceEnabled_ShouldValidateApiKey()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            UseHuggingFace = true,
            HuggingFaceApiKey = null
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("HuggingFace API key is required"));
    }

    [Fact]
    public void Validate_WithOpenRouterEnabled_ShouldValidateApiKey()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            UseOpenRouter = true,
            OpenRouterApiKey = ""
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("OpenRouter API key is required"));
    }

    [Fact]
    public void Validate_WithMultipleProvidersEnabled_ShouldReturnError()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            AiProvider = "OpenAI",
            UseHuggingFace = true,
            OpenAiApiKey = "test-key",
            HuggingFaceApiKey = "test-key"
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Only one AI provider can be enabled at a time"));
    }

    [Fact]
    public void Validate_WithValidHuggingFace_ShouldPass()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            UseHuggingFace = true,
            HuggingFaceApiKey = "test-key",
            HuggingFaceModel = "test-model"
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithValidOpenRouter_ShouldPass()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            UseOpenRouter = true,
            OpenRouterApiKey = "test-key",
            OpenRouterModel = "test-model"
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithAiCalculationsEnabled_ShouldValidateProvider()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAiCalculations = true,
            UseOpenRouter = true,
            OpenRouterApiKey = null
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("OpenRouter API key is required"));
    }

    [Fact]
    public void Validate_WithAiDisabled_ShouldNotValidateApiKeys()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = false,
            EnableAiCalculations = false,
            UseHuggingFace = true,
            UseOpenRouter = true,
            HuggingFaceApiKey = null,
            OpenRouterApiKey = null
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithInvalidAiProvider_ShouldReturnError()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            AiProvider = "InvalidProvider",
            OpenAiApiKey = "test-key"
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Invalid AiProvider specified"));
    }

    [Fact]
    public void Validate_WithNoProviderConfigured_ShouldReturnError()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("At least one AI provider must be configured"));
    }

    [Fact]
    public void ToJsonForLogging_ShouldMaskAllApiKeys()
    {
        // Arrange
        var settings = new Settings
        {
            OpenAiApiKey = "openai-secret",
            AnthropicApiKey = "anthropic-secret",
            HuggingFaceApiKey = "huggingface-secret",
            OpenRouterApiKey = "openrouter-secret"
        };

        // Act
        var json = settings.ToJsonForLogging();

        // Assert
        Assert.Contains("\"***\"", json);
        Assert.DoesNotContain("openai-secret", json);
        Assert.DoesNotContain("anthropic-secret", json);
        Assert.DoesNotContain("huggingface-secret", json);
        Assert.DoesNotContain("openrouter-secret", json);
    }

    [Fact]
    public void Clone_ShouldCopyAllAiProperties()
    {
        // Arrange
        var original = new Settings
        {
            EnableAIParsing = true,
            UseHuggingFace = true,
            UseOpenRouter = false,
            HuggingFaceApiKey = "test-key",
            HuggingFaceModel = "test-model",
            OpenRouterApiKey = "router-key",
            OpenRouterModel = "router-model"
        };

        // Act
        var cloned = original.Clone();

        // Assert
        Assert.Equal(original.EnableAIParsing, cloned.EnableAIParsing);
        Assert.Equal(original.UseHuggingFace, cloned.UseHuggingFace);
        Assert.Equal(original.UseOpenRouter, cloned.UseOpenRouter);
        Assert.Equal(original.HuggingFaceApiKey, cloned.HuggingFaceApiKey);
        Assert.Equal(original.HuggingFaceModel, cloned.HuggingFaceModel);
        Assert.Equal(original.OpenRouterApiKey, cloned.OpenRouterApiKey);
        Assert.Equal(original.OpenRouterModel, cloned.OpenRouterModel);
    }

    [Fact]
    public void Validate_WithOpenAiProvider_ShouldValidateApiKey()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            AiProvider = "OpenAI",
            OpenAiApiKey = ""
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("OpenAI API key is required"));
    }

    [Fact]
    public void Validate_WithAnthropicProvider_ShouldValidateApiKey()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            AiProvider = "Anthropic",
            AnthropicApiKey = null
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Anthropic API key is required"));
    }

    [Fact]
    public void Validate_WithValidOpenAiProvider_ShouldPass()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            AiProvider = "OpenAI",
            OpenAiApiKey = "test-key",
            OpenAiModel = "gpt-4"
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithValidAnthropicProvider_ShouldPass()
    {
        // Arrange
        var settings = new Settings
        {
            EnableAIParsing = true,
            AiProvider = "Anthropic",
            AnthropicApiKey = "test-key",
            AnthropicModel = "claude-3-sonnet"
        };

        // Act
        var isValid = settings.Validate(out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }
}