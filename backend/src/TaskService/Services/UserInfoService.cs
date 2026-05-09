using System.Security.Claims;

namespace TaskService.Services;

public interface IUserInfoService
{
    int GetUserId();
    int GetCompanyId();
    string? GetRole();
}

public class UserInfoServiceImplementation : IUserInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserInfoServiceImplementation(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim ?? "0");
    }

    public int GetCompanyId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst("companyId")?.Value;
        return int.Parse(claim ?? "0");
    }

    public string? GetRole()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
