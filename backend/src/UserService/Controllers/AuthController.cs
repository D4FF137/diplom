using Microsoft.AspNetCore.Mvc;
using UserService.Services;
using Shared.Common;
using Shared.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly JwtHelper _jwtHelper;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IUserService userService,
        JwtHelper jwtHelper,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _userService = userService;
        _jwtHelper = jwtHelper;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userService.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists" });
        }

        var user = new User
        {
            CompanyId = request.CompanyId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var createdUser = await _userService.CreateAsync(user, request.Password);

        var token = _jwtHelper.GenerateToken(createdUser.Id, createdUser.CompanyId, createdUser.Email, createdUser.Role);
        AppendAuthCookie(token);

        return Ok(new RegisterResponse
        {
            UserId = createdUser.Id,
            CompanyId = createdUser.CompanyId,
            Email = createdUser.Email,
            FirstName = createdUser.FirstName,
            LastName = createdUser.LastName,
            Role = createdUser.Role
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        if (user.IsBlocked)
        {
            return Unauthorized(new { message = "Account is blocked" });
        }

        var isValidPassword = await _userService.ValidatePasswordAsync(request.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = _jwtHelper.GenerateToken(user.Id, user.CompanyId, user.Email, user.Role);
        AppendAuthCookie(token);

        return Ok(new LoginResponse
        {
            UserId = user.Id,
            CompanyId = user.CompanyId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthTokenHelper.CookieName, BuildCookieOptions(DateTimeOffset.UtcNow.AddDays(-1)));
        return NoContent();
    }

    private void AppendAuthCookie(string token)
    {
        Response.Cookies.Append(AuthTokenHelper.CookieName, token, BuildCookieOptions());
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset? expires = null)
    {
        var sameSite = SameSiteMode.Strict;
        var configuredSameSite = _configuration["AUTH_COOKIE_SAME_SITE"];

        if (!string.IsNullOrWhiteSpace(configuredSameSite) &&
            Enum.TryParse<SameSiteMode>(configuredSameSite, true, out var parsedSameSite))
        {
            sameSite = parsedSameSite;
        }

        var secureFromConfig = _configuration.GetValue<bool?>("AUTH_COOKIE_SECURE");
        var configuredDomain = _configuration["AUTH_COOKIE_DOMAIN"];
        var expirationMinutes = _configuration.GetValue<int?>("JWT_EXPIRATION_MINUTES") ?? 60;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secureFromConfig ?? (_environment.IsProduction() || Request.IsHttps),
            SameSite = sameSite,
            Path = "/",
            Domain = string.IsNullOrWhiteSpace(configuredDomain) ? null : configuredDomain,
            Expires = expires ?? DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        };
    }
}

public class RegisterRequest
{
    public int CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
}

