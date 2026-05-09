namespace Shared.Models;

public enum TaskType
{
    Simple,
    Checklist
}

public enum UserTaskStatus
{
    Todo,
    InProgress,
    Done,
    Cancelled
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public class UserTask
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int CreatorId { get; set; }
    public int? TargetGroupId { get; set; }
    public int? TargetUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskType Type { get; set; } = TaskType.Simple;
    public UserTaskStatus Status { get; set; } = UserTaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<ChecklistItem>? ChecklistItems { get; set; }
}
