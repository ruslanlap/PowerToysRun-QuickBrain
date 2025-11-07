using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wox.Plugin;

namespace QuickBrain.Modules
{
    public class AiStreamingModule : IDisposable
    {
        private readonly Settings _settings;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly Random _random = new();
        private readonly object _sessionGate = new();

        private StreamingSession? _session;
        private bool _uiRefreshPending;

        private const int DefaultMaxTokens = 128;
        private const double DefaultTemperature = 0.2;
        private const int DefaultTimeoutSeconds = 3;

        private static readonly IReadOnlyDictionary<string, ProviderConfiguration> ProviderConfigurations =
            new Dictionary<string, ProviderConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                ["Groq"] = new("https://api.groq.com/openai/v1/chat/completions", ProviderSchemaType.OpenAI),
                ["Together"] = new("https://api.together.xyz/v1/chat/completions", ProviderSchemaType.OpenAI),
                ["Fireworks"] = new("https://api.fireworks.ai/inference/v1/chat/completions", ProviderSchemaType.OpenAI),
                ["OpenRouter"] = new("https://openrouter.ai/api/v1/chat/completions", ProviderSchemaType.OpenAI),
                ["HuggingFace"] = new("https://api-inference.huggingface.co/models", ProviderSchemaType.HuggingFace),
                ["Cohere"] = new("https://api.cohere.com/v1/chat", ProviderSchemaType.Cohere),
                ["Google"] = new("https://generativelanguage.googleapis.com/v1beta", ProviderSchemaType.Google)
            };

        public AiStreamingModule(Settings settings, HttpClient? httpClient = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (httpClient != null)
            {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else
            {
                _httpClient = CreateHttpClient();
                _ownsHttpClient = true;
            }
        }

        public bool IsStreaming
        {
            get
            {
                lock (_sessionGate)
                {
                    return _session != null;
                }
            }
        }

        public Result? ProcessQuery(string rawQuery, string prompt, string iconPath, Action<string> triggerRefresh)
        {
            lock (_sessionGate)
            {
                if (_uiRefreshPending && _session is not null && string.Equals(_session.RawQuery, rawQuery, StringComparison.Ordinal))
                {
                    _uiRefreshPending = false;
                    return _session.BuildResult(iconPath, GetProvider(), GetModel());
                }

                if (_session is not null && !string.Equals(_session.RawQuery, rawQuery, StringComparison.Ordinal))
                {
                    _session.Cancel();
                    _session = null;
                }

                if (!HasConfiguredApiKey())
                {
                    return new Result
                    {
                        Title = "AI API key required",
                        SubTitle = "Configure AI API keys in QuickBrain settings",
                        IcoPath = iconPath,
                        Score = 100
                    };
                }

                // If session is active, show streaming result
                if (_session is not null)
                {
                    return _session.BuildResult(iconPath, GetProvider(), GetModel());
                }

                // Otherwise, show prompt ready to submit
                var displayTitle = prompt.Length > 100 ? prompt.Substring(0, 97) + "..." : prompt;
                return new Result
                {
                    Title = $"AI: {displayTitle}",
                    SubTitle = $"Press Enter to ask {GetProvider()} · {GetModel()}",
                    IcoPath = iconPath,
                    Score = 100,
                    Action = _ =>
                    {
                        StartQuery(rawQuery, prompt, triggerRefresh);
                        return false;
                    }
                };
            }
        }

        public List<ContextMenuResult> BuildContextMenu(string? responseText, string pluginName)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return new List<ContextMenuResult>();
            }

