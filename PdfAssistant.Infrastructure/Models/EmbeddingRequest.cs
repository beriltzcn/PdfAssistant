using System.Text.Json.Serialization;

namespace PdfAssistant.Infrastructure.Models;

public record EmbeddingRequest(
    [property: JsonPropertyName("model")]
    string Model,

    [property: JsonPropertyName("input")]
    IReadOnlyList<string> Input,

    [property: JsonPropertyName("encoding_format")]
    string EncodingFormat = "float");