namespace FeedService.Services;

public interface IRabbitMQService
{
    Task PublishPostCreatedAsync(int postId, int companyId, int userId, string content);
}


