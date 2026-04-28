namespace Shared.Contracts;

public class MessageSentEvent
{
    public string MessageId { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}


