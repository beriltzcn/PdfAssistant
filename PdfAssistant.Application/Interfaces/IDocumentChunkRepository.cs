using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentChunkRepository
{
    Task SaveAsync(
     string fileName,
     IReadOnlyList<DocumentEmbedding> embeddings,
     CancellationToken cancellationToken = default);
}