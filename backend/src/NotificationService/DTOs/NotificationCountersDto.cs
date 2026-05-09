namespace NotificationService.DTOs;

public class NotificationCountersDto
{
    public Dictionary<string, int> ChatUnread { get; set; } = new();
    public int FeedUnread { get; set; }
    public int TasksUnread { get; set; }
}




