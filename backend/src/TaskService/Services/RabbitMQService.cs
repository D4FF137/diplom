using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Shared.Contracts;

namespace TaskService.Services;

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
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);
        }
    }

    public Task PublishTaskCreatedAsync(int taskId, int companyId, int creatorId, int? targetGroupId, int? targetUserId, string title)
    {
        try
        {
            EnsureConnection();
            
            var message = new TaskCreatedEvent
            {
                TaskId = taskId,
                CompanyId = companyId,
                CreatorId = creatorId,
                TargetGroupId = targetGroupId,
                TargetUserId = targetUserId,
                Title = title
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            _channel!.BasicPublish(
                exchange: _exchangeName,
                routingKey: "task.created",
                basicProperties: null,
                body: body
            );
        }
        catch (Exception)
        {
            // Fail silently or log
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
