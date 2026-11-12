
namespace PdfAssistant.Application.Interfaces;

public interface IDocumentStorage
{
    Task<string> SaveAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default);

    string? GetFilePath(string fileName);
}
