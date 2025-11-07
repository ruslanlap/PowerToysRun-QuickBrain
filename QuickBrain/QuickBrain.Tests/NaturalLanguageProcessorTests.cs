using System.Net;
using System.Text;
using System.Text.Json;
using QuickBrain.Modules;
using Xunit;
using Moq;
using Moq.Protected;

namespace QuickBrain.Tests;

public class NaturalLanguageProcessorTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly Settings _testSettings;

    public NaturalLanguageProcessorTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _testSettings = CreateTestSettings();
    }

    private Settings CreateTestSettings()
    {
        return new Settings
        {
            EnableAIParsing = true,
            EnableAiCalculations = true,
            AiTimeout = 5000,
            OfflineFirst = false,
            UseOpenRouter = true,
            OpenRouterApiKey = "test-openrouter-key",
            OpenRouterModel = "test-model"
        };
    }

    [Fact]
    public async Task ProcessAsync_OfflineFirst_ShouldTryOfflineFirst()
    {
        // Arrange
        var settings = new Settings
        {
            OfflineFirst = true,
            EnableAIParsing = false
        };
        var processor = new NaturalLanguageProcessor(settings);

        // Act
        var result = await processor.ProcessAsync("what is 5 + 3");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("5 + 3", result.Result);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
    }

    [Fact]
    public async Task ProcessAsync_WithOpenRouter_ShouldCallApi()
    {
        // Arrange
        var expectedResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new AiResponse
                        {
                            Expression = "5 + 3",
                            Result = "8",
                            Explanation = "Addition of 5 and 3",
                            Confidence = 0.95,
                            Type = "arithmetic"
                        })
                    }
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("what is five plus three");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("8", result.Result);
        Assert.Equal("5 + 3", result.Title);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
        Assert.Equal(95, result.Score); // 0.95 * 100

        // Verify the API was called
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Method == HttpMethod.Post &&
                (req.RequestUri != null && req.RequestUri.ToString().Contains("openrouter.ai"))),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithHuggingFace_ShouldCallApi()
    {
        // Arrange
        var settings = new Settings
        {
            UseHuggingFace = true,
            HuggingFaceApiKey = "test-hf-key",
            HuggingFaceModel = "test-model",
            AiTimeout = 5000,
            EnableAIParsing = true
        };

        var expectedResponse = new[]
        {
            new
            {
                generated_text = JsonSerializer.Serialize(new AiResponse
                {
                    Expression = "10 km to miles",
                    Result = "6.21371",
                    Explanation = "Convert 10 kilometers to miles",
                    Confidence = 0.88,
                    Type = "conversion"
                })
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var processor = new NaturalLanguageProcessor(settings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("convert 10 kilometers to miles");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("6.21371", result.Result);
        Assert.Equal("10 km to miles", result.Title);
        Assert.Equal(CalculationType.UnitConversion, result.Type);
        Assert.Equal(88, result.Score);

        // Verify the API was called
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                (req.RequestUri != null && req.RequestUri.ToString().Contains("huggingface.co"))),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithOpenAI_ShouldCallApi()
    {
        // Arrange
        var settings = new Settings
        {
            AiProvider = "OpenAI",
            OpenAiApiKey = "test-openai-key",
            OpenAiModel = "gpt-4",
            AiTimeout = 5000,
            EnableAIParsing = true
        };

        var expectedResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(new AiResponse
                        {
                            Expression = "25 * 4",
                            Result = "100",
                            Explanation = "Multiplication of 25 and 4",
                            Confidence = 0.99,
                            Type = "arithmetic"
                        })
                    }
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var processor = new NaturalLanguageProcessor(settings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("what is twenty five times four");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("100", result.Result);
        Assert.Equal("25 * 4", result.Title);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
        Assert.Equal(99, result.Score);

        // Verify the API was called
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                (req.RequestUri != null && req.RequestUri.ToString().Contains("api.openai.com"))),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithAnthropic_ShouldCallApi()
    {
        // Arrange
        var settings = new Settings
        {
            AiProvider = "Anthropic",
            AnthropicApiKey = "test-anthropic-key",
            AnthropicModel = "claude-3-sonnet",
            AiTimeout = 5000,
            EnableAIParsing = true
        };

        var expectedResponse = new
        {
            content = new[]
            {
                new
                {
                    text = JsonSerializer.Serialize(new AiResponse
                    {
                        Expression = "sqrt(16)",
                        Result = "4",
                        Explanation = "Square root of 16",
                        Confidence = 0.92,
                        Type = "arithmetic"
                    })
                }
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var processor = new NaturalLanguageProcessor(settings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("what is the square root of sixteen");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("4", result.Result);
        Assert.Equal("sqrt(16)", result.Title);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
        Assert.Equal(92, result.Score);

        // Verify the API was called
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                (req.RequestUri != null && req.RequestUri.ToString().Contains("api.anthropic.com"))),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ApiTimeout_ShouldRetryWithBackoff()
    {
        // Arrange
        var callCount = 0;
        
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw new OperationCanceledException("Timeout");
                }
                
                var expectedResponse = new
                {
                    choices = new[]
                    {
                        new
                        {
                            message = new
                            {
                                content = JsonSerializer.Serialize(new AiResponse
                                {
                                    Expression = "5 + 3",
                                    Result = "8",
                                    Explanation = "Addition",
                                    Confidence = 0.95,
                                    Type = "arithmetic"
                                })
                            }
                        }
                    }
                };

                var responseJson = JsonSerializer.Serialize(expectedResponse);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            });

        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await processor.ProcessAsync("what is 5 + 3");

        // Assert
        stopwatch.Stop();
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("8", result.Result);
        
        // Should have been called 3 times (2 failures + 1 success)
        Assert.Equal(3, callCount);
        
        // Should have taken some time due to backoff delays
        Assert.True(stopwatch.ElapsedMilliseconds > 1000, "Should have waited due to backoff");
    }

    [Fact]
    public async Task ProcessAsync_AllRetriesFail_ShouldReturnError()
    {
        // Arrange
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("what is 5 + 3");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Contains("Unable to process natural language query", result.ErrorMessage);

        // Should have been retried 3 times
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_InvalidJsonResponse_ShouldReturnError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("what is 5 + 3");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ProcessAsync_OfflineFallback_ShouldTryOfflineAfterAiFailure()
    {
        // Arrange
        var settings = new Settings
        {
            OfflineFirst = false,
            EnableAIParsing = true,
            UseOpenRouter = true,
            OpenRouterApiKey = "test-key"
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var processor = new NaturalLanguageProcessor(settings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("what is 5 + 3");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Equal("5 + 3", result.Result);
        Assert.Equal(CalculationType.Arithmetic, result.Type);
    }

    [Fact]
    public async Task ProcessAsync_EmptyQuery_ShouldReturnNull()
    {
        // Arrange
        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);

        // Act
        var result = await processor.ProcessAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessAsync_NullQuery_ShouldReturnNull()
    {
        // Arrange
        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);

        // Act
        var result = await processor.ProcessAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_NullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NaturalLanguageProcessor(null!));
    }

    [Fact]
    public async Task ProcessAsync_RequestHeaders_ShouldIncludeCorrectHeaders()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"{\\\"expression\\\":\\\"5 + 3\\\"}\"}}]}", Encoding.UTF8, "application/json")
            });

        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);

        // Act
        await processor.ProcessAsync("test");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Authorization?.Parameter?.Contains("test-openrouter-key") == true);
        
        if (_testSettings.UseOpenRouter)
        {
            Assert.True(capturedRequest.Headers.Contains("HTTP-Referer"));
            Assert.True(capturedRequest.Headers.Contains("X-Title"));
        }
    }

    [Fact]
    public async Task ProcessAsync_CancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage _, CancellationToken token) =>
            {
                await Task.Delay(10000, token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var processor = new NaturalLanguageProcessor(_testSettings, _httpClient);

        // Act
        cts.Cancel();
        var result = await processor.ProcessAsync("test", cts.Token);

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}