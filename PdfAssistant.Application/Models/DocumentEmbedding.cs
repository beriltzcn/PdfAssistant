namespace PdfAssistant.Application.Models;

public record DocumentEmbedding(
    TextChunk Chunk,
    IReadOnlyList<float> Vector
    );

