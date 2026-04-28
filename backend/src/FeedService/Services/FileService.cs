using System.IO;

namespace FeedService.Services;

public class FileService : IFileService
{
    private readonly string _uploadsPath;
    private readonly string _postsPath;
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
        _postsPath = Path.Combine(_uploadsPath, "posts");
        
        // Создаем директории, если их нет
        if (!Directory.Exists(_postsPath))
        {
            Directory.CreateDirectory(_postsPath);
        }
    }

    public async Task<string> SavePostImageAsync(IFormFile file, int postId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        // Проверяем расширение файла
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Invalid file type. Allowed: jpg, jpeg, png, gif, webp");
        }

        // Проверяем размер файла (максимум 5 МБ)
        if (file.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("File size exceeds 5 MB limit");
        }

        // Генерируем уникальное имя файла
        var fileName = $"{postId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_postsPath, fileName);

        // Сохраняем файл
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fileName;
    }

    public string GetImageUrl(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        return $"/uploads/posts/{fileName}";
    }

    public Task<bool> DeleteImageAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return Task.FromResult(false);
        }

        var filePath = Path.Combine(_postsPath, fileName);
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }
}

