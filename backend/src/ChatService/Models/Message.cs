using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ChatService.Models;

public class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public int CompanyId { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string ChatId { get; set; } = string.Empty;
    
    public int SenderId { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    public string? AttachmentUrl { get; set; }
    
    public string Type { get; set; } = "text"; // "text", "poll"

    public PollData? Poll { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;
    
    public bool IsEdited { get; set; } = false;
    
    public List<string> ReadBy { get; set; } = new(); // User IDs who read this
    
    public List<MessageReaction> Reactions { get; set; } = new();
}

public class MessageReaction
{
    public string Emoji { get; set; } = string.Empty;
    public List<int> UserIds { get; set; } = new();
}

public class PollData
{
    public string Question { get; set; } = string.Empty;
    public List<PollOption> Options { get; set; } = new();
    public bool IsAnonymous { get; set; } = false;
    public bool IsMultipleChoice { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
}

public class PollOption
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<int> VoterIds { get; set; } = new();
}
