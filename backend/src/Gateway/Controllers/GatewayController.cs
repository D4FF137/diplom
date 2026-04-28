using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gateway.Services;

namespace Gateway.Controllers;

[ApiController]
[Route("api")]
public class GatewayController : ControllerBase
{
    private readonly IRoutingService _routingService;

    public GatewayController(IRoutingService routingService)
    {
        _routingService = routingService;
    }

    // User Service Routes (register removed: only boss adds members)
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login()
    {
        var response = await _routingService.RouteToUserServiceAsync(Request, "api/auth/login");
        return await HandleResponse(response);
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        var response = await _routingService.RouteToUserServiceAsync(Request, "api/auth/logout");
        return await HandleResponse(response);
    }

    [HttpGet("users")]
    [HttpGet("users/{id}")]
    [HttpGet("users/search")]
    [HttpGet("users/me")]
    [HttpPost("users")]
    [HttpPost("users/members")]
    [HttpPut("users/{id}")]
    [HttpPut("users/me")]
    [HttpPost("users/me/password")]
    [HttpPost("users/{id}/block")]
    [HttpPost("users/{id}/unblock")]
    [HttpPost("users/{id}/password")]
    [HttpPost("users/import")]
    [HttpDelete("users/{id}")]
    [Authorize]
    public async Task<IActionResult> Users()
    {
        // Keep /api prefix for routing to microservice
        var path = Request.Path.Value ?? "";
        var response = await _routingService.RouteToUserServiceAsync(Request, path);
        return await HandleResponse(response);
    }

    // Company Service Routes
    [HttpGet("companies")]
    [HttpGet("companies/{id}")]
    [HttpPost("companies")]
    [HttpPut("companies/{id}")]
    [HttpDelete("companies/{id}")]
    public async Task<IActionResult> Companies()
    {
        // Keep /api prefix for routing to microservice
        var path = Request.Path.Value ?? "";
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<GatewayController>>();
        logger.LogInformation("Routing to CompanyService: {Path}, Method: {Method}", path, Request.Method);
        
        var response = await _routingService.RouteToCompanyServiceAsync(Request, path);
        logger.LogInformation("CompanyService response: Status {StatusCode}", response.StatusCode);
        
        return await HandleResponse(response);
    }

    // Feed Service Routes
    [HttpGet("feed")]
    [HttpGet("feed/posts")]
    [HttpGet("feed/posts/{id}")]
    [HttpGet("feed/posts/{id}/comments")]
    [HttpPost("feed/posts")]
    [HttpPost("feed/posts/{id}/like")]
    [HttpPost("feed/posts/{id}/comments")]
    [HttpPut("feed/posts/{id}")]
    [HttpDelete("feed/posts/{id}")]
    [HttpDelete("feed/posts/{id}/like")]
    [Authorize]
    public async Task<IActionResult> Feed()
    {
        // Convert /api/feed/posts to /api/posts, but keep nested paths like /comments and /like
        var path = Request.Path.Value ?? "";
        // Replace /api/feed/posts with /api/posts, but preserve everything after (like /4/comments)
        if (path.StartsWith("/api/feed/posts"))
        {
            path = "/api/posts" + path.Substring("/api/feed/posts".Length);
        }
        else if (path.StartsWith("/api/feed"))
        {
            path = path.Replace("/api/feed", "/api/posts");
        }
        var response = await _routingService.RouteToFeedServiceAsync(Request, path);
        return await HandleResponse(response);
    }

    // Chat Service Routes
    [HttpGet("chat/chats")]
    [HttpGet("chat/chats/{id}")]
    [HttpPost("chat/chats")]
    [HttpDelete("chat/chats/{id}")]
    [HttpDelete("chat/chats/{id}/leave")]
    [HttpGet("chat/chats/{id}/messages/search")]
    [HttpGet("chat/messages")]
    [HttpGet("chat/messages/{id}")]
    [HttpPost("chat/messages")]
    [HttpPut("chat/messages/{id}")]
    [HttpDelete("chat/messages/{id}")]
    [Authorize]
    public async Task<IActionResult> Chat()
    {
        // Convert /api/chat/chats to /api/chats, /api/chat/messages to /api/messages
        var path = Request.Path.Value?.Replace("/api/chat/chats", "/api/chats")
                                      .Replace("/api/chat/messages", "/api/messages") ?? "";
        var response = await _routingService.RouteToChatServiceAsync(Request, path);
        return await HandleResponse(response);
    }

    [HttpGet("chat/uploads/{**path}")]
    public async Task<IActionResult> GetChatUploads(string path)
    {
        var response = await _routingService.RouteToChatServiceAsync(Request, "/uploads/" + path);
        
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync();
        
        // Pass through Content-Disposition if present (for downloads)
        if (response.Content.Headers.ContentDisposition != null)
        {
            Response.Headers.Append("Content-Disposition", response.Content.Headers.ContentDisposition.ToString());
        }

        return File(stream, contentType);
    }

