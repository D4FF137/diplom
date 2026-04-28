# Примеры API запросов

## Базовый URL

Все запросы идут через Gateway: `http://localhost:5000`

## Аутентификация

### Регистрация пользователя

```http
POST /api/auth/register
Content-Type: application/json

{
  "companyId": 1,
  "email": "user@example.com",
  "password": "password123",
  "firstName": "John",
  "lastName": "Doe"
}
```

### Вход в систему

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Ответ:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "companyId": 1,
  "email": "user@example.com"
}
```

## Компании

### Получить все компании

```http
GET /api/companies
```

### Получить компанию по ID

```http
GET /api/companies/1
```

### Создать компанию

```http
POST /api/companies
Content-Type: application/json

{
  "name": "Acme Corporation"
}
```

## Пользователи

Все запросы требуют JWT токен в заголовке `Authorization: Bearer <token>`

### Получить всех пользователей своей компании

```http
GET /api/users
Authorization: Bearer <token>
```

### Получить пользователя по ID

```http
GET /api/users/1
Authorization: Bearer <token>
```

### Обновить пользователя

```http
PUT /api/users/1
Authorization: Bearer <token>
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@example.com"
}
```

### Удалить пользователя

```http
DELETE /api/users/1
Authorization: Bearer <token>
```

## Лента новостей

### Получить ленту новостей

```http
GET /api/feed?skip=0&take=20
Authorization: Bearer <token>
```

### Получить пост по ID

```http
GET /api/feed/posts/1
Authorization: Bearer <token>
```

### Создать пост

```http
POST /api/feed/posts
Authorization: Bearer <token>
Content-Type: application/json

{
  "content": "Это мой первый пост в корпоративной социальной сети!"
}
```

### Обновить пост

```http
PUT /api/feed/posts/1
Authorization: Bearer <token>
Content-Type: application/json

{
  "content": "Обновленный текст поста"
}
```

### Удалить пост

```http
DELETE /api/feed/posts/1
Authorization: Bearer <token>
```

## Чаты

### Получить все чаты своей компании

```http
GET /api/chat/chats
Authorization: Bearer <token>
```

### Создать чат

```http
POST /api/chat/chats
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Общий чат",
  "type": "group"
}
```

### Получить сообщения чата

```http
GET /api/chat/messages/chat/1?skip=0&take=50
Authorization: Bearer <token>
```

### Отправить сообщение

```http
POST /api/chat/messages
Authorization: Bearer <token>
Content-Type: application/json

{
  "chatId": 1,
  "content": "Привет всем!"
}
```

## WebSocket (SignalR)

### Подключение к чату

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5004/chatHub", {
        accessTokenFactory: () => "your-jwt-token"
    })
    .build();

// Присоединиться к чату
await connection.invoke("JoinChat", chatId);

// Отправить сообщение
await connection.invoke("SendMessage", chatId, "Привет!");

// Получить сообщение
connection.on("ReceiveMessage", (message) => {
    console.log("Новое сообщение:", message);
});
```

## Health Checks

### Проверить здоровье сервисов

```http
GET /health
```

## Примеры с curl

### Регистрация

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "companyId": 1,
    "email": "user@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### Вход

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

### Получить пользователей (с токеном)

```bash
TOKEN="your-jwt-token-here"

curl -X GET http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN"
```

### Создать пост

```bash
TOKEN="your-jwt-token-here"

curl -X POST http://localhost:5000/api/feed/posts \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Мой первый пост!"
  }'
```

## Изоляция компаний

Важно: все запросы автоматически фильтруются по `CompanyId` из JWT токена. Пользователь может видеть и изменять только данные своей компании.

### Пример: пользователь из компании 1 не может получить данные компании 2

```bash
# Токен пользователя из компании 1
TOKEN_COMPANY1="token-for-company-1-user"

# Попытка получить пользователей - вернутся только пользователи компании 1
curl -X GET http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN_COMPANY1"
```

Даже если в базе данных есть пользователи из других компаний, они не будут возвращены благодаря изоляции на уровне:
- JWT токена (содержит CompanyId)
- Middleware (проверяет CompanyId)
- Базы данных (Row-Level Security)
- Сервисов (фильтрация по CompanyId)


