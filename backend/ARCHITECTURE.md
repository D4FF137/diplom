# Архитектура системы

## Обзор

Система корпоративной социальной сети построена на микросервисной архитектуре с полной изоляцией данных между компаниями (multi-tenancy).

## Компоненты системы

### 1. API Gateway

**Назначение**: Единая точка входа для всех клиентских запросов.

**Функции**:
- Маршрутизация запросов к соответствующим микросервисам
- Аутентификация и авторизация (JWT)
- Проверка принадлежности пользователя к компании
- Интеграция с RabbitMQ для асинхронной обработки

**Технологии**: ASP.NET Core, Ocelot (опционально) или кастомный роутинг

### 2. UserService

**Назначение**: Управление пользователями и их аутентификация.

**Функции**:
- Регистрация пользователей
- Аутентификация (JWT)
- Управление профилями пользователей
- Привязка пользователя к компании

**База данных**: PostgreSQL, таблица `Users` с `CompanyId`

### 3. CompanyService

**Назначение**: Управление компаниями и их настройками.

**Функции**:
- CRUD операции для компаний
- Управление настройками компаний
- Валидация существования компании

**База данных**: PostgreSQL, таблица `Companies`

### 4. FeedService

**Назначение**: Управление лентой новостей и постами.

**Функции**:
- Создание, чтение, обновление, удаление постов
- Лента новостей компании
- Лайки и комментарии

**База данных**: PostgreSQL, таблицы `Posts`, `Likes`, `Comments` с `CompanyId`

### 5. ChatService

**Назначение**: Система чатов в реальном времени.

**Функции**:
- Отправка и получение сообщений
- WebSocket соединения через SignalR
- Групповые и приватные чаты
- Изоляция чатов по компаниям

**База данных**: PostgreSQL, таблицы `Chats`, `Messages`, `ChatMembers` с `CompanyId`
**Real-time**: SignalR Hub с группами по компаниям

## Изоляция данных

### Уровень базы данных

#### Row-Level Security (RLS) в PostgreSQL

Каждая таблица защищена политиками RLS:

```sql
-- Пример для таблицы Posts
ALTER TABLE Posts ENABLE ROW LEVEL SECURITY;

CREATE POLICY company_isolation_policy ON Posts
    FOR ALL
    USING (company_id = current_setting('app.current_company_id')::int);
```

#### Схема базы данных

```
Companies (id, name, created_at)
  └── Users (id, company_id, email, ...)
  └── Posts (id, company_id, user_id, ...)
  └── Chats (id, company_id, name, ...)
      └── Messages (id, company_id, chat_id, ...)
```

### Уровень приложения

#### JWT Token Structure

```json
{
  "userId": "123",
  "companyId": "456",
  "email": "user@company.com",
  "exp": 1234567890
}
```

#### Middleware для изоляции

Каждый сервис проверяет `CompanyId` из JWT токена и устанавливает его в контекст запроса.

### Уровень кэширования (Redis)

Ключи сегментированы по `CompanyId`:

```
user:{companyId}:{userId}
post:{companyId}:{postId}
chat:{companyId}:{chatId}
```

### Уровень SignalR

Группы чатов изолированы:

```csharp
// Пользователь присоединяется только к чатам своей компании
await Groups.AddToGroupAsync(Context.ConnectionId, $"company_{companyId}_chat_{chatId}");
```

## Взаимодействие сервисов

### Синхронное взаимодействие

```
Client → API Gateway → Microservice → Database
```

### Асинхронное взаимодействие (RabbitMQ)

```
Client → API Gateway → RabbitMQ → Microservice → Database
```

#### Очереди RabbitMQ

- `user.created` - событие создания пользователя
- `post.created` - событие создания поста
- `message.sent` - событие отправки сообщения

## Схема базы данных

### Таблица Companies

```sql
CREATE TABLE Companies (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Таблица Users

```sql
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    CompanyId INT NOT NULL REFERENCES Companies(Id),
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100),
    LastName VARCHAR(100),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_company ON Users(CompanyId);
```

### Таблица Posts

```sql
CREATE TABLE Posts (
    Id SERIAL PRIMARY KEY,
    CompanyId INT NOT NULL REFERENCES Companies(Id),
    UserId INT NOT NULL REFERENCES Users(Id),
    Content TEXT NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_posts_company ON Posts(CompanyId);
```

### Таблица Chats

```sql
CREATE TABLE Chats (
    Id SERIAL PRIMARY KEY,
    CompanyId INT NOT NULL REFERENCES Companies(Id),
    Name VARCHAR(255),
    Type VARCHAR(50) NOT NULL, -- 'private' or 'group'
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_chats_company ON Chats(CompanyId);
```

### Таблица Messages

```sql
CREATE TABLE Messages (
    Id SERIAL PRIMARY KEY,
    CompanyId INT NOT NULL REFERENCES Companies(Id),
    ChatId INT NOT NULL REFERENCES Chats(Id),
    UserId INT NOT NULL REFERENCES Users(Id),
    Content TEXT NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_messages_company ON Messages(CompanyId);
CREATE INDEX idx_messages_chat ON Messages(ChatId);
```

## Миграции

### Создание миграции

```bash
cd src/UserService
dotnet ef migrations add InitialCreate --project . --startup-project .
```

### Применение миграций

```bash
dotnet ef database update
```

### Миграции в Docker

```bash
docker-compose exec userservice dotnet ef database update
```

## Точки разграничения компаний

### 1. API Gateway

- Проверка JWT токена
- Извлечение `CompanyId` из токена
- Добавление `CompanyId` в заголовки запроса

### 2. Микросервисы

- Получение `CompanyId` из заголовков
- Фильтрация всех запросов по `CompanyId`
- Валидация принадлежности ресурса к компании

### 3. База данных

- Row-Level Security политики
- Индексы по `CompanyId` для производительности
- Foreign Key constraints

### 4. Redis

- Префиксы ключей с `CompanyId`
- Отдельные каналы pub/sub для каждой компании

### 5. SignalR

- Группы с префиксом `company_{companyId}_`
- Проверка принадлежности к компании перед присоединением к группе

## Масштабирование

### Горизонтальное масштабирование

Каждый микросервис может масштабироваться независимо:

```yaml
services:
  feedservice:
    deploy:
      replicas: 3
```

### Вертикальное масштабирование

Увеличение ресурсов для конкретных сервисов в зависимости от нагрузки.

## Мониторинг

### Health Checks

Каждый сервис предоставляет endpoint `/health`:

- Проверка подключения к БД
- Проверка подключения к RabbitMQ
- Проверка подключения к Redis

### Логирование

Централизованное логирование через Serilog или аналогичное решение.

## Безопасность

### Аутентификация

- JWT токены с ограниченным временем жизни
- Refresh tokens для обновления доступа

### Авторизация

- Проверка `CompanyId` на каждом запросе
- Пользователь может принадлежать только одной компании
- Невозможность доступа к данным другой компании

### Защита данных

- Хеширование паролей (BCrypt)
- HTTPS в production
- Валидация всех входных данных

## Развертывание

### Docker Compose

Все сервисы запускаются через Docker Compose для локальной разработки и тестирования.

### Production

Рекомендуется использовать:
- Kubernetes для оркестрации
- Helm charts для управления конфигурацией
- CI/CD pipeline для автоматического развертывания


