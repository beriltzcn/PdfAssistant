using System;
using System.Collections.Generic;
using System.Text;

namespace PdfAssistant.Application.Interfaces;

public interface IChatCompletionService
{
    Task<string> GenerateAnswerAsync(
        string question,
        string context,
        CancellationToken cancellationToken=default);
}
