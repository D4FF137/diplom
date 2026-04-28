using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Shared.Contracts;

namespace FeedService.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly string _exchangeName = "corporate_social_network";
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new object();

    public RabbitMQService(IConfiguration configuration)
    {
        _configuration = configuration;
        // Lazy initialization - connection will be created on first use
    }

    private void EnsureConnection()
    {
        if (_connection != null && _connection.IsOpen) return;

        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen) return;

            var hostName = _configuration["RABBITMQ_HOST"] ?? "rabbitmq";
            var port = int.Parse(_configuration["RABBITMQ_PORT"] ?? "5672");
            var userName = _configuration["RABBITMQ_USER"] ?? "guest";
            var password = _configuration["RABBITMQ_PASSWORD"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            // Retry connection with exponential backoff
            var maxRetries = 5;
            var retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);
                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        throw new InvalidOperationException($"Failed to connect to RabbitMQ after {maxRetries} attempts", ex);
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                }
            }
        }
    }

    public Task PublishPostCreatedAsync(int postId, int companyId, int userId, string content)
    {
        try
        {
            EnsureConnection();
            
            var message = new PostCreatedEvent
            {
                PostId = postId,
                CompanyId = companyId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            _channel!.BasicPublish(
                exchange: _exchangeName,
                routingKey: "post.created",
                basicProperties: null,
                body: body
            );
        }
        catch (Exception)
        {
            // Log error but don't fail the request if RabbitMQ is unavailable
            // In production, you might want to use a fallback mechanism
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}


