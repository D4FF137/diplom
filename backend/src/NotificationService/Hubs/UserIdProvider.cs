using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace NotificationService.Hubs;

public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userIdClaim = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim;
    }
}




