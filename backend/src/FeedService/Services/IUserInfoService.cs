using FeedService.DTOs;

namespace FeedService.Services;

public interface IUserInfoService
{
    Task<UserDto?> GetUserInfoAsync(int userId, int companyId, string? jwtToken = null);
    Task<Dictionary<int, UserDto>> GetUsersInfoAsync(List<int> userIds, int companyId, string? jwtToken = null);
}