    // Storage Service Routes
    [HttpGet("storage/files")]
    [HttpPost("storage/files/upload")]
    [HttpPut("storage/files/{id}/important")]
    [HttpDelete("storage/files/{id}")]
    [Authorize]
    public async Task<IActionResult> Storage()
    {
        // Convert /api/storage/files to /api/files
        var path = Request.Path.Value?.Replace("/api/storage/files", "/api/files") ?? "";
        var response = await _routingService.RouteToStorageServiceAsync(Request, path);
        return await HandleResponse(response);
    }

    [HttpGet("storage/uploads/{**path}")]
    public async Task<IActionResult> GetStorageUploads(string path)
    {
        var response = await _routingService.RouteToStorageServiceAsync(Request, "/uploads/" + path);
        
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var stream = await response.Content.ReadAsStreamAsync();
        
        if (response.Content.Headers.ContentDisposition != null)
        {
            Response.Headers.Append("Content-Disposition", response.Content.Headers.ContentDisposition.ToString());
        }

        return File(stream, contentType);
    }

    // Notification Service Routes
    [HttpGet("notifications/counters")]
    [HttpPost("notifications/chats/{id}/read")]
    [HttpPost("notifications/feed/read")]
    [Authorize]
    public async Task<IActionResult> Notifications()
    {
        // Convert /api/notifications/* to /api/notifications/*
        var path = Request.Path.Value ?? "";
        var response = await _routingService.RouteToNotificationServiceAsync(Request, path);
        return await HandleResponse(response);
    }

    [HttpGet("uploads/{**path}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGeneralUploads(string path)
    {
        // Try routing to StorageService as default for /api/uploads/
        var response = await _routingService.RouteToStorageServiceAsync(Request, "/uploads/" + path);
        if (response.IsSuccessStatusCode)
        {
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var stream = await response.Content.ReadAsStreamAsync();
            if (response.Content.Headers.ContentDisposition != null)
            {
                Response.Headers.Append("Content-Disposition", response.Content.Headers.ContentDisposition.ToString());
            }
            return File(stream, contentType);
        }

        // If not found, try ChatService (legacy/different bucket)
        var chatResponse = await _routingService.RouteToChatServiceAsync(Request, "/uploads/" + path);
        if (chatResponse.IsSuccessStatusCode)
        {
            var contentType = chatResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var stream = await chatResponse.Content.ReadAsStreamAsync();
            if (chatResponse.Content.Headers.ContentDisposition != null)
            {
                Response.Headers.Append("Content-Disposition", chatResponse.Content.Headers.ContentDisposition.ToString());
            }
            return File(stream, contentType);
        }

        // Try UserService for avatars
        var userResponse = await _routingService.RouteToUserServiceAsync(Request, "/uploads/" + path);
        if (userResponse.IsSuccessStatusCode)
        {
            var contentType = userResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var stream = await userResponse.Content.ReadAsStreamAsync();
            if (userResponse.Content.Headers.ContentDisposition != null)
            {
                Response.Headers.Append("Content-Disposition", userResponse.Content.Headers.ContentDisposition.ToString());
            }
            return File(stream, contentType);
        }

        return NotFound();
    }

    private async Task<IActionResult> HandleResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<GatewayController>>();

        CopyProxyResponseHeaders(response);
        
        logger.LogInformation("Handling response: Status {StatusCode}, ContentLength: {Length}, ContentType: {ContentType}", 
            response.StatusCode, content?.Length ?? 0, response.Content.Headers.ContentType?.ToString() ?? "none");
        
        // Determine content type
        string contentType = "application/json; charset=utf-8";
        if (response.Content.Headers.ContentType != null)
        {
            contentType = response.Content.Headers.ContentType.ToString();
        }
        else if (!string.IsNullOrEmpty(content) && (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("[")))
        {
            contentType = "application/json; charset=utf-8";
        }
        else if (!string.IsNullOrEmpty(content))
        {
            contentType = "text/plain; charset=utf-8";
        }
        
        // Return empty response if no content
        if (string.IsNullOrEmpty(content))
        {
            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = contentType;
            return new EmptyResult();
        }
        
        // Return JSON response using ObjectResult for proper serialization
        var isJson = contentType.Contains("json") || 
                     (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["));
        
        if (isJson)
        {
            try
            {
                // Parse and return as object for proper JSON serialization
                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                var jsonObject = System.Text.Json.JsonSerializer.Deserialize<object>(content);
                
                return new ObjectResult(jsonObject)
                {
                    StatusCode = (int)response.StatusCode
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse JSON response, returning as string");
                // If parsing fails, return as string
            }
        }
        
        // Return as string content
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = contentType
        };
    }

    private void CopyProxyResponseHeaders(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                Response.Headers.Append("Set-Cookie", cookie);
            }
        }
    }
}

