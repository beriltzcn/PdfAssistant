
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;
using UglyToad.PdfPig;

namespace PdfAssistant.Infrastructure.Pdf;

public class PdfTextExtractor : IPdfTextExtractor
{
    public Task<IReadOnlyList<PdfPageText>> ExtractTextAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var document = PdfDocument.Open(filePath);

        IReadOnlyList<PdfPageText> pages = document.GetPages()
            .Select(page => new PdfPageText(
                page.Number,
                page.Text))
            .ToList();

        return Task.FromResult(pages);
    }
}
