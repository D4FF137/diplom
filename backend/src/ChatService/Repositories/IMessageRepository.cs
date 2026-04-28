using ChatService.Models;

namespace ChatService.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(string id);
    Task<List<Message>> GetByChatIdAsync(string chatId, int skip, int take);
    Task<List<Message>> SearchAsync(string chatId, string query, int skip, int take);
    Task CreateAsync(Message message);
    Task UpdateAsync(Message message);
    Task DeleteAsync(string id);
}
