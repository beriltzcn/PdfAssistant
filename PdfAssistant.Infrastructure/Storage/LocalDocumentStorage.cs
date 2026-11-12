
using PdfAssistant.Application.Interfaces;

namespace PdfAssistant.Infrastructure.Storage;

public class LocalDocumentStorage : IDocumentStorage
{
    private readonly string _uploadFolder;

    public LocalDocumentStorage(string uploadFolder)
    {
        _uploadFolder = uploadFolder;
    }

    public async Task<string> SaveAsync(
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_uploadFolder);

        var fileName = $"{Guid.NewGuid()}.pdf";
        var filePath = Path.Combine(_uploadFolder, fileName);

        await using var outputStream = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await fileStream.CopyToAsync(
            outputStream,
            cancellationToken);

        return fileName;
    }

    public string? GetFilePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(
                fileName,
                Path.GetFileName(fileName),
                StringComparison.Ordinal))
        {
            return null;
        }
        var filePath = Path.Combine(_uploadFolder, fileName);

        return File.Exists(filePath) ? filePath : null;
    }
}
