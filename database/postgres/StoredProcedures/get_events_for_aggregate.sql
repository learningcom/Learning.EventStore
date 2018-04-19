CREATE OR REPLACE FUNCTION get_events_for_aggregate
(
    "AggregateId" varchar(255),
    "ApplicationName" varchar(255),
    "AggregateType" varchar(255),
    "FromVersion" int = 1
)
RETURNS SETOF text AS $$
BEGIN
    RETURN query
    SELECT e.event_data #>> '{}'
    FROM event e
    INNER JOIN aggregate ag ON e.aggregate_key = ag.aggregate_key 
    WHERE ag.aggregate_id = "AggregateId"
        AND ag.application_name = "ApplicationName"
        AND e.version > "FromVersion"
    ORDER BY e.version ASC;
END;
$$ LANGUAGE plpgsql;
