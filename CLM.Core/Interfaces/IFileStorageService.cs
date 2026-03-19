namespace CLM.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
    Task<(Stream stream, string contentType)> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
