using Basses.SimpleEventStore.Reactions;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlReactorStateStore : PostgreSqlEventSubscriberStateStore, IReactorStateStore
{
    public PostgreSqlReactorStateStore(string connectionString, string schema, string tableName, IInstrumentation? instrumentation = null)
        : base(connectionString, schema, tableName, "reactor", instrumentation)
    {
    }
}
