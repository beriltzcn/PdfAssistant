using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentEmbeddingService
{
    Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
        string fileName,
        CancellationToken cancellationToken = default
        );
}
