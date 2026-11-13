using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentChunkRepository
{
    Task SaveAsync(
     string fileName,
     IReadOnlyList<DocumentEmbedding> embeddings,
     CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentEmbedding>> GetByFileNameAsync(
    string fileName,
    CancellationToken cancellationToken = default);

}