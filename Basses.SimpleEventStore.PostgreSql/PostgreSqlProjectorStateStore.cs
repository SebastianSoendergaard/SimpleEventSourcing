using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.Projections;
using Npgsql;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlProjectorStateStore : IProjectorStateStore
{
    private readonly NpgsqlConnection _connection;
    private readonly string _schema;
    private readonly string _stateStoreName;

    public PostgreSqlProjectorStateStore(string connectionString, string schema, string tableName)
    {
        _schema = schema;
        _stateStoreName = tableName;

        PostgreSqlHelper.EnsureDatabase(connectionString);

        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();

        CreateStateStoreTableIfNotExists();
    }

    public async Task<ProjectorProcessingState> GetProcessingState(IProjector projector)
    {
        var sql = $@"SELECT 
                        latest_successful_processing_time, 
                        confirmed_sequence_number,
                        error_message, 
                        stacktrace, 
                        processing_attempts, 
                        latest_retry_time
                        FROM {_schema}.{_stateStoreName} 
                        WHERE projector_name=@projector_name";

        var state = new ProjectorProcessingState(DateTimeOffset.MinValue, 0);

        try
        {
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            cmd.Parameters.AddWithValue("projector_name", projector.Name);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    var latestSuccessfulProcessingTime = reader.GetFieldValue<DateTimeOffset>(0);
                    var confirmedSequenceNumber = reader.GetInt32(1);

                    ProjectorProcessingError? error = null;
                    if (!reader.IsDBNull(2))
                    {
                        var errorMessage = reader.GetString(2);
                        var stackTrace = reader.GetString(3);
                        var processingAttempts = reader.GetInt32(4);
                        var latestRetryTime = reader.GetFieldValue<DateTimeOffset>(5);
                        error = new ProjectorProcessingError(errorMessage, stackTrace, processingAttempts, latestRetryTime);
                    }

                    state = new ProjectorProcessingState(latestSuccessfulProcessingTime, confirmedSequenceNumber, error);
                }
            }
            return state;
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get processing state", ex);
        }
    }

    public async Task SaveProcessingState(IProjector projector, ProjectorProcessingState state)
    {
        var sql = $@"UPDATE {_schema}.{_stateStoreName} 
                        SET 
                            latest_successful_processing_time=@processing_time, 
                            confirmed_sequence_number=@confirmed_sequence_number,
                            error_message=@error_message, 
                            stacktrace=@stacktrace, 
                            processing_attempts=@attempts, 
                            latest_retry_time=@retry_time
                        WHERE projector_name=@projector_name;";

        try
        {
            using var cmd1 = new NpgsqlCommand(sql);
            cmd1.Connection = _connection;
            cmd1.Parameters.AddWithValue("projector_name", projector.Name);
            cmd1.Parameters.AddWithValue("processing_time", state.LatestSuccessfulProcessingTime);
            cmd1.Parameters.AddWithValue("confirmed_sequence_number", state.ConfirmedSequenceNumber);
            cmd1.Parameters.AddWithValue("error_message", (object?)state.ProcessingError?.ErrorMessage ?? DBNull.Value);
            cmd1.Parameters.AddWithValue("stacktrace", (object?)state.ProcessingError?.Stacktrace ?? DBNull.Value);
            cmd1.Parameters.AddWithValue("attempts", (object?)state.ProcessingError?.ProcessingAttempts ?? DBNull.Value);
            cmd1.Parameters.AddWithValue("retry_time", (object?)state.ProcessingError?.LatestRetryTime ?? DBNull.Value);

            await cmd1.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not upsert projector", ex);
        }
    }

    public async Task UpsertProjector(IProjector projector)
    {
        var sql1 = $@"INSERT INTO {_schema}.{_stateStoreName} (projector_name, latest_successful_processing_time, confirmed_sequence_number)
                        VALUES (@projector_name, @latest_successful_processing_time, @confirmed_sequence_number)
                        ON CONFLICT (projector_name)
                        DO NOTHING;";

        try
        {
            using var cmd1 = new NpgsqlCommand(sql1);
            cmd1.Connection = _connection;
            cmd1.Parameters.AddWithValue("projector_name", projector.Name);
            cmd1.Parameters.AddWithValue("latest_successful_processing_time", DateTimeOffset.MinValue);
            cmd1.Parameters.AddWithValue("confirmed_sequence_number", 0);

            await cmd1.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not upsert projector", ex);
        }
    }

    private void CreateStateStoreTableIfNotExists()
    {
        try
        {
            using var transaction = _connection.BeginTransaction();

            var sql1 = $"CREATE SCHEMA IF NOT EXISTS {_schema}";
            using var cmd1 = new NpgsqlCommand(sql1);
            cmd1.Connection = _connection;
            cmd1.ExecuteNonQuery();

            var sql2 = $@"CREATE TABLE IF NOT EXISTS {_schema}.{_stateStoreName} (
                            projector_name varchar(100),
                            latest_successful_processing_time timestamptz,
                            confirmed_sequence_number bigserial, 
                            error_message varchar(1000) NULL,
                            stacktrace varchar(10000) NULL,
                            processing_attempts integer NULL,
                            latest_retry_time timestamptz NULL,
                            PRIMARY KEY (projector_name),
                            CONSTRAINT {_stateStoreName}_unique_projector_name UNIQUE (projector_name)
                        );";
            using var cmd2 = new NpgsqlCommand(sql2);
            cmd2.Connection = _connection;
            cmd2.ExecuteNonQuery();

            var sql3 = $@"CREATE INDEX IF NOT EXISTS {_stateStoreName}_index_projector_name ON {_schema}.{_stateStoreName}(projector_name);";
            using var cmd3 = new NpgsqlCommand(sql3);
            cmd3.Connection = _connection;
            cmd3.ExecuteNonQuery();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            throw new ProjectorException("Could not create projector state database table", ex);
        }
    }
}
