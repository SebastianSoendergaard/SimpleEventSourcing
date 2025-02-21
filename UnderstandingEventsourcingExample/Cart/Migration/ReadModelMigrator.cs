using Npgsql;

namespace UnderstandingEventsourcingExample.Cart.Migration;

public static class ReadModelMigrator
{
    public static void Migrate(string connectionString)
    {
        var scripts = new List<string>
        {
            "create_read_model_projector_state.sql",
            "create_GetInventory_read_model.sql"
        };

        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        try
        {
            foreach (var script in scripts)
            {
                var sql = LoadFile(script);
                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.ExecuteNonQuery();
            }
        }
        finally
        {
            connection.Close();
        }
    }

    private static string LoadFile(string filename)
    {
        var assembly = typeof(ReadModelMigrator).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        var resourceName = resourceNames.FirstOrDefault(str => str.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
        if (resourceName == null)
        {
            throw new ArgumentException($"Resource '{filename}' was not found in assembly '{assembly}'. If file should exist, add it to the project and mark it as embedded recource.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return string.Empty;
    }
}
