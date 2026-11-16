namespace PdfAssistant.Application.Interfaces;

public interface IDocumentAnswerService
{
    Task<string> AskAsync(
        string fileName,
        string question,
        CancellationToken cancellationToken = default);
}