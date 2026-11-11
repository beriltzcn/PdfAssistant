
using Microsoft.AspNetCore.Mvc;
using PdfAssistant.Api.Models;
using PdfAssistant.Application.Interfaces;

namespace PdfAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok("PDF Assistant API is working!");
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentRequest request)
    {
        var file = request.File;

        if (file == null || file.Length == 0)
        {
            return BadRequest("Please select a PDF file.");
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest("File size cannot exceed 10 MB.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(
            extension,
            ".pdf",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only PDF files are allowed.");
        }

        await using var inputStream = file.OpenReadStream();

        byte[] header = new byte[5];
        int bytesRead = await inputStream.ReadAsync(header);

        bool isPdf = bytesRead == 5 &&
                     System.Text.Encoding.ASCII.GetString(header) == "%PDF-";

        if (!isPdf)
        {
            return BadRequest("Invalid PDF file.");
        }

        var uploadFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "App_Data",
            "UploadedFiles");

        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid()}.pdf";
        var filePath = Path.Combine(uploadFolder, fileName);

        inputStream.Position = 0;

        await using (var outputStream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write))
        {
            await inputStream.CopyToAsync(outputStream);
        }

        return Ok(new
        {
            Message = "PDF uploaded successfully!",
            FileName = fileName,
            OriginalFileName = Path.GetFileName(file.FileName),
            SizeInBytes = file.Length
        });
    }

    [HttpGet("{fileName}/text")]
    public async Task<IActionResult> GetPdfText(
        string fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(
                fileName,
                Path.GetFileName(fileName),
                StringComparison.Ordinal))
        {
            return BadRequest("Invalid file name.");
        }

        var uploadFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "App_Data",
            "UploadedFiles");

        var filePath = Path.Combine(uploadFolder, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("PDF file not found.");
        }

        var pages = await _documentService.ExtractTextAsync(
            filePath,
            cancellationToken);

        return Ok(new
        {
            FileName = fileName,
            PageCount = pages.Count,
            Pages = pages
        });
    }
}
