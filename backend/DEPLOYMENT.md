# Руководство по развертыванию

## Production Deployment

### 1. Подготовка переменных окружения

Создайте файл `.env` с production значениями:

```env
POSTGRES_USER=production_user
POSTGRES_PASSWORD=strong_password_here
JWT_SECRET=very-long-random-secret-key-minimum-32-characters
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=strong_password
```

### 2. Безопасность

- **JWT_SECRET**: Используйте криптографически стойкий случайный ключ (минимум 32 символа)
- **Пароли БД**: Используйте сильные пароли
- **HTTPS**: Настройте reverse proxy (nginx/traefik) с SSL сертификатами
- **Firewall**: Ограничьте доступ к портам базы данных

### 3. Docker Compose для Production

Создайте `docker-compose.prod.yml`:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: always
    # Не экспортируйте порт в production, используйте внутреннюю сеть

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
    restart: always

  rabbitmq:
    image: rabbitmq:3-management-alpine
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    restart: always

  # Сервисы с restart: always и без экспорта портов
  # Используйте reverse proxy для доступа
```

### 4. Reverse Proxy (Nginx)

Пример конфигурации nginx:

```nginx
upstream gateway {
    server gateway:5000;
}

server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://gateway;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /chatHub {
        proxy_pass http://chatservice:5004;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```

### 5. Мониторинг

Рекомендуемые инструменты:

- **Health Checks**: Используйте встроенные `/health` endpoints
- **Logging**: Настройте централизованное логирование (ELK, Seq, etc.)
- **Metrics**: Prometheus + Grafana для метрик
- **Tracing**: OpenTelemetry для распределенной трассировки

### 6. Масштабирование

#### Горизонтальное масштабирование

```bash
# Запустить несколько экземпляров
docker-compose up -d --scale feedservice=3 --scale chatservice=2
```

#### Load Balancing

Используйте nginx или traefik для балансировки нагрузки между экземплярами.

### 7. Резервное копирование

#### PostgreSQL

```bash
# Создать бэкап
docker-compose exec postgres pg_dump -U postgres userservice_db > backup.sql

# Восстановить
docker-compose exec -T postgres psql -U postgres userservice_db < backup.sql
```

#### Автоматические бэкапы

Настройте cron job для регулярных бэкапов:

```bash
0 2 * * * docker-compose exec -T postgres pg_dump -U postgres userservice_db > /backups/userservice_$(date +\%Y\%m\%d).sql
```

### 8. Миграции в Production

```bash
# Применить миграции перед запуском
docker-compose exec userservice dotnet ef database update
docker-compose exec companyservice dotnet ef database update
docker-compose exec feedservice dotnet ef database update
docker-compose exec chatservice dotnet ef database update
```

### 9. Обновление сервисов

```bash
# 1. Остановить сервис
docker-compose stop userservice

# 2. Пересобрать образ
docker-compose build userservice

# 3. Применить миграции (если есть)
docker-compose exec userservice dotnet ef database update

# 4. Запустить обновленный сервис
docker-compose up -d userservice
```

### 10. Откат изменений

```bash
# Откатить миграцию
docker-compose exec userservice dotnet ef database update PreviousMigration

# Откатить образ к предыдущей версии
docker-compose pull userservice:previous-tag
docker-compose up -d userservice
```

## Kubernetes Deployment

### Пример Deployment для UserService

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: userservice
spec:
  replicas: 3
  selector:
    matchLabels:
      app: userservice
  template:
    metadata:
      labels:
        app: userservice
    spec:
      containers:
      - name: userservice
        image: your-registry/userservice:latest
        ports:
        - containerPort: 5001
        env:
        - name: ConnectionStrings__PostgreSQL
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: jwt-secret
              key: secret
        livenessProbe:
          httpGet:
            path: /health
            port: 5001
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5001
          initialDelaySeconds: 10
          periodSeconds: 5
```

## CI/CD Pipeline

### GitHub Actions пример

```yaml
name: Build and Deploy

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Build Docker images
        run: |
          docker-compose build
      
      - name: Run tests
        run: |
          docker-compose up -d postgres redis rabbitmq
          dotnet test
      
      - name: Push to registry
        run: |
          docker push your-registry/gateway:latest
          docker push your-registry/userservice:latest
          # ...
```

## Troubleshooting Production

### Проблемы с производительностью

1. **Медленные запросы к БД**: Проверьте индексы, используйте EXPLAIN ANALYZE
2. **Высокая нагрузка на Redis**: Увеличьте память, настройте eviction policy
3. **Очереди RabbitMQ**: Мониторьте длину очередей, добавьте воркеров

### Проблемы с памятью

```bash
# Проверить использование памяти
docker stats

# Ограничить память для контейнера
services:
  userservice:
    deploy:
      resources:
        limits:
          memory: 512M
```

### Логирование

```bash
# Просмотр логов всех сервисов
docker-compose logs -f

# Логи конкретного сервиса за последний час
docker-compose logs --since 1h userservice

# Экспорт логов
docker-compose logs > logs.txt
```


