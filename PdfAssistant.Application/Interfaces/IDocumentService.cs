using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentSummary> SaveDocumentAsync(
        Stream fileStream,
        string originalFileName,
        long sizeInBytes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentSummary>> GetDocumentsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PdfPageText>?> ExtractTextAsync(
        string fileName,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);
}
