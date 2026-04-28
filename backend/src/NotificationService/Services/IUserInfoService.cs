namespace NotificationService.Services;

public interface IUserInfoService
{
    Task<List<int>> GetChatMembersAsync(int chatId, int companyId, string jwtToken);
    Task<List<int>> GetCompanyUsersAsync(int companyId, string jwtToken);
}




