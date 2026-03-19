using CLM.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace CLM.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _uploadPath = Path.Combine(env.ContentRootPath, "uploads");
        _logger = logger;
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        var unique = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(_uploadPath, unique);

        await using var fs = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(fs);

        _logger.LogInformation("File saved: {FileName}", unique);
        return unique;
    }

    public Task<(Stream stream, string contentType)> GetFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_uploadPath, filePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", filePath);

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var mime = ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult((stream, mime));
    }

    public Task DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_uploadPath, filePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
