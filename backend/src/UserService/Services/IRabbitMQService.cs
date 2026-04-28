namespace UserService.Services;

public interface IRabbitMQService
{
    Task PublishUserCreatedAsync(int userId, int companyId, string email);
}


