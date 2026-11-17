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
    private readonly IDocumentEmbeddingService _documentEmbeddingService;
    private readonly IDocumentSearchService _documentSearchService;
    private readonly IDocumentAnswerService _documentAnswerService;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly IDocumentDeletionService _documentDeletionService;

    public DocumentsController(
        IDocumentService documentService,
        IDocumentEmbeddingService documentEmbeddingService,
        IDocumentSearchService documentSearchService,
        IDocumentAnswerService documentAnswerService,
        IDocumentProcessingService documentProcessingService,
        IDocumentDeletionService documentDeletionService)
    {
        _documentService = documentService;
        _documentEmbeddingService = documentEmbeddingService;
        _documentSearchService = documentSearchService;
        _documentAnswerService = documentAnswerService;
        _documentProcessingService = documentProcessingService;
        _documentDeletionService = documentDeletionService;
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

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Message = "PDF Assistant API is working!"
        });
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

    [HttpGet("{fileName}/text")]
    public async Task<IActionResult> GetPdfText(
        string fileName,
        CancellationToken cancellationToken)
    {
        var pages = await _documentService.ExtractTextAsync(
            fileName,
            cancellationToken);

        if (pages is null)
        {
            return NotFound("PDF file not found.");
        }

        return Ok(new
        {
            FileName = fileName,
            PageCount = pages.Count,
            Pages = pages
        });
    }

    [HttpPost("{fileName}/embeddings")]
    public async Task<IActionResult> GenerateEmbeddings(
        string fileName,
        CancellationToken cancellationToken)
    {
        var embeddings = await _documentEmbeddingService.GenerateAsync(
            fileName,
            cancellationToken);

        return Ok(new
        {
            FileName = fileName,
            ChunkCount = embeddings.Count,
            EmbeddingDimensions =
                embeddings.FirstOrDefault()?.Vector.Count ?? 0
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
        var deleted = await _documentDeletionService.DeleteAsync(
            fileName,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound("Document not found.");
    }

    [HttpPost("{fileName}/search")]
    public async Task<IActionResult> Search(
        string fileName,
        [FromBody] string question,
        CancellationToken cancellationToken)
    {
        var results = await _documentSearchService.SearchAsync(
            fileName,
            question,
            topK: 5,
            cancellationToken);

        return Ok(results);
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
