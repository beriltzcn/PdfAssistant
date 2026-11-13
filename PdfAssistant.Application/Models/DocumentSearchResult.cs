namespace PdfAssistant.Application.Models;

public record DocumentSearchResult(
    TextChunk Chunk,
    double Similarity);