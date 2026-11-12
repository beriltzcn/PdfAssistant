
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using PdfAssistant.Infrastructure.Configuration;
using PdfAssistant.Infrastructure.Embeddings;

namespace PdfAssistant.Tests;

public class OpenRouterEmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingsAsync_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-api-key",
            Model = "test-model"
        });

        var service = new OpenRouterEmbeddingService(
            httpClient,
            options);

        // Act
        var result = await service.GenerateEmbeddingsAsync(
            Array.Empty<string>());

        // Assert
        Assert.Empty(result);
        Assert.False(handler.WasCalled);
    }
}


public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseBody;

    public bool WasCalled { get; private set; }

    public FakeHttpMessageHandler(
        string responseBody = """{"data":[]}""")
    {
        _responseBody = responseBody;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        WasCalled = true;

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                _responseBody,
                Encoding.UTF8,
                "application/json")
        });
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_ValidResponse_ReturnsEmbeddingsInCorrectOrder()
    {
        // Arrange
        var responseBody = """
    {
        "data": [
            {
                "index": 1,
                "embedding": [3.0, 4.0]
            },
            {
                "index": 0,
                "embedding": [1.0, 2.0]
            }
        ]
    }
    """;

        var handler = new FakeHttpMessageHandler(responseBody);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-api-key",
            Model = "test-model"
        });

        var service = new OpenRouterEmbeddingService(httpClient, options);

        var texts = new List<string>
    {
        "Birinci metin",
        "İkinci metin"
    };

        // Act
        var result = await service.GenerateEmbeddingsAsync(texts);

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(new float[] { 1.0f, 2.0f }, result[0]);
        Assert.Equal(new float[] { 3.0f, 4.0f }, result[1]);

        Assert.True(handler.WasCalled);
    }
}



