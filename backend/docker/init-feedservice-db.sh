#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    DO \$\$
    BEGIN
        IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'feedservice_db') THEN
            CREATE DATABASE feedservice_db;
        END IF;
    END
    \$\$;
EOSQL




