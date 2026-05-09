namespace NotificationService.Models;

public class UnreadTask
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int UserId { get; set; }
    public int Count { get; set; }
    public DateTime? LastReadAt { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
