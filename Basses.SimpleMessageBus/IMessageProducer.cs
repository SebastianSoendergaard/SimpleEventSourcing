namespace Basses.SimpleMessageBus;

public interface IMessageProducer
{
    Task<bool> SendMessage<T>(string topic, string messageName, T message) where T : class;
}
