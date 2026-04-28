using Shared.Models;

namespace ChatService.Services;

public interface IMessageService
{
    Task<Message?> GetByIdAsync(string id, int companyId);
    Task<List<Message>> GetByChatIdAsync(string chatId, int companyId, int skip = 0, int take = 50);
    Task<List<Message>> SearchMessagesAsync(string chatId, string query, int companyId, int skip = 0, int take = 50);
    Task<Message> CreateAsync(Message message);
    Task<Message?> UpdateAsync(string id, string content, int companyId);
    Task<bool> DeleteAsync(string id, int companyId);
    Task<Message?> VoteAsync(string messageId, int optionId, int userId, int companyId);
    Task<Message?> AddReactionAsync(string messageId, string emoji, int userId, int companyId);
    Task<Message?> RemoveReactionAsync(string messageId, string emoji, int userId, int companyId);
}


