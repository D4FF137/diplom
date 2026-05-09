namespace Shared.Models;

public class Chat
{
    public string Id { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "group"; // "private", "group", or "department"
    public int CreatorId { get; set; }
    public int? CompanyGroupId { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatMember
{
    public string Id { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

