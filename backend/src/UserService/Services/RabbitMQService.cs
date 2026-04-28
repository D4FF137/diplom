using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Shared.Contracts;

namespace UserService.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _exchangeName = "corporate_social_network";

    public RabbitMQService(IConfiguration configuration)
    {
        var hostName = configuration["RABBITMQ_HOST"] ?? "rabbitmq";
        var port = int.Parse(configuration["RABBITMQ_PORT"] ?? "5672");
        var userName = configuration["RABBITMQ_USER"] ?? "guest";
        var password = configuration["RABBITMQ_PASSWORD"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);
    }

    public Task PublishUserCreatedAsync(int userId, int companyId, string email)
    {
        var message = new UserCreatedEvent
        {
            UserId = userId,
            CompanyId = companyId,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: "user.created",
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}


