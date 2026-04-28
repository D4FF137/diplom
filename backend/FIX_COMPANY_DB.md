# Исправление проблемы с таблицей companies

## Шаг 1: Проверить состояние базы данных

Выполните эти команды в PowerShell или командной строке:

```powershell
# Проверить, существует ли база данных
docker exec postgres psql -U postgres -c "\l" | findstr companyservice_db

# Проверить таблицы в базе данных
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"

# Проверить примененные миграции
docker exec postgres psql -U postgres -d companyservice_db -c "SELECT * FROM \"__EFMigrationsHistory\";"
```

## Шаг 2: Применить миграции вручную

### Вариант A: Через dotnet ef (если установлен в контейнере)

```powershell
# Применить миграции
docker exec companyservice dotnet ef database update --project /src/src/CompanyService
```

### Вариант B: Пересоздать базу данных

```powershell
# Удалить и пересоздать базу данных
docker exec postgres psql -U postgres -c "DROP DATABASE IF EXISTS companyservice_db;"
docker exec postgres psql -U postgres -c "CREATE DATABASE companyservice_db;"

# Перезапустить сервис (миграции применятся автоматически)
docker-compose restart companyservice
```

### Вариант C: Создать таблицу вручную (быстрое решение)

```powershell
# Подключиться к базе и создать таблицу
docker exec postgres psql -U postgres -d companyservice_db -c @"
CREATE TABLE IF NOT EXISTS companies (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
"@
```

## Шаг 3: Проверить результат

```powershell
# Проверить, что таблица создана
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"

# Должна быть таблица "companies"
```

## Шаг 4: Проверить логи CompanyService

```powershell
# Проверить логи на наличие ошибок миграций
docker-compose logs companyservice | Select-String -Pattern "migration|error" -Context 2
```

## Быстрое решение (одна команда)

Если ничего не помогло, выполните это:

```powershell
# Пересоздать базу и перезапустить сервис
docker exec postgres psql -U postgres -c "DROP DATABASE IF EXISTS companyservice_db; CREATE DATABASE companyservice_db;"
docker-compose restart companyservice

# Подождать 30 секунд и проверить таблицы
Start-Sleep -Seconds 30
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"
```




