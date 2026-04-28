# Postman команды для создания компаний и пользователей

## Базовый URL
Все запросы идут через Gateway: `http://localhost:5000`

---

## 1. Создание компании

### Postman (cURL):
```bash
curl --location 'http://localhost:5000/api/companies' \
--header 'Content-Type: application/json' \
--data '{
    "name": "Acme Corporation"
}'
```

### Postman настройки:
- **Method:** `POST`
- **URL:** `http://localhost:5000/api/companies`
- **Headers:**
  - `Content-Type: application/json`
- **Body (raw JSON):**
```json
{
    "name": "Acme Corporation"
}
```

**Ответ:**
```json
{
    "id": 1,
    "name": "Acme Corporation",
    "createdAt": "2024-01-01T00:00:00Z"
}
```

**Важно:** Сохраните `id` компании для создания пользователей!

---

## 2. Создание пользователя (Регистрация)

### Postman (cURL):
```bash
curl --location 'http://localhost:5000/api/auth/register' \
--header 'Content-Type: application/json' \
--data '{
    "companyId": 1,
    "email": "user@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe"
}'
```

### Postman настройки:
- **Method:** `POST`
- **URL:** `http://localhost:5000/api/auth/register`
- **Headers:**
  - `Content-Type: application/json`
- **Body (raw JSON):**
```json
{
    "companyId": 1,
    "email": "user@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe"
}
```

**Ответ:**
```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "userId": 1,
    "companyId": 1,
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
}
```

---

## 3. Полный пример: Создание компании и пользователя

### Шаг 1: Создать компанию
```bash
curl --location 'http://localhost:5000/api/companies' \
--header 'Content-Type: application/json' \
--data '{
    "name": "Tech Solutions Inc"
}'
```

**Ответ (пример):**
```json
{
    "id": 7,
    "name": "Tech Solutions Inc",
    "createdAt": "2024-11-23T16:00:00Z"
}
```

### Шаг 2: Создать пользователя для этой компании
```bash
curl --location 'http://localhost:5000/api/auth/register' \
--header 'Content-Type: application/json' \
--data '{
    "companyId": 7,
    "email": "admin@techsolutions.com",
    "password": "SecurePass123!",
    "firstName": "Admin",
    "lastName": "User"
}'
```

---

## 4. Дополнительные команды

### Получить все компании
```bash
curl --location 'http://localhost:5000/api/companies'
```

### Вход в систему (если пользователь уже создан)
```bash
curl --location 'http://localhost:5000/api/auth/login' \
--header 'Content-Type: application/json' \
--data '{
    "email": "user@example.com",
    "password": "password123"
}'
```

### Получить всех пользователей (требует авторизацию)
```bash
curl --location 'http://localhost:5000/api/users' \
--header 'Authorization: Bearer YOUR_TOKEN_HERE'
```

---

## Примеры для быстрого тестирования

### Создать компанию "Test Company"
```bash
curl --location 'http://localhost:5000/api/companies' \
--header 'Content-Type: application/json' \
--data '{"name": "Test Company"}'
```

### Создать пользователя для компании с ID=1
```bash
curl --location 'http://localhost:5000/api/auth/register' \
--header 'Content-Type: application/json' \
--data '{
    "companyId": 1,
    "email": "test@test.com",
    "password": "test123",
    "firstName": "Test",
    "lastName": "User"
}'
```

---

## Важные заметки

1. **Порядок операций:** Сначала создайте компанию, затем пользователей для этой компании
2. **CompanyId:** Используйте `id` из ответа создания компании
3. **Email:** Должен быть уникальным
4. **Password:** Минимум 1 символ (рекомендуется использовать надежные пароли)
5. **Gateway:** Все запросы идут через Gateway на порту 5000




