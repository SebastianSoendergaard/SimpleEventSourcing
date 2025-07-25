using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.EventSubscriber;

namespace Basses.SimpleEventStore.PostgreSql;

public abstract class PostgreSqlEventSubscriberStateStore : IEventSubscriberStateStore
{
    private readonly PostgreSqlHelper _sqlHelper;
    private readonly string _schema;
    private readonly string _stateStoreName;
    private readonly string _subscriberTypeName;

    public PostgreSqlEventSubscriberStateStore(string connectionString, string schema, string tableName, string subscriberTypeName)
    {
        _sqlHelper = new PostgreSqlHelper(connectionString);
        _schema = schema;
        _stateStoreName = tableName;
        _subscriberTypeName = subscriberTypeName;

        _sqlHelper.EnsureDatabase();

        CreateStateStoreTableIfNotExists();
    }

    public async Task<EventSubscriberProcessingState> GetProcessingState(IEventSubscriber subscriber)
    {
        var sql = $@"SELECT 
                        latest_successful_processing_time, 
                        confirmed_sequence_number,
                        error_message, 
                        stacktrace, 
                        processing_attempts, 
                        latest_retry_time
                        FROM {_schema}.{_stateStoreName} 
                        WHERE {_subscriberTypeName}_name=@subscriber_name";

        var parameters = new[]
        {
            new PostgreSqlParameter("subscriber_name", subscriber.Name)
        };

        try
        {
            var state = await _sqlHelper.QuerySingleOrDefaultAsync(sql, parameters, reader =>
            {
                var latestSuccessfulProcessingTime = reader.GetFieldValue<DateTimeOffset>(0);
                var confirmedSequenceNumber = reader.GetInt32(1);

                EventSubscriberProcessingError? error = null;
                if (!reader.IsDBNull(2))
                {
                    var errorMessage = reader.GetString(2);
                    var stackTrace = reader.GetString(3);
                    var processingAttempts = reader.GetInt32(4);
                    var latestRetryTime = reader.GetFieldValue<DateTimeOffset>(5);
                    error = new EventSubscriberProcessingError(errorMessage, stackTrace, processingAttempts, latestRetryTime);
                }

                return new EventSubscriberProcessingState(latestSuccessfulProcessingTime, confirmedSequenceNumber, error);
            });
            return state ?? new EventSubscriberProcessingState(DateTimeOffset.MinValue, 0);
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get processing state", ex);
        }
    }

    public async Task SaveProcessingState(IEventSubscriber subscriber, EventSubscriberProcessingState state)
    {
        var sql = $@"UPDATE {_schema}.{_stateStoreName} 
                        SET 
                            latest_successful_processing_time=@processing_time, 
                            confirmed_sequence_number=@confirmed_sequence_number,
                            error_message=@error_message, 
                            stacktrace=@stacktrace, 
                            processing_attempts=@attempts, 
                            latest_retry_time=@retry_time
                        WHERE {_subscriberTypeName}_name=@subscriber_name;";

        var parameters = new[]
        {
            new PostgreSqlParameter("subscriber_name", subscriber.Name),
            new PostgreSqlParameter("processing_time", state.LatestSuccessfulProcessingTime),
            new PostgreSqlParameter("confirmed_sequence_number", state.ConfirmedSequenceNumber),
            new PostgreSqlParameter("error_message", (object?)state.ProcessingError?.ErrorMessage ?? DBNull.Value),
            new PostgreSqlParameter("stacktrace", (object?)state.ProcessingError?.Stacktrace ?? DBNull.Value),
            new PostgreSqlParameter("attempts", (object?)state.ProcessingError?.ProcessingAttempts ?? DBNull.Value),
            new PostgreSqlParameter("retry_time", (object?)state.ProcessingError?.LatestRetryTime ?? DBNull.Value)
        };

        try
        {
            await _sqlHelper.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not update subscriber processing state", ex);
        }
    }

    public async Task UpsertSubscriber(IEventSubscriber subscriber)
    {
        var sql = $@"INSERT INTO {_schema}.{_stateStoreName} ({_subscriberTypeName}_name, latest_successful_processing_time, confirmed_sequence_number)
                        VALUES (@subscriber_name, @latest_successful_processing_time, @confirmed_sequence_number)
                        ON CONFLICT ({_subscriberTypeName}_name)
                        DO NOTHING;";

        var parameters = new[]
        {
            new PostgreSqlParameter("subscriber_name", subscriber.Name),
            new PostgreSqlParameter("latest_successful_processing_time", DateTimeOffset.MinValue),
            new PostgreSqlParameter("confirmed_sequence_number", 0)
        };

        try
        {
            await _sqlHelper.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not upsert subscriber", ex);
        }
    }

    private void CreateStateStoreTableIfNotExists()
    {
        var sql1 = $"CREATE SCHEMA IF NOT EXISTS {_schema}";

        var sql2 = $@"CREATE TABLE IF NOT EXISTS {_schema}.{_stateStoreName} (
                            {_subscriberTypeName}_name varchar(100),
                            latest_successful_processing_time timestamptz,
                            confirmed_sequence_number bigserial, 
                            error_message varchar(1000) NULL,
                            stacktrace varchar(10000) NULL,
                            processing_attempts integer NULL,
                            latest_retry_time timestamptz NULL,
                            PRIMARY KEY ({_subscriberTypeName}_name),
                            CONSTRAINT {_stateStoreName}_unique_{_subscriberTypeName}_name UNIQUE ({_subscriberTypeName}_name)
                        );";

        var sql3 = $@"CREATE INDEX IF NOT EXISTS {_stateStoreName}_index_{_subscriberTypeName}_name ON {_schema}.{_stateStoreName}({_subscriberTypeName}_name);";

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
            throw new EventSubscriberException("Could not create subscriber state database table", ex);
        }
    }
}
