# Тестовые запросы для проверки API

## 1. Создание компании

**Method:** `POST`  
**URL:** `http://localhost:5000/api/companies`  
**Headers:**
```
Content-Type: application/json
```

**Body (raw JSON):**
```json
{
  "name": "Test Company"
}
```

**Ожидаемый ответ (201 Created):**
```json
{
  "id": 1,
  "name": "Test Company",
  "createdAt": "2025-11-22T18:30:00Z"
}
```

## 2. Регистрация пользователя

**Method:** `POST`  
**URL:** `http://localhost:5000/api/auth/register`  
**Headers:**
```
Content-Type: application/json
```

**Body (raw JSON):**
```json
{
  "companyId": 1,
  "email": "user@test.com",
  "password": "password123",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Ожидаемый ответ (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "companyId": 1,
  "email": "user@test.com",
  "firstName": "John",
  "lastName": "Doe"
}
```

## 3. Вход в систему

**Method:** `POST`  
**URL:** `http://localhost:5000/api/auth/login`  
**Headers:**
```
Content-Type: application/json
```

**Body (raw JSON):**
```json
{
  "email": "user@test.com",
  "password": "password123"
}
```

## 4. Получить все компании

**Method:** `GET`  
**URL:** `http://localhost:5000/api/companies`

## 5. Получить компанию по ID

**Method:** `GET`  
**URL:** `http://localhost:5000/api/companies/1`

## Admin API (админ-панель)

Админ-панель: **http://localhost:5173/admin** (фронтенд).  

Все запросы требуют заголовок `X-Admin-Secret`. Секрет задаётся в `ADMIN_SECRET` (бэкенд) и `VITE_ADMIN_SECRET` (фронтенд .env). В Docker: `ADMIN_SECRET` в `.env` или при `docker compose up`.

### Создать организацию

**Method:** `POST`  
**URL:** `http://localhost:5000/api/admin/companies`  
**Headers:**
```
Content-Type: application/json
X-Admin-Secret: <ваш ADMIN_SECRET>
```

**Body:**
```json
{ "name": "Новая организация" }
```

### Создать начальника организации

**Method:** `POST`  
**URL:** `http://localhost:5000/api/admin/companies/{companyId}/boss`  
**Headers:**
```
Content-Type: application/json
X-Admin-Secret: <ваш ADMIN_SECRET>
```

**Body:**
```json
{
  "email": "boss@company.com",
  "password": "securepassword",
  "firstName": "Иван",
  "lastName": "Иванов"
}
```

Начальник может входить через **Логин** на `/auth` (email + пароль).

---

## Troubleshooting

Если получаете 400 Bad Request:

1. Проверьте, что Content-Type установлен как `application/json`
2. Убедитесь, что JSON валидный (проверьте на jsonlint.com)
3. Не отправляйте поля `id` и `createdAt` при создании - они генерируются автоматически
4. Проверьте логи: `docker-compose logs gateway --tail 50`
5. Проверьте логи сервиса: `docker-compose logs companyservice --tail 50`






