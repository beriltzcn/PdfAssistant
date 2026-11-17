using Microsoft.EntityFrameworkCore;
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;
using PdfAssistant.Infrastructure.Data;
using PdfAssistant.Infrastructure.Models;

namespace PdfAssistant.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly PdfAssistantDbContext _dbContext;

    public DocumentRepository(PdfAssistantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentSummary> AddAsync(
        string storedFileName,
        string originalFileName,
        long sizeInBytes,
        int pageCount,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = new PdfDocumentEntity
        {
            StoredFileName = storedFileName,
            OriginalFileName = originalFileName,
            SizeInBytes = sizeInBytes,
            PageCount = pageCount,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Documents.Add(entity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<IReadOnlyList<DocumentSummary>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DocumentSummary(
                x.Id,
                x.StoredFileName,
                x.OriginalFileName,
                x.SizeInBytes,
                x.PageCount,
                x.Status,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentSummary?> SetStatusAsync(
        string storedFileName,
        string status,
        int? pageCount,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Documents
            .SingleOrDefaultAsync(
                x => x.StoredFileName == storedFileName,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = status;

        if (pageCount.HasValue)
        {
            entity.PageCount = pageCount.Value;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<bool> DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Documents
            .SingleOrDefaultAsync(
                x => x.StoredFileName == storedFileName,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Documents.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static DocumentSummary Map(PdfDocumentEntity entity)
    {
        return new DocumentSummary(
            entity.Id,
            entity.StoredFileName,
            entity.OriginalFileName,
            entity.SizeInBytes,
            entity.PageCount,
            entity.Status,
            entity.CreatedAtUtc);
    }
}
