using System;
using System.Collections.Generic;
using System.Text;

namespace PdfAssistant.Infrastructure.Models;

public class PdfDocumentEntity
{
    public int Id { get; set; }

    public string StoredFileName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public int PageCount { get; set; }

    public string Status { get; set; } = "uploaded";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
