
using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IDocumentService
{
    Task<string> SaveDocumentAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PdfPageText>> ExtractTextAsync(
        string fileName,
        CancellationToken cancellationToken = default);
}
