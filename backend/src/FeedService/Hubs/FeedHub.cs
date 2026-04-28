using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Shared.Common;

namespace FeedService.Hubs;

[Authorize]
public class FeedHub : Hub
{
    private int? GetCompanyId()
    {
        var companyIdClaim = Context.User?.FindFirst("CompanyId")?.Value;
        if (int.TryParse(companyIdClaim, out var companyId))
        {
            return companyId;
        }
        return null;
    }

    public override async Task OnConnectedAsync()
    {
        var companyId = GetCompanyId();
        if (companyId.HasValue)
        {
            // Присоединяемся к группе компании для получения обновлений ленты
            await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId.Value}_feed");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var companyId = GetCompanyId();
        if (companyId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company_{companyId.Value}_feed");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}






