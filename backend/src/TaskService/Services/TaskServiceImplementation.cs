using Microsoft.EntityFrameworkCore;
using Shared.Models;
using TaskService.Data;

namespace TaskService.Services;

public class TaskServiceImplementation : ITaskService
{
    private readonly TaskDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;

    public TaskServiceImplementation(TaskDbContext context, IRabbitMQService rabbitMQService)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
    }

    public async Task<IEnumerable<UserTask>> GetTasksAsync(int companyId, int userId)
    {
        return await _context.Tasks
            .Include(t => t.ChecklistItems)
            .Where(t => t.CompanyId == companyId && 
                       (t.CreatorId == userId || t.TargetUserId == userId || t.TargetGroupId != null))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserTask?> GetTaskByIdAsync(int id, int companyId)
    {
        return await _context.Tasks
            .Include(t => t.ChecklistItems)
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);
    }

    public async Task<UserTask> CreateTaskAsync(UserTask task, List<string>? checklistItems)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        if (checklistItems != null && checklistItems.Any())
        {
            var items = checklistItems.Select(content => new ChecklistItem
            {
                TaskId = task.Id,
                Content = content,
                IsCompleted = false
            });
            _context.ChecklistItems.AddRange(items);
            await _context.SaveChangesAsync();
        }

        await _rabbitMQService.PublishTaskCreatedAsync(
            task.Id, 
            task.CompanyId, 
            task.CreatorId, 
            task.TargetGroupId, 
            task.TargetUserId, 
            task.Title);

        return task;
    }

    public async Task<bool> UpdateTaskStatusAsync(int id, int companyId, UserTaskStatus status)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);
        if (task == null) return false;

        task.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleChecklistItemAsync(int itemId, int companyId, int userId)
    {
        var item = await _context.ChecklistItems
            .FirstOrDefaultAsync(i => i.Id == itemId);
            
        if (item == null) return false;
        
        var task = await _context.Tasks.FindAsync(item.TaskId);
        if (task == null || task.CompanyId != companyId) return false;

        item.IsCompleted = !item.IsCompleted;
        if (item.IsCompleted)
        {
            item.CompletedByUserId = userId;
            item.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            item.CompletedByUserId = null;
            item.CompletedAt = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTaskAsync(int id, int companyId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }
}
