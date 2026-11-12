using System.Text.Json.Serialization;

namespace PdfAssistant.Infrastructure.Models;

public class EmbeddingResponse
{
    [JsonPropertyName("data")]
    public List<EmbeddingData> Data { get; set; } = [];
}

public class EmbeddingData
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = [];
}