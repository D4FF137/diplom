using Shared.Models;

namespace ChatService.Services;

public interface IChatMemberService
{
    Task<ChatMember> AddMemberAsync(string chatId, int userId, int companyId);
    Task<bool> RemoveMemberAsync(string chatId, int userId, int companyId);
    Task<List<ChatMember>> GetByChatIdAsync(string chatId, int companyId);
    Task<bool> IsMemberAsync(string chatId, int userId, int companyId);
}






