
namespace PdfAssistant.Infrastructure.Models;

public class DocumentChunkEntity
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public int PageNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public string EmbeddingJson { get; set; } = string.Empty;
}
