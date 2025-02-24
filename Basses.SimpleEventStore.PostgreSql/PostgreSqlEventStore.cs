using Basses.SimpleEventStore.EventStore;
using Npgsql;
using NpgsqlTypes;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlEventStore : IEventStore, IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly string _schema;
    private readonly string _eventStoreName;
    private readonly IEventSerializer _serializer;
    private readonly UpcastManager _upcastManager;
    private Func<Task>? _onEventsAppended;

    public PostgreSqlEventStore(string connectionString, string schema, string tableName, IEventSerializer? eventSerializer = null)
    {
        _schema = schema;
        _eventStoreName = tableName;
        _serializer = eventSerializer ?? new DefaultEventSerializer();
        _upcastManager = new UpcastManager(_serializer);

        PostgreSqlHelper.EnsureDatabase(connectionString);

        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();

        CreateEventStoreTableIfNotExists();
    }

    public async Task AppendEvents(string streamId, int version, IEnumerable<object> events)
    {
        var sql = $@"INSERT INTO {_schema}.{_eventStoreName} (stream_id, version, timestamp, event_type, event) " +
                        "VALUES(@streamId, @version, @timestamp, @eventType, @eventJson)";

        try
        {
            using var transaction = _connection.BeginTransaction();

            foreach (var @event in events)
            {
                var serializedEvent = _serializer.Serialize(@event);

                using var cmd = new NpgsqlCommand(sql);
                cmd.Connection = _connection;
                cmd.Parameters.AddWithValue("streamId", streamId);
                cmd.Parameters.AddWithValue("version", version++);
                cmd.Parameters.AddWithValue("timestamp", DateTimeOffset.UtcNow);
                cmd.Parameters.AddWithValue("eventType", serializedEvent.EventType);
                cmd.Parameters.AddWithValue("eventJson", NpgsqlDbType.Jsonb, serializedEvent.EventPayload);

                await cmd.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch (PostgresException ex) when (ex.Message.Contains("unique_stream_version"))
        {
            throw new VersionConflictException($"Stream '{streamId}' with version '{version}' already exists");
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not append events", ex);
        }

        if (_onEventsAppended != null)
        {
            await _onEventsAppended.Invoke();
        }
    }

    public async Task<long> GetHeadSequenceNumber()
    {
        var sql = $"SELECT MAX(sequence_number) FROM {_schema}.{_eventStoreName}";
        long maxSequenceNumber = 0;

        try
        {
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    maxSequenceNumber = reader.GetInt64(0);
                }
            }
            return maxSequenceNumber;
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get max sequence number", ex);
        }
    }

    public async Task<IEnumerable<EventEntry>> LoadEvents(string streamId)
    {
        var sql = $@"SELECT sequence_number, stream_id, version, timestamp, event_type, event 
                        FROM {_schema}.{_eventStoreName} 
                        WHERE stream_id = @streamId
                        ORDER BY sequence_number";

        using var cmd = new NpgsqlCommand(sql);
        cmd.Parameters.AddWithValue("streamId", streamId);

        return await LoadEventsFromDatabase(cmd);
    }

    public async Task<IEnumerable<EventEntry>> LoadEvents(string streamId, long startSequenceNumber, int max)
    {
        var sql = $@"SELECT sequence_number, stream_id, version, timestamp, event_type, event 
                        FROM {_schema}.{_eventStoreName} 
                        WHERE stream_id = @streamId
                        AND sequence_number >= @startSequenceNumber
                        ORDER BY sequence_number
                        LIMIT @max";

        using var cmd = new NpgsqlCommand(sql);
        cmd.Parameters.AddWithValue("streamId", streamId);
        cmd.Parameters.AddWithValue("startSequenceNumber", startSequenceNumber);
        cmd.Parameters.AddWithValue("max", max);

        return await LoadEventsFromDatabase(cmd);
    }

    public async Task<IEnumerable<EventEntry>> LoadEvents(long startSequenceNumber, int max)
    {
        var sql = $@"SELECT sequence_number, stream_id, version, timestamp, event_type, event 
                        FROM {_schema}.{_eventStoreName} 
                        WHERE sequence_number >= @startSequenceNumber
                        ORDER BY sequence_number
                        LIMIT @max";

        using var cmd = new NpgsqlCommand(sql);
        cmd.Parameters.AddWithValue("startSequenceNumber", startSequenceNumber);
        cmd.Parameters.AddWithValue("max", max);

        return await LoadEventsFromDatabase(cmd);
    }

    private async Task<IEnumerable<EventEntry>> LoadEventsFromDatabase(NpgsqlCommand cmd)
    {
        List<EventEntry> events = [];

        try
        {
            cmd.Connection = _connection;
            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                var eventType = reader.GetString(4);
                var eventJson = reader.GetString(5);

                var @event = _upcastManager.Deserialize(eventJson, eventType);

                var eventEntry = new EventEntry(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    @event.GetType().AssemblyQualifiedName ?? "",
                    @event
                );

                events.Add(eventEntry);
            }
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not load events", ex);
        }

        return events;
    }

    private void CreateEventStoreTableIfNotExists()
    {
        try
        {
            using var transaction = _connection.BeginTransaction();

            var sql1 = $"CREATE SCHEMA IF NOT EXISTS {_schema}";
            using var cmd1 = new NpgsqlCommand(sql1);
            cmd1.Connection = _connection;
            cmd1.ExecuteNonQuery();

            var sql2 = $@"CREATE TABLE IF NOT EXISTS {_schema}.{_eventStoreName} (
                            sequence_number bigserial, 
                            stream_id varchar(100),
                            version integer,
                            timestamp timestamptz,
                            event_type varchar(200),
                            event jsonb,
                            PRIMARY KEY (sequence_number),
                            CONSTRAINT {_eventStoreName}_unique_stream_version UNIQUE (stream_id, version)
                        );";
            using var cmd2 = new NpgsqlCommand(sql2);
            cmd1.Connection = _connection;
            cmd1.ExecuteNonQuery();

            var sql3 = $@"CREATE INDEX IF NOT EXISTS {_eventStoreName}_index_stream_id ON {_schema}.{_eventStoreName}(stream_id);";
            using var cmd3 = new NpgsqlCommand(sql3);
            cmd2.Connection = _connection;
            cmd2.ExecuteNonQuery();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not create event store database table", ex);
        }
    }

    public void RegisterUpcaster(IUpcaster upcaster)
    {
        _upcastManager.RegisterUpcaster(upcaster);
    }

    public void RegisterForEventsAppendedNotifications(Func<Task> onEventsAppended)
    {
        _onEventsAppended += onEventsAppended;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
