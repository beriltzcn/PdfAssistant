using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Services;

public class DocumentSearchService : IDocumentSearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentChunkRepository _documentChunkRepository;

    public DocumentSearchService(
        IEmbeddingService embeddingService,
        IDocumentChunkRepository documentChunkRepository)
    {
        _embeddingService = embeddingService;
        _documentChunkRepository = documentChunkRepository;
    }

    public async Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        string fileName,
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question cannot be empty.");

        if (topK <= 0)
            throw new ArgumentOutOfRangeException(nameof(topK));

        // 1. Kullanıcının sorusunu embedding'e çevir.
        var questionVectors = await _embeddingService.GenerateEmbeddingsAsync(
            new[] { question },
            cancellationToken);

        var questionVector = questionVectors[0];

        // 2. PDF'nin kayıtlı embedding'lerini getir.
        var documentEmbeddings =
            await _documentChunkRepository.GetByFileNameAsync(
                fileName,
                cancellationToken);

        // 3. Her parçanın benzerlik puanını hesapla.
        var results = documentEmbeddings
            .Select(embedding => new DocumentSearchResult(
                embedding.Chunk,
                CosineSimilarity.Calculate(
                    questionVector,
                    embedding.Vector)))
            .OrderByDescending(result => result.Similarity)
            .Take(topK)
            .ToList();

        return results;
    }
}