namespace StorageService.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file);
    Task DeleteFileAsync(string relativePath);
    string GetFileUrl(string relativePath);
}
