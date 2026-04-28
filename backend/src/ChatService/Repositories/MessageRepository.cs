using ChatService.Models;
using MongoDB.Driver;

namespace ChatService.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly IMongoCollection<Message> _messages;

    public MessageRepository(IMongoDatabase database)
    {
        _messages = database.GetCollection<Message>("messages");
    }

    public async Task<Message?> GetByIdAsync(string id)
    {
        return await _messages.Find(m => m.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Message>> GetByChatIdAsync(string chatId, int skip, int take)
    {
        return await _messages.Find(m => m.ChatId == chatId)
            .SortByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();
    }

    public async Task<List<Message>> SearchAsync(string chatId, string query, int skip, int take)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.ChatId, chatId),
            Builders<Message>.Filter.Regex(m => m.Content, new MongoDB.Bson.BsonRegularExpression(query, "i"))
        );

        return await _messages.Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Limit(take)
            .ToListAsync();
    }

    public async Task CreateAsync(Message message)
    {
        await _messages.InsertOneAsync(message);
    }

    public async Task UpdateAsync(Message message)
    {
        await _messages.ReplaceOneAsync(m => m.Id == message.Id, message);
    }

    public async Task DeleteAsync(string id)
    {
        await _messages.DeleteOneAsync(m => m.Id == id);
    }
}
