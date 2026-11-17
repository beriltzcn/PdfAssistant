using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentRepository
{
    Task<DocumentSummary> AddAsync(
        string storedFileName,
        string originalFileName,
        long sizeInBytes,
        int pageCount,
        string status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentSummary>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<DocumentSummary?> SetStatusAsync(
        string storedFileName,
        string status,
        int? pageCount,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);
}
