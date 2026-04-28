using Shared.Models;

namespace ChatService.Services;

public interface IChatService
{
    Task<Chat?> GetByIdAsync(string id, int companyId);
    Task<Chat?> GetByIdInternalAsync(string id); // For internal service-to-service calls
    Task<List<Chat>> GetByCompanyIdAsync(int companyId);
    Task<Chat> CreateAsync(Chat chat);
    Task<bool> DeleteAsync(string id, int companyId);
    Task<Chat?> FindPrivateChatBetweenUsersAsync(int userId1, int userId2, int companyId);
}


