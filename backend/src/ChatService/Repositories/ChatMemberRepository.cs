using ChatService.Models;
using MongoDB.Driver;

namespace ChatService.Repositories;

public class ChatMemberRepository : IChatMemberRepository
{
    private readonly IMongoCollection<ChatMember> _members;

    public ChatMemberRepository(IMongoDatabase database)
    {
        _members = database.GetCollection<ChatMember>("chat_members");
    }

    public async Task AddMemberAsync(ChatMember member)
    {
        await _members.InsertOneAsync(member);
    }

    public async Task RemoveMemberAsync(string chatId, int userId)
    {
        await _members.DeleteOneAsync(m => m.ChatId == chatId && m.UserId == userId);
    }

    public async Task<List<ChatMember>> GetByChatIdAsync(string chatId)
    {
        return await _members.Find(m => m.ChatId == chatId).ToListAsync();
    }

    public async Task<ChatMember?> GetMemberAsync(string chatId, int userId)
    {
        return await _members.Find(m => m.ChatId == chatId && m.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<bool> IsMemberAsync(string chatId, int userId)
    {
        return await _members.Find(m => m.ChatId == chatId && m.UserId == userId).AnyAsync();
    }
}
