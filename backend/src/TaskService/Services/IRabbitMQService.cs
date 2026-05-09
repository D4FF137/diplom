namespace TaskService.Services;

public interface IRabbitMQService
{
    Task PublishTaskCreatedAsync(int taskId, int companyId, int creatorId, int? targetGroupId, int? targetUserId, string title);
}
