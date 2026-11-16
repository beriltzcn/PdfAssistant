using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

using PdfAssistant.Infrastructure.Models;

namespace PdfAssistant.Infrastructure.Models;

public class ChatCompletionRequest
{
    public string Model { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}