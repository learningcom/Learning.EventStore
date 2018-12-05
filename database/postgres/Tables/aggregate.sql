CREATE TABLE aggregate
(
    aggregate_key bigserial PRIMARY KEY,
    aggregate_id varchar NOT NULL,
    aggregate_type varchar(255) NOT NULL,
    application_name varchar(255) NOT NULL,
    audit_create_date timestamp with time zone DEFAULT current_timestamp,
    UNIQUE(aggregate_id, application_name, aggregate_type)
);
