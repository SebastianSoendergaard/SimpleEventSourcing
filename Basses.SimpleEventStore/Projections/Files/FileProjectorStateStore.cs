using Basses.SimpleEventStore.EventSubscriber.Files;

namespace Basses.SimpleEventStore.Projections.Files;

public class FileProjectorStateStore : FileEventSubscriberStateStore, IProjectorStateStore
{
    public FileProjectorStateStore(string fileDirectoryPath) : base(fileDirectoryPath)
    {
    }
}
