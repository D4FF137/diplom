using Shared.Models;

namespace TaskService.Services;

public interface ITaskService
{
    Task<IEnumerable<UserTask>> GetTasksAsync(int companyId, int userId);
    Task<UserTask?> GetTaskByIdAsync(int id, int companyId);
    Task<UserTask> CreateTaskAsync(UserTask task, List<string>? checklistItems);
    Task<bool> UpdateTaskStatusAsync(int id, int companyId, UserTaskStatus status);
    Task<bool> ToggleChecklistItemAsync(int itemId, int companyId, int userId);
    Task<bool> DeleteTaskAsync(int id, int companyId);
}
