create table if not exists cart.get_carts_with_products_read_model (
    cart_id uuid not null,
    product_id uuid not null,
    primary key (cart_id, product_id)
);