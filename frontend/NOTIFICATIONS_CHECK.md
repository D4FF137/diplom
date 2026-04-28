# Проверка уведомлений

## Что было добавлено:

1. **Логирование** - в консоли браузера будут видны сообщения о подключении и обновлениях
2. **Визуальные индикаторы**:
   - В заголовке "Чаты" показывается общее количество непрочитанных сообщений
   - В заголовке "Лента" показывается количество непрочитанных постов
   - В каждом чате показывается количество непрочитанных сообщений
   - В ленте показывается баннер с количеством новых постов

## Как проверить:

### 1. Откройте консоль браузера (F12)

Должны появиться сообщения:
- `[Notifications] Fetching initial counters...`
- `[Notifications] Initial counters received: {...}`
- `[Notifications] Attempting to connect to WebSocket...`
- `[Notifications] WebSocket connected successfully`

### 2. Проверьте визуальные индикаторы:

- **В заголовке "Чаты"** - должен быть красный badge с количеством непрочитанных сообщений
- **В заголовке "Лента"** - должен быть синий badge с количеством непрочитанных постов
- **В списке чатов** - каждый чат с непрочитанными сообщениями должен иметь красный badge
- **В ленте** - если есть непрочитанные посты, должен быть синий баннер

### 3. Проверьте подключение:

В консоли выполните:
```javascript
// Проверить состояние уведомлений
const store = window.__ZUSTAND_STORES__?.notificationStore;
if (store) {
  const state = store.getState();
  console.log('Counters:', state.counters);
  console.log('Connected:', state.isConnected);
}
```

### 4. Проверьте API:

```bash
# Проверить счетчики через API
curl -H "Authorization: Bearer YOUR_TOKEN" http://localhost:5000/api/notifications/counters
```

## Возможные проблемы:

1. **WebSocket не подключается**
   - Проверьте, что NotificationService запущен: `docker ps | grep notificationservice`
   - Проверьте логи: `docker logs notificationservice`
   - Проверьте URL в консоли - должен быть `http://localhost:5005/notificationsHub`

2. **Счетчики всегда 0**
   - Проверьте, что RabbitMQ работает
   - Проверьте, что события публикуются при отправке сообщений/создании постов
   - Проверьте логи NotificationService на наличие ошибок

3. **Уведомления не обновляются**
   - Проверьте, что WebSocket подключен (в консоли должно быть "WebSocket connected")
   - Проверьте, что события приходят (в консоли должны быть сообщения "[Notifications] Received counter update")



