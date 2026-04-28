using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services;
using Shared.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/internal")]
[AllowAnonymous]
public class InternalController : ControllerBase
{
    private readonly IUserService _userService;

    public InternalController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("users/{id}")]
    public async Task<ActionResult<User>> GetUser(int id, [FromQuery] int? companyId = null)
    {
        // If companyId is provided, we can use it for extra safety, 
        // but since it's an internal API, we can just fetch by ID.
        var user = await _userService.GetByIdAsync(id, companyId ?? 0);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<User>>> GetUsers([FromQuery] int companyId)
    {
        var users = await _userService.GetByCompanyIdAsync(companyId);
        return Ok(users);
    }

    [HttpPost("users/presence")]
    public async Task<IActionResult> UpdatePresence([FromBody] UserPresenceRequest request)
    {
        await _userService.UpdateLastSeenAsync(request.UserId, request.CompanyId, request.LastSeen);
        return Ok();
    }
}

public class UserPresenceRequest
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public DateTime LastSeen { get; set; }
}

