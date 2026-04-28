# Отладка уведомлений

## Проверка подключения

1. Откройте консоль браузера (F12)
2. Проверьте сообщения:
   - "Notification WebSocket connected" - должно появиться при загрузке
   - "Error connecting to notification WebSocket" - ошибка подключения

## Проверка счетчиков

1. В консоли выполните:
```javascript
// Проверить состояние уведомлений
const store = window.__ZUSTAND_STORES__?.notificationStore;
console.log('Notification counters:', store?.getState()?.counters);

// Или через React DevTools
```

## Проверка API

1. Проверьте, что NotificationService работает:
```bash
curl http://localhost:5005/health
```

2. Проверьте счетчики:
```bash
curl -H "Authorization: Bearer YOUR_TOKEN" http://localhost:5005/api/notifications/counters
```

## Возможные проблемы

1. **WebSocket не подключается**
   - Проверьте URL: должен быть `http://localhost:5005/notificationsHub`
   - Проверьте, что NotificationService запущен
   - Проверьте CORS настройки

2. **Счетчики не обновляются**
   - Проверьте, что RabbitMQ работает
   - Проверьте, что события публикуются
   - Проверьте логи NotificationService

3. **Уведомления не отображаются**
   - Проверьте, что `useNotifications` вызывается
   - Проверьте, что счетчики не равны 0
   - Проверьте компоненты ChatSidebar и FeedCenter



