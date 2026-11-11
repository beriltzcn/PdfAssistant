using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentService
{
    Task<IReadOnlyList<PdfPageText>> ExtractTextAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}