using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace NotificationService.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        Console.WriteLine($"[NotificationsHub] User connected: ConnectionId={Context.ConnectionId}, UserId={userId}");
        
        if (userId > 0)
        {
            // Присоединяемся к группе пользователя для отправки персональных уведомлений
            var groupName = $"user_{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            Console.WriteLine($"[NotificationsHub] Added connection {Context.ConnectionId} to group {groupName}");
        }
        else
        {
            Console.WriteLine($"[NotificationsHub] WARNING: Could not extract userId from token. Claims: {string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Enumerable.Empty<string>())}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        
        await base.OnDisconnectedAsync(exception);
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
}


