using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;
using System.Text;
using System.Text.Json;

namespace NotificationService.Services;

public class RabbitMQConsumerService : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _exchangeName = "corporate_social_network";
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly IConfiguration _configuration;

    public RabbitMQConsumerService(
        IConfiguration configuration,
        ILogger<RabbitMQConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    private async Task<bool> ConnectToRabbitMQAsync(CancellationToken cancellationToken)
    {
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

        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(3);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to RabbitMQ... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);

                var queueName = "notifications_queue";
                _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queueName, _exchangeName, "message.sent");
                _channel.QueueBind(queueName, _exchangeName, "message.updated");
                _channel.QueueBind(queueName, _exchangeName, "message.deleted");
                _channel.QueueBind(queueName, _exchangeName, "post.created");
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation("Successfully connected to RabbitMQ");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries}). Error: {Error}",
                    i + 1, maxRetries, ex.Message);

                if (i == maxRetries - 1)
                {
                    _logger.LogError("Failed to connect to RabbitMQ after {MaxRetries} attempts", maxRetries);
                    return false;
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await ConnectToRabbitMQAsync(stoppingToken))
        {
            _logger.LogError("Failed to connect to RabbitMQ. Consumer will not start.");
            return;
        }

        if (_channel == null)
        {
            _logger.LogError("Channel is null. Consumer will not start.");
            return;
        }

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                if (routingKey == "message.sent")
                {
                    var eventData = JsonSerializer.Deserialize<MessageSentEvent>(message);
                    if (eventData != null)
                    {
                        await HandleMessageSentAsync(eventData);
                    }
                }
                else if (routingKey == "message.updated")
                {
                    var eventData = JsonSerializer.Deserialize<MessageUpdatedEvent>(message);
                    if (eventData != null)
                    {
                        await HandleMessageUpdatedAsync(eventData);
                    }
                }
                else if (routingKey == "message.deleted")
                {
                    var eventData = JsonSerializer.Deserialize<MessageDeletedEvent>(message);
                    if (eventData != null)
                    {
                        await HandleMessageDeletedAsync(eventData);
                    }
                }
                else if (routingKey == "post.created")
                {
                    var eventData = JsonSerializer.Deserialize<PostCreatedEvent>(message);
                    if (eventData != null)
                    {
                        await HandlePostCreatedAsync(eventData);
                    }
                }

                _channel?.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from RabbitMQ");
                _channel?.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume("notifications_queue", false, consumer);

        _logger.LogInformation("RabbitMQ consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private static string GetMessageSentEventKey(MessageSentEvent eventData)
    {
        return $"{eventData.CompanyId}:{eventData.ChatId}:{eventData.MessageId}";
    }

    private static string GetPostCreatedEventKey(PostCreatedEvent eventData)
    {
        return $"{eventData.CompanyId}:{eventData.PostId}";
    }

    private async Task HandleMessageSentAsync(MessageSentEvent eventData)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        _logger.LogInformation(
            "[RabbitMQ] Message sent event received: ChatId={ChatId}, UserId={UserId}, CompanyId={CompanyId}",
            eventData.ChatId,
            eventData.UserId,
            eventData.CompanyId);

        try
        {
            var chatServiceUrl = configuration["CHAT_SERVICE_URL"] ?? "http://chatservice:5004";
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{chatServiceUrl}/api/internal/chats/{eventData.ChatId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "[RabbitMQ] Failed to get chat members for chat {ChatId}: {StatusCode}, URL: {Url}, Content: {Content}",
                    eventData.ChatId,
                    response.StatusCode,
                    $"{chatServiceUrl}/api/internal/chats/{eventData.ChatId}",
                    errorContent);
                throw new InvalidOperationException($"Failed to get chat members for chat {eventData.ChatId}: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("[RabbitMQ] Received chat data: {Content}", content);

            var chat = JsonSerializer.Deserialize<ChatResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var recipientUserIds = chat?.Members?
                .Where(member => member.Id != eventData.UserId)
                .Select(member => member.Id)
                .ToArray() ?? Array.Empty<int>();

            var eventKey = GetMessageSentEventKey(eventData);
            var processed = await notificationService.IncrementChatUnreadForEventAsync(
                eventKey,
                eventData.ChatId,
                eventData.CompanyId,
                recipientUserIds);

            _logger.LogInformation(
                "[RabbitMQ] message.sent event {EventKey} processed={Processed}, recipients={RecipientCount}",
                eventKey,
                processed,
                recipientUserIds.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message sent event for chat {ChatId}", eventData.ChatId);
            throw;
        }
    }

    private async Task HandlePostCreatedAsync(PostCreatedEvent eventData)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        _logger.LogInformation(
            "[RabbitMQ] Post created event received: PostId={PostId}, UserId={UserId}, CompanyId={CompanyId}",
            eventData.PostId,
            eventData.UserId,
            eventData.CompanyId);

        try
        {
            var userServiceUrl = configuration["USER_SERVICE_URL"] ?? "http://userservice:5001";
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{userServiceUrl}/api/internal/users?companyId={eventData.CompanyId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "[RabbitMQ] Failed to get company users for company {CompanyId}: {StatusCode}, URL: {Url}, Content: {Content}",
                    eventData.CompanyId,
                    response.StatusCode,
                    $"{userServiceUrl}/api/internal/users?companyId={eventData.CompanyId}",
                    errorContent);
                throw new InvalidOperationException($"Failed to get company users for company {eventData.CompanyId}: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var recipientUserIds = users?
                .Where(user => user.Id != eventData.UserId)
                .Select(user => user.Id)
                .ToArray() ?? Array.Empty<int>();

            var eventKey = GetPostCreatedEventKey(eventData);
            var processed = await notificationService.IncrementFeedUnreadForEventAsync(
                eventKey,
                eventData.CompanyId,
                recipientUserIds);

            _logger.LogInformation(
                "[RabbitMQ] post.created event {EventKey} processed={Processed}, recipients={RecipientCount}",
                eventKey,
                processed,
                recipientUserIds.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling post created event for post {PostId}", eventData.PostId);
            throw;
        }
    }

    private Task HandleMessageUpdatedAsync(MessageUpdatedEvent eventData)
    {
        _logger.LogInformation("[RabbitMQ] Message updated event received: MessageId={MessageId}, ChatId={ChatId}", eventData.MessageId, eventData.ChatId);
        return Task.CompletedTask;
    }

    private Task HandleMessageDeletedAsync(MessageDeletedEvent eventData)
    {
        _logger.LogInformation("[RabbitMQ] Message deleted event received: MessageId={MessageId}, ChatId={ChatId}", eventData.MessageId, eventData.ChatId);
        return Task.CompletedTask;
    }

    private class ChatResponse
    {
        public string Id { get; set; } = string.Empty;
        public List<MemberResponse>? Members { get; set; }
    }

    private class MemberResponse
    {
        public int Id { get; set; }
    }

    private class UserResponse
    {
        public int Id { get; set; }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMQ channel");
        }

        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMQ connection");
        }

        base.Dispose();
    }
}
