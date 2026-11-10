using Microsoft.AspNetCore.Http;

namespace PdfAssistant.Api.Models;

public class UploadDocumentRequest
{
    public IFormFile File { get; set; } = default!;
}