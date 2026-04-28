namespace Shared.Contracts;

public class MessageUpdatedEvent
{
    public string MessageId { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public DateTime UpdatedAt { get; set; }
}
