using Basses.SimpleEventStore.EventStore;
using Npgsql;

namespace UnderstandingEventsourcingExample.Cart.GetInventory;

public sealed class GetInventoryProjectorRepository : IDisposable
{
    private NpgsqlConnection _connection;
    private readonly Guid _projectorId;
    private readonly string _schema;

    public GetInventoryProjectorRepository(Guid projectorId, string connectionString, string schema)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        _projectorId = projectorId;
        _schema = schema;
    }

    public async Task<long> GetLastProcessedSequenceNumber()
    {
        var sql = $"SELECT last_processed_sequence_number FROM {_schema}.read_model_projector_state WHERE projector_id = '{_projectorId}'";
        long sequenceNumber = 0;

        try
        {
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    sequenceNumber = reader.GetInt64(0);
                }
            }
            return sequenceNumber;
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get sequence number", ex);
        }
    }

    public async Task<InventoryReadModel> GetByProductId(Guid productId)
    {
        var sql = $"SELECT inventory FROM {_schema}.get_inventory_read_model WHERE product_id = '{productId}'";
        int inventory = 0;

        try
        {
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    inventory = reader.GetInt32(0);
                }
            }
            return new InventoryReadModel(productId, inventory);
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get inventory", ex);
        }
    }

    public async Task Upsert(long sequenceNumber, IEnumerable<InventoryReadModel> inventories)
    {
        var sql1 = $@"INSERT INTO {_schema}.get_inventory_read_model (product_id, inventory)
                        VALUES (@product_id, @inventory)
                        ON CONFLICT (product_id)
                        DO UPDATE SET
                            inventory = EXCLUDED.inventory;";

        var sql2 = $@"INSERT INTO {_schema}.read_model_projector_state (projector_id, last_processed_sequence_number)
                        VALUES (@projector_id, @last_processed_sequence_number)
                        ON CONFLICT (projector_id)
                        DO UPDATE SET
                            last_processed_sequence_number = EXCLUDED.last_processed_sequence_number;";

        try
        {
            using var transaction = _connection.BeginTransaction();

            foreach (var inventory in inventories)
            {
                using var cmd1 = new NpgsqlCommand(sql1);
                cmd1.Connection = _connection;
                cmd1.Parameters.AddWithValue("product_id", inventory.ProductId);
                cmd1.Parameters.AddWithValue("inventory", inventory.Inventory);

                await cmd1.ExecuteNonQueryAsync();
            }

            using var cmd2 = new NpgsqlCommand(sql2);
            cmd2.Connection = _connection;
            cmd2.Parameters.AddWithValue("projector_id", _projectorId);
            cmd2.Parameters.AddWithValue("last_processed_sequence_number", sequenceNumber);

            await cmd2.ExecuteNonQueryAsync();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not upsert inventories", ex);
        }
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
