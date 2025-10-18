using Basses.SimpleEventStore.Projections;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlProjectorStateStore : PostgreSqlEventSubscriberStateStore, IProjectorStateStore
{
    public PostgreSqlProjectorStateStore(string connectionString, string schema, string tableName, IInstrumentation? instrumentation = null)
        : base(connectionString, schema, tableName, "projector", instrumentation)
    {
    }
}