            var menu = new List<ContextMenuResult>
            {
                new ContextMenuResult
                {
                    PluginName = pluginName,
                    Title = "Show full response (Enter)",
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE8A7", // View icon
                    AcceleratorKey = System.Windows.Input.Key.Enter,
                    AcceleratorModifiers = System.Windows.Input.ModifierKeys.None,
                    Action = _ =>
                    {
                        try
                        {
                            System.Windows.MessageBox.Show(responseText, "QuickBrain AI Response", 
                                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                },
                new ContextMenuResult
                {
                    PluginName = pluginName,
                    Title = "Copy response (Ctrl+C)",
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE8C8", // Copy icon
                    AcceleratorKey = System.Windows.Input.Key.C,
                    AcceleratorModifiers = System.Windows.Input.ModifierKeys.Control,
                    Action = _ =>
                    {
                        try
                        {
                            System.Windows.Clipboard.SetText(responseText);
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                },
                new ContextMenuResult
                {
                    PluginName = pluginName,
                    Title = "Restart query (Ctrl+R)",
                    FontFamily = "Segoe MDL2 Assets",
                    Glyph = "\xE72C", // Refresh icon
                    AcceleratorKey = System.Windows.Input.Key.R,
                    AcceleratorModifiers = System.Windows.Input.ModifierKeys.Control,
                    Action = _ =>
                    {
                        lock (_sessionGate)
                        {
                            _session?.Restart();
                        }
                        return true;
                    }
                }
            };

            return menu;
        }

        public void CancelSession()
        {
            lock (_sessionGate)
            {
                _session?.Cancel();
                _session = null;
            }
        }

        private void StartQuery(string rawQuery, string prompt, Action<string> triggerRefresh)
        {
            lock (_sessionGate)
            {
                if (_session is null)
                {
                    _session = new StreamingSession(this, rawQuery, prompt, triggerRefresh);
                    _session.Start();
                }
            }

            TriggerRefresh(rawQuery, triggerRefresh);
        }

        private void TriggerRefresh(string rawQuery, Action<string> triggerRefresh)
        {
            lock (_sessionGate)
            {
                if (_session is null || !string.Equals(_session.RawQuery, rawQuery, StringComparison.Ordinal))
                {
                    return;
                }

                _uiRefreshPending = true;
            }

            triggerRefresh(rawQuery);
        }

        private bool HasConfiguredApiKey()
        {
            var provider = GetProvider();
            return provider switch
            {
                "Groq" or "Together" or "Fireworks" or "OpenRouter" => !string.IsNullOrWhiteSpace(_settings.OpenRouterApiKey),
                "HuggingFace" => !string.IsNullOrWhiteSpace(_settings.HuggingFaceApiKey),
                "Google" => !string.IsNullOrWhiteSpace(_settings.HuggingFaceApiKey),
                "Cohere" => !string.IsNullOrWhiteSpace(_settings.HuggingFaceApiKey),
                _ => !string.IsNullOrWhiteSpace(_settings.OpenAiApiKey)
            };
        }

        private string GetProvider()
        {
            return _settings.UseOpenRouter ? "OpenRouter" : 
                   _settings.UseHuggingFace ? "HuggingFace" : 
                   "OpenRouter";
        }

        private string GetModel()
        {
            return _settings.UseOpenRouter ? (_settings.OpenRouterModel ?? "meta-llama/llama-3.2-3b-instruct:free") :
                   _settings.UseHuggingFace ? (_settings.HuggingFaceModel ?? "meta-llama/Llama-3.2-3B-Instruct") :
                   "meta-llama/llama-3.2-3b-instruct:free";
        }

        private string? GetApiKey()
        {
            var provider = GetProvider();
            return provider switch
            {
                "OpenRouter" => _settings.OpenRouterApiKey,
                "Groq" or "Together" or "Fireworks" => _settings.OpenRouterApiKey,
                "HuggingFace" => _settings.HuggingFaceApiKey,
                "Google" or "Cohere" => _settings.HuggingFaceApiKey,
                _ => _settings.OpenAiApiKey
            };
        }

        internal void BeginStreaming(StreamingSession session)
        {
            var configuration = new ConfigurationSnapshot(
                GetProvider(),
                GetApiKey(),
                null,
                GetModel(),
                DefaultMaxTokens,
                DefaultTemperature,
                DefaultTimeoutSeconds
            );

            _ = Task.Run(async () =>
            {
                await StreamWithConfigurationAsync(session, configuration).ConfigureAwait(false);
            });
        }

        private async Task StreamWithConfigurationAsync(StreamingSession session, ConfigurationSnapshot configuration)
        {
            if (!ProviderConfigurations.TryGetValue(configuration.Provider, out var providerConfiguration))
            {
                session.SetError($"Unsupported provider: {configuration.Provider}");
                session.TriggerRefresh();
                return;
            }

            session.SetStatus($"Requesting from {configuration.Provider}...");
            session.TriggerRefresh();

            var prompt = session.SnapshotPrompt();

            if (string.IsNullOrWhiteSpace(configuration.PrimaryApiKey))
            {
                session.SetError("Configure an API key for this provider.");
                session.TriggerRefresh();
                return;
            }

            try
            {
                await foreach (var chunk in ExecuteStreamingRequestAsync(
                    providerConfiguration,
                    configuration,
                    prompt,
                    configuration.PrimaryApiKey!,
                    session.Token).ConfigureAwait(false))
                {
                    session.Append(chunk);
                }

                session.MarkCompleted();
            }
            catch (AuthenticationException authEx)
            {
                session.SetError($"Authentication failed: {authEx.Message}");
                session.TriggerRefresh();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                session.SetError($"Request failed: {ex.Message}");
                session.TriggerRefresh();
            }
        }

        private async IAsyncEnumerable<string> ExecuteStreamingRequestAsync(
            ProviderConfiguration providerConfiguration,
            ConfigurationSnapshot configuration,
            string prompt,
            string apiKey,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var request = BuildHttpRequest(providerConfiguration, configuration, prompt, apiKey);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(configuration.TimeoutSeconds));

            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token)
                .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new AuthenticationException("Authentication failed for the configured API key.");
            }

            if (response.StatusCode == HttpStatusCode.Gone)
            {
                throw new InvalidOperationException($"Model '{configuration.Model}' is no longer available (410 Gone). Try a different model.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException($"API request failed ({(int)response.StatusCode}): {errorBody}");
            }

            await foreach (var token in ParseStreamAsync(response, providerConfiguration, timeoutCts, configuration.TimeoutSeconds, cancellationToken).ConfigureAwait(false))
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(configuration.TimeoutSeconds));
                yield return token;
            }
        }

