using Basses.SimpleEventStore.PostgreSql;
using Basses.SimpleEventStore.Projections;
using Npgsql;

namespace Basses.SimpleEventStore.Tests.Projections;

public class PostgreSqlStoreFixture : IDisposable
{
    private readonly string _dbNamePrefix = "simple_event_sourcing_test_";
    private readonly List<PostgreSqlProjectorStateStore> _stateStores = [];
    private string _storeName = "";


    public IProjectorStateStore CreateEventStore()
    {
        _storeName = Guid.NewGuid().ToString()[..8];
        var connectionString = $"Server=localhost;Port=9090;User Id=postgres;Password=Passw0rd;Database={_dbNamePrefix}{_storeName};Pooling=true";
        var store = new PostgreSqlProjectorStateStore(connectionString, "unit_test", $"projections_{_storeName}");
        _stateStores.Add(store);
        return store;
    }

    public void Dispose()
    {
        _stateStores.Clear();

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
