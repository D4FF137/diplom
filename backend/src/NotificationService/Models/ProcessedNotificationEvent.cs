namespace NotificationService.Models;

public class ProcessedNotificationEvent
{
    public int Id { get; set; }
    public string RoutingKey { get; set; } = string.Empty;
    public string EventKey { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
