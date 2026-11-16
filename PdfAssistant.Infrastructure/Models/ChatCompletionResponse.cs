using System;
using System.Collections.Generic;
using System.Text;

namespace PdfAssistant.Infrastructure.Models;

public class ChatCompletionResponse
{
    public List<ChatCompletionChoise> Choices { get; set; } = new();
}

public class ChatCompletionChoise
{
    public ChatMessage Message { get; set; }
}
