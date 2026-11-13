using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Infrastructure.Configuration;
using PdfAssistant.Infrastructure.Models;

namespace PdfAssistant.Infrastructure.Embeddings;

public class OpenRouterEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterEmbeddingService(
        HttpClient httpClient,
        IOptions<OpenRouterOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress =
            new Uri("https://openrouter.ai/api/v1/");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey);
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>>
        GenerateEmbeddingsAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        var request = new EmbeddingRequest(
            _options.Model,
            texts);

        var response = await _httpClient.PostAsJsonAsync(
            "embeddings",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"OpenRouter error: {(int)response.StatusCode} - {errorBody}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<EmbeddingResponse>(
                cancellationToken: cancellationToken);

        if (result is null ||
            result.Data.Count != texts.Count)
        {
            throw new InvalidOperationException(
                "Invalid embedding response received.");
        }

        return result.Data
            .OrderBy(item => item.Index)
            .Select(item => (IReadOnlyList<float>)item.Embedding)
            .ToList();
    }
}