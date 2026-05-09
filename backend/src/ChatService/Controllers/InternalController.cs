using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Services;
using System.Linq;
using Shared.Models;

namespace ChatService.Controllers;

[ApiController]
[Route("api/internal")]
[AllowAnonymous]
public class InternalController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IChatMemberService _chatMemberService;

    public InternalController(
        IChatService chatService,
        IChatMemberService chatMemberService)
    {
        _chatService = chatService;
        _chatMemberService = chatMemberService;
    }

    [HttpGet("chats/{id}")]
    public async Task<ActionResult<object>> GetChat(string id)
    {
        var chat = await _chatService.GetByIdInternalAsync(id);
        if (chat == null)
        {
            return NotFound();
        }

        var members = await _chatMemberService.GetByChatIdAsync(chat.Id, chat.CompanyId);
        var memberUserIds = members.Select(m => m.UserId).ToList();

        return Ok(new
        {
            Id = chat.Id,
            Name = chat.Name,
            Type = chat.Type,
            CompanyId = chat.CompanyId,
            Members = memberUserIds.Select(uid => new { Id = uid }).ToList()
        });
    }

    [HttpPost("department-chats")]
    public async Task<ActionResult<object>> CreateDepartmentChat([FromBody] CreateDepartmentChatRequest request)
    {
        if (request == null ||
            request.CompanyId <= 0 ||
            request.CompanyGroupId <= 0 ||
            request.CreatorId <= 0 ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "CompanyId, CompanyGroupId, CreatorId and Name are required" });
        }

        var chat = new Chat
        {
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            Type = "department",
            CreatorId = request.CreatorId,
            CompanyGroupId = request.CompanyGroupId,
            IsSystem = true
        };

        var created = await _chatService.CreateAsync(chat);
        foreach (var userId in request.UserIds.Distinct())
        {
            await _chatMemberService.AddMemberAsync(created.Id, userId, request.CompanyId);
        }

        return StatusCode(201, new
        {
            created.Id,
            created.CompanyId,
            created.Name,
            created.Type,
            created.CompanyGroupId,
            created.IsSystem
        });
    }

    [HttpPost("chats/{chatId}/members")]
    public async Task<IActionResult> AddChatMember(string chatId, [FromBody] SyncChatMemberRequest request)
    {
        if (request == null || request.CompanyId <= 0 || request.UserId <= 0)
            return BadRequest(new { message = "CompanyId and UserId are required" });

        var chat = await _chatService.GetByIdInternalAsync(chatId);
        if (chat == null || chat.CompanyId != request.CompanyId)
            return NotFound();

        await _chatMemberService.AddMemberAsync(chatId, request.UserId, request.CompanyId);
        return NoContent();
    }

    [HttpDelete("chats/{chatId}/members/{userId:int}")]
    public async Task<IActionResult> RemoveChatMember(string chatId, int userId, [FromQuery] int companyId)
    {
        if (companyId <= 0 || userId <= 0)
            return BadRequest(new { message = "CompanyId and UserId are required" });

        var chat = await _chatService.GetByIdInternalAsync(chatId);
        if (chat == null || chat.CompanyId != companyId)
            return NotFound();

        await _chatMemberService.RemoveMemberAsync(chatId, userId, companyId);
        return NoContent();
    }
}

public class CreateDepartmentChatRequest
{
    public int CompanyId { get; set; }
    public int CompanyGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CreatorId { get; set; }
    public List<int> UserIds { get; set; } = new();
}

public class SyncChatMemberRequest
{
    public int CompanyId { get; set; }
    public int UserId { get; set; }
}
