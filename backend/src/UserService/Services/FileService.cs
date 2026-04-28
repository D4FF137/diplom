using System.IO;

namespace UserService.Services;

public class FileService : IFileService
{
    private readonly string _uploadsPath;
    private readonly string _avatarsPath;
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
        _avatarsPath = Path.Combine(_uploadsPath, "avatars");
        
        // Создаем директории, если их нет
        if (!Directory.Exists(_avatarsPath))
        {
            Directory.CreateDirectory(_avatarsPath);
        }
    }

    public async Task<string> SaveAvatarAsync(IFormFile file, int userId)
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

        // Проверяем размер файла (максимум 2 МБ)
        if (file.Length > 2 * 1024 * 1024)
        {
            throw new ArgumentException("File size exceeds 2 MB limit");
        }

        // Генерируем уникальное имя файла
        var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_avatarsPath, fileName);

        // Сохраняем файл
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fileName;
    }

    public string GetAvatarUrl(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        return $"/uploads/avatars/{fileName}";
    }

    public Task<bool> DeleteAvatarAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return Task.FromResult(false);
        }

        var filePath = Path.Combine(_avatarsPath, fileName);
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

