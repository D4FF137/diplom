# Применение миграций вручную

Если миграции не применяются автоматически, выполните следующие команды:

## CompanyService

```bash
# Войти в контейнер
docker exec -it companyservice bash

# Применить миграции
dotnet ef database update --project /src/src/CompanyService

# Или извне контейнера
docker exec -it companyservice dotnet ef database update --project /src/src/CompanyService
```

## UserService

```bash
docker exec -it userservice dotnet ef database update --project /src/src/UserService
```

## ChatService

```bash
docker exec -it chatservice dotnet ef database update --project /src/src/ChatService
```

## FeedService

```bash
docker exec -it feedservice dotnet ef database update --project /src/src/FeedService
```

## NotificationService

```bash
docker exec -it notificationservice dotnet ef database update --project /src/src/NotificationService
```

## Альтернатива: Пересоздать базу данных

Если миграции не работают, можно удалить и пересоздать базу:

```bash
# Подключиться к PostgreSQL
docker exec -it postgres psql -U postgres

# Удалить и пересоздать базу
DROP DATABASE IF EXISTS companyservice_db;
CREATE DATABASE companyservice_db;
\q

# Перезапустить сервис (миграции применятся автоматически)
docker-compose restart companyservice
```




