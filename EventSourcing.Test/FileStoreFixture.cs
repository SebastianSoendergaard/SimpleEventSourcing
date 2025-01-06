﻿using EventSourcing.EventStore;
using EventSourcing.EventStore.Files;

namespace EventSourcing.Test;

public class FileStoreFixture : IDisposable
{
    private readonly string _directoryPrefix = @"c:/temp/eventstore_test_";
    private readonly List<string> _names = [];

    public IEventStore CreateEventStore()
    {
        var name = Guid.NewGuid().ToString()[..8];
        _names.Add(name);
        return new FileEventStore(_directoryPrefix + name);
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
