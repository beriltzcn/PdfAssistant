using System;
using System.Collections.Generic;
using System.Text;

namespace PdfAssistant.Infrastructure.Models;

public class ChatCompletionResponse
{
    public List<ChatCompletionChoice> Choices { get; set; } = [];
}

public class ChatCompletionChoice
{
    public ChatMessage Message { get; set; } = new();
}
