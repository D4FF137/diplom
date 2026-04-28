namespace ChatService.Services;

public interface IRabbitMQService
{
    Task PublishMessageSentAsync(string messageId, int companyId, string chatId, int userId, string content, string? attachmentUrl);
    Task PublishMessageUpdatedAsync(string messageId, int companyId, string chatId, string content);
    Task PublishMessageDeletedAsync(string messageId, int companyId, string chatId);
}