        private async IAsyncEnumerable<string> ParseStreamAsync(
            HttpResponseMessage response,
            ProviderConfiguration configuration,
            CancellationTokenSource timeoutSource,
            int timeoutSeconds,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var payload = line[5..].Trim();
                if (string.IsNullOrEmpty(payload))
                {
                    continue;
                }

                if (payload.Equals("[DONE]", StringComparison.OrdinalIgnoreCase))
                {
                    yield break;
                }

                string? token = null;

                try
                {
                    using var document = JsonDocument.Parse(payload);
                    token = configuration.SchemaType switch
                    {
                        ProviderSchemaType.OpenAI => ExtractOpenAiDelta(document.RootElement),
                        ProviderSchemaType.HuggingFace => ExtractHuggingFaceDelta(document.RootElement),
                        ProviderSchemaType.Cohere => ExtractCohereDelta(document.RootElement),
                        ProviderSchemaType.Google => ExtractGoogleDelta(document.RootElement),
                        _ => null
                    };
                }
                catch (JsonException)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(token))
                {
                    timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                    yield return token;
                }
            }
        }

        private HttpRequestMessage BuildHttpRequest(
            ProviderConfiguration providerConfiguration,
            ConfigurationSnapshot configuration,
            string prompt,
            string apiKey)
        {
            string endpoint = providerConfiguration.Endpoint;
            string json;

            switch (providerConfiguration.SchemaType)
            {
                case ProviderSchemaType.HuggingFace:
                    // HuggingFace uses model in URL path
                    endpoint = $"{providerConfiguration.Endpoint}/{configuration.Model}";
                    var hfPayload = new
                    {
                        inputs = prompt,
                        parameters = new
                        {
                            temperature = configuration.Temperature,
                            max_new_tokens = configuration.MaxTokens,
                            return_full_text = false
                        },
                        options = new
                        {
                            use_cache = false,
                            wait_for_model = true
                        },
                        stream = true
                    };
                    json = JsonSerializer.Serialize(hfPayload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    break;

                case ProviderSchemaType.Google:
                    endpoint = $"{providerConfiguration.Endpoint}/models/{configuration.Model}:streamGenerateContent?alt=sse";
                    var googlePayload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[] { new { text = prompt } }
                            }
                        },
                        generationConfig = new
                        {
                            temperature = configuration.Temperature,
                            maxOutputTokens = configuration.MaxTokens
                        }
                    };
                    json = JsonSerializer.Serialize(googlePayload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    break;

                case ProviderSchemaType.Cohere:
                    var coherePayload = new
                    {
                        model = configuration.Model,
                        message = prompt,
                        stream = true,
                        temperature = configuration.Temperature,
                        max_tokens = configuration.MaxTokens
                    };
                    json = JsonSerializer.Serialize(coherePayload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    break;

                case ProviderSchemaType.OpenAI:
                default:
                    var openAiPayload = new
                    {
                        model = configuration.Model,
                        messages = new[] { new { role = "user", content = prompt } },
                        stream = true,
                        temperature = configuration.Temperature,
                        max_tokens = configuration.MaxTokens
                    };
                    json = JsonSerializer.Serialize(openAiPayload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    break;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

            if (providerConfiguration.SchemaType == ProviderSchemaType.Google)
            {
                request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
            }
            else if (providerConfiguration.SchemaType == ProviderSchemaType.HuggingFace)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("PowerToys-QuickBrain/1.0");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            if (string.Equals(configuration.Provider, "OpenRouter", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://github.com/microsoft/PowerToys");
                request.Headers.TryAddWithoutValidation("X-Title", "PowerToys Run QuickBrain");
            }

            request.Content = new StringContent(json, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            return request;
        }

        private static string? ExtractOpenAiDelta(JsonElement root)
        {
            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return null;
            }

            var choice = choices[0];
            if (choice.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.Object)
            {
                if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString();
                }
            }

            return null;
        }

        private static string? ExtractHuggingFaceDelta(JsonElement root)
        {
            // HuggingFace streaming format: {"token":{"text":"chunk"},"generated_text":null}
            if (root.TryGetProperty("token", out var token))
            {
                if (token.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }
            }

            // Alternative format: {"generated_text":"full text","token":{"text":"chunk"}}
            if (root.TryGetProperty("generated_text", out var generatedText) && 
                generatedText.ValueKind == JsonValueKind.String)
            {
                // This is the final response, not a chunk
                return null;
            }

            return null;
        }

        private static string? ExtractCohereDelta(JsonElement root)
        {
            if (root.TryGetProperty("event_type", out var eventType) && eventType.ValueKind == JsonValueKind.String)
            {
                var type = eventType.GetString();
                if (string.Equals(type, "text-generation", StringComparison.OrdinalIgnoreCase) &&
                    root.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }
            }

            return null;
        }

        private static string? ExtractGoogleDelta(JsonElement root)
        {
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var content))
            {
                return null;
            }

            if (!content.TryGetProperty("parts", out var parts) || parts.GetArrayLength() == 0)
            {
                return null;
            }

            var part = parts[0];
            if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            {
                return text.GetString();
            }

            return null;
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 10,
                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.Deflate | DecompressionMethods.GZip,
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                },
                UseCookies = false
            };

            var client = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan,
                DefaultRequestVersion = new Version(2, 0),
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            return client;
        }

        public void Dispose()
        {
            lock (_sessionGate)
            {
                _session?.Dispose();
                _session = null;
            }

            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
        }

        private enum ProviderSchemaType
        {
            OpenAI,
            HuggingFace,
            Cohere,
            Google
        }

        private sealed record ProviderConfiguration(string Endpoint, ProviderSchemaType SchemaType);

        private sealed record ConfigurationSnapshot(
            string Provider,
            string? PrimaryApiKey,
            string? SecondaryApiKey,
            string Model,
            int MaxTokens,
            double Temperature,
            int TimeoutSeconds);

        internal sealed class StreamingSession : IDisposable
        {
            private readonly AiStreamingModule _owner;
            private readonly object _sync = new();
            private readonly StringBuilder _buffer = new();
            private readonly Action<string> _triggerRefresh;
            private CancellationTokenSource _cts = new();
            private string _prompt;
            private string? _status;
            private bool _hasError;
            private bool _completed;

            private int _chunksSinceLastRefresh = 0;
            private const int ChunksPerRefresh = 1;
            private DateTime _lastRefreshTime = DateTime.UtcNow;
            private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromMilliseconds(50);

            public StreamingSession(AiStreamingModule owner, string rawQuery, string prompt, Action<string> triggerRefresh)
            {
                _owner = owner;
                RawQuery = rawQuery;
                _prompt = prompt;
                _triggerRefresh = triggerRefresh;
            }

            public string RawQuery { get; }

            public CancellationToken Token
            {
                get
                {
                    lock (_sync)
                    {
                        return _cts.Token;
                    }
                }
            }

            public bool HasCompleted
            {
                get
                {
                    lock (_sync)
                    {
                        return _completed;
                    }
                }
            }

            public void Start()
            {
                _owner.BeginStreaming(this);
            }

            public void Restart()
            {
                lock (_sync)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = new CancellationTokenSource();
                    _buffer.Clear();
                    _status = null;
                    _hasError = false;
                    _completed = false;
                    _chunksSinceLastRefresh = 0;
                    _lastRefreshTime = DateTime.UtcNow;
                }

                _owner.BeginStreaming(this);
            }

            public void Cancel()
            {
                lock (_sync)
                {
                    _cts.Cancel();
                }
            }

            public void Append(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                bool shouldRefresh = false;

                lock (_sync)
                {
                    _buffer.Append(text);
                    _status = null;
                    _chunksSinceLastRefresh++;

                    var timeSinceRefresh = DateTime.UtcNow - _lastRefreshTime;

                    if (_chunksSinceLastRefresh >= ChunksPerRefresh || timeSinceRefresh >= MinRefreshInterval)
                    {
                        shouldRefresh = true;
                        _chunksSinceLastRefresh = 0;
                        _lastRefreshTime = DateTime.UtcNow;
                    }
                }

                if (shouldRefresh)
                {
                    TriggerRefresh();
                }
            }

            public void MarkCompleted()
            {
                lock (_sync)
                {
                    _completed = true;
                }

                TriggerRefresh();
            }

            public void SetStatus(string message)
            {
                lock (_sync)
                {
                    _status = message;
                    _hasError = false;
                }
            }

            public void SetError(string message)
            {
                lock (_sync)
                {
                    _status = message;
                    _hasError = true;
                }
            }

            public string SnapshotPrompt()
            {
                lock (_sync)
                {
                    return _prompt;
                }
            }

            public void TriggerRefresh()
            {
                _triggerRefresh(RawQuery);
            }

            public Result BuildResult(string iconPath, string provider, string model)
            {
                lock (_sync)
                {
                    var responseText = _buffer.ToString();
                    var title = string.Empty;
                    var subtitle = string.Empty;

                    if (_buffer.Length > 0)
                    {
                        var lines = responseText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        if (lines.Length > 0)
                        {
                            title = lines[0].Length > 100 ? lines[0].Substring(0, 97) + "..." : lines[0];
                            
                            if (lines.Length > 1)
                            {
                                var secondLine = lines[1].Length > 80 ? lines[1].Substring(0, 77) + "..." : lines[1];
                                subtitle = _completed 
                                    ? $"{secondLine} | {provider} · {model}"
                                    : $"{secondLine} | Streaming...";
                            }
                            else
                            {
                                subtitle = _completed ? $"{provider} · {model}" : "Streaming...";
                            }
                        }
                        else
                        {
                            title = responseText.Length > 100 ? responseText.Substring(0, 97) + "..." : responseText;
                            subtitle = _completed ? $"{provider} · {model}" : "Streaming...";
                        }
                    }
                    else
                    {
                        title = _status ?? "Streaming response...";
                        subtitle = _hasError ? "Request failed." : $"{provider} · {model}";
                    }

                    return new Result
                    {
                        Title = title,
                        SubTitle = subtitle,
                        IcoPath = iconPath,
                        Score = 100,
                        Action = _ => CopyToClipboard(),
                        ContextData = responseText
                    };
                }
            }

            private bool CopyToClipboard()
            {
                string text;

                lock (_sync)
                {
                    text = _buffer.ToString();
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                try
                {
                    System.Windows.Clipboard.SetText(text);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public void Dispose()
            {
                lock (_sync)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
            }
        }
    }
}
