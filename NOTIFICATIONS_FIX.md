# Исправление уведомлений

## Что было сделано:

1. ✅ Исправлено множественное подключение WebSocket
2. ✅ Добавлено подробное логирование во всех компонентах
3. ✅ Добавлены визуальные индикаторы уведомлений

## Как проверить, что уведомления работают:

### 1. Перезапустите сервисы

```powershell
cd backend
docker-compose restart notificationservice
```

### 2. Откройте консоль браузера (F12)

Должны появиться сообщения:
- `[Notifications] WebSocket connected successfully`
- `[Notifications] Initial counters received: {...}`

### 3. Проверьте логи NotificationService

```powershell
docker logs notificationservice -f
```

При отправке сообщения должно появиться:
- `[RabbitMQ] Message sent event received: ChatId=..., UserId=..., CompanyId=...`
- `[RabbitMQ] Chat ... has X members`
- `[RabbitMQ] Incrementing unread count for user ... in chat ...`
- `[NotificationService] Sending counters to user ...`
- `[NotificationsHub] User connected: ConnectionId=..., UserId=...`

### 4. Проверьте визуальные индикаторы

- В заголовке "Чаты" должен быть красный badge с количеством непрочитанных сообщений
- В заголовке "Лента" должен быть синий badge с количеством непрочитанных постов
- В каждом чате должен быть красный badge с непрочитанными сообщениями

### 5. Проверьте базу данных

```powershell
docker exec postgres psql -U postgres -d notificationservice_db -c "SELECT * FROM \"UnreadMessages\";"
docker exec postgres psql -U postgres -d notificationservice_db -c "SELECT * FROM \"UnreadFeeds\";"
```

### 6. Проверьте userId в токене

В консоли браузера выполните:
```javascript
const token = localStorage.getItem('token');
const payload = JSON.parse(atob(token.split('.')[1]));
console.log('User ID:', payload.nameid);
console.log('Company ID:', payload.companyId);
```

## Возможные проблемы:

### Проблема 1: WebSocket не подключается
**Решение:** Проверьте, что NotificationService запущен и доступен на порту 5005

### Проблема 2: События не получаются из RabbitMQ
**Решение:** 
- Проверьте, что RabbitMQ работает: `docker ps | grep rabbitmq`
- Проверьте логи ChatService/FeedService - должны публиковаться события
- Проверьте логи NotificationService - должны получаться события

### Проблема 3: userId не извлекается из токена
**Решение:** 
- Проверьте логи NotificationService - должно быть `[NotificationsHub] User connected: UserId=...`
- Если UserId=0, проверьте токен в консоли браузера

### Проблема 4: Уведомления не отправляются через SignalR
**Решение:**
- Проверьте логи NotificationService - должно быть `[NotificationService] Sending counters to user ...`
- Проверьте, что пользователь подключен к правильной группе: `[NotificationsHub] Added connection ... to group user_...`

## Тестирование:

1. Откройте приложение в двух вкладках с разными пользователями
2. Отправьте сообщение от одного пользователя другому
3. Во второй вкладке должен появиться badge с количеством непрочитанных сообщений
4. Создайте пост от одного пользователя
5. Во второй вкладке должен появиться badge с количеством непрочитанных постов



