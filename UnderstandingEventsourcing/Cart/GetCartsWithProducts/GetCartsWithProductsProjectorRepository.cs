using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.PostgreSql;
using Npgsql;

namespace UnderstandingEventsourcingExample.Cart.GetCartsWithProducts;

public sealed class GetCartsWithProductsProjectorRepository
{
    private readonly PostgreSqlHelper _sqlHelper;
    private readonly string _projectorName;
    private readonly string _schema;

    public GetCartsWithProductsProjectorRepository(string projectorName, string connectionString, string schema)
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

    public async Task<IEnumerable<CartProduct>> GetByProductId(Guid productId)
    {
        var sql = $"SELECT cart_id FROM {_schema}.get_carts_with_products_read_model WHERE product_id = @product_id";

        var parameters = new[]
        {
            new PostgreSqlParameter("product_id", productId)
        };

        try
        {
            var carts = await _sqlHelper.QueryAsync(sql, parameters, reader =>
            {
                var cartId = reader.GetGuid(0);
                return new CartProduct(cartId, productId);
            });
            return carts;
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get cart with products", ex);
        }
    }

    public async Task AddProductToCart(Guid cartId, Guid itemId, Guid productId, long sequenceNumber)
    {
        var sql = $@"INSERT INTO {_schema}.get_carts_with_products_read_model (cart_id, item_id, product_id)
                        VALUES (@cart_id, @item_id, @product_id)";

        var parameters = new[]
        {
            new PostgreSqlParameter("cart_id", cartId),
            new PostgreSqlParameter("item_id", itemId),
            new PostgreSqlParameter("product_id", productId)
        };

        try
        {
            await _sqlHelper.Transaction(async (conn, tx) =>
            {
                await _sqlHelper.ExecuteAsync(sql, parameters, conn, tx);
                await StoreProjectorState(sequenceNumber, conn, tx);
            });
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not add product to cart", ex);
        }
    }

    public async Task RemoveItemFromCart(Guid cartId, Guid itemId, long sequenceNumber)
    {
        var sql = $@"DELETE FROM {_schema}.get_carts_with_products_read_model 
                        WHERE cart_id = @cart_id AND item_id = @item_id";

        var parameters = new[]
        {
            new PostgreSqlParameter("cart_id", cartId),
            new PostgreSqlParameter("item_id", itemId)
        };

        try
        {
            await _sqlHelper.Transaction(async (conn, tx) =>
            {
                await _sqlHelper.ExecuteAsync(sql, parameters, conn, tx);
                await StoreProjectorState(sequenceNumber, conn, tx);
            });
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not remove product from cart", ex);
        }
    }

    public async Task RemoveAllItemsFromCart(Guid cartId, long sequenceNumber)
    {
        var sql = $@"DELETE FROM {_schema}.get_carts_with_products_read_model 
                        WHERE cart_id = @cart_id";

        var parameters = new[]
        {
            new PostgreSqlParameter("cart_id", cartId)
        };

        try
        {
            await _sqlHelper.Transaction(async (conn, tx) =>
            {
                await _sqlHelper.ExecuteAsync(sql, parameters, conn, tx);
                await StoreProjectorState(sequenceNumber, conn, tx);
            });
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not remove all products from cart", ex);
        }
    }

    public async Task SetLastProcessedSequenceNumber(long sequenceNumber)
    {
        try
        {
            await _sqlHelper.Transaction(async (conn, tx) =>
            {
                await StoreProjectorState(sequenceNumber, conn, tx);
            });
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not set sequence number", ex);
        }
    }

    private async Task StoreProjectorState(long sequenceNumber, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var sql = $@"INSERT INTO {_schema}.read_model_projector_state (projector_name, last_processed_sequence_number)
                        VALUES (@projector_name, @last_processed_sequence_number)
                        ON CONFLICT (projector_name)
                        DO UPDATE SET
                            last_processed_sequence_number = EXCLUDED.last_processed_sequence_number;";

        var parameters = new[]
        {
            new PostgreSqlParameter("projector_name", _projectorName),
            new PostgreSqlParameter("last_processed_sequence_number", sequenceNumber)
        };

        await _sqlHelper.ExecuteAsync(sql, parameters, connection, transaction);
    }
}
