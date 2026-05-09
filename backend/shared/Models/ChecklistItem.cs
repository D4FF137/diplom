namespace Shared.Models;

public class ChecklistItem
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int? CompletedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
}
