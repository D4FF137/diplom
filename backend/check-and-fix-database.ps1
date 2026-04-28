# Скрипт для проверки и исправления базы данных CompanyService

Write-Host "=== Проверка базы данных companyservice_db ===" -ForegroundColor Cyan

# Проверяем, существует ли база данных
Write-Host "`n1. Проверка существования базы данных..." -ForegroundColor Yellow
docker exec postgres psql -U postgres -c "\l" | Select-String "companyservice_db"

# Проверяем таблицы в базе данных
Write-Host "`n2. Проверка таблиц в базе данных..." -ForegroundColor Yellow
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"

# Проверяем миграции
Write-Host "`n3. Проверка примененных миграций..." -ForegroundColor Yellow
docker exec postgres psql -U postgres -d companyservice_db -c "SELECT * FROM \"__EFMigrationsHistory\";"

Write-Host "`n=== Применение миграций ===" -ForegroundColor Cyan

# Применяем миграции через dotnet ef
Write-Host "`n4. Применение миграций..." -ForegroundColor Yellow
docker exec companyservice dotnet ef database update --project /src/src/CompanyService

# Проверяем таблицы после миграции
Write-Host "`n5. Проверка таблиц после миграции..." -ForegroundColor Yellow
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"

Write-Host "`n=== Готово! ===" -ForegroundColor Green




