using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/internal")]
[AllowAnonymous]
public class InternalController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public InternalController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpDelete("chats/{chatId}/notifications")]
    public async Task<IActionResult> DeleteChatNotifications(string chatId, [FromQuery] int companyId)
    {
        try
        {
            Console.WriteLine($"[InternalController] DeleteChatNotifications called: chatId={chatId}, companyId={companyId}");
            await _notificationService.DeleteChatUnreadAsync(chatId, companyId);
            Console.WriteLine($"[InternalController] Chat notifications deleted successfully for chat {chatId}");
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InternalController] Error deleting chat notifications: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}



