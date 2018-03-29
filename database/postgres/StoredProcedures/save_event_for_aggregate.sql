CREATE OR REPLACE FUNCTION save_event_for_aggregate
(
    "AggregateId" varchar(255), 
    "AggregateType" varchar(255),
    "ApplicationName" varchar(255),
    "Version" int,
    "TimeStamp" timestamp with time zone,
    "EventType" varchar(255),
    "EventData" text
)
RETURNS void AS $$
DECLARE _aggregate_key bigint;
BEGIN
    _aggregate_key := (SELECT aggregate_key 
                        FROM aggregate 
                        WHERE aggregate_id = "AggregateId"
                        AND aggregate_type = "AggregateType"
                        AND application_name = "ApplicationName");
	
    IF _aggregate_key IS NULL THEN        
        INSERT INTO aggregate
        (
            aggregate_id, 
            aggregate_type,
            application_name
        )
        SELECT "AggregateId",
                "AggregateType", 
                "ApplicationName";

		_aggregate_key := LASTVAL();
	END IF;

    INSERT INTO event
    (
        aggregate_key,
        time_stamp,
        version,
        event_type,
        event_data
    )
    VALUES
    (
        _aggregate_key,
        "TimeStamp",
        "Version",
        "EventType",
        to_json("EventData"::text)
    );
END;
$$ LANGUAGE plpgsql;
