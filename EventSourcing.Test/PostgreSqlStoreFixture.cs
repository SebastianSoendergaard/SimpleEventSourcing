using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;
using Npgsql;

namespace EventSourcing.Test;

public class PostgreSqlStoreFixture : IDisposable
{
    private readonly string _dbNamePrefix = "simple_event_sourcing_test_";
    private readonly List<PostgreSqlEventStore> _eventStores = [];
    private string _storeName = "";


    public IEventStore CreateEventStore()
    {
        _storeName = Guid.NewGuid().ToString()[..8];
        var connectionString = $"Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database={_dbNamePrefix}{_storeName};";
        PostgreSqlEventStore.CreateIfNotExist(connectionString, $"store_{_storeName}");
        var store = new PostgreSqlEventStore(connectionString, $"store_{_storeName}");
        _eventStores.Add(store);
        return store;
    }

    public void Dispose()
    {
        foreach (var store in _eventStores)
        {
            store.Dispose();
        }

        _eventStores.Clear();

        var connectionString = $"Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;";
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var sql = $"DROP DATABASE {_dbNamePrefix}{_storeName} WITH (FORCE)";

        using var cmd = new NpgsqlCommand(sql);
        cmd.Connection = connection;
        cmd.ExecuteNonQuery();

        connection.Close();
    }
}
