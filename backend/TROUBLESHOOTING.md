# Troubleshooting Guide

## Проверка логов сервисов

### Проверить логи userservice:
```bash
docker logs userservice --tail 100
```

### Проверить логи chatservice:
```bash
docker logs chatservice --tail 100
```

### Проверить все логи:
```bash
docker-compose logs --tail 50
```

## Проблемы с миграциями

Если сервисы не проходят healthcheck из-за миграций:

1. **Проверить, что базы данных созданы:**
```bash
docker exec -it postgres psql -U postgres -c "\l" | grep -E "(companyservice|userservice|chatservice|feedservice|notificationservice)"
```

2. **Создать базы вручную, если их нет:**
```bash
docker exec -it postgres psql -U postgres <<EOF
CREATE DATABASE companyservice_db;
CREATE DATABASE userservice_db;
CREATE DATABASE chatservice_db;
CREATE DATABASE feedservice_db;
CREATE DATABASE notificationservice_db;
EOF
```

3. **Проверить, что миграции применены:**
```bash
# Для userservice
docker exec -it postgres psql -U postgres -d userservice_db -c "\dt"

# Для chatservice
docker exec -it postgres psql -U postgres -d chatservice_db -c "\dt"
```

## Перезапуск сервисов

```bash
# Перезапустить конкретный сервис
docker-compose restart userservice
docker-compose restart chatservice

# Перезапустить все сервисы
docker-compose restart
```

## Полная пересборка

```bash
# Остановить и удалить volumes
docker-compose down -v

# Пересобрать и запустить
docker-compose up -d --build
```




