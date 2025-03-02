namespace Basses.SimpleEventStore.EventStore;

public interface IUpcaster
{
    bool CanUpcast(string eventType);
    object DoUpcast(string eventType, string eventJson, IEventSerializer serializer);
}
