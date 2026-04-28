using ChatService.Models;

namespace ChatService.Repositories;

public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(string id);
    Task<List<Chat>> GetByCompanyIdAsync(int companyId);
    Task CreateAsync(Chat chat);
    Task DeleteAsync(string id);
    Task<Chat?> FindPrivateChatBetweenUsersAsync(int userId1, int userId2, int companyId);
    Task AddMemberIdAsync(string chatId, int userId);
    Task RemoveMemberIdAsync(string chatId, int userId);
}
