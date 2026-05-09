namespace Shared.Models;

public class CompanyGroup
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LeaderUserId { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CompanyGroupMember
{
    public int Id { get; set; }
    public int CompanyGroupId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
