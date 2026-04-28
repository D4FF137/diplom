namespace Shared.Models;

public class Message
{
    public string Id { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string Type { get; set; } = "text"; // "text", "poll"
    public PollData? Poll { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEdited { get; set; } = false;
    public List<MessageReaction> Reactions { get; set; } = new();
}

public class MessageReaction
{
    public string Emoji { get; set; } = string.Empty;
    public List<int> UserIds { get; set; } = new(); // User IDs who reacted with this emoji
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
    public List<int> VoterIds { get; set; } = new(); // List of user IDs who voted for this option
    public int VoteCount => VoterIds.Count;
}
