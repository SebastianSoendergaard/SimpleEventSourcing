using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace EventSourcing.EventStore.PostgreSql;

public class PostgreSqlEventStore : IEventStore, IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly string _eventStoreName;
    private Func<Task>? _onEventsAppended;

    public PostgreSqlEventStore(string connectionString, string eventStoreName)
    {
        _eventStoreName = eventStoreName;

        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
    }

    public async Task AppendEvents(Guid streamId, int version, IEnumerable<object> events)
    {
        var sql = $@"INSERT INTO {_eventStoreName} (stream_id, version, timestamp, event_type, event) " +
                        "VALUES(@streamId, @version, @timestamp, @eventType, @eventJson)";

        try
        {
            using var transaction = _connection.BeginTransaction();

            foreach (var @event in events)
            {
                var eventJson = JsonSerializer.Serialize(@event);

                using var cmd = new NpgsqlCommand(sql);
                cmd.Connection = _connection;
                cmd.Parameters.AddWithValue("streamId", streamId);
                cmd.Parameters.AddWithValue("version", version++);
                cmd.Parameters.AddWithValue("timestamp", DateTimeOffset.UtcNow);
                cmd.Parameters.AddWithValue("eventType", @event.GetType().AssemblyQualifiedName ?? "");
                cmd.Parameters.AddWithValue("eventJson", NpgsqlDbType.Jsonb, eventJson);

                await cmd.ExecuteNonQueryAsync();
            }

            if (_onEventsAppended != null)
            {
                await _onEventsAppended.Invoke();
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
    }

    public async Task<long> GetHeadSequenceNumber()
    {
        var sql = $"SELECT MAX(sequence_number) FROM {_eventStoreName}";
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

    public async Task<IEnumerable<EventEntry>> LoadEvents(Guid streamId)
    {
        var sql = $@"SELECT sequence_number, stream_id, version, timestamp, event_type, event 
                        FROM {_eventStoreName} 
                        WHERE stream_id = @streamId
                        ORDER BY sequence_number";

        using var cmd = new NpgsqlCommand(sql);
        cmd.Parameters.AddWithValue("streamId", streamId);

        return await LoadEventsFromDatabase(cmd);
    }

    public async Task<IEnumerable<EventEntry>> LoadEvents(Guid streamId, long startSequenceNumber, int max)
    {
        var sql = $@"SELECT sequence_number, stream_id, version, timestamp, event_type, event 
                        FROM {_eventStoreName} 
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
                        FROM {_eventStoreName} 
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
                var @event = DeserializeEvent(eventType, eventJson);

                var eventEntry = new EventEntry(
                    reader.GetInt64(0),
                    reader.GetGuid(1),
                    reader.GetInt32(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    eventType,
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

    private object DeserializeEvent(string eventType, string eventJson)
    {
        var type = Type.GetType(eventType);
        if (type == null)
        {
            throw new EventStoreException($"Unknown event type: {eventType}");
        }

        var @event = JsonSerializer.Deserialize(eventJson, type);
        if (@event == null)
        {
            throw new EventStoreException($"Deserialization failed for event type: {eventType}");
        }

        return @event;
    }

    public static void CreateIfNotExist(string connectionString, string eventStoreName)
    {
        var connectionProperties = connectionString
            .Split(';')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x =>
            {
                var values = x.Split('=');
                return new { Key = values[0], Value = values[1] };
            });

        var connectionPropertiesWithoutDatabase = connectionProperties
            .Where(x => !x.Key.StartsWith("database", StringComparison.OrdinalIgnoreCase));

        var connectionStringWithoutDatabase = string.Join(';', connectionPropertiesWithoutDatabase.Select(x => $"{x.Key}={x.Value}"));

        var databaseName = connectionProperties
            .Where(x => x.Key.StartsWith("database", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value)
            .Single();

        CreateDatabaseIfNotExists(connectionStringWithoutDatabase, databaseName);

        CreateEventStoreTableIfNotExists(connectionString, eventStoreName);
    }

    private static void CreateDatabaseIfNotExists(string connectionString, string databaseName)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var sql1 = $"SELECT COUNT(*) FROM pg_database WHERE datname = '{databaseName}'";
            using var cmd1 = new NpgsqlCommand(sql1);
            cmd1.Connection = connection;
            var tableCount = (long)(cmd1.ExecuteScalar() ?? 0);
            if (tableCount == 0)
            {
                var sql2 = $"CREATE DATABASE {databaseName}";
                using var cmd2 = new NpgsqlCommand(sql2);
                cmd2.Connection = connection;
                cmd2.ExecuteNonQuery();
            }
            connection.Close();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not create database", ex);
        }
    }

    private static void CreateEventStoreTableIfNotExists(string connectionString, string eventStoreTableNames)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            var sql1 = $@"CREATE TABLE IF NOT EXISTS public.{eventStoreTableNames} (
                            sequence_number bigserial, 
                            stream_id uuid,
                            version integer,
                            timestamp timestamptz,
                            event_type varchar(200),
                            event jsonb,
                            PRIMARY KEY (sequence_number),
                            CONSTRAINT unique_stream_version UNIQUE (stream_id, version)
                        );";
            using var cmd1 = new NpgsqlCommand(sql1);
            cmd1.Connection = connection;
            cmd1.ExecuteNonQuery();

            var sql2 = $@"CREATE INDEX IF NOT EXISTS index_stream_id ON public.{eventStoreTableNames}(stream_id);";
            using var cmd2 = new NpgsqlCommand(sql2);
            cmd2.Connection = connection;
            cmd2.ExecuteNonQuery();

            transaction.Commit();

            connection.Close();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not create database table", ex);
        }
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
