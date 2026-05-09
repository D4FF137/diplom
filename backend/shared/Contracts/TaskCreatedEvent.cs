namespace Shared.Contracts;

public class TaskCreatedEvent
{
    public int TaskId { get; set; }
    public int CompanyId { get; set; }
    public int CreatorId { get; set; }
    public int? TargetGroupId { get; set; }
    public int? TargetUserId { get; set; }
    public string Title { get; set; } = string.Empty;
}
