using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatService.Models;

public class Chat
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "group"; // "private", "group", or "department"
    public int? CompanyGroupId { get; set; }
    public bool IsSystem { get; set; }
    
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CreatorId { get; set; }
    
    public List<int> MemberIds { get; set; } = new();
    
    public Message? LastMessage { get; set; }
}
