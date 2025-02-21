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
                        projector_id, 
                        latest_successful_processing_time, 
                        confirmed_sequence_number,
                        error_message, 
                        stacktrace, 
                        processing_attempts, 
                        latest_retry_time
                        FROM {_schema}.{_stateStoreName} 
                        WHERE projector_id = '{projector.Id}'";

        var state = new ProjectorProcessingState(DateTimeOffset.MinValue, 0);

        try
        {
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    var projectorId = reader.GetGuid(0);
                    var latestSuccessfulProcessingTime = reader.GetFieldValue<DateTimeOffset>(1);
                    var confirmedSequenceNumber = reader.GetInt32(2);

                    ProjectorProcessingError? error = null;
                    if (!reader.IsDBNull(3))
                    {
                        var errorMessage = reader.GetString(3);
                        var stackTrace = reader.GetString(4);
                        var processingAttempts = reader.GetInt32(5);
                        var latestRetryTime = reader.GetFieldValue<DateTimeOffset>(6);
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
                        WHERE projector_id=@projector_id;";

        try
        {
            using var cmd1 = new NpgsqlCommand(sql);
            cmd1.Connection = _connection;
            cmd1.Parameters.AddWithValue("projector_id", projector.Id);
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
        var sql1 = $@"INSERT INTO {_schema}.{_stateStoreName} (projector_id, latest_successful_processing_time, confirmed_sequence_number)
                        VALUES (@projector_id, @latest_successful_processing_time, @confirmed_sequence_number)
                        ON CONFLICT (projector_id)
                        DO NOTHING;";

        try
        {
            using var cmd1 = new NpgsqlCommand(sql1);
            cmd1.Connection = _connection;
            cmd1.Parameters.AddWithValue("projector_id", projector.Id);
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
                            projector_id uuid,
                            latest_successful_processing_time timestamptz,
                            confirmed_sequence_number bigserial, 
                            error_message varchar(1000) NULL,
                            stacktrace varchar(10000) NULL,
                            processing_attempts integer NULL,
                            latest_retry_time timestamptz NULL,
                            PRIMARY KEY (projector_id),
                            CONSTRAINT {_stateStoreName}_unique_projector_id UNIQUE (projector_id)
                        );";
            using var cmd2 = new NpgsqlCommand(sql2);
            cmd2.Connection = _connection;
            cmd2.ExecuteNonQuery();

            var sql3 = $@"CREATE INDEX IF NOT EXISTS {_stateStoreName}_index_projector_id ON {_schema}.{_stateStoreName}(projector_id);";
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
