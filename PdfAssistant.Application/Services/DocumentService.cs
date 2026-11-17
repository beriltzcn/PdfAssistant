using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;
namespace PdfAssistant.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IDocumentStorage _documentStorage;
    private readonly IDocumentRepository _documentRepository;

    public DocumentService(
        IPdfTextExtractor pdfTextExtractor,
        IDocumentStorage documentStorage,
        IDocumentRepository documentRepository)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _documentStorage = documentStorage;
        _documentRepository = documentRepository;
    }

    public async Task<DocumentSummary> SaveDocumentAsync(
        Stream fileStream,
        string originalFileName,
        long sizeInBytes,
        CancellationToken cancellationToken = default)
    {
        var storedFileName = await _documentStorage.SaveAsync(
            fileStream,
            cancellationToken);

        var pageCount = 0;
        var status = DocumentStatus.Uploaded;

        try
        {
            var pages = await ExtractTextAsync(
                storedFileName,
                cancellationToken);

            if (pages is not null)
            {
                pageCount = pages.Count;
                status = DocumentStatus.Extracted;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            status = DocumentStatus.Failed;
        }

        return await _documentRepository.AddAsync(
            storedFileName,
            originalFileName,
            sizeInBytes,
            pageCount,
            status,
            cancellationToken);
    }

    public Task<IReadOnlyList<DocumentSummary>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        return _documentRepository.GetAllAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PdfPageText>?> ExtractTextAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var filePath = _documentStorage.GetFilePath(fileName);

        if (filePath is null)
        {
            return null;
        }

        return await _pdfTextExtractor.ExtractTextAsync(
            filePath,
            cancellationToken);
    }
}
