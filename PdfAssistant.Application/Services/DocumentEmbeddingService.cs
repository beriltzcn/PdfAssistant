
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Services;

public class DocumentEmbeddingService : IDocumentEmbeddingService
{
    private readonly IDocumentService _documentService;
    private readonly ITextChunker _textChunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDocumentChunkRepository _documentChunkRepository;

    public DocumentEmbeddingService(
    IDocumentService documentService,
    ITextChunker textChunker,
    IEmbeddingService embeddingService,
    IDocumentChunkRepository documentChunkRepository)
    {
        _documentService = documentService;
        _textChunker = textChunker;
        _embeddingService = embeddingService;
        _documentChunkRepository = documentChunkRepository;
    }

    public async Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var pages = await _documentService.ExtractTextAsync(
            fileName,
            cancellationToken);

        if (pages is null)
        {
            throw new FileNotFoundException(
                $"PDF file not found: {fileName}");
        }

        var chunks = _textChunker.Chunk(pages);

        var texts = chunks
            .Select(chunk => chunk.Text)
            .ToList();

        var vectors = await _embeddingService.GenerateEmbeddingsAsync(
            texts,
            cancellationToken);

        if (vectors.Count != chunks.Count)
        {
            throw new InvalidOperationException(
                "Embedding count does not match chunk count.");
        }

        var documentEmbeddings = chunks
     .Select((chunk, index) => new DocumentEmbedding(
         chunk,
         vectors[index]))
     .ToList();

        await _documentChunkRepository.SaveAsync(
            fileName,
            documentEmbeddings,
            cancellationToken);

        return documentEmbeddings;
    }
}
