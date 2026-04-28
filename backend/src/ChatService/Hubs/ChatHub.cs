using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChatService.Services;
using Shared.Models;
using Shared.Common;

namespace ChatService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IChatService _chatService;
    private readonly IChatMemberService _chatMemberService;
    private readonly IUserInfoService _userInfoService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    
    // Временное хранилище онлайн-пользователей (в продакшене лучше использовать Redis)
    // Словарь: CompanyId -> Set of UserIds
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, System.Collections.Concurrent.ConcurrentDictionary<int, byte>> _onlineUsers = new();

    public ChatHub(
        IMessageService messageService, 
        IChatService chatService, 
        IChatMemberService chatMemberService,
        IUserInfoService userInfoService,
        IHttpClientFactory httpClientFactory, 
        IConfiguration configuration)
    {
        _messageService = messageService;
        _chatService = chatService;
        _chatMemberService = chatMemberService;
        _userInfoService = userInfoService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public override async Task OnConnectedAsync()
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();
        
        if (companyId.HasValue)
        {
            // Присоединяемся к группе компании
            await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId.Value}");
        }
        
        if (userId > 0)
        {
            // Присоединяемся к группе пользователя для отправки персональных сообщений
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Оповещаем компанию о том, что пользователь в сети
            if (companyId.HasValue)
            {
                var companyUsers = _onlineUsers.GetOrAdd(companyId.Value, _ => new System.Collections.Concurrent.ConcurrentDictionary<int, byte>());
                companyUsers.TryAdd(userId, 0);

                Console.WriteLine($"[ChatHub] User {userId} is online in company {companyId.Value}");
                await Clients.Group($"company_{companyId.Value}").SendAsync("UserOnline", userId);
                await ReportPresence(userId, companyId.Value, DateTime.UtcNow);
            }
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();
        
        if (companyId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyId.Value}");
        }
        
        if (userId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Оповещаем компанию о том, что пользователь вышел из сети
            if (companyId.HasValue)
            {
                if (_onlineUsers.TryGetValue(companyId.Value, out var companyUsers))
                {
                    companyUsers.TryRemove(userId, out _);
                }

                Console.WriteLine($"[ChatHub] User {userId} is offline in company {companyId.Value}");
                await Clients.Group($"company_{companyId.Value}").SendAsync("UserOffline", userId, DateTime.UtcNow.ToString("O"));
                await ReportPresence(userId, companyId.Value, DateTime.UtcNow);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(string chatId)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        // Проверяем, что чат принадлежит компании
        var chat = await _chatService.GetByIdAsync(chatId, companyId.Value);
        if (chat == null)
        {
            await Clients.Caller.SendAsync("Error", "Chat not found");
            return;
        }

        // Присоединяемся к группе чата (изолированной по компании)
        await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId.Value}_chat_{chatId}");
        await Clients.Caller.SendAsync("JoinedChat", chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyId.Value}_chat_{chatId}");
        await Clients.Caller.SendAsync("LeftChat", chatId);
    }

    public async Task SendMessage(string chatId, string content)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();
        
        if (!companyId.HasValue || userId == 0)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        // Проверяем, что чат принадлежит компании
        var chat = await _chatService.GetByIdAsync(chatId, companyId.Value);
        if (chat == null)
        {
            await Clients.Caller.SendAsync("Error", "Chat not found");
            return;
        }

        // Создаем сообщение
        var message = new Message
        {
            CompanyId = companyId.Value,
            ChatId = chatId,
            UserId = userId,
            Content = content
        };

        var createdMessage = await _messageService.CreateAsync(message);

        // Отправляем сообщение только участникам чата этой компании
        await Clients.Group($"company_{companyId.Value}_chat_{chatId}")
            .SendAsync("ReceiveMessage", new
            {
                id = createdMessage.Id,
                chatId = createdMessage.ChatId,
                userId = createdMessage.UserId,
                content = createdMessage.Content,
                type = createdMessage.Type,
                poll = createdMessage.Poll,
                createdAt = createdMessage.CreatedAt
            });

        // Отправляем событие обновления чата всем пользователям компании для обновления порядка чатов
        await Clients.Group($"company_{companyId.Value}")
            .SendAsync("ChatUpdated", new
            {
                chatId = chatId,
                lastMessage = createdMessage.Content,
                lastMessageAt = createdMessage.CreatedAt.ToString("O")
            });
    }

    public async Task SendPoll(string chatId, string question, List<string> options, bool isAnonymous)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();

        if (!companyId.HasValue || userId == 0)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        var poll = new PollData
        {
            Question = question,
            IsAnonymous = isAnonymous,
            Options = options.Select((text, index) => new PollOption { Id = index, Text = text }).ToList()
        };

        var message = new Message
        {
            CompanyId = companyId.Value,
            ChatId = chatId,
            UserId = userId,
            Content = question, // Text fallback for notifications
            Type = "poll",
            Poll = poll
        };

        var createdMessage = await _messageService.CreateAsync(message);

        // Broadcast to SignalR chat group
        await Clients.Group($"company_{companyId.Value}_chat_{chatId}")
            .SendAsync("ReceiveMessage", new
            {
                id = createdMessage.Id,
                chatId = createdMessage.ChatId,
                userId = createdMessage.UserId,
                content = createdMessage.Content,
                type = createdMessage.Type,
                poll = createdMessage.Poll,
                createdAt = createdMessage.CreatedAt
            });

        // Update chat list
        await Clients.Group($"company_{companyId.Value}")
            .SendAsync("ChatUpdated", new
            {
                chatId = chatId,
                lastMessage = $"📊 Poll: {question}",
                lastMessageAt = createdMessage.CreatedAt.ToString("O")
            });
    }

    public async Task SendTyping(string chatId, bool isTyping)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();

        if (!companyId.HasValue || userId == 0) return;

        // Отправляем событие "печатает" всем участникам чата
        await Clients.Group($"company_{companyId.Value}_chat_{chatId}")
            .SendAsync("UserTyping", new
            {
                chatId = chatId,
                userId = userId,
                isTyping = isTyping
            });
    }

    public async Task Vote(string messageId, int optionId)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();

        if (!companyId.HasValue || userId == 0) return;

        await _messageService.VoteAsync(messageId, optionId, userId, companyId.Value);
    }

    public async Task AddMember(string chatId, int targetUserId)
    {
        var companyId = GetCompanyId();
        var callerUserId = GetUserId();

        if (!companyId.HasValue || callerUserId == 0) return;

        var chat = await _chatService.GetByIdAsync(chatId, companyId.Value);
        if (chat == null || chat.Type != "group" || chat.CreatorId != callerUserId)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized or not a group chat");
            return;
        }

        // Check if already a member
        if (await _chatMemberService.IsMemberAsync(chatId, targetUserId, companyId.Value))
        {
            await Clients.Caller.SendAsync("Error", "User is already a member");
            return;
        }

        await _chatMemberService.AddMemberAsync(chatId, targetUserId, companyId.Value);

                var jwtToken = AuthTokenHelper.ExtractToken(Context.GetHttpContext());

        // Get info for system message
        var callerInfo = await _userInfoService.GetUserInfoAsync(callerUserId, companyId.Value, jwtToken);
        var targetInfo = await _userInfoService.GetUserInfoAsync(targetUserId, companyId.Value, jwtToken);

        var callerName = GetFormattedName(callerInfo);
        var targetName = GetFormattedName(targetInfo);
        var content = $"{callerName} добавил(а) {targetName}";

        // Create system message
        var sysMsg = new Message
        {
            CompanyId = companyId.Value,
            ChatId = chatId,
            UserId = callerUserId, // Or 0 for system
            Content = content,
            Type = "system"
        };
        var createdMsg = await _messageService.CreateAsync(sysMsg);

        // Broadcast to SignalR
        await Clients.Group($"company_{companyId.Value}_chat_{chatId}").SendAsync("ReceiveMessage", new
        {
            id = createdMsg.Id,
            chatId = createdMsg.ChatId,
            userId = createdMsg.UserId,
            content = createdMsg.Content,
            type = createdMsg.Type,
            createdAt = createdMsg.CreatedAt
        });

        // Notify specifically about member change for real-time list update
        await Clients.Group($"company_{companyId.Value}_chat_{chatId}").SendAsync("MemberAdded", new
        {
            chatId = chatId,
            user = targetInfo
        });

        // Also notify the target user specifically that they were added to a chat
        // This allows them to add the chat to their sidebar in real-time
        var chatInfo = await _chatService.GetByIdAsync(chatId, companyId.Value);
        if (chatInfo != null)
        {
            var members = await _chatMemberService.GetByChatIdAsync(chatId, companyId.Value);
            var memberUserIds = members.Select(m => m.UserId).ToList();
            var usersInfo = await _userInfoService.GetUsersInfoAsync(memberUserIds, companyId.Value, jwtToken);
            
            var chatMembers = memberUserIds
                .Where(uid => usersInfo.ContainsKey(uid))
                .Select(uid => usersInfo[uid])
                .ToList();

            await Clients.Group($"user_{targetUserId}").SendAsync("NewChat", new
            {
                id = chatInfo.Id,
                name = chatInfo.Name,
                companyId = chatInfo.CompanyId,
                type = chatInfo.Type,
                createdAt = chatInfo.CreatedAt,
                creatorId = chatInfo.CreatorId,
                members = chatMembers,
                lastMessage = createdMsg.Content,
                lastMessageAt = createdMsg.CreatedAt.ToString("O")
            });
        }
    }

    public async Task RemoveMember(string chatId, int targetUserId)
    {
        var companyId = GetCompanyId();
        var callerUserId = GetUserId();

        if (!companyId.HasValue || callerUserId == 0) return;

        var chat = await _chatService.GetByIdAsync(chatId, companyId.Value);
        if (chat == null || chat.Type != "group" || chat.CreatorId != callerUserId)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized or not a group chat");
            return;
        }

        bool success = await _chatMemberService.RemoveMemberAsync(chatId, targetUserId, companyId.Value);
        if (!success) return;

                var jwtToken = AuthTokenHelper.ExtractToken(Context.GetHttpContext());

        // Get info for system message
        var callerInfo = await _userInfoService.GetUserInfoAsync(callerUserId, companyId.Value, jwtToken);
        var targetInfo = await _userInfoService.GetUserInfoAsync(targetUserId, companyId.Value, jwtToken);

        var callerName = GetFormattedName(callerInfo);
        var targetName = GetFormattedName(targetInfo);
        var content = $"{callerName} удалил(а) {targetName}";

        // Create system message
        var sysMsg = new Message
        {
            CompanyId = companyId.Value,
            ChatId = chatId,
            UserId = callerUserId,
            Content = content,
            Type = "system"
        };
        var createdMsg = await _messageService.CreateAsync(sysMsg);

        // Broadcast to SignalR chat group
        await Clients.Group($"company_{companyId.Value}_chat_{chatId}").SendAsync("ReceiveMessage", new
        {
            id = createdMsg.Id,
            chatId = createdMsg.ChatId,
            userId = createdMsg.UserId,
            content = createdMsg.Content,
            type = createdMsg.Type,
            createdAt = createdMsg.CreatedAt
        });

        // Notify specifically about member removal
        await Clients.Group($"company_{companyId.Value}_chat_{chatId}").SendAsync("MemberRemoved", new
        {
            chatId = chatId,
            userId = targetUserId
        });

        // Also notify the target user specifically that they were removed
        await Clients.Group($"user_{targetUserId}").SendAsync("ChatRemoved", chatId);
    }

    public Task<List<int>> GetOnlineUsers()
    {
        var companyId = GetCompanyId();
        if (companyId.HasValue && _onlineUsers.TryGetValue(companyId.Value, out var companyUsers))
        {
            return Task.FromResult(companyUsers.Keys.ToList());
        }
        return Task.FromResult(new List<int>());
    }

    private async Task ReportPresence(int userId, int companyId, DateTime lastSeen)
    {
        try
        {
            var userServiceUrl = _configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync($"{userServiceUrl}/api/internal/users/presence", new
            {
                UserId = userId,
                CompanyId = companyId,
                LastSeen = lastSeen
            });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ChatHub] Failed to report presence for user {userId}: {response.StatusCode} - {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ChatHub] Error reporting presence for user {userId}: {ex.Message}");
        }
    }

    private int? GetCompanyId()
    {
        var companyIdClaim = Context.User?.FindFirst("companyId")?.Value;
        if (int.TryParse(companyIdClaim, out var companyId))
        {
            return companyId;
        }
        return null;
    }

    private int GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return 0;
    }
    private string GetFormattedName(User? user)
    {
        if (user == null) return "Пользователь";
        if (string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName))
            return user.Email;
        
        var name = user.FirstName;
        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            name += " " + user.LastName[0] + ".";
        }
        return name.Trim();
    }

    public async Task AddReaction(string chatId, string messageId, string emoji)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();

        if (!companyId.HasValue || userId == 0) return;

        await _messageService.AddReactionAsync(messageId, emoji, userId, companyId.Value);
    }

    public async Task RemoveReaction(string chatId, string messageId, string emoji)
    {
        var companyId = GetCompanyId();
        var userId = GetUserId();

        if (!companyId.HasValue || userId == 0) return;

        await _messageService.RemoveReactionAsync(messageId, emoji, userId, companyId.Value);
    }
}
