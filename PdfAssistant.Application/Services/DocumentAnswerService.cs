using PdfAssistant.Application.Interfaces;

namespace PdfAssistant.Application.Services;

public class DocumentAnswerService : IDocumentAnswerService
{
    private readonly IDocumentSearchService _documentSearchService;
    private readonly IChatCompletionService _chatCompletionService;

    public DocumentAnswerService(
        IDocumentSearchService documentSearchService,
        IChatCompletionService chatCompletionService)
    {
        _documentSearchService = documentSearchService;
        _chatCompletionService = chatCompletionService;
    }

    public async Task<string> AskAsync(
        string fileName,
        string question,
        CancellationToken cancellationToken = default)
    {
        // 1. Soruyla ilgili PDF parçalarını bul.
        var searchResults = await _documentSearchService.SearchAsync(
            fileName,
            question,
            topK: 5,
            cancellationToken);

        if (searchResults.Count == 0)
        {
            return "Belgede sorunuzla ilgili bir içerik bulunamadı.";
        }

        // 2. Bulunan metinleri tek bir context haline getir.
        var context = string.Join(
            "\n\n",
            searchResults.Select(result =>
                $"Sayfa {result.Chunk.PageNumber}:\n{result.Chunk.Text}"));

        // 3. Context ve soruyu AI modeline gönder.
        var answer = await _chatCompletionService.GenerateAnswerAsync(
            question,
            context,
            cancellationToken);

        return answer;
    }
}