namespace PdfAssistant.Infrastructure.Configuration;

public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } =
        "nvidia/nemotron-3.5-embed-1b:free";
}