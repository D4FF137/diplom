using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Services;
using System.Linq;

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
}

