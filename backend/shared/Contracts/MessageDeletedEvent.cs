namespace Shared.Contracts;

public class MessageDeletedEvent
{
    public string MessageId { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public int CompanyId { get; set; }
}
