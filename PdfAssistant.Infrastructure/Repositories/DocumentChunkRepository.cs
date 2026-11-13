using System.Text.Json;
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;
using PdfAssistant.Infrastructure.Data;
using PdfAssistant.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    public async Task<IReadOnlyList<DocumentEmbedding>> GetByFileNameAsync(
    string fileName,
    CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.DocumentChunks
            .Where(x => x.FileName == fileName)
            .ToListAsync(cancellationToken);

        var embeddings = entities
            .Select(entity => new DocumentEmbedding(
                new TextChunk(
                    entity.ChunkIndex,
                    entity.PageNumber,
                    entity.Text),
                JsonSerializer.Deserialize<List<float>>(
                    entity.EmbeddingJson) ?? new List<float>()))
            .ToList();

        return embeddings;
    }
}