using Basses.SimpleEventStore.Reactions;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlReactorStateStore : PostgreSqlEventSubscriberStateStore, IReactorStateStore
{
    public PostgreSqlReactorStateStore(string connectionString, string schema, string tableName)
        : base(connectionString, schema, tableName, "reactor")
    {
    }
}
