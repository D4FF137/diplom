# Корпоративная социальная сеть

Дипломный проект на ASP.NET Core (C#) + React с микросервисной архитектурой и полной изоляцией компаний.

## Архитектура

Система построена на микросервисной архитектуре с полной изоляцией данных между компаниями:

- **API Gateway** - единая точка входа для всех запросов
- **UserService** - управление пользователями
- **CompanyService** - управление компаниями
- **FeedService** - лента новостей
- **ChatService** - система чатов с WebSocket (SignalR)

### Технологический стек

- **Backend**: ASP.NET Core 8.0, C#
- **Database**: PostgreSQL с row-level security
- **Message Queue**: RabbitMQ
- **Cache/Pub-Sub**: Redis
- **Real-time**: SignalR
- **Authentication**: JWT
- **Containerization**: Docker, Docker Compose

## Структура проекта

```
.
├── src/
│   ├── Gateway/              # API Gateway
│   ├── UserService/          # Сервис пользователей
│   ├── CompanyService/       # Сервис компаний
│   ├── FeedService/          # Сервис ленты новостей
│   ├── ChatService/          # Сервис чатов
│   └── ReactApp/             # React фронтенд
├── shared/
│   ├── Common/               # Общие утилиты
│   ├── Contracts/            # Контракты для RabbitMQ
│   └── Models/               # Общие модели данных
├── docker/
│   ├── Dockerfile.gateway
│   ├── Dockerfile.userservice
│   ├── Dockerfile.companyservice
│   ├── Dockerfile.feedservice
│   └── Dockerfile.chatservice
├── docker-compose.yml
├── .env.example
└── README.md
```

## Изоляция компаний

Система обеспечивает полную изоляцию данных между компаниями на всех уровнях:

1. **База данных**: Row-level security в PostgreSQL, каждая таблица фильтруется по `CompanyId`
2. **API Gateway**: Проверка принадлежности пользователя к компании перед маршрутизацией
3. **Микросервисы**: Все запросы содержат `CompanyId` в контексте
4. **Redis**: Ключи сегментированы по `CompanyId`
5. **SignalR**: Группы чатов изолированы по компаниям
6. **JWT**: Токен содержит `CompanyId`, доступ только к своей компании

## Быстрый старт

### Предварительные требования

- Docker Desktop
- .NET 8.0 SDK (для локальной разработки)
- Node.js 18+ (для React приложения)

### 1. Клонирование и настройка

Создайте файл `.env` в корне проекта со следующим содержимым:

```env
# Database Configuration
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres_password
POSTGRES_DB=corporate_social_network

# Redis Configuration
REDIS_HOST=redis
REDIS_PORT=6379

# RabbitMQ Configuration
RABBITMQ_HOST=rabbitmq
RABBITMQ_PORT=5672
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest

# JWT Configuration
JWT_SECRET=your-super-secret-jwt-key-change-in-production-min-32-chars
JWT_ISSUER=CorporateSocialNetwork
JWT_AUDIENCE=CorporateSocialNetwork
JWT_EXPIRATION_MINUTES=60
```

**Важно**: Измените `JWT_SECRET` на случайную строку минимум 32 символа для production!

### 2. Сборка и запуск

```bash
# Сборка всех образов
docker-compose build

# Запуск всех сервисов
docker-compose up -d

# Просмотр логов
docker-compose logs -f
```

### 3. Применение миграций

```bash
# Применить миграции для каждого сервиса
docker-compose exec userservice dotnet ef database update
docker-compose exec companyservice dotnet ef database update
docker-compose exec feedservice dotnet ef database update
docker-compose exec chatservice dotnet ef database update
```

### 4. Проверка работоспособности

```bash
# Health checks
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
curl http://localhost:5004/health
```

## API Endpoints

### Gateway (http://localhost:5000)

- `POST /api/auth/register` - Регистрация пользователя
- `POST /api/auth/login` - Вход в систему
- `GET /api/users` - Список пользователей (только своей компании)
- `GET /api/companies` - Информация о компании
- `GET /api/feed` - Лента новостей
- `POST /api/feed/posts` - Создать пост
- `GET /api/chat/messages` - Получить сообщения
- `POST /api/chat/messages` - Отправить сообщение

## Разработка

### Локальная разработка

```bash
# Запустить только инфраструктуру (PostgreSQL, RabbitMQ, Redis)
docker-compose up -d postgres rabbitmq redis

# Запустить сервисы локально через dotnet run
cd src/UserService
dotnet run
```

### Добавление миграций

```bash
# В директории сервиса
dotnet ef migrations add MigrationName --project . --startup-project .
```

## Тестирование

```bash
# Запуск всех тестов
dotnet test

# Запуск тестов конкретного проекта
dotnet test src/UserService.Tests
```

## Мониторинг

- **Health Checks**: `/health` endpoint на каждом сервисе
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **PostgreSQL**: localhost:5432

## Масштабирование

Система поддерживает горизонтальное масштабирование:

```bash
# Запустить несколько экземпляров сервиса
docker-compose up -d --scale feedservice=3
```

## Безопасность

- JWT токены содержат `CompanyId` и проверяются на каждом запросе
- Row-level security в PostgreSQL предотвращает доступ к чужим данным
- Все запросы проходят через API Gateway с проверкой прав доступа

## Документация

- [ARCHITECTURE.md](ARCHITECTURE.md) - Детальная архитектура системы
- [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) - Структура проекта
- [BUILD.md](BUILD.md) - Инструкции по сборке и запуску
- [API_EXAMPLES.md](API_EXAMPLES.md) - Примеры API запросов
- [DEPLOYMENT.md](DEPLOYMENT.md) - Руководство по развертыванию в production

## Лицензия

Дипломный проект

