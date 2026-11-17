using PdfAssistant.Application.Interfaces;

namespace PdfAssistant.Application.Services;

public class DocumentDeletionService : IDocumentDeletionService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentChunkRepository _documentChunkRepository;
    private readonly IDocumentStorage _documentStorage;

    public DocumentDeletionService(
        IDocumentRepository documentRepository,
        IDocumentChunkRepository documentChunkRepository,
        IDocumentStorage documentStorage)
    {
        _documentRepository = documentRepository;
        _documentChunkRepository = documentChunkRepository;
        _documentStorage = documentStorage;
    }

    public async Task<bool> DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        await _documentChunkRepository.DeleteByFileNameAsync(
            storedFileName,
            cancellationToken);

        var deleted = await _documentRepository.DeleteAsync(
            storedFileName,
            cancellationToken);

        if (deleted)
        {
            _documentStorage.Delete(storedFileName);
        }

        return deleted;
    }
}
