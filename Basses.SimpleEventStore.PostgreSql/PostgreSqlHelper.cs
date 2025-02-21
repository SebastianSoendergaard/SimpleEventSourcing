using Basses.SimpleEventStore.EventStore;
using Npgsql;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlHelper
{
    public static void EnsureDatabase(string connectionString)
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
}
