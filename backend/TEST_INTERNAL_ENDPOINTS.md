# Тестирование внутренних эндпоинтов

## Проверка работы внутренних эндпоинтов

### 1. Проверьте, что сервисы пересобраны

```powershell
cd backend
docker-compose build chatservice userservice notificationservice
docker-compose restart chatservice userservice notificationservice
```

### 2. Проверьте внутренние эндпоинты напрямую

```powershell
# Проверка ChatService внутреннего эндпоинта
docker exec chatservice curl http://localhost:5004/api/internal/chats/1

# Проверка UserService внутреннего эндпоинта
docker exec userservice curl http://localhost:5001/api/internal/users?companyId=1
```

### 3. Проверьте логи NotificationService

После отправки сообщения должно появиться:
- `[RabbitMQ] Received chat data: {...}` - если запрос успешен
- `Failed to get chat members for chat X: Unauthorized` - если запрос не прошел

### 4. Если все еще Unauthorized

Проверьте, что:
1. Сервисы пересобраны с новыми контроллерами
2. URL правильные: `/api/internal/chats/{id}` и `/api/internal/users?companyId={id}`
3. Запросы идут напрямую к сервисам, а не через Gateway

### 5. Альтернативное решение

Если внутренние эндпоинты не работают, можно передавать список участников прямо в событии MessageSentEvent.



