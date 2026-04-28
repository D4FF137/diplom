
using Shared.Models;
using ChatService.Repositories;
using ChatService.Hubs;
using Microsoft.AspNetCore.SignalR;
using DbMessage = ChatService.Models.Message;
using DbPollData = ChatService.Models.PollData;
using DbPollOption = ChatService.Models.PollOption;
using DbReaction = ChatService.Models.MessageReaction;

namespace ChatService.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _repository;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessageService(IMessageRepository repository, IRabbitMQService rabbitMQService, IHubContext<ChatHub> hubContext)
    {
        _repository = repository;
        _rabbitMQService = rabbitMQService;
        _hubContext = hubContext;
    }

    private Message MapToShared(DbMessage dbMessage)
    {
        return new Message
        {
            Id = dbMessage.Id,
            CompanyId = dbMessage.CompanyId,
            ChatId = dbMessage.ChatId,
            UserId = dbMessage.SenderId,
            Content = dbMessage.Content,
            AttachmentUrl = dbMessage.AttachmentUrl,
            Type = dbMessage.Type,
            Poll = dbMessage.Poll != null ? new PollData
            {
                Question = dbMessage.Poll.Question,
                IsAnonymous = dbMessage.Poll.IsAnonymous,
                IsMultipleChoice = dbMessage.Poll.IsMultipleChoice,
                ClosedAt = dbMessage.Poll.ClosedAt,
                Options = dbMessage.Poll.Options.Select(o => new PollOption
                {
                    Id = o.Id,
                    Text = o.Text,
                    VoterIds = o.VoterIds
                }).ToList()
            } : null,
            CreatedAt = dbMessage.CreatedAt,
            IsEdited = dbMessage.IsEdited,
            Reactions = dbMessage.Reactions.Select(r => new MessageReaction
            {
                Emoji = r.Emoji,
                UserIds = r.UserIds
            }).ToList()
        };
    }

    private DbMessage MapToDb(Message message)
    {
        return new DbMessage
        {
            Id = message.Id,
            CompanyId = message.CompanyId,
            ChatId = message.ChatId,
            SenderId = message.UserId,
            Content = message.Content,
            AttachmentUrl = message.AttachmentUrl,
            Type = message.Type ?? "text",
            Poll = message.Poll != null ? new DbPollData
            {
                Question = message.Poll.Question,
                IsAnonymous = message.Poll.IsAnonymous,
                IsMultipleChoice = message.Poll.IsMultipleChoice,
                ClosedAt = message.Poll.ClosedAt,
                Options = message.Poll.Options.Select(o => new DbPollOption
                {
                    Id = o.Id,
                    Text = o.Text,
                    VoterIds = o.VoterIds
                }).ToList()
            } : null,
            CreatedAt = message.CreatedAt,
            IsEdited = message.IsEdited,
            Reactions = message.Reactions.Select(r => new DbReaction
            {
                Emoji = r.Emoji,
                UserIds = r.UserIds
            }).ToList()
        };
    }

    public async Task<Message?> GetByIdAsync(string id, int companyId)
    {
        var message = await _repository.GetByIdAsync(id);
        if (message == null || message.CompanyId != companyId)
            return null;
            
        return MapToShared(message);
    }

    public async Task<List<Message>> GetByChatIdAsync(string chatId, int companyId, int skip = 0, int take = 50)
    {
        var messages = await _repository.GetByChatIdAsync(chatId, skip, take);
        // Ensure we filter by companyId if repository doesn't (though chatId should be unique enough, safety first)
        // Repo query filters by ChatId only.
        return messages.Where(m => m.CompanyId == companyId).Select(MapToShared).ToList();
    }

    public async Task<List<Message>> SearchMessagesAsync(string chatId, string query, int companyId, int skip = 0, int take = 50)
    {
        var messages = await _repository.SearchAsync(chatId, query, skip, take);
        return messages.Where(m => m.CompanyId == companyId).Select(MapToShared).ToList();
    }

    public async Task<Message> CreateAsync(Message message)
    {
        if (string.IsNullOrEmpty(message.Id))
        {
            message.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        }
        message.CreatedAt = DateTime.UtcNow;
        
        var dbMessage = MapToDb(message);
        await _repository.CreateAsync(dbMessage);

        await _rabbitMQService.PublishMessageSentAsync(
            message.Id, message.CompanyId, message.ChatId, message.UserId, message.Content, message.AttachmentUrl);

        return message;
    }

    public async Task<Message?> UpdateAsync(string id, string content, int companyId)
    {
        var dbMessage = await _repository.GetByIdAsync(id);
        if (dbMessage == null || dbMessage.CompanyId != companyId) return null;

        dbMessage.Content = content;
        dbMessage.IsEdited = true;
        await _repository.UpdateAsync(dbMessage);

        await _rabbitMQService.PublishMessageUpdatedAsync(id, companyId, dbMessage.ChatId, content);

        // Broadcast to SignalR
        await _hubContext.Clients.Group($"company_{companyId}_chat_{dbMessage.ChatId}")
            .SendAsync("ReceiveMessageUpdate", new { id, chatId = dbMessage.ChatId, content, isEdited = true });

        // Update last message preview in chat list
        await _hubContext.Clients.Group($"company_{companyId}")
            .SendAsync("ChatUpdated", new
            {
                chatId = dbMessage.ChatId,
                lastMessage = content,
                lastMessageAt = DateTime.UtcNow.ToString("O"),
                isEdited = true
            });

        return MapToShared(dbMessage);
    }

    public async Task<bool> DeleteAsync(string id, int companyId)
    {
        var message = await _repository.GetByIdAsync(id);
        if (message == null || message.CompanyId != companyId) return false;

        await _repository.DeleteAsync(id);
        
        await _rabbitMQService.PublishMessageDeletedAsync(id, companyId, message.ChatId);
        
        // Broadcast to SignalR
        await _hubContext.Clients.Group($"company_{companyId}_chat_{message.ChatId}")
            .SendAsync("ReceiveMessageDelete", new { id, chatId = message.ChatId });
        
        return true;
    }

    public async Task<Message?> VoteAsync(string messageId, int optionId, int userId, int companyId)
    {
        var dbMessage = await _repository.GetByIdAsync(messageId);
        if (dbMessage == null || dbMessage.CompanyId != companyId || dbMessage.Type != "poll" || dbMessage.Poll == null)
            return null;

        // Check if poll is closed
        if (dbMessage.Poll.ClosedAt.HasValue && dbMessage.Poll.ClosedAt <= DateTime.UtcNow)
            return null;

        // Remove user's previous votes if not multiple choice
        if (!dbMessage.Poll.IsMultipleChoice)
        {
            foreach (var option in dbMessage.Poll.Options)
            {
                option.VoterIds.Remove(userId);
            }
        }

        // Add new vote or toggle
        var selectedOption = dbMessage.Poll.Options.FirstOrDefault(o => o.Id == optionId);
        if (selectedOption != null)
        {
            if (!selectedOption.VoterIds.Contains(userId))
            {
                selectedOption.VoterIds.Add(userId);
            }
            else
            {
                // Toggle: remove vote if already voted
                selectedOption.VoterIds.Remove(userId);
            }
        }

        await _repository.UpdateAsync(dbMessage);

        var sharedMessage = MapToShared(dbMessage);

        // Broadcast update via SignalR
        await _hubContext.Clients.Group($"company_{companyId}_chat_{dbMessage.ChatId}")
            .SendAsync("ReceivePollUpdate", new { 
                messageId = dbMessage.Id, 
                chatId = dbMessage.ChatId, 
                poll = sharedMessage.Poll 
            });

        return sharedMessage;
    }
    public async Task<Message?> AddReactionAsync(string messageId, string emoji, int userId, int companyId)
    {
        var dbMessage = await _repository.GetByIdAsync(messageId);
        if (dbMessage == null || dbMessage.CompanyId != companyId) return null;

        var reaction = dbMessage.Reactions.FirstOrDefault(r => r.Emoji == emoji);
        if (reaction == null)
        {
            reaction = new DbReaction { Emoji = emoji, UserIds = new List<int> { userId } };
            dbMessage.Reactions.Add(reaction);
        }
        else if (!reaction.UserIds.Contains(userId))
        {
            reaction.UserIds.Add(userId);
        }

        await _repository.UpdateAsync(dbMessage);

        var sharedMessage = MapToShared(dbMessage);

        // Broadcast SignalR update
        await _hubContext.Clients.Group($"company_{companyId}_chat_{dbMessage.ChatId}")
            .SendAsync("ReceiveReactionUpdate", new
            {
                messageId = dbMessage.Id,
                chatId = dbMessage.ChatId,
                reactions = sharedMessage.Reactions
            });

        return sharedMessage;
    }

    public async Task<Message?> RemoveReactionAsync(string messageId, string emoji, int userId, int companyId)
    {
        var dbMessage = await _repository.GetByIdAsync(messageId);
        if (dbMessage == null || dbMessage.CompanyId != companyId) return null;

        var reaction = dbMessage.Reactions.FirstOrDefault(r => r.Emoji == emoji);
        if (reaction != null)
        {
            reaction.UserIds.Remove(userId);
            if (reaction.UserIds.Count == 0)
            {
                dbMessage.Reactions.Remove(reaction);
            }
            
            await _repository.UpdateAsync(dbMessage);

            var sharedMessage = MapToShared(dbMessage);

            // Broadcast SignalR update
            await _hubContext.Clients.Group($"company_{companyId}_chat_{dbMessage.ChatId}")
                .SendAsync("ReceiveReactionUpdate", new
                {
                    messageId = dbMessage.Id,
                    chatId = dbMessage.ChatId,
                    reactions = sharedMessage.Reactions
                });

            return sharedMessage;
        }

        return MapToShared(dbMessage);
    }
}
