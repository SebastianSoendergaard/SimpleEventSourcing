namespace Basses.SimpleMessageBus;

public interface IMessageConsumer
{
    bool Subscribe<T>(string topic, string messageName, Func<T, Task> onMessage) where T : class;
    Task RunAsync(CancellationToken stoppingToken);
}
