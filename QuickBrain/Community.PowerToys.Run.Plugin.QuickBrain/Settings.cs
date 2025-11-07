using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickBrain
{
    public sealed class Settings
    {
        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; } = 5;

        [JsonPropertyName("precision")]
        public int Precision { get; set; } = 10;

        [JsonPropertyName("angleUnit")]
        public AngleUnit AngleUnit { get; set; } = AngleUnit.Radians;

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "Auto";

        [JsonPropertyName("enableHistory")]
        public bool EnableHistory { get; set; } = true;

        [JsonPropertyName("historyLimit")]
        public int HistoryLimit { get; set; } = 100;

        [JsonPropertyName("enableNaturalLanguage")]
        public bool EnableNaturalLanguage { get; set; } = false;

        [JsonPropertyName("enableAiParsing")]
        public bool EnableAIParsing { get; set; } = false;

        [JsonPropertyName("enableAiCalculations")]
        public bool EnableAiCalculations { get; set; } = false;

        [JsonPropertyName("aiProvider")]
        public string AiProvider { get; set; } = "OpenAI";

        [JsonPropertyName("useHuggingFace")]
        public bool UseHuggingFace { get; set; } = false;

        [JsonPropertyName("useOpenRouter")]
        public bool UseOpenRouter { get; set; } = false;

        [JsonPropertyName("openAiApiKey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OpenAiApiKey { get; set; }

        [JsonPropertyName("openAiModel")]
        public string OpenAiModel { get; set; } = string.Empty;

        [JsonPropertyName("anthropicApiKey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AnthropicApiKey { get; set; }

        [JsonPropertyName("anthropicModel")]
        public string AnthropicModel { get; set; } = string.Empty;

        [JsonPropertyName("huggingFaceApiKey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HuggingFaceApiKey { get; set; }

        [JsonPropertyName("huggingFaceModel")]
        public string HuggingFaceModel { get; set; } = string.Empty;

        [JsonPropertyName("openRouterApiKey")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OpenRouterApiKey { get; set; }

        [JsonPropertyName("openRouterModel")]
        public string OpenRouterModel { get; set; } = "meta-llama/llama-3.3-70b-instruct";

        [JsonPropertyName("aiTimeout")]
        public int AiTimeout { get; set; } = 3000;

        [JsonPropertyName("offlineFirst")]
        public bool OfflineFirst { get; set; } = true;

        public Settings Clone()
        {
            return new Settings
            {
                MaxResults = MaxResults,
                Precision = Precision,
                AngleUnit = AngleUnit,
                Theme = Theme,
                EnableHistory = EnableHistory,
                HistoryLimit = HistoryLimit,
                EnableNaturalLanguage = EnableNaturalLanguage,
                EnableAIParsing = EnableAIParsing,
                EnableAiCalculations = EnableAiCalculations,
                AiProvider = AiProvider,
                UseHuggingFace = UseHuggingFace,
                UseOpenRouter = UseOpenRouter,
                OpenAiApiKey = OpenAiApiKey,
                OpenAiModel = OpenAiModel,
                AnthropicApiKey = AnthropicApiKey,
                AnthropicModel = AnthropicModel,
                HuggingFaceApiKey = HuggingFaceApiKey,
                HuggingFaceModel = HuggingFaceModel,
                OpenRouterApiKey = OpenRouterApiKey,
                OpenRouterModel = OpenRouterModel,
                AiTimeout = AiTimeout,
                OfflineFirst = OfflineFirst
            };
        }

        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (MaxResults < 1 || MaxResults > 20)
            {
                errors.Add("MaxResults must be between 1 and 20");
            }

            if (Precision < 0 || Precision > 28)
            {
                errors.Add("Precision must be between 0 and 28");
            }

            if (HistoryLimit < 0 || HistoryLimit > 1000)
            {
                errors.Add("HistoryLimit must be between 0 and 1000");
            }

            if (AiTimeout < 1000 || AiTimeout > 60000)
            {
                errors.Add("AiTimeout must be between 1000 and 60000 milliseconds");
            }

            // Validate conflicting AI provider options
            var aiProviderCount = 0;
            if (!string.IsNullOrWhiteSpace(AiProvider)) aiProviderCount++;
            if (UseHuggingFace) aiProviderCount++;
            if (UseOpenRouter) aiProviderCount++;

            if (aiProviderCount > 1)
            {
                errors.Add("Only one AI provider can be enabled at a time. Use either AiProvider, UseHuggingFace, or UseOpenRouter.");
            }

            if (EnableAIParsing || EnableAiCalculations)
            {
                var hasValidProvider = false;

                if (!string.IsNullOrWhiteSpace(AiProvider))
                {
                    if (AiProvider == "OpenAI" && !string.IsNullOrWhiteSpace(OpenAiApiKey))
                    {
                        hasValidProvider = true;
                    }
                    else if (AiProvider == "Anthropic" && !string.IsNullOrWhiteSpace(AnthropicApiKey))
                    {
                        hasValidProvider = true;
                    }
                    else if (AiProvider == "OpenAI")
                    {
                        errors.Add("OpenAI API key is required when using OpenAI provider");
                    }
                    else if (AiProvider == "Anthropic")
                    {
                        errors.Add("Anthropic API key is required when using Anthropic provider");
                    }
                    else
                    {
                        errors.Add("Invalid AiProvider specified");
                    }
                }
                else if (UseHuggingFace)
                {
                    if (string.IsNullOrWhiteSpace(HuggingFaceApiKey))
                    {
                        errors.Add("HuggingFace API key is required when UseHuggingFace is enabled");
                    }
                    else
                    {
                        hasValidProvider = true;
                    }
                }
                else if (UseOpenRouter)
                {
                    if (string.IsNullOrWhiteSpace(OpenRouterApiKey))
                    {
                        errors.Add("OpenRouter API key is required when UseOpenRouter is enabled");
                    }
                    else
                    {
                        hasValidProvider = true;
                    }
                }

                if (!hasValidProvider && errors.Count == 0)
                {
                    errors.Add("At least one AI provider must be configured when AI parsing or calculations are enabled");
                }
            }

            return errors.Count == 0;
        }

        public string ToJsonForLogging()
        {
            var safeSettings = Clone();
            safeSettings.OpenAiApiKey = string.IsNullOrWhiteSpace(OpenAiApiKey) ? null : "***";
            safeSettings.AnthropicApiKey = string.IsNullOrWhiteSpace(AnthropicApiKey) ? null : "***";
            safeSettings.HuggingFaceApiKey = string.IsNullOrWhiteSpace(HuggingFaceApiKey) ? null : "***";
            safeSettings.OpenRouterApiKey = string.IsNullOrWhiteSpace(OpenRouterApiKey) ? null : "***";
            return JsonSerializer.Serialize(safeSettings, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public enum AngleUnit
    {
        Radians,
        Degrees,
        Gradians
    }
}