using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;

namespace PdfAssistant.Application.Services;

public class TextChunker : ITextChunker
{
    public IReadOnlyList<TextChunk> Chunk(
        IReadOnlyList<PdfPageText> pages,
        int chunkSize = 1000,
        int overlap = 150)
    {
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (overlap < 0 || overlap >= chunkSize)
            throw new ArgumentOutOfRangeException(nameof(overlap));

        var chunks = new List<TextChunk>();

        foreach (var page in pages)
        {
            var text = page.Text;

            if (string.IsNullOrWhiteSpace(text))
                continue;

            int start = 0;
            int chunkIndex = 1;

            while (start < text.Length)
            {
                int end = Math.Min(start + chunkSize, text.Length);

                if(end < text.Length)
                {
                    int lastSpace = text.LastIndexOf(
                        ' ',
                        end - 1,
                        end - start);

                    if (lastSpace > start + chunkSize * 0.7)
                    {
                        end = lastSpace;
                    }
                }
                var chunkText = text[start..end].Trim();

                if (!string.IsNullOrWhiteSpace(chunkText))
                {
                    chunks.Add(new TextChunk(
                        chunkIndex,
                        page.PageNumber,
                        chunkText));
                }

                if (end >= text.Length)
                    break;

                start = Math.Max(end - overlap, start + 1);
                chunkIndex++;
            }
        }

        return chunks;
    }
}
