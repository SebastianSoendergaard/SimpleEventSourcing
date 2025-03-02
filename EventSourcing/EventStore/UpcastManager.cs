namespace Basses.SimpleEventStore.EventStore;

public class UpcastManager
{
    private readonly IEventSerializer _serializer;
    private readonly List<IUpcaster> _upcasters = [];

    public UpcastManager(IEventSerializer serializer)
    {
        _serializer = serializer;
    }

    public void RegisterUpcaster(IUpcaster upcaster)
    {
        _upcasters.Add(upcaster);
    }

    public object Deserialize(string eventPayload, string eventType)
    {
        var upCaster = _upcasters.FirstOrDefault(x => x.CanUpcast(eventType));
        if (upCaster == null)
        {
            var deserializedEvent = _serializer.Deserialize(eventPayload, eventType);
            return deserializedEvent;
        }

        return upCaster.DoUpcast(eventType, eventPayload, _serializer);
    }
}
