using Basses.SimpleEventStore.EventSubscriber.Files;

namespace Basses.SimpleEventStore.Reactions.Files;

public class FileReactorStateStore : FileEventSubscriberStateStore, IReactorStateStore
{
    public FileReactorStateStore(string fileDirectoryPath) : base(fileDirectoryPath)
    {
    }
}
