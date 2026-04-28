
using Shared.Models;
using ChatService.Repositories;
using DbChat = ChatService.Models.Chat;

namespace ChatService.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _repository;

    public ChatService(IChatRepository repository)
    {
        _repository = repository;
    }

    private Chat MapToShared(DbChat dbChat)
    {
        return new Chat
        {
            Id = dbChat.Id,
            CompanyId = dbChat.CompanyId,
            Name = dbChat.Name,
            Type = dbChat.Type,
            CreatedAt = dbChat.CreatedAt,
            CreatorId = dbChat.CreatorId
        };
    }

    private DbChat MapToDb(Chat chat)
    {
        return new DbChat
        {
            Id = chat.Id,
            CompanyId = chat.CompanyId,
            Name = chat.Name,
            Type = chat.Type,
            CreatedAt = chat.CreatedAt,
            CreatorId = chat.CreatorId,
            // MemberIds and LastMessage are empty/null for new chats from shared model
            MemberIds = new List<int>(),
            LastMessage = null
        };
    }

    public async Task<Chat?> GetByIdAsync(string id, int companyId)
    {
        var chat = await _repository.GetByIdAsync(id);
        if (chat == null || chat.CompanyId != companyId)
            return null;
        
        return MapToShared(chat);
    }

    public async Task<Chat?> GetByIdInternalAsync(string id)
    {
        var chat = await _repository.GetByIdAsync(id);
        return chat != null ? MapToShared(chat) : null;
    }

    public async Task<List<Chat>> GetByCompanyIdAsync(int companyId)
    {
        var chats = await _repository.GetByCompanyIdAsync(companyId);
        return chats.Select(MapToShared).ToList();
    }

    public async Task<Chat> CreateAsync(Chat chat)
    {
        if (string.IsNullOrEmpty(chat.Id))
        {
            chat.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        }
        chat.CreatedAt = DateTime.UtcNow;
        
        var dbChat = MapToDb(chat);
        await _repository.CreateAsync(dbChat);
        
        chat.Id = dbChat.Id; // Ensure ID is back in the shared model
        return chat;
    }

    public async Task<bool> DeleteAsync(string id, int companyId)
    {
        var chat = await _repository.GetByIdAsync(id);
        if (chat == null || chat.CompanyId != companyId) return false;

        await _repository.DeleteAsync(id);
        return true;
    }

    public async Task<Chat?> FindPrivateChatBetweenUsersAsync(int userId1, int userId2, int companyId)
    {
        var chat = await _repository.FindPrivateChatBetweenUsersAsync(userId1, userId2, companyId);
        return chat != null ? MapToShared(chat) : null;
    }
}


