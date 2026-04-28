using System.IO;

namespace ChatService.Services;

public interface IFileService
{
    Task<string> SaveFileAsync(IFormFile file);
    string GetFileUrl(string fileName);
    Task<bool> DeleteFileAsync(string fileName);
}
