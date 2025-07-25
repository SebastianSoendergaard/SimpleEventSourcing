using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;
using Npgsql;

namespace Basses.SimpleEventStore.Tests.EventStore;

public class PostgreSqlStoreFixture : IDisposable
{
    private readonly string _dbNamePrefix = "simple_event_sourcing_test_";
    private readonly List<PostgreSqlEventStore> _eventStores = [];
    private string _storeName = "";


    public IEventStore CreateEventStore()
    {
        _storeName = Guid.NewGuid().ToString()[..8];
        var connectionString = $"Server=localhost;Port=9090;User Id=postgres;Password=Passw0rd;Database={_dbNamePrefix}{_storeName};";
        var store = new PostgreSqlEventStore(connectionString, "unit_test", $"store_{_storeName}");
        _eventStores.Add(store);
        return store;
    }

    public void Dispose()
    {
        _eventStores.Clear();

        var connectionString = $"Server=localhost;Port=9090;User Id=postgres;Password=Passw0rd;";
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var sql = $"DROP DATABASE {_dbNamePrefix}{_storeName} WITH (FORCE)";

        using var cmd = new NpgsqlCommand(sql);
        cmd.Connection = connection;
        cmd.ExecuteNonQuery();

        connection.Close();
    }
}
