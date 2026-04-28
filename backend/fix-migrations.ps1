# Скрипт для исправления миграций - создание таблицы __EFMigrationsHistory и записей

Write-Host "--- Fixing Migrations History ---"
Write-Host ""

# UserService
Write-Host "Fixing UserService migration history..."
docker exec postgres psql -U postgres -d userservice_db -c "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId VARCHAR(150) PRIMARY KEY, ProductVersion VARCHAR(32) NOT NULL);"
docker exec postgres psql -U postgres -d userservice_db -c "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20240101000000_InitialCreate', '8.0.0') ON CONFLICT (MigrationId) DO NOTHING;"
Write-Host "UserService fixed."

# ChatService
Write-Host "Fixing ChatService migration history..."
docker exec postgres psql -U postgres -d chatservice_db -c "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId VARCHAR(150) PRIMARY KEY, ProductVersion VARCHAR(32) NOT NULL);"
docker exec postgres psql -U postgres -d chatservice_db -c "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20240101000000_InitialCreate', '8.0.0') ON CONFLICT (MigrationId) DO NOTHING;"
Write-Host "ChatService fixed."

# FeedService
Write-Host "Fixing FeedService migration history..."
docker exec postgres psql -U postgres -d feedservice_db -c "CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId VARCHAR(150) PRIMARY KEY, ProductVersion VARCHAR(32) NOT NULL);"
docker exec postgres psql -U postgres -d feedservice_db -c "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20240101000000_InitialCreate', '8.0.0') ON CONFLICT (MigrationId) DO NOTHING;"
Write-Host "FeedService fixed."

Write-Host ""
Write-Host "--- Migration History Fixed ---"
Write-Host "Now check if tables exist and restart services:"
Write-Host "  docker-compose restart userservice chatservice feedservice"



