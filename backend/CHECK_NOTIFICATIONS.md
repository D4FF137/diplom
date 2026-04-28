# Проверка работы уведомлений

## Шаги для диагностики:

### 1. Проверьте логи NotificationService

```powershell
docker logs notificationservice --tail 100
```

Ищите:
- `[RabbitMQ] Message sent event received` - при отправке сообщения
- `[RabbitMQ] Post created event received` - при создании поста
- `[NotificationService] Sending counters to user` - при отправке уведомления
- `[NotificationsHub] User connected` - при подключении WebSocket

### 2. Проверьте, что RabbitMQ получает события

```powershell
# Проверьте очереди в RabbitMQ
docker exec rabbitmq rabbitmqctl list_queues
docker exec rabbitmq rabbitmqctl list_exchanges
```

### 3. Проверьте базу данных уведомлений

```powershell
docker exec postgres psql -U postgres -d notificationservice_db -c "SELECT * FROM \"UnreadMessages\";"
docker exec postgres psql -U postgres -d notificationservice_db -c "SELECT * FROM \"UnreadFeeds\";"
```

### 4. Проверьте, что события публикуются

При отправке сообщения в ChatService должны быть логи:
- `PublishMessageSentAsync` вызывается

При создании поста в FeedService должны быть логи:
- `PublishPostCreatedAsync` вызывается

### 5. Проверьте WebSocket подключение

В консоли браузера должно быть:
- `[Notifications] WebSocket connected successfully`
- `[Notifications] Received counter update: {...}`

### 6. Проверьте userId в токене

В консоли браузера выполните:
```javascript
const token = localStorage.getItem('token');
const payload = JSON.parse(atob(token.split('.')[1]));
console.log('User ID from token:', payload.nameid);
console.log('Company ID from token:', payload.companyId);
```

### 7. Проверьте, что userId правильно извлекается в SignalR

В логах NotificationService должно быть:
- `[NotificationsHub] User connected: ConnectionId=..., UserId=...`
- Если UserId=0, значит токен не распознается

## Возможные проблемы:

1. **События не публикуются в RabbitMQ**
   - Проверьте, что ChatService и FeedService запущены
   - Проверьте логи этих сервисов

2. **События не получаются NotificationService**
   - Проверьте, что RabbitMQConsumerService запущен
   - Проверьте логи NotificationService

3. **userId не извлекается из токена**
   - Проверьте, что токен содержит claim `nameid`
   - Проверьте, что UserIdProvider правильно настроен

4. **WebSocket не отправляет уведомления**
   - Проверьте, что пользователь подключен к правильной группе
   - Проверьте логи NotificationService при отправке уведомления



