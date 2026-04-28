# Инструкция по сборке и запуску

## Предварительные требования

- Docker Desktop (для запуска через Docker Compose)
- .NET 8.0 SDK (для локальной разработки)
- Node.js 18+ (для React приложения, опционально)

## Быстрый старт с Docker Compose

### 1. Клонирование проекта

```bash
git clone <repository-url>
cd coursework
```

### 2. Настройка переменных окружения

```bash
# Скопируйте .env.example в .env
cp .env.example .env

# Отредактируйте .env файл при необходимости
# Особенно важно изменить JWT_SECRET для production
```

### 3. Сборка образов

```bash
docker-compose build
```

### 4. Запуск всех сервисов

```bash
docker-compose up -d
```

### 5. Применение миграций базы данных

Миграции применяются автоматически при запуске сервисов через `Program.cs`, но можно применить вручную:

```bash
# Для каждого сервиса
docker-compose exec userservice dotnet ef database update
docker-compose exec companyservice dotnet ef database update
docker-compose exec feedservice dotnet ef database update
docker-compose exec chatservice dotnet ef database update
```

### 6. Проверка работоспособности

```bash
# Health checks
curl http://localhost:5000/health  # Gateway
curl http://localhost:5001/health  # UserService
curl http://localhost:5002/health  # CompanyService
curl http://localhost:5003/health  # FeedService
curl http://localhost:5004/health  # ChatService
```

## Локальная разработка

### 1. Запуск инфраструктуры

```bash
# Запустить только PostgreSQL, RabbitMQ, Redis
docker-compose up -d postgres rabbitmq redis
```

### 2. Настройка переменных окружения

Убедитесь, что в `appsettings.json` каждого сервиса указаны правильные строки подключения.

### 3. Запуск сервисов локально

```bash
# Terminal 1 - Gateway
cd src/Gateway
dotnet run

# Terminal 2 - UserService
cd src/UserService
dotnet run

# Terminal 3 - CompanyService
cd src/CompanyService
dotnet run

# Terminal 4 - FeedService
cd src/FeedService
dotnet run

# Terminal 5 - ChatService
cd src/ChatService
dotnet run
```

### 4. Применение миграций локально

```bash
# В директории каждого сервиса
cd src/UserService
dotnet ef database update

cd ../CompanyService
dotnet ef database update

cd ../FeedService
dotnet ef database update

cd ../ChatService
dotnet ef database update
```

## Создание новых миграций

```bash
# В директории сервиса
cd src/UserService

# Создать новую миграцию
dotnet ef migrations add MigrationName --project . --startup-project .

# Применить миграцию
dotnet ef database update
```

## Тестирование

### Запуск тестов

```bash
# Все тесты
dotnet test

# Конкретный проект
dotnet test src/UserService.Tests
```

## Остановка сервисов

```bash
# Остановить все сервисы
docker-compose down

# Остановить и удалить volumes
docker-compose down -v
```

## Пересборка после изменений

```bash
# Пересобрать конкретный сервис
docker-compose build userservice

# Пересобрать все
docker-compose build

# Пересобрать и перезапустить
docker-compose up -d --build
```

## Просмотр логов

```bash
# Все сервисы
docker-compose logs -f

# Конкретный сервис
docker-compose logs -f userservice

# Последние 100 строк
docker-compose logs --tail=100 userservice
```

## Доступ к базам данных

```bash
# PostgreSQL
docker-compose exec postgres psql -U postgres -d userservice_db

# Redis CLI
docker-compose exec redis redis-cli

# RabbitMQ Management UI
# Откройте http://localhost:15672 (guest/guest)
```

## Масштабирование

```bash
# Запустить несколько экземпляров сервиса
docker-compose up -d --scale feedservice=3
```

## Troubleshooting

### Проблемы с подключением к БД

1. Убедитесь, что PostgreSQL запущен: `docker-compose ps postgres`
2. Проверьте строки подключения в `appsettings.json`
3. Проверьте логи: `docker-compose logs postgres`

### Проблемы с миграциями

1. Убедитесь, что база данных создана
2. Проверьте права доступа пользователя PostgreSQL
3. Удалите и пересоздайте миграции при необходимости

### Проблемы с RabbitMQ

1. Проверьте подключение: `docker-compose logs rabbitmq`
2. Убедитесь, что exchange создан через Management UI

### Проблемы с Redis

1. Проверьте подключение: `docker-compose logs redis`
2. Проверьте доступность: `docker-compose exec redis redis-cli ping`


