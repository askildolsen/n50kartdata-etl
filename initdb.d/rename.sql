DO
$$
DECLARE
    row record;
BEGIN
    FOR row IN SELECT schemaname FROM pg_tables WHERE tablename = 'malemetode'
    LOOP
        EXECUTE 'ALTER SCHEMA ' || quote_ident(row.schemaname) || ' RENAME TO n50kartdata;';
    END LOOP;
END;
$$;
