using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Services;
using Shared.Common;
using System;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly JwtHelper _jwtHelper;

    public NotificationsController(INotificationService notificationService, JwtHelper jwtHelper)
    {
        _notificationService = notificationService;
        _jwtHelper = jwtHelper;
    }

    [HttpGet("counters")]
    public async Task<ActionResult<NotificationCountersDto>> GetCounters()
    {
        try
        {
            var companyId = _jwtHelper.GetCompanyIdFromToken(User);
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (!companyId.HasValue || userId == 0)
            {
                return Unauthorized();
            }

            var counters = await _notificationService.GetCountersAsync(userId, companyId.Value);
            return Ok(counters);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("chats/{chatId}/read")]
    public async Task<IActionResult> MarkChatAsRead(string chatId)
    {
        try
        {
            var companyId = _jwtHelper.GetCompanyIdFromToken(User);
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            Console.WriteLine($"[NotificationsController] MarkChatAsRead called: chatId={chatId}, userId={userId}, companyId={companyId}");
            
            if (!companyId.HasValue || userId == 0)
            {
                Console.WriteLine($"[NotificationsController] Unauthorized: companyId={companyId}, userId={userId}");
                return Unauthorized();
            }

            await _notificationService.ResetChatUnreadAsync(chatId, userId, companyId.Value);
            Console.WriteLine($"[NotificationsController] Chat {chatId} marked as read successfully");
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationsController] Error marking chat as read: {ex.Message}");
            Console.WriteLine($"[NotificationsController] Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("feed/read")]
    public async Task<IActionResult> MarkFeedAsRead()
    {
        try
        {
            var companyId = _jwtHelper.GetCompanyIdFromToken(User);
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (!companyId.HasValue || userId == 0)
            {
                return Unauthorized();
            }

            await _notificationService.ResetFeedUnreadAsync(userId, companyId.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("tasks/read")]
    public async Task<IActionResult> MarkTasksAsRead()
    {
        try
        {
            var companyId = _jwtHelper.GetCompanyIdFromToken(User);
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (!companyId.HasValue || userId == 0)
            {
                return Unauthorized();
            }

            await _notificationService.ResetTaskUnreadAsync(userId, companyId.Value);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}

