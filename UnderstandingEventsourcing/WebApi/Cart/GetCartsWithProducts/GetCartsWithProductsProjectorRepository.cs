using Basses.SimpleEventStore.EventStore;
using Npgsql;

namespace UnderstandingEventsourcingExample.Cart.GetCartsWithProducts;

public sealed class GetCartsWithProductsProjectorRepository : IDisposable
{
    private NpgsqlConnection _connection;
    private readonly string _projectorName;
    private readonly string _schema;

    public GetCartsWithProductsProjectorRepository(string projectorName, string connectionString, string schema)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();
        _projectorName = projectorName;
        _schema = schema;
    }

    public async Task<long> GetLastProcessedSequenceNumber()
    {
        var sql = $"SELECT last_processed_sequence_number FROM {_schema}.read_model_projector_state WHERE projector_name = '{_projectorName}'";
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

    public async Task<IEnumerable<CartProduct>> GetByProductId(Guid productId)
    {
        var sql = $"SELECT cart_id FROM {_schema}.get_carts_with_products_read_model WHERE product_id = '{productId}'";
        var carts = new List<CartProduct>();

        try
        {
            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    var cartId = reader.GetGuid(0);
                    carts.Add(new CartProduct(cartId, productId));
                }
            }
            return carts;
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not get cart with products", ex);
        }
    }

    public async Task AddProductToCart(Guid cartId, Guid productId, long sequenceNumber)
    {
        var sql = $@"INSERT INTO {_schema}.get_carts_with_products_read_model (cart_id, product_id)
                        VALUES (@cart_id, @product_id)";

        try
        {
            using var transaction = _connection.BeginTransaction();

            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            cmd.Parameters.AddWithValue("cart_id", cartId);
            cmd.Parameters.AddWithValue("product_id", productId);

            await cmd.ExecuteNonQueryAsync();

            await StoreProjectorState(sequenceNumber);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not add product to cart", ex);
        }
    }

    public async Task RemoveProductFromCart(Guid cartId, Guid productId, long sequenceNumber)
    {
        var sql = $@"DELETE FROM {_schema}.get_carts_with_products_read_model 
                        WHERE cart_id = @cart_id AND product_id = @product_id";

        try
        {
            using var transaction = _connection.BeginTransaction();

            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            cmd.Parameters.AddWithValue("cart_id", cartId);
            cmd.Parameters.AddWithValue("product_id", productId);

            await cmd.ExecuteNonQueryAsync();

            await StoreProjectorState(sequenceNumber);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not remove product from cart", ex);
        }
    }

    public async Task RemoveAllProductsFromCart(Guid cartId, long sequenceNumber)
    {
        var sql = $@"DELETE FROM {_schema}.get_carts_with_products_read_model 
                        WHERE cart_id = @cart_id";

        try
        {
            using var transaction = _connection.BeginTransaction();

            using var cmd = new NpgsqlCommand(sql);
            cmd.Connection = _connection;
            cmd.Parameters.AddWithValue("cart_id", cartId);

            await cmd.ExecuteNonQueryAsync();

            await StoreProjectorState(sequenceNumber);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            throw new EventStoreException("Could not remove all products from cart", ex);
        }
    }

    private async Task StoreProjectorState(long sequenceNumber)
    {
        var sql = $@"INSERT INTO {_schema}.read_model_projector_state (projector_name, last_processed_sequence_number)
                        VALUES (@projector_name, @last_processed_sequence_number)
                        ON CONFLICT (projector_name)
                        DO UPDATE SET
                            last_processed_sequence_number = EXCLUDED.last_processed_sequence_number;";

        using var cmd = new NpgsqlCommand(sql);
        cmd.Connection = _connection;
        cmd.Parameters.AddWithValue("projector_name", _projectorName);
        cmd.Parameters.AddWithValue("last_processed_sequence_number", sequenceNumber);

        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
