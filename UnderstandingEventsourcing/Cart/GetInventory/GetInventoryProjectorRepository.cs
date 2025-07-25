using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;

namespace UnderstandingEventsourcingExample.Cart.GetInventory;

public sealed class GetInventoryProjectorRepository
{
    private readonly PostgreSqlHelper _sqlHelper;
    private readonly string _projectorName;
    private readonly string _schema;

    public GetInventoryProjectorRepository(string projectorName, string connectionString, string schema)
    {
        _sqlHelper = new PostgreSqlHelper(connectionString);
        _projectorName = projectorName;
        _schema = schema;
    }

    public async Task<long> GetLastProcessedSequenceNumber()
    {
        var sql = $"SELECT last_processed_sequence_number FROM {_schema}.read_model_projector_state WHERE projector_name = @projector_name";

        var parameters = new[]
        {
            new PostgreSqlParameter("projector_name", _projectorName)
        };

        try
        {
            var sequenceNumber = await _sqlHelper.QuerySingleOrDefaultAsync(sql, parameters, reader => reader.GetInt64(0));
            return sequenceNumber;
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get sequence number", ex);
        }
    }

    public async Task<InventoryReadModel> GetByProductId(Guid productId)
    {
        var sql = $"SELECT inventory FROM {_schema}.get_inventory_read_model WHERE product_id = @product_id";

        var parameters = new[]
        {
            new PostgreSqlParameter("product_id", productId)
        };

        try
        {
            var inventory = await _sqlHelper.QuerySingleOrDefaultAsync(sql, parameters, reader => reader.GetInt32(0));
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

        var sql2 = $@"INSERT INTO {_schema}.read_model_projector_state (projector_name, last_processed_sequence_number)
                        VALUES (@projector_name, @last_processed_sequence_number)
                        ON CONFLICT (projector_name)
                        DO UPDATE SET
                            last_processed_sequence_number = EXCLUDED.last_processed_sequence_number;";

        try
        {
            await _sqlHelper.Transaction(async (conn, tx) =>
            {
                foreach (var inventory in inventories)
                {
                    var parameters1 = new[]
                    {
                        new PostgreSqlParameter("product_id", inventory.ProductId),
                        new PostgreSqlParameter("inventory", inventory.Inventory)
                    };

                    await _sqlHelper.ExecuteAsync(sql1, parameters1, conn, tx);
                }

                var parameters2 = new[]
                {
                    new PostgreSqlParameter("projector_name", _projectorName),
                    new PostgreSqlParameter("last_processed_sequence_number", sequenceNumber)
                };

                await _sqlHelper.ExecuteAsync(sql2, parameters2, conn, tx);
            });
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not upsert inventories", ex);
        }
    }
}
