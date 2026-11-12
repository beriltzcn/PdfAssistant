namespace PdfAssistant.Application.Models
{
    public record TextChunk(
        int ChunkIndex,
        int PageNumber,
        string Text);
}
