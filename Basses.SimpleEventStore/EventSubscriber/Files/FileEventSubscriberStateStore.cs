using System.Text.Json;

namespace Basses.SimpleEventStore.EventSubscriber.Files;

public abstract class FileEventSubscriberStateStore : IEventSubscriberStateStore
{
    private readonly string _fileDirectoryPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileEventSubscriberStateStore(string fileDirectoryPath)
    {
        _fileDirectoryPath = fileDirectoryPath;
        Directory.CreateDirectory(_fileDirectoryPath);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
    }

    public async Task<EventSubscriberProcessingState> GetProcessingState(IEventSubscriber projector)
    {
        var filename = GenerateFilename(projector);

        if (!File.Exists(filename))
        {
            throw new NotFoundException($"Projector state not found, Name: {projector.Name}");
        }

        var json = await File.ReadAllTextAsync(filename);
        var state = JsonSerializer.Deserialize<EventSubscriberProcessingState>(json) ?? new EventSubscriberProcessingState(DateTimeOffset.MinValue, 0);

        return state;
    }

    public async Task SaveProcessingState(IEventSubscriber projector, EventSubscriberProcessingState state)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        var filename = GenerateFilename(projector);
        await File.WriteAllTextAsync(filename, json);
    }

    public async Task UpsertSubscriber(IEventSubscriber projector)
    {
        var existingFilename = Directory.EnumerateFiles(_fileDirectoryPath).FirstOrDefault(f => f.Contains(projector.Name));
        var newFilename = GenerateFilename(projector);

        if (existingFilename == null)
        {
            var state = new EventSubscriberProcessingState(DateTimeOffset.MinValue, 0);
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            await File.WriteAllTextAsync(newFilename, json);
            return;
        }

        if (newFilename != existingFilename)
        {
            var existingState = await File.ReadAllTextAsync(existingFilename);
            await File.WriteAllTextAsync(newFilename, existingState);
            File.Delete(existingFilename);
        }
    }

    private string GenerateFilename(IEventSubscriber projector)
    {
        var filename = $"{projector.Name}.json".Replace(' ', '-');
        return Path.Combine(_fileDirectoryPath, filename);
    }
}
