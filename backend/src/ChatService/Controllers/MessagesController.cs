using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Services;
using Shared.Models;
using Shared.Common;

namespace ChatService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly JwtHelper _jwtHelper;
    private readonly IFileService _fileService;

    public MessagesController(IMessageService messageService, JwtHelper jwtHelper, IFileService fileService)
    {
        _messageService = messageService;
        _jwtHelper = jwtHelper;
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Message>>> GetMessages([FromQuery] string chatId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        if (string.IsNullOrEmpty(chatId))
        {
            return BadRequest("chatId is required");
        }

        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var messages = await _messageService.GetByChatIdAsync(chatId, companyId.Value, skip, take);
        return Ok(messages);
    }

    [HttpGet("chat/{chatId}")]
    public async Task<ActionResult<List<Message>>> GetMessagesByPath(string chatId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var messages = await _messageService.GetByChatIdAsync(chatId, companyId.Value, skip, take);
        return Ok(messages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetMessage(string id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var message = await _messageService.GetByIdAsync(id, companyId.Value);
        if (message == null)
        {
            return NotFound();
        }

        return Ok(message);
    }

    [HttpPost]
    public async Task<ActionResult<Message>> CreateMessage([FromForm] CreateMessageRequest request)
    {
        Console.WriteLine($"[CreateMessage] Received request. ChatId: {request.ChatId}, Content: {request.Content}, File: {request.File?.FileName}, FileLength: {request.File?.Length}");

        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            Console.WriteLine("[CreateMessage] Unauthorized");
            return Unauthorized();
        }

        string? attachmentUrl = null;
        if (request.File != null && request.File.Length > 0)
        {
            try 
            {
                var fileName = await _fileService.SaveFileAsync(request.File);
                attachmentUrl = _fileService.GetFileUrl(fileName);
                Console.WriteLine($"[CreateMessage] File saved. AttachmentUrl: {attachmentUrl}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"[CreateMessage] File save failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        var message = new Message
        {
            CompanyId = companyId.Value,
            ChatId = request.ChatId,
            UserId = userId,
            Content = request.Content ?? string.Empty,
            AttachmentUrl = attachmentUrl
        };

        var createdMessage = await _messageService.CreateAsync(message);
        Console.WriteLine($"[CreateMessage] Message created. Id: {createdMessage.Id}, AttachmentUrl: {createdMessage.AttachmentUrl}");
        return CreatedAtAction(nameof(GetMessage), new { id = createdMessage.Id }, createdMessage);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(string id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var deleted = await _messageService.DeleteAsync(id, companyId.Value);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Message>> UpdateMessage(string id, [FromBody] UpdateMessageRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var updatedMessage = await _messageService.UpdateAsync(id, request.Content, companyId.Value);
        if (updatedMessage == null)
        {
            return NotFound();
        }

        return Ok(updatedMessage);
    }
}

public class UpdateMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class CreateMessageRequest
{
    public string ChatId { get; set; } = string.Empty;
    public string? Content { get; set; }
    public IFormFile? File { get; set; }
}

