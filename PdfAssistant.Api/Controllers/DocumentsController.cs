
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

    private readonly IDocumentEmbeddingService _documentEmbeddingService;
    private readonly IDocumentSearchService _documentSearchService;

    public DocumentsController(
    IDocumentService documentService,
    IDocumentEmbeddingService documentEmbeddingService,
    IDocumentSearchService documentSearchService)
    {
        _documentService = documentService;
        _documentEmbeddingService = documentEmbeddingService;
        _documentSearchService = documentSearchService;
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
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
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

        int bytesRead = await inputStream.ReadAsync(
            header,
            cancellationToken);

        bool isPdf = bytesRead == 5 &&
            System.Text.Encoding.ASCII.GetString(header) == "%PDF-";

        if (!isPdf)
        {
            return BadRequest("Invalid PDF file.");
        }

        inputStream.Position = 0;

        var fileName = await _documentService.SaveDocumentAsync(
            inputStream,
            cancellationToken);

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
            EmbeddingDimensions = embeddings.FirstOrDefault()?.Vector.Count ?? 0
        });
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

}
