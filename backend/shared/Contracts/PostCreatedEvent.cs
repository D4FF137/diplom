namespace Shared.Contracts;

public class PostCreatedEvent
{
    public int PostId { get; set; }
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}


