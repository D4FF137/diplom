using ChatService.Models;
using MongoDB.Driver;

namespace ChatService.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly IMongoCollection<Chat> _chats;

    public ChatRepository(IMongoDatabase database)
    {
        _chats = database.GetCollection<Chat>("chats");
    }

    public async Task<Chat?> GetByIdAsync(string id)
    {
        return await _chats.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Chat>> GetByCompanyIdAsync(int companyId)
    {
        return await _chats.Find(c => c.CompanyId == companyId).ToListAsync();
    }

    public async Task CreateAsync(Chat chat)
    {
        await _chats.InsertOneAsync(chat);
    }

    public async Task DeleteAsync(string id)
    {
        await _chats.DeleteOneAsync(c => c.Id == id);
    }

    public async Task<Chat?> FindPrivateChatBetweenUsersAsync(int userId1, int userId2, int companyId)
    {
        var filter = Builders<Chat>.Filter.And(
            Builders<Chat>.Filter.Eq(c => c.CompanyId, companyId),
            Builders<Chat>.Filter.Eq(c => c.Type, "private"),
            Builders<Chat>.Filter.Size(c => c.MemberIds, 2),
            Builders<Chat>.Filter.All(c => c.MemberIds, new[] { userId1, userId2 })
        );

        return await _chats.Find(filter).FirstOrDefaultAsync();
    }

    public async Task AddMemberIdAsync(string chatId, int userId)
    {
        var update = Builders<Chat>.Update.AddToSet(c => c.MemberIds, userId);
        await _chats.UpdateOneAsync(c => c.Id == chatId, update);
    }

    public async Task RemoveMemberIdAsync(string chatId, int userId)
    {
        var update = Builders<Chat>.Update.Pull(c => c.MemberIds, userId);
        await _chats.UpdateOneAsync(c => c.Id == chatId, update);
    }
}
