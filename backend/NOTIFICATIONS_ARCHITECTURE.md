# Архитектура микросервиса уведомлений

## Обзор

Микросервис уведомлений (`NotificationService`) отвечает за отслеживание и доставку счетчиков непрочитанных сообщений и постов пользователям в реальном времени через SignalR.

## Архитектурная схема потока

```
┌─────────────────┐
│  ChatService    │ ──[RabbitMQ]──> ┌──────────────────────┐
│  (новое         │                  │  NotificationService  │
│   сообщение)    │                  │  (RabbitMQ Consumer)  │
└─────────────────┘                  └──────────────────────┘
                                              │
┌─────────────────┐                          │
│  FeedService    │ ──[RabbitMQ]──>          │
│  (новый пост)   │                          │
└─────────────────┘                          │
                                              ▼
                                     ┌──────────────────────┐
                                     │  NotificationService │
                                     │  (SignalR Hub)       │
                                     └──────────────────────┘
                                              │
                                              │ WebSocket
                                              ▼
                                     ┌──────────────────────┐
                                     │  React Frontend      │
                                     │  (useNotifications)  │
                                     └──────────────────────┘
```

## Компоненты

### Backend

#### 1. Модели данных (NotificationService/Models/)
- `UnreadMessage` - счетчики непрочитанных сообщений по чатам
- `UnreadFeed` - счетчик непрочитанных постов в ленте

#### 2. Сервисы (NotificationService/Services/)
- `INotificationService` / `NotificationService` - основная бизнес-логика работы со счетчиками
- `RabbitMQConsumerService` - фоновый сервис для подписки на события из RabbitMQ
- `IUserInfoService` / `UserInfoService` - получение информации о пользователях и участниках чатов

#### 3. SignalR Hub (NotificationService/Hubs/)
- `NotificationsHub` - хаб для отправки уведомлений через WebSocket
- `UserIdProvider` - провайдер для идентификации пользователей по JWT токену

#### 4. API (NotificationService/Controllers/)
- `NotificationsController` - REST API для получения счетчиков и пометки как прочитанных

### Frontend

#### 1. WebSocket сервис (services/notificationWebSocket.ts)
- Подключение к SignalR хабу уведомлений
- Автоматическое переподключение

#### 2. React хук (hooks/useNotifications.ts)
- Управление состоянием счетчиков
- Методы для пометки как прочитанных
- Подписка на обновления через WebSocket

#### 3. Компоненты
- `ChatSidebar` - отображение бейджей непрочитанных сообщений
- `ChatWindow` - автоматическая пометка чата как прочитанного при открытии
- `FeedCenter` - отображение счетчика непрочитанных постов

## Поток данных

### 1. Создание сообщения в чате

1. Пользователь отправляет сообщение через `ChatService`
2. `ChatService` публикует событие `MessageSentEvent` в RabbitMQ (routing key: `message.sent`)
3. `RabbitMQConsumerService` получает событие
4. Сервис получает список участников чата через HTTP запрос к `ChatService`
5. Для каждого участника (кроме отправителя) инкрементируется счетчик в БД
6. Через SignalR отправляются обновленные счетчики всем затронутым пользователям

### 2. Создание поста в ленте

1. Пользователь создает пост через `FeedService`
2. `FeedService` публикует событие `PostCreatedEvent` в RabbitMQ (routing key: `post.created`)
3. `RabbitMQConsumerService` получает событие
4. Сервис получает список пользователей компании через HTTP запрос к `UserService`
5. Для каждого пользователя (кроме автора) инкрементируется счетчик ленты в БД
6. Через SignalR отправляются обновленные счетчики всем затронутым пользователям

### 3. Пометка как прочитанного

1. Пользователь открывает чат или ленту
2. Frontend вызывает API `POST /api/notifications/chats/{id}/read` или `POST /api/notifications/feed/read`
3. `NotificationService` сбрасывает счетчик в БД
4. Через SignalR отправляются обновленные счетчики пользователю

## База данных

### Таблицы

#### `unreadmessages`
- `id` (PK)
- `companyid` (Index)
- `chatid` (Index)
- `userid` (Index)
- `count` - количество непрочитанных сообщений
- `lastupdatedat` - время последнего обновления
- Уникальный индекс: `(chatid, userid, companyid)`

#### `unreadfeeds`
- `id` (PK)
- `companyid` (Index)
- `userid` (Index)
- `count` - количество непрочитанных постов
- `lastreadat` - время последнего прочтения
- `lastupdatedat` - время последнего обновления
- Уникальный индекс: `(userid, companyid)`

## API Endpoints

### GET /api/notifications/counters
Получить текущие счетчики для авторизованного пользователя.

**Response:**
```json
{
  "chatUnread": {
    "1": "5",
    "2": "3"
  },
  "feedUnread": 10
}
```

### POST /api/notifications/chats/{chatId}/read
Пометить чат как прочитанный.

### POST /api/notifications/feed/read
Пометить ленту как прочитанную.

## SignalR Events

### notificationCounters
Отправляется пользователю при изменении счетчиков.

**Payload:**
```json
{
  "chatUnread": {
    "1": "5",
    "2": "3"
  },
  "feedUnread": 10
}
```

## Конфигурация

### Environment Variables

- `JWT_SECRET` - секретный ключ для JWT
- `JWT_ISSUER` - издатель JWT токенов
- `JWT_AUDIENCE` - аудитория JWT токенов
- `RABBITMQ_HOST` - хост RabbitMQ
- `RABBITMQ_PORT` - порт RabbitMQ
- `RABBITMQ_USER` - пользователь RabbitMQ
- `RABBITMQ_PASSWORD` - пароль RabbitMQ
- `CHAT_SERVICE_URL` - URL ChatService для получения участников чатов
- `USER_SERVICE_URL` - URL UserService для получения пользователей компании

## Best Practices

1. **Event-Driven Architecture**: Использование RabbitMQ для слабой связанности сервисов
2. **Idempotency**: Операции инкремента и сброса счетчиков идемпотентны
3. **Real-time Updates**: SignalR для мгновенной доставки обновлений
4. **Scalability**: Микросервис может масштабироваться независимо
5. **Resilience**: Автоматическое переподключение WebSocket и обработка ошибок RabbitMQ

## Улучшения для продакшена

1. **Service-to-Service Authentication**: Использование внутренних токенов для межсервисных вызовов
2. **Caching**: Кэширование списков участников чатов и пользователей компании
3. **Batch Processing**: Группировка обновлений счетчиков для уменьшения нагрузки на БД
4. **Monitoring**: Добавление метрик и логирования для отслеживания производительности
5. **Rate Limiting**: Ограничение частоты обновлений счетчиков




