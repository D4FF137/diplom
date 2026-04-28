namespace UserService.Services;

public interface IFileService
{
    Task<string> SaveAvatarAsync(IFormFile file, int userId);
    string GetAvatarUrl(string fileName);
    Task<bool> DeleteAvatarAsync(string fileName);
}

