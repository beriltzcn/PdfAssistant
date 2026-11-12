using PdfAssistant.Application.Models;
using PdfAssistant.Application.Services;

namespace PdfAssistant.Tests;

public class TextChunkerTests
{
    [Fact]
    public void Chunk_TextShorterThanChunkSize_ReturnSingleChunk()
    {
        var chunker = new TextChunker();

        var pages = new List<PdfPageText>
        {
            new(1, "Bu kısa bir test metnidir.")
        };
        var result = chunker.Chunk(pages);

        Assert.Single(result);
        Assert.Equal(1, result[0].PageNumber);
        Assert.Equal("Bu kısa bir test metnidir.", result[0].Text);
    }

    [Fact]
    public void Chunk_LongText_ReturnsMultipleChunks()
    {
        // Arrange
        var chunker = new TextChunker();

        var longText = new string('A', 2500);

        var pages = new List<PdfPageText>
    {
        new(1, longText)
    };

        // Act
        var result = chunker.Chunk(pages);

        // Assert
        Assert.True(result.Count > 1);
    }

    [Fact]
    public void Chunk_LongText_ChunksHaveExpectedOverlap()
    {
        // Arrange
        var chunker = new TextChunker();

        var longText = new string(
            Enumerable.Range(0, 2500)
                .Select(i => (char)(0x1000 + i))
                .ToArray());

        var pages = new List<PdfPageText>
    {
        new(1, longText)
    };

        // Act
        var result = chunker.Chunk(pages);

        // Assert
        Assert.Equal(1000, result[0].Text.Length);

        Assert.Equal(
            longText[850..1850],
            result[1].Text);

        Assert.Equal(
            result[0].Text[^150..],
            result[1].Text[..150]);
    }

    [Fact]
    public void Chunk_EmptyPage_ReturnsNoChunks()
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<PdfPageText>
    {
        new(1, ""),
        new(2, "   ")
    };

        // Act
        var result = chunker.Chunk(pages);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_InvalidChunkSize_ThrowsException()
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<PdfPageText>
    {
        new(1, "Test metni")
    };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            chunker.Chunk(pages, chunkSize: 0));
    }

    [Fact]
    public void Chunk_InvalidOverlap_ThrowsException()
    {
        // Arrange
        var chunker = new TextChunker();

        var pages = new List<PdfPageText>
    {
        new(1, "Test metni")
    };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            chunker.Chunk(pages, chunkSize: 1000, overlap: 1000));
    }
}
