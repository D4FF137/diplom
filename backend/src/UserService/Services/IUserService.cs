using Shared.Models;

namespace UserService.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id, int companyId);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user, string password);
    Task<User?> UpdateAsync(int id, int companyId, User user);
    Task<bool> DeleteAsync(int id, int companyId);
    Task<List<User>> GetByCompanyIdAsync(int companyId);
    Task<List<User>> SearchAsync(int companyId, string query);
    Task<bool> ValidatePasswordAsync(string password, string hash);
    Task<bool> ChangePasswordAsync(int userId, int companyId, string oldPassword, string newPassword);
    Task<bool> BlockAsync(int userId, int companyId);
    Task<bool> UnblockAsync(int userId, int companyId);
    Task<bool> SetPasswordByBossAsync(int userId, int companyId, string newPassword);
    Task UpdateLastSeenAsync(int userId, int companyId, DateTime lastSeen);
}


