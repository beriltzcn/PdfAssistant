using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IPdfTextExtractor _pdfTextExtractor;

    public DocumentService(IPdfTextExtractor pdfTextExtractor)
    {
        _pdfTextExtractor = pdfTextExtractor;
    }

    public async Task<IReadOnlyList<PdfPageText>> ExtractTextAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        return await _pdfTextExtractor.ExtractTextAsync(
            filePath,
            cancellationToken);
    }
}