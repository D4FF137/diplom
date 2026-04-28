using System.IO;

namespace ChatService.Services;

public class FileService : IFileService
{
    private readonly string _uploadsPath;
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
        _uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", "chat_attachments");
        
        // Создаем директории, если их нет
        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        // Проверяем расширение файла (разрешаем картинки и документы)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".txt", ".zip", ".rar" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        // В телеграме можно кидать что угодно, но пока ограничим разумным списком
        // if (!allowedExtensions.Contains(extension))
        // {
        //     throw new ArgumentException("Invalid file type.");
        // }

        // Проверяем размер файла (максимум 20 МБ)
        if (file.Length > 20 * 1024 * 1024)
        {
            throw new ArgumentException("File size exceeds 20 MB limit");
        }

        // Генерируем уникальное имя файла
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadsPath, fileName);

        // Сохраняем файл
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return fileName;
    }

    public string GetFileUrl(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return string.Empty;
        }

        return $"/uploads/chat_attachments/{fileName}";
    }

    public Task<bool> DeleteFileAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return Task.FromResult(false);
        }

        var filePath = Path.Combine(_uploadsPath, fileName);
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
