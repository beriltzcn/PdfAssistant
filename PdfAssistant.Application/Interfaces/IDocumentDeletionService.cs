namespace PdfAssistant.Application.Interfaces;

public interface IDocumentDeletionService
{
    Task<bool> DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);
}
