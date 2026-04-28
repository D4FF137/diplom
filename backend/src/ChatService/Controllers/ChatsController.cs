using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ChatService.Services;
using ChatService.Hubs;
using Shared.Models;
using Shared.Common;

namespace ChatService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IChatMemberService _chatMemberService;
    private readonly IMessageService _messageService;
    private readonly JwtHelper _jwtHelper;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly IUserInfoService _userInfoService;

    public ChatsController(
        IChatService chatService,
        IChatMemberService chatMemberService,
        IMessageService messageService,
        JwtHelper jwtHelper,
        IHubContext<ChatHub> hubContext,
        IUserInfoService userInfoService,
        IConfiguration configuration)
    {
        _chatService = chatService;
        _chatMemberService = chatMemberService;
        _messageService = messageService;
        _jwtHelper = jwtHelper;
        _hubContext = hubContext;
        _userInfoService = userInfoService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetChats()
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

        var chats = await _chatService.GetByCompanyIdAsync(companyId.Value);
        
        // Получаем JWT токен из заголовков
                var jwtToken = AuthTokenHelper.ExtractToken(Request);
        
        // Загружаем участников для каждого чата и фильтруем только те, где пользователь является участником
        var chatsWithMembers = new List<object>();
        foreach (var chat in chats)
        {
            var members = await _chatMemberService.GetByChatIdAsync(chat.Id, companyId.Value);
            var memberUserIds = members.Select(m => m.UserId).ToList();
            
            // Пропускаем чаты, где пользователь не является участником
            if (!memberUserIds.Contains(userId))
            {
                continue;
            }
            
            // Получаем информацию о пользователях
            var usersInfo = await _userInfoService.GetUsersInfoAsync(memberUserIds, companyId.Value, jwtToken);
            
            // Создаем список участников с полной информацией
            var chatMembers = memberUserIds
                .Where(userId => usersInfo.ContainsKey(userId))
                .Select(userId => usersInfo[userId])
                .ToList();
            
            // Получаем последнее сообщение чата
            string? lastMessage = null;
            string? lastMessageAt = null;
            try
            {
                var messages = await _messageService.GetByChatIdAsync(chat.Id, companyId.Value, skip: 0, take: 1);
                if (messages.Any())
                {
                    var lastMsg = messages.First();
                    lastMessage = lastMsg.Content;
                    lastMessageAt = lastMsg.CreatedAt.ToString("O");
                }
            }
            catch
            {
                // Игнорируем ошибки при загрузке сообщений
            }
            
            // Создаем анонимный объект с участниками и последним сообщением
            var chatWithMembers = new
            {
                Id = chat.Id,
                Name = chat.Name,
                CompanyId = chat.CompanyId,
                Type = chat.Type,
                CreatedAt = chat.CreatedAt,
                CreatorId = chat.CreatorId,
                Members = chatMembers,
                LastMessage = lastMessage,
                LastMessageAt = lastMessageAt
            };
            
            chatsWithMembers.Add(chatWithMembers);
        }
        
        return Ok(chatsWithMembers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Chat>> GetChat(string id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        var chat = await _chatService.GetByIdAsync(id, companyId.Value);
        if (chat == null)
        {
            return NotFound();
        }

        return Ok(chat);
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateChat([FromBody] CreateChatRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

        var chatType = request.Type ?? "group";
        
        // Для приватных чатов проверяем, не существует ли уже чат между этими пользователями
        if (chatType == "private" && request.UserIds != null && request.UserIds.Count == 1)
        {
            var otherUserId = request.UserIds[0];
            var existingChat = await _chatService.FindPrivateChatBetweenUsersAsync(userId, otherUserId, companyId.Value);
            
            if (existingChat != null)
            {
                // Возвращаем существующий чат с участниками
                var existingMembers = await _chatMemberService.GetByChatIdAsync(existingChat.Id, companyId.Value);
                var existingMemberUserIds = existingMembers.Select(m => m.UserId).ToList();
            var existingJwtToken = AuthTokenHelper.ExtractToken(Request);
                var existingUsersInfo = await _userInfoService.GetUsersInfoAsync(existingMemberUserIds, companyId.Value, existingJwtToken);
                
                var existingChatMembers = existingMemberUserIds
                    .Where(uid => existingUsersInfo.ContainsKey(uid))
                    .Select(uid => existingUsersInfo[uid])
                    .ToList();
                
                // Получаем последнее сообщение
                string? existingLastMessage = null;
                string? existingLastMessageAt = null;
                try
                {
                    var existingMessages = await _messageService.GetByChatIdAsync(existingChat.Id, companyId.Value, skip: 0, take: 1);
                    if (existingMessages.Any())
                    {
                        var existingLastMsg = existingMessages.First();
                        existingLastMessage = existingLastMsg.Content;
                        existingLastMessageAt = existingLastMsg.CreatedAt.ToString("O");
                    }
                }
                catch
                {
                    // Игнорируем ошибки при загрузке сообщений
                }
                
                var existingChatWithMembers = new
                {
                    Id = existingChat.Id,
                    Name = existingChat.Name,
                    CompanyId = existingChat.CompanyId,
                    Type = existingChat.Type,
                    CreatedAt = existingChat.CreatedAt,
                    CreatorId = existingChat.CreatorId,
                    Members = existingChatMembers,
                    LastMessage = existingLastMessage,
                    LastMessageAt = existingLastMessageAt
                };
                
                return Ok(existingChatWithMembers);
            }
        }

        var chat = new Chat
        {
            CompanyId = companyId.Value,
            Name = request.Name,
            Type = chatType,
            CreatorId = userId
        };

        var createdChat = await _chatService.CreateAsync(chat);

        // Добавляем создателя в участники
        await _chatMemberService.AddMemberAsync(createdChat.Id, userId, companyId.Value);

        // Добавляем других участников, если указаны
        if (request.UserIds != null && request.UserIds.Any())
        {
            foreach (var memberUserId in request.UserIds)
            {
                if (memberUserId != userId) // Не добавляем создателя дважды
                {
                    await _chatMemberService.AddMemberAsync(createdChat.Id, memberUserId, companyId.Value);
                }
            }
        }

        // Получаем участников чата с их данными
        var members = await _chatMemberService.GetByChatIdAsync(createdChat.Id, companyId.Value);
        var memberUserIds = members.Select(m => m.UserId).ToList();
                var jwtToken = AuthTokenHelper.ExtractToken(Request);
        var usersInfo = await _userInfoService.GetUsersInfoAsync(memberUserIds, companyId.Value, jwtToken);
        
        var chatMembers = memberUserIds
            .Where(userId => usersInfo.ContainsKey(userId))
            .Select(userId => usersInfo[userId])
            .ToList();
        
        // Для нового чата lastMessage будет null
        var chatWithMembers = new
        {
            Id = createdChat.Id,
            Name = createdChat.Name,
            CompanyId = createdChat.CompanyId,
            Type = createdChat.Type,
            CreatedAt = createdChat.CreatedAt,
            CreatorId = createdChat.CreatorId,
            Members = chatMembers,
            LastMessage = (string?)null,
            LastMessageAt = (string?)null
        };

        // Отправляем событие о создании нового чата только участникам этого чата
        foreach (var memberId in memberUserIds)
        {
            await _hubContext.Clients.Group($"user_{memberId}")
                .SendAsync("NewChat", chatWithMembers);
        }

        return CreatedAtAction(nameof(GetChat), new { id = createdChat.Id }, chatWithMembers);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChat(string id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue)
        {
            return Unauthorized();
        }

        // Проверяем, что чат существует
        var chat = await _chatService.GetByIdAsync(id, companyId.Value);
        if (chat == null)
        {
            return NotFound();
        }

        var deleted = await _chatService.DeleteAsync(id, companyId.Value);
        if (!deleted)
        {
            return NotFound();
        }

        // Удаляем все уведомления для этого чата
        try
        {
            var notificationServiceUrl = _configuration["NOTIFICATION_SERVICE_URL"] ?? "http://notificationservice:5005";
            var client = new HttpClient();
            var response = await client.DeleteAsync($"{notificationServiceUrl}/api/internal/chats/{id}/notifications?companyId={companyId.Value}");
            if (!response.IsSuccessStatusCode)
            {
                // Логируем ошибку, но не прерываем удаление чата
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ChatService] Failed to delete notifications for chat {id}: {response.StatusCode}, {errorContent}");
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку, но не прерываем удаление чата
            Console.WriteLine($"[ChatService] Error deleting notifications for chat {id}: {ex.Message}");
        }

        // Отправляем событие об удалении чата всем пользователям компании
        await _hubContext.Clients.Group($"company_{companyId.Value}")
            .SendAsync("ChatDeleted", new { chatId = id });

        return NoContent();
    }

    [HttpDelete("{id}/leave")]
    public async Task<IActionResult> LeaveChat(string id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        if (!companyId.HasValue || userId == 0)
        {
            return Unauthorized();
        }

        // Проверяем, что чат существует
        var chat = await _chatService.GetByIdAsync(id, companyId.Value);
        if (chat == null)
        {
            return NotFound();
        }

        // Проверяем, что пользователь является участником
        var isMember = await _chatMemberService.IsMemberAsync(id, userId, companyId.Value);
        if (!isMember)
        {
            return NotFound();
        }

        // Удаляем пользователя из чата
        var removed = await _chatMemberService.RemoveMemberAsync(id, userId, companyId.Value);
        if (!removed)
        {
            return NotFound();
        }

        // Отправляем событие об уходе пользователя из чата
        await _hubContext.Clients.Group($"company_{companyId.Value}_chat_{id}")
            .SendAsync("UserLeft", userId, id);

        // Отправляем событие конкретному пользователю через группу, что чат удален из его списка
        // Это нужно для обновления UI без перезагрузки страницы
        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ChatRemoved", new { chatId = id });

        // Отправляем событие об обновлении чата для обновления списка чатов у других пользователей
        await _hubContext.Clients.Group($"company_{companyId.Value}")
            .SendAsync("ChatUpdated", new
            {
                chatId = id,
                lastMessageAt = DateTime.UtcNow.ToString("O")
            });

        return NoContent();
    }

    [HttpGet("{chatId}/messages/search")]
    public async Task<ActionResult<IEnumerable<Message>>> SearchMessages(string chatId, [FromQuery] string query, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var chat = await _chatService.GetByIdAsync(chatId, companyId.Value);
        if (chat == null) return NotFound();

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var isMember = await _chatMemberService.IsMemberAsync(chatId, userId, companyId.Value);
        
        if (!isMember) return Forbid();

        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        var messages = await _messageService.SearchMessagesAsync(chatId, query, companyId.Value, skip, take);
        return Ok(messages);
    }
}

public class CreateChatRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; } = "group";
    public List<int>? UserIds { get; set; }
}
