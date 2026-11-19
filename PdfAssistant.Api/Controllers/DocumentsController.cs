using Microsoft.AspNetCore.Mvc;
using PdfAssistant.Api.Models;
using PdfAssistant.Application.Interfaces;
using PdfAssistant.Application.Models;

namespace PdfAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private readonly IDocumentService _documentService;
    private readonly IDocumentAnswerService _documentAnswerService;
    private readonly IDocumentProcessingService _documentProcessingService;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentAnswerService documentAnswerService,
        IDocumentProcessingService documentProcessingService)
    {
        _documentService = documentService;
        _documentAnswerService = documentAnswerService;
        _documentProcessingService = documentProcessingService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<DocumentSummary>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetDocumentsAsync(
            cancellationToken);

        return Ok(documents);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var file = request.File;

        if (file is null || file.Length == 0)
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

        var header = new byte[5];

        var bytesRead = await inputStream.ReadAsync(
            header,
            cancellationToken);

        var isPdf = bytesRead == 5
            && System.Text.Encoding.ASCII.GetString(header) == "%PDF-";

        if (!isPdf)
        {
            return BadRequest("Invalid PDF file.");
        }

        inputStream.Position = 0;

        var originalFileName = Path.GetFileName(file.FileName);

        var document = await _documentService.SaveDocumentAsync(
            inputStream,
            originalFileName,
            file.Length,
            cancellationToken);

        return Ok(new
        {
            Message = "PDF uploaded successfully!",
            Document = document
        });
    }

    [HttpPost("{fileName}/process")]
    [ProducesResponseType(
        typeof(DocumentSummary),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Process(
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = await _documentProcessingService.ProcessAsync(
                fileName,
                cancellationToken);

            return Ok(document);
        }
        catch (FileNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(
        string fileName,
        CancellationToken cancellationToken)
    {
        var deleted = await _documentService.DeleteAsync(
            fileName,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound("Document not found.");
    }

    [HttpPost("{fileName}/ask")]
    public async Task<IActionResult> Ask(
        string fileName,
        [FromBody] AskDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question cannot be empty.");
        }

        var answer = await _documentAnswerService.AskAsync(
            fileName,
            request.Question,
            cancellationToken);

        return Ok(new
        {
            FileName = fileName,
            Question = request.Question,
            Answer = answer
        });
    }
}
