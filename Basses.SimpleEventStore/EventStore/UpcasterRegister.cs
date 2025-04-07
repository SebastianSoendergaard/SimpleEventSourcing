using Basses.SimpleEventStore.EventStore;

namespace Basses.SimpleEventStore.EventSubscriber;

public class UpcasterRegister
{
    private readonly List<IUpcaster> _upcasters = [];

    public void RegisterUpcaster(IUpcaster upcaster)
    {
        _upcasters.Add(upcaster);
    }

    internal IEnumerable<IUpcaster> Upcasters { get { return _upcasters; } }
}
