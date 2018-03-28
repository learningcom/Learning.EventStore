CREATE OR REPLACE FUNCTION save_event_for_aggregate
(
    aggregate_id varchar(255), 
    aggregate_type varchar(255),
    application_name varchar(255),
    version int,
    time_stamp timestamp,
    event_type varchar(255) ,
    event_data jsonb
)
RETURNS void AS $$
DECLARE aggregate_key bigint;
BEGIN
    aggregate_key := (SELECT aggregate_key 
						FROM aggregate 
						WHERE aggregate_id = aggregate_id
						AND aggregate_type = aggregate_type
						AND application_name = application_name);
	
    IF aggregate_key IS NULL THEN        
		INSERT INTO aggregate
		(
			aggregate_id, 
			aggregate_type,
			application_name
		)
		SELECT aggregate_id,
				aggregate_type, 
				application_name;

		aggregate_key := LASTVAL();
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
        aggregate_key,
        time_stamp,
        version,
        event_type,
        event_data
    );
END;
$$ LANGUAGE plpgsql;

