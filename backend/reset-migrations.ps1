# Скрипт для сброса истории миграций и принудительного применения миграций

Write-Host "--- Reset Migrations Script ---"
Write-Host "This script will clear migration history and force re-application of migrations"
Write-Host ""

# UserService
Write-Host "Resetting UserService migrations..."
docker exec postgres psql -U postgres -d userservice_db -c "DELETE FROM __EFMigrationsHistory;" 2>$null
docker exec postgres psql -U postgres -d userservice_db -c "DROP TABLE IF EXISTS users CASCADE;" 2>$null
Write-Host "UserService reset done."

# ChatService
Write-Host "Resetting ChatService migrations..."
docker exec postgres psql -U postgres -d chatservice_db -c "DELETE FROM __EFMigrationsHistory;" 2>$null
docker exec postgres psql -U postgres -d chatservice_db -c "DROP TABLE IF EXISTS messages CASCADE;" 2>$null
docker exec postgres psql -U postgres -d chatservice_db -c "DROP TABLE IF EXISTS chats CASCADE;" 2>$null
docker exec postgres psql -U postgres -d chatservice_db -c "DROP TABLE IF EXISTS chatmembers CASCADE;" 2>$null
Write-Host "ChatService reset done."

# FeedService
Write-Host "Resetting FeedService migrations..."
docker exec postgres psql -U postgres -d feedservice_db -c "DELETE FROM __EFMigrationsHistory;" 2>$null
docker exec postgres psql -U postgres -d feedservice_db -c "DROP TABLE IF EXISTS posts CASCADE;" 2>$null
docker exec postgres psql -U postgres -d feedservice_db -c "DROP TABLE IF EXISTS likes CASCADE;" 2>$null
docker exec postgres psql -U postgres -d feedservice_db -c "DROP TABLE IF EXISTS comments CASCADE;" 2>$null
Write-Host "FeedService reset done."

Write-Host ""
Write-Host "--- Reset Complete ---"
Write-Host "Now restart services to apply migrations:"
Write-Host "  docker-compose restart userservice chatservice feedservice"
Write-Host ""
Write-Host "Or rebuild and restart all services:"
Write-Host "  docker-compose down"
Write-Host "  docker-compose build --no-cache"
Write-Host "  docker-compose up -d"

