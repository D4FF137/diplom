namespace FeedService.Services;

public interface IFileService
{
    Task<string> SavePostImageAsync(IFormFile file, int postId);
    string GetImageUrl(string fileName);
    Task<bool> DeleteImageAsync(string fileName);
}

