# Быстрое исправление проблемы с таблицей companies

## Просто скопируйте и выполните эти команды по порядку:

### 1. Проверить, что база данных существует:
```powershell
docker exec postgres psql -U postgres -c "\l" | findstr companyservice_db
```

### 2. Проверить, какие таблицы есть в базе:
```powershell
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"
```

### 3. Создать таблицу вручную (самый быстрый способ):
```powershell
docker exec postgres psql -U postgres -d companyservice_db -c "CREATE TABLE IF NOT EXISTS companies (id SERIAL PRIMARY KEY, name VARCHAR(255) NOT NULL, createdat TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP);"
```

### 4. Проверить, что таблица создана:
```powershell
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"
```

### 5. Попробовать создать компанию снова в Postman

---

## Если это не помогло, пересоздайте базу:

```powershell
# Удалить базу
docker exec postgres psql -U postgres -c "DROP DATABASE IF EXISTS companyservice_db;"

# Создать базу заново
docker exec postgres psql -U postgres -c "CREATE DATABASE companyservice_db;"

# Перезапустить сервис
docker-compose restart companyservice

# Подождать 30 секунд
Start-Sleep -Seconds 30

# Проверить таблицы
docker exec postgres psql -U postgres -d companyservice_db -c "\dt"
```

---

## Проверить логи CompanyService:

```powershell
docker-compose logs companyservice --tail 50
```

Ищите строки с "migration" или "error" в логах.




