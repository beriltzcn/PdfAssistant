namespace PdfAssistant.Application.Models;

public record DocumentSummary(
    int Id,
    string StoredFileName,
    string OriginalFileName,
    long SizeInBytes,
    int PageCount,
    string Status,
    DateTime CreatedAtUtc);
