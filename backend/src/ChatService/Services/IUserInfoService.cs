using Shared.Models;

namespace ChatService.Services;

public interface IUserInfoService
{
    Task<User?> GetUserInfoAsync(int userId, int companyId, string? jwtToken = null);
    Task<Dictionary<int, User>> GetUsersInfoAsync(List<int> userIds, int companyId, string? jwtToken = null);
}





