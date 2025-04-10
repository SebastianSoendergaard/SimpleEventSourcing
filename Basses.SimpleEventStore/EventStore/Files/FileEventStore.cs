﻿using System.Text.Json;

namespace Basses.SimpleEventStore.EventStore.Files;

public class FileEventStore : IEventStore
{
    private readonly string _fileDirectoryPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _lock = new();
    private readonly UpcastManager _upcastManager;
    private Func<Task>? _onEventsAppended;

    public FileEventStore(string fileDirectoryPath)
    {
        _fileDirectoryPath = fileDirectoryPath;
        _upcastManager = new UpcastManager(new DefaultEventSerializer());

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        Directory.CreateDirectory(_fileDirectoryPath);
    }

    public async Task AppendEvents(string streamId, int version, IEnumerable<object> events)
    {
        lock (_lock)
        {
            var existingEvents = LoadEventsFromFiles().ToList();

            var sequenceNumber = existingEvents
                .Select(x => x.SequenceNumber)
                .DefaultIfEmpty(0)
                .Max() + 1;

            foreach (var @event in events)
            {
                if (existingEvents.Any(e => e.StreamId == streamId && e.Version == version))
                {
                    throw new VersionConflictException($"Stream '{streamId}' with version '{version}' already exists");
                }

                EventEntry eventEntry = new(
                    sequenceNumber++,
                    streamId,
                    version++,
                    DateTimeOffset.UtcNow,
                    @event.GetType().AssemblyQualifiedName ?? "",
                    @event
                );

                WriteToFile(eventEntry);
            }
        }

        if (_onEventsAppended != null)
        {
            await _onEventsAppended.Invoke();
        }
    }

    public Task<long> GetHeadSequenceNumber()
    {
        var head = LoadEventsFromFiles()
            .Select(x => x.SequenceNumber)
            .DefaultIfEmpty(0)
            .Max();

        return Task.FromResult(head);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(string streamId)
    {
        var events = LoadEventsFromFiles()
            .Where(x => x.StreamId == streamId);

        return Task.FromResult(events);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(string streamId, long startSequenceNumber, int max)
    {
        var events = LoadEventsFromFiles()
            .Where(x => x.StreamId == streamId && x.SequenceNumber >= startSequenceNumber)
            .Take(max);

        return Task.FromResult(events);
    }

    public Task<IEnumerable<EventEntry>> LoadEvents(long startSequenceNumber, int max)
    {
        var events = LoadEventsFromFiles()
            .Where(x => x.SequenceNumber >= startSequenceNumber)
            .Take(max);

        return Task.FromResult(events);
    }

    public void RegisterForEventsAppendedNotifications(Func<Task> onEventsAppended)
    {
        _onEventsAppended += onEventsAppended;
    }

    private void WriteToFile(EventEntry eventEntry)
    {
        var json = JsonSerializer.Serialize(eventEntry, _jsonOptions);
        var filename = $"{eventEntry.SequenceNumber:D4}_{eventEntry.StreamId}_v{eventEntry.Version}_{eventEntry.Event.GetType().Name}.json";
        File.WriteAllText(Path.Combine(_fileDirectoryPath, filename), json);
    }

    private IEnumerable<EventEntry> LoadEventsFromFiles()
    {
        return Directory.EnumerateFiles(_fileDirectoryPath)
            .Select(File.ReadAllText)
            .Select(Deserialize)
            .Where(x => x != null)
            .Select(x => x!)
            .OrderBy(x => x.SequenceNumber);
    }

    private EventEntry Deserialize(string json)
    {
        var entry = JsonSerializer.Deserialize<EventEntry>(json) ?? throw new EventStoreException($"Could not deserialize event entry: {json}");
        var eventJson = ((JsonElement)entry.Event).GetRawText();

        var @event = _upcastManager.Deserialize(eventJson, entry.EventType);

        return new EventEntry(
                    entry.SequenceNumber,
                    entry.StreamId,
                    entry.Version,
                    entry.Timestamp,
                    @event.GetType().AssemblyQualifiedName ?? "",
                    @event
                );
    }

    public void RegisterUpcaster(IUpcaster upcaster)
    {
        _upcastManager.RegisterUpcaster(upcaster);
    }
}
