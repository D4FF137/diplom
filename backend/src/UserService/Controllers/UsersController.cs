using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.Services;
using Shared.Models;
using Shared.Common;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly JwtHelper _jwtHelper;
    private readonly IFileService _fileService;
    private readonly ICompanyGroupService _groupService;
    private readonly IConfiguration _config;

    public UsersController(
        IUserService userService,
        JwtHelper jwtHelper,
        IFileService fileService,
        ICompanyGroupService groupService,
        IConfiguration config)
    {
        _userService = userService;
        _jwtHelper = jwtHelper;
        _fileService = fileService;
        _groupService = groupService;
        _config = config;
    }

    private static User ToSafe(User u) => new()
    {
        Id = u.Id,
        CompanyId = u.CompanyId,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        AvatarUrl = u.AvatarUrl,
        Role = u.Role,
        IsBlocked = u.IsBlocked,
        CreatedAt = u.CreatedAt
    };

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var users = await _userService.GetByCompanyIdAsync(companyId.Value);
        return Ok(users.Select(ToSafe).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var user = await _userService.GetByIdAsync(id, companyId.Value);
        if (user == null) return NotFound();
        return Ok(ToSafe(user));
    }

    /// <summary>
    /// Admin only (X-Admin-Secret): create user with CompanyId + Role. Boss only (JWT): use POST /api/users/members.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
    {
        var adminSecret = _config["ADMIN_SECRET"];
        var headerSecret = Request.Headers["X-Admin-Secret"].FirstOrDefault();

        if (!string.IsNullOrEmpty(adminSecret) && headerSecret == adminSecret)
        {
            if (request == null || request.CompanyId <= 0 || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "CompanyId, Email, Password required" });
            var role = string.IsNullOrWhiteSpace(request.Role) ? "Worker" : request.Role.Trim();
            if (role != "Boss" && role != "Worker") role = "Worker";
            var user = new User
            {
                CompanyId = request.CompanyId,
                Email = request.Email.Trim(),
                FirstName = request.FirstName ?? "",
                LastName = request.LastName ?? "",
                Role = role
            };
            var created = await _userService.CreateAsync(user, request.Password);
            if (created.Role == "Boss")
            {
                await _groupService.AddBossToDepartmentChatsAsync(created.CompanyId, created.Id);
            }
            return CreatedAtAction(nameof(GetUser), new { id = created.Id }, ToSafe(created));
        }

        if (!User.Identity?.IsAuthenticated ?? true)
            return Unauthorized(new { message = "Unauthorized" });
        if (_jwtHelper.GetRoleFromToken(User) != "Boss")
            return Forbid();

        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email, Password required" });

        var existing = await _userService.GetByEmailAsync(request.Email.Trim());
        if (existing != null)
            return BadRequest(new { message = "User with this email already exists" });

        var requestedRole = request.Role?.Trim() == "Boss" ? "Boss" : "Worker";
        var member = new User
        {
            CompanyId = companyId.Value,
            Email = request.Email.Trim(),
            FirstName = request.FirstName ?? "",
            LastName = request.LastName ?? "",
            Role = requestedRole
        };
        var createdMember = await _userService.CreateAsync(member, request.Password);
        if (createdMember.Role == "Boss")
        {
            await _groupService.AddBossToDepartmentChatsAsync(companyId.Value, createdMember.Id);
        }
        if (request.GroupIds?.Any() == true)
        {
            await _groupService.AddMemberToGroupsAsync(companyId.Value, createdMember.Id, request.GroupIds);
        }
        return CreatedAtAction(nameof(GetUser), new { id = createdMember.Id }, ToSafe(createdMember));
    }

    /// <summary>
    /// Boss only: add member to own organization. Alternative to POST /api/users with JWT.
    /// </summary>
    [HttpPost("members")]
    public async Task<ActionResult<User>> CreateMember([FromBody] BossCreateMemberRequest request)
    {
        if (_jwtHelper.GetRoleFromToken(User) != "Boss")
            return Forbid();
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email, Password required" });

        var existing = await _userService.GetByEmailAsync(request.Email.Trim());
        if (existing != null)
            return BadRequest(new { message = "User with this email already exists" });

        var requestedRole = request.Role?.Trim() == "Boss" ? "Boss" : "Worker";
        var member = new User
        {
            CompanyId = companyId.Value,
            Email = request.Email.Trim(),
            FirstName = request.FirstName ?? "",
            LastName = request.LastName ?? "",
            Role = requestedRole
        };
        var created = await _userService.CreateAsync(member, request.Password);
        if (created.Role == "Boss")
        {
            await _groupService.AddBossToDepartmentChatsAsync(companyId.Value, created.Id);
        }
        if (request.GroupIds?.Any() == true)
        {
            await _groupService.AddMemberToGroupsAsync(companyId.Value, created.Id, request.GroupIds);
        }
        return CreatedAtAction(nameof(GetUser), new { id = created.Id }, ToSafe(created));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email ?? string.Empty
        };

        var updated = await _userService.UpdateAsync(id, companyId.Value, user);
        if (updated == null) return NotFound();
        return Ok(ToSafe(updated));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var deleted = await _userService.DeleteAsync(id, companyId.Value);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<User>>> SearchUsers([FromQuery] string? q)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<User>());

        var users = await _userService.SearchAsync(companyId.Value, q);
        return Ok(users.Select(ToSafe).ToList());
    }

    [HttpGet("me")]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (!companyId.HasValue || userId == 0) return Unauthorized();

        var user = await _userService.GetByIdAsync(userId, companyId.Value);
        if (user == null) return NotFound();
        return Ok(ToSafe(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<User>> UpdateCurrentUser([FromForm] UpdateProfileRequest request)
    {
        try
        {
            var companyId = _jwtHelper.GetCompanyIdFromToken(User);
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (!companyId.HasValue || userId == 0) return Unauthorized();

            string? avatarUrl = null;
            if (request.Avatar != null && request.Avatar.Length > 0)
            {
                try
                {
                    var fileName = await _fileService.SaveAvatarAsync(request.Avatar, userId);
                    var url = _fileService.GetAvatarUrl(fileName);
                    if (!string.IsNullOrEmpty(url)) avatarUrl = url;
                }
                catch (ArgumentException ex) { return BadRequest(ex.Message); }
                catch (Exception) { return StatusCode(500, "Error saving avatar"); }
            }

            var existing = await _userService.GetByIdAsync(userId, companyId.Value);
            if (existing == null) return NotFound();

            var user = new User
            {
                FirstName = !string.IsNullOrEmpty(request.FirstName) ? request.FirstName : existing.FirstName,
                LastName = !string.IsNullOrEmpty(request.LastName) ? request.LastName : existing.LastName,
                AvatarUrl = avatarUrl ?? existing.AvatarUrl
            };

            var updated = await _userService.UpdateAsync(userId, companyId.Value, user);
            if (updated == null) return NotFound();
            return Ok(ToSafe(updated));
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (!companyId.HasValue || userId == 0) return Unauthorized();

        var ok = await _userService.ChangePasswordAsync(userId, companyId.Value, request.OldPassword, request.NewPassword);
        if (!ok) return BadRequest("Invalid old password");
        return NoContent();
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(int id)
    {
        if (_jwtHelper.GetRoleFromToken(User) != "Boss") return Forbid();
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (id == currentUserId) return BadRequest(new { message = "Cannot block yourself" });

        var ok = await _userService.BlockAsync(id, companyId.Value);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/unblock")]
    public async Task<IActionResult> Unblock(int id)
    {
        if (_jwtHelper.GetRoleFromToken(User) != "Boss") return Forbid();
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var ok = await _userService.UnblockAsync(id, companyId.Value);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/password")]
    public async Task<IActionResult> SetPassword(int id, [FromBody] SetPasswordRequest request)
    {
        if (_jwtHelper.GetRoleFromToken(User) != "Boss") return Forbid();
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "NewPassword required" });

        var ok = await _userService.SetPasswordByBossAsync(id, companyId.Value, request.NewPassword);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("import")]
    public async Task<ActionResult<List<ImportMemberResult>>> ImportMembers([FromBody] List<ImportMemberItem> requests)
    {
        if (_jwtHelper.GetRoleFromToken(User) != "Boss")
            return Forbid();
        var companyId = _jwtHelper.GetCompanyIdFromToken(User);
        if (!companyId.HasValue) return Unauthorized();

        var results = new List<ImportMemberResult>();
        foreach (var req in requests)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email))
                {
                    results.Add(new ImportMemberResult { Email = "Unknown", Error = "Email required", Success = false });
                    continue;
                }

                var existing = await _userService.GetByEmailAsync(req.Email.Trim());
                if (existing != null)
                {
                    results.Add(new ImportMemberResult { Email = req.Email.Trim(), Error = "Already exists", Success = false });
                    continue;
                }

                var password = GenerateRandomPassword(10);
                var member = new User
                {
                    CompanyId = companyId.Value,
                    Email = req.Email.Trim(),
                    FirstName = req.FirstName ?? "",
                    LastName = req.LastName ?? "",
                    Role = "Worker"
                };

                await _userService.CreateAsync(member, password);
                results.Add(new ImportMemberResult { Email = req.Email.Trim(), Password = password, Success = true });
            }
            catch (Exception ex)
            {
                results.Add(new ImportMemberResult { Email = req.Email, Error = ex.Message, Success = false });
            }
        }
        return Ok(results);
    }

    private static string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

public class ImportMemberItem
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class ImportMemberResult
{
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? Error { get; set; }
    public bool Success { get; set; }
}

public class CreateUserRequest
{
    public int CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public List<int>? GroupIds { get; set; }
}

public class BossCreateMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public List<int>? GroupIds { get; set; }
}

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public IFormFile? Avatar { get; set; }
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class SetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
