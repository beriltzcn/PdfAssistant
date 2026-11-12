using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Interfaces;

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(
        IReadOnlyList<PdfPageText> pages,
        int chunkSize = 1000,
        int overlap = 150);
}
