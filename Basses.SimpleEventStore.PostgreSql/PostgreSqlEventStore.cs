using Basses.SimpleEventStore.EventStore;
using Npgsql;
using NpgsqlTypes;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlEventStore : IEventStore
{
    private readonly PostgreSqlHelper _sqlHelper;
    private readonly string _schema;
    private readonly string _eventStoreName;
    private readonly IEventSerializer _serializer;
    private readonly UpcastManager _upcastManager;
    private Func<Task>? _onEventsAppended;

    public PostgreSqlEventStore(string connectionString, string schema, string tableName, IEventSerializer? eventSerializer = null)
    {
        _sqlHelper = new PostgreSqlHelper(connectionString);
        _schema = schema;
        _eventStoreName = tableName;
        _serializer = eventSerializer ?? new DefaultEventSerializer();
        _upcastManager = new UpcastManager(_serializer);

        _sqlHelper.EnsureDatabase();

        CreateEventStoreTableIfNotExists();
    }

    public async Task AppendEvents(string streamId, int version, IEnumerable<object> events)
    {
        var sql = $@"INSERT INTO {_schema}.{_eventStoreName} (stream_id, version, timestamp, event_type, event) " +
                        "VALUES(@streamId, @version, @timestamp, @eventType, @eventJson)";

        try
        {
            await _sqlHelper.Transaction(async (conn, tx) =>
            {
                foreach (var @event in events)
                {
                    var serializedEvent = _serializer.Serialize(@event);

                    var parameters = new[]
                    {
                        new PostgreSqlParameter("streamId", streamId),
                        new PostgreSqlParameter("version", version++),
                        new PostgreSqlParameter("timestamp", DateTimeOffset.UtcNow),
                        new PostgreSqlParameter("eventType", serializedEvent.EventType),
                        new PostgreSqlParameter("eventJson", serializedEvent.EventPayload, NpgsqlDbType.Jsonb)
                    };

                    await _sqlHelper.ExecuteAsync(sql, parameters, conn, tx);
                }
            });
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
        var sql = $"SELECT COALESCE(MAX(sequence_number), 0) FROM {_schema}.{_eventStoreName}";

        try
        {
            var maxSequenceNumber = await _sqlHelper.QuerySingleOrDefaultAsync(sql, [], reader => reader.GetInt64(0));
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

        var parameters = new[]
        {
            new PostgreSqlParameter("streamId", streamId)
        };

        return await LoadEventsFromDatabase(sql, parameters);
    }

    public async Task<IEnumerable<EventEntry>> LoadEvents(string streamId, long startSequenceNumber, int max)
    {
        var sql = $@"SELECT sequence_number, stream_id, version, timestamp, event_type, event 
                        FROM {_schema}.{_eventStoreName} 
                        WHERE stream_id = @streamId
                        AND sequence_number >= @startSequenceNumber
                        ORDER BY sequence_number
                        LIMIT @max";

        var parameters = new[]
        {
            new PostgreSqlParameter("streamId", streamId),
            new PostgreSqlParameter("startSequenceNumber", startSequenceNumber),
            new PostgreSqlParameter("max", max)
        };

        return await LoadEventsFromDatabase(sql, parameters);
    }

    public async Task<IEnumerable<EventEntry>> LoadEvents(long startSequenceNumber, int max)
    {
        var sql = $@"SELECT sequence_number, stream_id, version, timestamp, event_type, event 
                        FROM {_schema}.{_eventStoreName} 
                        WHERE sequence_number >= @startSequenceNumber
                        ORDER BY sequence_number
                        LIMIT @max";

        var parameters = new[]
        {
            new PostgreSqlParameter("startSequenceNumber", startSequenceNumber),
            new PostgreSqlParameter("max", max)
        };

        return await LoadEventsFromDatabase(sql, parameters);
    }

    private async Task<IEnumerable<EventEntry>> LoadEventsFromDatabase(string sql, IEnumerable<PostgreSqlParameter> parameters)
    {
        try
        {
            var events = await _sqlHelper.QueryAsync(sql, parameters, reader =>
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

                return eventEntry;
            });

            return events;
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not load events", ex);
        }
    }

    private void CreateEventStoreTableIfNotExists()
    {
        var sql1 = $"CREATE SCHEMA IF NOT EXISTS {_schema}";

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

        var sql3 = $@"CREATE INDEX IF NOT EXISTS {_eventStoreName}_index_stream_id ON {_schema}.{_eventStoreName}(stream_id);";

        try
        {
            _sqlHelper.Transaction(async (conn, tx) =>
            {
                await _sqlHelper.ExecuteAsync(sql1, [], conn, tx);
                await _sqlHelper.ExecuteAsync(sql2, [], conn, tx);
                await _sqlHelper.ExecuteAsync(sql3, [], conn, tx);
            })
            .GetAwaiter()
            .GetResult();
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
}
