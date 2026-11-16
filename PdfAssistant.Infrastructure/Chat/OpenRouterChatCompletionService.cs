
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Infrastructure.Configuration;
using PdfAssistant.Infrastructure.Models;

namespace PdfAssistant.Infrastructure.Chat;

public class OpenRouterChatCompletionService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterChatCompletionService(
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

    public async Task<string> GenerateAnswerAsync(
        string question,
        string context,
        CancellationToken cancellationToken = default)
    {
        var request = new ChatCompletionRequest
        {
            Model = _options.ChatModel,
            Messages = new List<ChatMessage>
            {
                new()
                {
                    Role = "system",
                    Content = """
                        You are a PDF question-answering assistant.
                        Answer using only the provided document context.
                        If the answer is not in the context, say that
                        the document does not contain this information.
                        Answer in the same language as the user's question.
                        """
                },
                new()
                {
                    Role = "user",
                    Content = $"""
                        Document context:
                        {context}

                        Question:
                        {question}
                        """
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "chat/completions",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"OpenRouter chat error: {(int)response.StatusCode} - {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
            cancellationToken: cancellationToken);

        return result?.Choices.FirstOrDefault()?.Message.Content
            ?? throw new InvalidOperationException(
                "OpenRouter returned an empty response.");
    }
}
