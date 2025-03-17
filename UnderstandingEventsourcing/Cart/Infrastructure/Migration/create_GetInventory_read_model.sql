create table if not exists cart.get_inventory_read_model (
    product_id varchar(36) not null, 
    inventory integer, 
    primary key (product_id)
);