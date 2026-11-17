using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentProcessingService
{
    Task<DocumentSummary> ProcessAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);
}
