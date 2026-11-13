using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentSearchService
{
    Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        string fileName,
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default);
}