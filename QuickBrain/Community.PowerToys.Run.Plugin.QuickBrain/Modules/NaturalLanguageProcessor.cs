using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace QuickBrain.Modules;  // Add this namespace declaration

public class AiResponse
{
    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class NaturalLanguageProcessor : IDisposable
{
    private readonly Settings _settings;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly Random _random = new();

    public NaturalLanguageProcessor(Settings settings, HttpClient? httpClient = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(_settings.AiTimeout + 1000) };
            _ownsHttpClient = true;
        }
    }

    public async Task<CalculationResult?> ProcessAsync(string naturalLanguageQuery, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageQuery))
        {
            return null;
        }

        try
        {
            // Always try offline parsing first for speed (BMI, tips, conversions, etc.)
            var offlineResult = await TryOfflineParsingAsync(naturalLanguageQuery, cancellationToken);
            if (offlineResult != null)
            {
                return offlineResult;
            }

            // Try AI parsing only if offline failed and AI is enabled
            if (_settings.EnableAIParsing)
            {
                var aiResult = await TryAiParsingAsync(naturalLanguageQuery, cancellationToken);
                if (aiResult != null)
                {
                    return aiResult;
                }
            }

            return CalculationResult.Error("Unable to process natural language query", naturalLanguageQuery);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing natural language query: {ex.Message}");
            return CalculationResult.Error($"Processing error: {ex.Message}", naturalLanguageQuery);
        }
    }

    private async Task<CalculationResult?> TryOfflineParsingAsync(string query, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        
        // Simple offline pattern matching for common expressions
        var patterns = new Dictionary<string, Func<System.Text.RegularExpressions.Match, CalculationResult?>>
        {
            [@"what\s+is\s+([+\-*/\d\s().]+)\??"] = match =>
            {
                var expression = match.Groups[1].Value.Trim();
                return CalculationResult.Success(query, expression, CalculationType.Arithmetic);
            },
            [@"calculate\s+([+\-*/\d\s().]+)"] = match =>
            {
                var expression = match.Groups[1].Value.Trim();
                return CalculationResult.Success(query, expression, CalculationType.Arithmetic);
            },
            [@"convert\s+(\d+(?:\.\d+)?)\s*(\w+)\s+to\s+(\w+)"] = match =>
            {
                var value = match.Groups[1].Value;
                var fromUnit = match.Groups[2].Value;
                var toUnit = match.Groups[3].Value;
                var conversion = $"{value} {fromUnit} to {toUnit}";
                return CalculationResult.Success(query, conversion, CalculationType.UnitConversion);
            },
            [@"bmi\s+(\d+(?:\.\d+)?)\s*(cm|m)\s+(\d+(?:\.\d+)?)\s*(?:kg)?"] = match =>
            {
                var hVal = double.Parse(match.Groups[1].Value);
                var hUnit = match.Groups[2].Value.ToLower();
                var wVal = double.Parse(match.Groups[3].Value);
                var heightMeters = hUnit == "cm" ? hVal / 100.0 : hVal;
                var bmi = wVal / (heightMeters * heightMeters);
                
                string category;
                string emoji;
                if (bmi < 18.5)
                {
                    category = "Underweight";
                    emoji = "⚠️";
                }
                else if (bmi < 25)
                {
                    category = "Normal";
                    emoji = "✅";
                }
                else if (bmi < 30)
                {
                    category = "Overweight";
                    emoji = "⚠️";
                }
                else
                {
                    category = "Obese";
                    emoji = "🔴";
                }
                
                var result = CalculationResult.Success(query, bmi.ToString("0.##"), CalculationType.Health)
                    .WithSubTitle($"{emoji} BMI: {bmi:0.##} ({category})");
                return result;
            },
            [@"tip\s+(\d+(?:\.\d+)?)%\s+(\d+(?:\.\d+)?)(?!.*%)"] = match =>
            {
                var percent = double.Parse(match.Groups[1].Value) / 100.0;
                var baseAmount = double.Parse(match.Groups[2].Value);
                var tipAmount = baseAmount * percent;
                var total = baseAmount + tipAmount;
                var resultStr = tipAmount.ToString("0.00");
                var result = CalculationResult.Success(query, resultStr, CalculationType.Money)
                    .WithSubTitle($"💰 Tip: {tipAmount:0.00} | Total: {total:0.00}");
                return result;
            },
            [@"(\d+(?:\.\d+)?)%\s+of\s+(\d+(?:\.\d+)?)"] = match =>
            {
                var percent = double.Parse(match.Groups[1].Value) / 100.0;
                var amount = double.Parse(match.Groups[2].Value);
                var resultValue = amount * percent;
                var result = CalculationResult.Success(query, resultValue.ToString("0.##"), CalculationType.Arithmetic)
                    .WithSubTitle($"{match.Groups[1].Value}% of {amount} = {resultValue:0.##}");
                return result;
            },
            [@"(\d+(?:\.\d+)?)\s*(celsius|fahrenheit|c|f)\s+to\s+(celsius|fahrenheit|c|f)"] = match =>
            {
                var value = double.Parse(match.Groups[1].Value);
                var fromUnit = match.Groups[2].Value.ToLower();
                var toUnit = match.Groups[3].Value.ToLower();
                var conversion = $"{value} {fromUnit} to {toUnit}";
                return CalculationResult.Success(query, conversion, CalculationType.UnitConversion);
            }
        };

        foreach (var pattern in patterns)
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern.Key, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var match = regex.Match(query);
            if (match.Success)
            {
                return pattern.Value(match);
            }
        }

        return null;
    }

    private async Task<CalculationResult?> TryAiParsingAsync(string query, CancellationToken cancellationToken)
    {
        var prompt = CreatePrompt(query);
        
        if (_settings.UseHuggingFace && !string.IsNullOrWhiteSpace(_settings.HuggingFaceApiKey))
        {
            return await CallHuggingFaceApiAsync(query, prompt, cancellationToken);
        }
        else if (_settings.UseOpenRouter && !string.IsNullOrWhiteSpace(_settings.OpenRouterApiKey))
        {
            return await CallOpenRouterApiAsync(query, prompt, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(_settings.AiProvider))
        {
            if (_settings.AiProvider == "OpenAI" && !string.IsNullOrWhiteSpace(_settings.OpenAiApiKey))
            {
                return await CallOpenAiApiAsync(query, prompt, cancellationToken);
            }
            else if (_settings.AiProvider == "Anthropic" && !string.IsNullOrWhiteSpace(_settings.AnthropicApiKey))
            {
                return await CallAnthropicApiAsync(query, prompt, cancellationToken);
            }
        }

        return null;
    }

    private string CreatePrompt(string query)
    {
        return $@"You are a precise mathematical assistant. Respond ONLY with valid JSON.

Query: ""{query}""

Required JSON format:
{{
  ""expression"": ""calculable expression (e.g., 5+3, 10*2, 25 km to miles)"",
  ""result"": ""numerical result if calculable"",
  ""explanation"": ""brief explanation"",
  ""confidence"": 0.95,
  ""type"": ""arithmetic|conversion|date|logic|other""
}}

Examples:
Input: ""what is 5+3""
Output: {{""expression"":""5+3"",""result"":""8"",""confidence"":0.95,""type"":""arithmetic"",""explanation"":""Simple addition""}}

Input: ""10 km to miles""
Output: {{""expression"":""10 km to miles"",""result"":""6.21"",""confidence"":0.9,""type"":""conversion"",""explanation"":""Distance conversion""}}

Respond ONLY with JSON. No markdown, no explanations outside JSON.";
    }

    private async Task<CalculationResult?> CallHuggingFaceApiAsync(string originalQuery, string prompt, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var baseDelay = TimeSpan.FromMilliseconds(1000);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_settings.AiTimeout));

                var requestBody = new
                {
                    inputs = prompt,
                    parameters = new
                    {
                        max_new_tokens = 150,
                        temperature = 0.1,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://api-inference.huggingface.co/models/{_settings.HuggingFaceModel}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.HuggingFaceApiKey);
                request.Content = content;

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var aiResponse = ParseHuggingFaceResponse(responseJson);

                if (aiResponse != null && (!string.IsNullOrWhiteSpace(aiResponse.Expression) || !string.IsNullOrWhiteSpace(aiResponse.Result)))
                {
                    var expressionOrResult = aiResponse.Expression ?? aiResponse.Result ?? string.Empty;
                    var calculationType = DetermineCalculationType(aiResponse.Type);
                    var result = CalculationResult.Success(originalQuery, expressionOrResult, calculationType)
                        .WithSubTitle(aiResponse.Explanation ?? $"AI parsed: {expressionOrResult}")
                        .WithScore((int)(aiResponse.Confidence * 100));

                    if (!string.IsNullOrWhiteSpace(aiResponse.Result))
                    {
                        result.Result = aiResponse.Result;
                        if (double.TryParse(aiResponse.Result, out var numericValue))
                        {
                            result.WithNumericValue(numericValue);
                        }
                    }

                    return result;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("HuggingFace API call cancelled");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"HuggingFace API call timed out (attempt {attempt}/{maxRetries})");
                if (attempt == maxRetries) return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HuggingFace API call failed (attempt {attempt}/{maxRetries}): {ex.Message}");
                if (attempt == maxRetries) return null;
            }

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1) + _random.Next(0, 1000));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return null;
    }

    private async Task<CalculationResult?> CallOpenRouterApiAsync(string originalQuery, string prompt, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var baseDelay = TimeSpan.FromMilliseconds(1000);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_settings.AiTimeout));

                var requestBody = new
                {
                    model = _settings.OpenRouterModel,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.1,
                    max_tokens = 150
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.OpenRouterApiKey);
                request.Headers.Add("HTTP-Referer", "https://github.com/microsoft/PowerToys");
                request.Headers.Add("X-Title", "QuickBrain PowerToys Plugin");
                request.Content = content;

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var aiResponse = ParseOpenRouterResponse(responseJson);

                if (aiResponse != null && !string.IsNullOrWhiteSpace(aiResponse.Expression))
                {
                    var calculationType = DetermineCalculationType(aiResponse.Type);
                    var result = CalculationResult.Success(originalQuery, aiResponse.Expression, calculationType)
                        .WithSubTitle(aiResponse.Explanation ?? $"AI parsed: {aiResponse.Expression}")
                        .WithScore((int)(aiResponse.Confidence * 100));

                    if (!string.IsNullOrWhiteSpace(aiResponse.Result))
                    {
                        result.Result = aiResponse.Result;
                        if (double.TryParse(aiResponse.Result, out var numericValue))
                        {
                            result.WithNumericValue(numericValue);
                        }
                    }

                    return result;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("OpenRouter API call cancelled");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"OpenRouter API call timed out (attempt {attempt}/{maxRetries})");
                if (attempt == maxRetries) return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenRouter API call failed (attempt {attempt}/{maxRetries}): {ex.Message}");
                if (attempt == maxRetries) return null;
            }

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1) + _random.Next(0, 1000));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return null;
    }

    private async Task<CalculationResult?> CallOpenAiApiAsync(string originalQuery, string prompt, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var baseDelay = TimeSpan.FromMilliseconds(1000);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_settings.AiTimeout));

                var requestBody = new
                {
                    model = _settings.OpenAiModel,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.1,
                    max_tokens = 150
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.OpenAiApiKey);
                request.Content = content;

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var aiResponse = ParseOpenAiResponse(responseJson);

                if (aiResponse != null && !string.IsNullOrWhiteSpace(aiResponse.Expression))
                {
                    var calculationType = DetermineCalculationType(aiResponse.Type);
                    var result = CalculationResult.Success(originalQuery, aiResponse.Expression, calculationType)
                        .WithSubTitle(aiResponse.Explanation ?? $"AI parsed: {aiResponse.Expression}")
                        .WithScore((int)(aiResponse.Confidence * 100));

                    if (!string.IsNullOrWhiteSpace(aiResponse.Result))
                    {
                        result.Result = aiResponse.Result;
                        if (double.TryParse(aiResponse.Result, out var numericValue))
                        {
                            result.WithNumericValue(numericValue);
                        }
                    }

                    return result;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("OpenAI API call cancelled");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"OpenAI API call timed out (attempt {attempt}/{maxRetries})");
                if (attempt == maxRetries) return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI API call failed (attempt {attempt}/{maxRetries}): {ex.Message}");
                if (attempt == maxRetries) return null;
            }

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1) + _random.Next(0, 1000));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return null;
    }

    private async Task<CalculationResult?> CallAnthropicApiAsync(string originalQuery, string prompt, CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        var baseDelay = TimeSpan.FromMilliseconds(1000);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_settings.AiTimeout));

                var requestBody = new
                {
                    model = _settings.AnthropicModel,
                    max_tokens = 150,
                    temperature = 0.1,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.AnthropicApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = content;

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var aiResponse = ParseAnthropicResponse(responseJson);

                if (aiResponse != null && !string.IsNullOrWhiteSpace(aiResponse.Expression))
                {
                    var calculationType = DetermineCalculationType(aiResponse.Type);
                    var result = CalculationResult.Success(originalQuery, aiResponse.Expression, calculationType)
                        .WithSubTitle(aiResponse.Explanation ?? $"AI parsed: {aiResponse.Expression}")
                        .WithScore((int)(aiResponse.Confidence * 100));

                    if (!string.IsNullOrWhiteSpace(aiResponse.Result))
                    {
                        result.Result = aiResponse.Result;
                        if (double.TryParse(aiResponse.Result, out var numericValue))
                        {
                            result.WithNumericValue(numericValue);
                        }
                    }

                    return result;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Anthropic API call cancelled");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Anthropic API call timed out (attempt {attempt}/{maxRetries})");
                if (attempt == maxRetries) return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Anthropic API call failed (attempt {attempt}/{maxRetries}): {ex.Message}");
                if (attempt == maxRetries) return null;
            }

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1) + _random.Next(0, 1000));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return null;
    }

    private AiResponse? ParseHuggingFaceResponse(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            if (document.RootElement.EnumerateArray().FirstOrDefault().TryGetProperty("generated_text", out var textElement))
            {
                var text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Try direct JSON parse first
                    try
                    {
                        var direct = JsonSerializer.Deserialize<AiResponse>(text);
                        if (direct != null && (!string.IsNullOrWhiteSpace(direct.Expression) || !string.IsNullOrWhiteSpace(direct.Result)))
                        {
                            return direct;
                        }
                    }
                    catch { }

                    // Extract JSON from text (might have markdown or extra text)
                    var jsonMatch = System.Text.RegularExpressions.Regex.Match(text, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (jsonMatch.Success)
                    {
                        try
                        {
                            return JsonSerializer.Deserialize<AiResponse>(jsonMatch.Value);
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse HuggingFace response: {ex.Message}");
        }

        return null;
    }

    private AiResponse? ParseOpenRouterResponse(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("choices", out var choicesElement) &&
                choicesElement.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var content = contentElement.GetString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Some models return raw JSON, others wrap JSON in text. Try direct deserialize first.
                    try
                    {
                        var direct = JsonSerializer.Deserialize<AiResponse>(content);
                        if (direct != null && (!string.IsNullOrWhiteSpace(direct.Expression) || !string.IsNullOrWhiteSpace(direct.Result)))
                        {
                            return direct;
                        }
                    }
                    catch { /* fall through to regex extraction */ }

                    // Extract JSON - use greedy match for nested JSON
                    var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (jsonMatch.Success)
                    {
                        try
                        {
                            return JsonSerializer.Deserialize<AiResponse>(jsonMatch.Value);
                        }
                        catch { /* ignore */ }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse OpenRouter response: {ex.Message}");
        }

        return null;
    }

    private AiResponse? ParseOpenAiResponse(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("choices", out var choicesElement) &&
                choicesElement.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var content = contentElement.GetString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        var direct = JsonSerializer.Deserialize<AiResponse>(content);
                        if (direct != null && (!string.IsNullOrWhiteSpace(direct.Expression) || !string.IsNullOrWhiteSpace(direct.Result)))
                        {
                            return direct;
                        }
                    }
                    catch { }

                    var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (jsonMatch.Success)
                    {
                        try
                        {
                            return JsonSerializer.Deserialize<AiResponse>(jsonMatch.Value);
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse OpenAI response: {ex.Message}");
        }

        return null;
    }

    private AiResponse? ParseAnthropicResponse(string responseJson)
    {
        try
        {
            using var document = JsonDocument.Parse(responseJson);
            if (document.RootElement.TryGetProperty("content", out var contentElement) &&
                contentElement.EnumerateArray().FirstOrDefault().TryGetProperty("text", out var textElement))
            {
                var text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        var direct = JsonSerializer.Deserialize<AiResponse>(text);
                        if (direct != null && (!string.IsNullOrWhiteSpace(direct.Expression) || !string.IsNullOrWhiteSpace(direct.Result)))
                        {
                            return direct;
                        }
                    }
                    catch { }

                    var jsonMatch = System.Text.RegularExpressions.Regex.Match(text, @"\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (jsonMatch.Success)
                    {
                        try
                        {
                            return JsonSerializer.Deserialize<AiResponse>(jsonMatch.Value);
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse Anthropic response: {ex.Message}");
        }

        return null;
    }

    private CalculationType DetermineCalculationType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "arithmetic" => CalculationType.Arithmetic,
            "conversion" => CalculationType.UnitConversion,
            "date" => CalculationType.DateCalculation,
            "logic" => CalculationType.LogicEvaluation,
            "other" => CalculationType.NaturalLanguage,
            _ => CalculationType.AiAssisted
        };
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}