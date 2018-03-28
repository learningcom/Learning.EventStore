CREATE TABLE event
(
    event_id bigserial PRIMARY KEY,
    aggregate_key bigint REFERENCES aggregate,
    time_stamp timestamp with time zone NOT NULL,
    version int NOT NULL,
    event_type varchar(255) NOT NULL,
    event_data jsonb NOT NULL,
    audit_create_date timestamp with time zone DEFAULT current_timestamp,
    UNIQUE(aggregate_key, version)
);
