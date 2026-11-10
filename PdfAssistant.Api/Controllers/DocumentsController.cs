using Microsoft.AspNetCore.Mvc;
using PdfAssistant.Api.Models;

namespace PdfAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    [HttpGet]
    public IActionResult Get()
    {
        return Ok("PDF Assistant API is working!");
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
        {
            return BadRequest("Please select a a PDF file.");
        }

        if(file.Length > MaxFileSize)
        {
            return BadRequest("File size cannot exceed 10 MB.");
        }

        var extension = Path.GetExtension(file.FileName);

        if(!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only PDF files are allowed.");
        }

        await using var inputStream = file.OpenReadStream();

        byte[] header = new byte[5];
        int bytesRead = await inputStream.ReadAsync(header);

        bool isPdf = bytesRead == 5 && System.Text.Encoding.ASCII.GetString(header) == "%PDF-";

        if (!isPdf)
        {
            return BadRequest("Invalid PDF file.");
        }

        var uploadFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "App_Data",
            "UploadedFiles"
        );

        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid()}";
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
}