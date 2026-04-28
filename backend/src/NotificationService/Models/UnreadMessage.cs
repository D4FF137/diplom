namespace NotificationService.Models;

public class UnreadMessage
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string ChatId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int Count { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}




