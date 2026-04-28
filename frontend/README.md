# Frontend - Corporate Social Network

React приложение для корпоративной социальной сети в стиле Apple.

## Технологии

- **React 18** с TypeScript
- **Vite** - сборщик
- **Tailwind CSS** - стилизация в стиле Apple
- **Framer Motion** - анимации
- **React Router** - маршрутизация
- **Axios** - HTTP клиент
- **SignalR** - WebSocket для чата

## Установка

```bash
npm install
```

## Запуск

```bash
npm run dev
```

Приложение будет доступно на `http://localhost:5173`

## Переменные окружения

Создайте файл `.env`:

```
VITE_API_URL=http://localhost:5000/api
VITE_WS_URL=http://localhost:5004
```

## Функциональность

- ✅ Авторизация и регистрация
- ✅ Управление компаниями
- ✅ Лента новостей (Feed)
- ✅ Чат с WebSocket поддержкой

## Структура проекта

```
src/
├── components/     # React компоненты
│   ├── auth/      # Авторизация
│   ├── companies/ # Компании
│   ├── feed/      # Лента
│   └── chat/      # Чат
├── services/       # API и WebSocket сервисы
├── hooks/         # React хуки
├── types/         # TypeScript типы
└── utils/         # Утилиты
```






