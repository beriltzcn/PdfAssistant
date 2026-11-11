using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface IPdfTextExtractor
{
    Task<IReadOnlyList<PdfPageText>> ExtractTextAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

public record PdfPAgeText(int PageNumber, string Text);
