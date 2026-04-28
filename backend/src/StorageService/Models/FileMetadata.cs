using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StorageService.Models;

public class FileMetadata
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    
    public int OwnerId { get; set; }
    public int CompanyId { get; set; }
    
    public bool IsImportant { get; set; }
    public bool IsPrivate { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
