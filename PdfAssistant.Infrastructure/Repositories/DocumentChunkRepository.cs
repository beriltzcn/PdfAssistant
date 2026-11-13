using System.Text.Json;
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;
using PdfAssistant.Infrastructure.Data;
using PdfAssistant.Infrastructure.Models;

namespace PdfAssistant.Infrastructure.Repositories;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly PdfAssistantDbContext _dbContext;

    public DocumentChunkRepository(PdfAssistantDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(
        string fileName,
        IReadOnlyList<DocumentEmbedding> embeddings,
        CancellationToken cancellationToken = default)
    {
        var entities = embeddings.Select(embedding =>
            new DocumentChunkEntity
            {
                FileName = fileName,
                ChunkIndex = embedding.Chunk.ChunkIndex,
                PageNumber = embedding.Chunk.PageNumber,
                Text = embedding.Chunk.Text,
                EmbeddingJson = JsonSerializer.Serialize(embedding.Vector)
            });

        await _dbContext.DocumentChunks.AddRangeAsync(
            entities,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}