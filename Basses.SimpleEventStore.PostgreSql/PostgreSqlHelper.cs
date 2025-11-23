using Basses.SimpleEventStore.EventStore;
using Npgsql;
using NpgsqlTypes;

namespace Basses.SimpleEventStore.PostgreSql;

public class PostgreSqlHelper
{
    private readonly string _connectionString;

    public PostgreSqlHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> ExecuteAsync(string sql, IEnumerable<PostgreSqlParameter> parameters)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await ExecuteAsync(sql, parameters, connection);
    }

    public async Task<int> ExecuteAsync(string sql, IEnumerable<PostgreSqlParameter> parameters, NpgsqlConnection connection, NpgsqlTransaction? transaction = null)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var cmd = new NpgsqlCommand(sql, connection, transaction);
        AddParametersToCommand(cmd, parameters);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task Transaction(Func<NpgsqlConnection, NpgsqlTransaction, Task> execute)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var transaction = connection.BeginTransaction();

        try
        {
            await execute(connection, transaction);
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, IEnumerable<PostgreSqlParameter> parameters, Func<NpgsqlDataReader, T> createResult)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        using var cmd = new NpgsqlCommand(sql, connection);
        AddParametersToCommand(cmd, parameters);
        using var reader = await cmd.ExecuteReaderAsync();

        List<T> results = [];
        while (reader.Read())
        {
            results.Add(createResult(reader));
        }

        return results;
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, IEnumerable<PostgreSqlParameter> parameters, Func<NpgsqlDataReader, T> createResult)
    {
        var results = await QueryAsync(sql, parameters, createResult);
        return results.SingleOrDefault();
    }

    private void AddParametersToCommand(NpgsqlCommand command, IEnumerable<PostgreSqlParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.Type != null)
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Type.Value, parameter.Value);
            }
            else
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }
        }
    }

    public void EnsureDatabase()
    {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.Database ?? "";
        builder.Database = null;

        CreateDatabaseIfNotExists(builder.ConnectionString, databaseName);
    }

    private static void CreateDatabaseIfNotExists(string connectionString, string databaseName)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            if (!DoesDatabaseExist(connection, databaseName))
            {
                CreateDatabase(connection, databaseName);
            }

            connection.Close();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not create database", ex);
        }
    }

    private static bool DoesDatabaseExist(NpgsqlConnection connection, string databaseName)
    {
        if (databaseName.Contains('"'))
        {
            throw new ArgumentException("Invalid database name");
        }

        var sql = $"SELECT COUNT(*) FROM pg_database WHERE datname = '{databaseName}'";
        using var cmd = new NpgsqlCommand(sql, connection);
        var tableCount = (long)(cmd.ExecuteScalar() ?? 0);
        return tableCount > 0;
    }

    private static void CreateDatabase(NpgsqlConnection connection, string databaseName)
    {
        if (databaseName.Contains('"'))
        {
            throw new ArgumentException("Invalid database name");
        }

        var sql = $"CREATE DATABASE \"{databaseName}\"";
        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.ExecuteNonQuery();
    }
}

public record PostgreSqlParameter
{
    public PostgreSqlParameter(string name, object value, NpgsqlDbType? type = null)
    {
        Name = name;
        Value = value;
        Type = type;
    }

    public string Name { get; }
    public object Value { get; }
    public NpgsqlDbType? Type { get; }
}
