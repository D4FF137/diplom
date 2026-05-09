#!/bin/bash
set -e

echo "Creating databases..."

# Создаем базы данных, если они не существуют
for db in companyservice_db userservice_db chatservice_db feedservice_db notificationservice_db taskservice_db; do
    echo "Creating database $db..."
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -tc "SELECT 1 FROM pg_database WHERE datname = '$db'" | grep -q 1 || \
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -c "CREATE DATABASE $db"
done

echo "All databases created successfully."
