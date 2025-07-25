using Basses.SimpleEventStore.Projections;
using Basses.SimpleEventStore.Projections.Files;

namespace Basses.SimpleEventStore.Tests.Projections;

public class FileStoreFixture : IDisposable
{
    private readonly string _directoryPrefix = @"c:/temp/eventstore_projections_test_";
    private readonly List<string> _names = [];

    public IProjectorStateStore CreateEventStore()
    {
        var name = Guid.NewGuid().ToString()[..8];
        _names.Add(name);
        return new FileProjectorStateStore(_directoryPrefix + name);
    }

    public void Dispose()
    {
        foreach (var name in _names)
        {
            Directory.Delete(_directoryPrefix + name, true);
        }

        _names.Clear();
    }
}
