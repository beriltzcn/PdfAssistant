using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentEmbeddingService _documentEmbeddingService;
    private readonly IDocumentRepository _documentRepository;

    public DocumentProcessingService(
        IDocumentService documentService,
        IDocumentEmbeddingService documentEmbeddingService,
        IDocumentRepository documentRepository)
    {
        _documentService = documentService;
        _documentEmbeddingService = documentEmbeddingService;
        _documentRepository = documentRepository;
    }

    public async Task<DocumentSummary> ProcessAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        var processingDocument = await _documentRepository.SetStatusAsync(
            storedFileName,
            DocumentStatus.Processing,
            pageCount: null,
            cancellationToken);

        if (processingDocument is null)
        {
            throw new FileNotFoundException(
                $"Document not found: {storedFileName}");
        }

        try
        {
            var pages = await _documentService.ExtractTextAsync(
                storedFileName,
                cancellationToken);

            if (pages is null)
            {
                throw new FileNotFoundException(
                    $"PDF file not found: {storedFileName}");
            }

            var embeddings = await _documentEmbeddingService.GenerateAsync(
                storedFileName,
                cancellationToken);

            var readyDocument = await _documentRepository.SetStatusAsync(
                storedFileName,
                DocumentStatus.Ready,
                pages.Count,
                cancellationToken);

            return readyDocument
                ?? throw new InvalidOperationException(
                    "Document metadata could not be updated.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await _documentRepository.SetStatusAsync(
                storedFileName,
                DocumentStatus.Failed,
                pageCount: null,
                cancellationToken);

            throw;
        }
    }
}
