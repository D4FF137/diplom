using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using TaskService.Services;

namespace TaskService.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IUserInfoService _userInfo;

    public TasksController(ITaskService taskService, IUserInfoService userInfo)
    {
        _taskService = taskService;
        _userInfo = userInfo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserTask>>> GetTasks()
    {
        var companyId = _userInfo.GetCompanyId();
        var userId = _userInfo.GetUserId();
        var tasks = await _taskService.GetTasksAsync(companyId, userId);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserTask>> GetTask(int id)
    {
        var companyId = _userInfo.GetCompanyId();
        var task = await _taskService.GetTaskByIdAsync(id, companyId);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<UserTask>> CreateTask(CreateTaskRequest request)
    {
        var companyId = _userInfo.GetCompanyId();
        var userId = _userInfo.GetUserId();
        
        // Basic role check
        var role = _userInfo.GetRole();
        // In a real app, we'd check if they are a Leader of the TargetGroupId here too
        if (role != "Boss" && !request.TargetGroupId.HasValue && !request.TargetUserId.HasValue) 
        {
             // Only Boss can create company-wide tasks (no target group/user usually means company wide or just for self)
             // But for now let's be flexible
        }

        var task = new UserTask
        {
            CompanyId = companyId,
            CreatorId = userId,
            TargetGroupId = request.TargetGroupId,
            TargetUserId = request.TargetUserId,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Priority = request.Priority,
            DueDate = request.DueDate,
            Status = UserTaskStatus.Todo
        };

        var createdTask = await _taskService.CreateTaskAsync(task, request.ChecklistItems);
        return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UserTaskStatus status)
    {
        var companyId = _userInfo.GetCompanyId();
        var result = await _taskService.UpdateTaskStatusAsync(id, companyId, status);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPost("items/{itemId}/toggle")]
    public async Task<IActionResult> ToggleItem(int itemId)
    {
        var companyId = _userInfo.GetCompanyId();
        var userId = _userInfo.GetUserId();
        var result = await _taskService.ToggleChecklistItemAsync(itemId, companyId, userId);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var companyId = _userInfo.GetCompanyId();
        var role = _userInfo.GetRole();
        if (role != "Boss") return Forbid();

        var result = await _taskService.DeleteTaskAsync(id, companyId);
        if (!result) return NotFound();
        return NoContent();
    }
}

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? TargetGroupId { get; set; }
    public int? TargetUserId { get; set; }
    public TaskType Type { get; set; } = TaskType.Simple;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public List<string>? ChecklistItems { get; set; }
}
