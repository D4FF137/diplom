using MongoDB.Driver;
using StorageService.Models;

namespace StorageService.Repositories;

public class StorageRepository : IStorageRepository
{
    private readonly IMongoCollection<FileMetadata> _files;

    public StorageRepository(IMongoDatabase database)
    {
        _files = database.GetCollection<FileMetadata>("files");
    }

    public async Task<FileMetadata?> GetByIdAsync(string id)
    {
        return await _files.Find(f => f.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<FileMetadata>> GetFilesAsync(int companyId, int? userId = null, string? category = null, string? type = null, string? search = null, int skip = 0, int take = 50)
    {
        var filterBuilder = Builders<FileMetadata>.Filter;
        var filter = filterBuilder.Eq(f => f.CompanyId, companyId);

        // Category logic
        if (category == "private" && userId.HasValue)
        {
            filter &= filterBuilder.Eq(f => f.OwnerId, userId.Value) & filterBuilder.Eq(f => f.IsPrivate, true);
        }
        else if (category == "important")
        {
            filter &= filterBuilder.Eq(f => f.IsImportant, true);
        }
        else if (category == "shared")
        {
            filter &= filterBuilder.Eq(f => f.IsPrivate, false);
        }

        // Type filtering
        if (!string.IsNullOrEmpty(type))
        {
            if (type == "image")
                filter &= filterBuilder.Regex(f => f.ContentType, new MongoDB.Bson.BsonRegularExpression("^image/", "i"));
            else if (type == "document")
                filter &= filterBuilder.Regex(f => f.ContentType, new MongoDB.Bson.BsonRegularExpression("pdf|doc|txt|xls", "i"));
        }

        // Search
        if (!string.IsNullOrEmpty(search))
        {
            filter &= filterBuilder.Regex(f => f.FileName, new MongoDB.Bson.BsonRegularExpression(search, "i"));
        }

        return await _files.Find(filter)
            .SortByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();
    }

    public async Task CreateAsync(FileMetadata metadata)
    {
        await _files.InsertOneAsync(metadata);
    }

    public async Task UpdateAsync(FileMetadata metadata)
    {
        await _files.ReplaceOneAsync(f => f.Id == metadata.Id, metadata);
    }

    public async Task DeleteAsync(string id)
    {
        await _files.DeleteOneAsync(f => f.Id == id);
    }
}
