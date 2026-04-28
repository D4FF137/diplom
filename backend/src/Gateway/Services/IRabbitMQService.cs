namespace Gateway.Services;

public interface IRabbitMQService
{
    Task PublishToQueueAsync(string routingKey, object message);
}


