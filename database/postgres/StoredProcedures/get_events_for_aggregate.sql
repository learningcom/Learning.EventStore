CREATE OR REPLACE FUNCTION get_events_for_aggregate
(
    aggregate_id varchar(255),
    application_name varchar(255),
    aggregate_type varchar(255),
    from_version int = 1
)
RETURNS SETOF jsonb AS $$
BEGIN
    SELECT e.event_data
    FROM event e
    INNER JOIN aggregate ag ON e.aggregate_key = ag.aggregate_key 
    WHERE ag.aggregate_id = aggregate_id 
        AND e.version > from_version
    ORDER BY e.version ASC;
END;
$$ LANGUAGE plpgsql;
