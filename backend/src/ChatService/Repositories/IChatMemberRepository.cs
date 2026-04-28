using ChatService.Models;

namespace ChatService.Repositories;

public interface IChatMemberRepository
{
    Task AddMemberAsync(ChatMember member);
    Task RemoveMemberAsync(string chatId, int userId);
    Task<List<ChatMember>> GetByChatIdAsync(string chatId);
    Task<ChatMember?> GetMemberAsync(string chatId, int userId);
    Task<bool> IsMemberAsync(string chatId, int userId);
}
