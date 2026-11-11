
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IDocumentStorage _documentStorage;

    public DocumentService(
        IPdfTextExtractor pdfTextExtractor,
        IDocumentStorage documentStorage)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _documentStorage = documentStorage;
    }

    public async Task<string> SaveDocumentAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        return await _documentStorage.SaveAsync(
            fileStream,
            cancellationToken);
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
