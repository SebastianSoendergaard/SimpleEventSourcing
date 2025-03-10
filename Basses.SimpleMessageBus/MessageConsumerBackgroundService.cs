using Microsoft.Extensions.Hosting;

namespace Basses.SimpleMessageBus;

internal class MessageConsumerBackgroundService(IMessageConsumer _messageConsumer) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _messageConsumer.RunAsync(stoppingToken);
    }
}
