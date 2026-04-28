# Скрипт для очистки старых таблиц с неправильными именами (с заглавной буквы)
# Используйте этот скрипт, если миграции создали таблицы с неправильными именами

Write-Host "--- Cleanup Old Tables Script ---"
Write-Host "This script will drop old tables with incorrect names (uppercase)"
Write-Host ""

# UserService
Write-Host "Cleaning up UserService database..."
docker exec postgres psql -U postgres -d userservice_db -c "DROP TABLE IF EXISTS \"Users\" CASCADE;" 2>$null
docker exec postgres psql -U postgres -d userservice_db -c "DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE;" 2>$null
Write-Host "UserService cleanup done."

# ChatService
Write-Host "Cleaning up ChatService database..."
docker exec postgres psql -U postgres -d chatservice_db -c "DROP TABLE IF EXISTS \"Messages\" CASCADE;" 2>$null
docker exec postgres psql -U postgres -d chatservice_db -c "DROP TABLE IF EXISTS \"Chats\" CASCADE;" 2>$null
docker exec postgres psql -U postgres -d chatservice_db -c "DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE;" 2>$null
Write-Host "ChatService cleanup done."

# FeedService
Write-Host "Cleaning up FeedService database..."
docker exec postgres psql -U postgres -d feedservice_db -c "DROP TABLE IF EXISTS \"Posts\" CASCADE;" 2>$null
docker exec postgres psql -U postgres -d feedservice_db -c "DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE;" 2>$null
Write-Host "FeedService cleanup done."

Write-Host ""
Write-Host "--- Cleanup Complete ---"
Write-Host "Now rebuild and restart services to apply correct migrations."




