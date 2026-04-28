using StorageService.Models;

namespace StorageService.Repositories;

public interface IStorageRepository
{
    Task<FileMetadata?> GetByIdAsync(string id);
    Task<List<FileMetadata>> GetFilesAsync(int companyId, int? userId = null, string? category = null, string? type = null, string? search = null, int skip = 0, int take = 50);
    Task CreateAsync(FileMetadata metadata);
    Task UpdateAsync(FileMetadata metadata);
    Task DeleteAsync(string id);
}
