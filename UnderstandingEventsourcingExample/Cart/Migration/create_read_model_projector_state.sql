create table cart.read_model_projector_state (
    projector_id varchar(36) not null, 
    last_processed_sequence_number integer, 
    primary key (projector_id)
);