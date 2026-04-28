# Структура проекта

```
coursework/
├── src/
│   ├── Gateway/                          # API Gateway
│   │   ├── Controllers/
│   │   │   └── GatewayController.cs      # Маршрутизация запросов
│   │   ├── Services/
│   │   │   ├── IRoutingService.cs
│   │   │   ├── RoutingService.cs         # Маршрутизация к микросервисам
│   │   │   ├── IRabbitMQService.cs
│   │   │   └── RabbitMQService.cs        # Интеграция с RabbitMQ
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Gateway.csproj
│   │
│   ├── UserService/                       # Сервис пользователей
│   │   ├── Controllers/
│   │   │   ├── UsersController.cs        # CRUD пользователей
│   │   │   └── AuthController.cs         # Аутентификация
│   │   ├── Data/
│   │   │   └── UserDbContext.cs          # EF Core контекст
│   │   ├── Services/
│   │   │   ├── IUserService.cs
│   │   │   ├── UserService.cs            # Бизнес-логика
│   │   │   ├── IRabbitMQService.cs
│   │   │   └── RabbitMQService.cs        # Публикация событий
│   │   ├── Migrations/                    # EF Core миграции
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── UserService.csproj
│   │
│   ├── CompanyService/                    # Сервис компаний
│   │   ├── Controllers/
│   │   │   └── CompaniesController.cs     # CRUD компаний
│   │   ├── Data/
│   │   │   └── CompanyDbContext.cs
│   │   ├── Services/
│   │   │   ├── ICompanyService.cs
│   │   │   └── CompanyService.cs
│   │   ├── Migrations/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── CompanyService.csproj
│   │
│   ├── FeedService/                       # Сервис ленты новостей
│   │   ├── Controllers/
│   │   │   └── PostsController.cs        # CRUD постов
│   │   ├── Data/
│   │   │   └── FeedDbContext.cs
│   │   ├── Services/
│   │   │   ├── IPostService.cs
│   │   │   ├── PostService.cs
│   │   │   ├── IRabbitMQService.cs
│   │   │   └── RabbitMQService.cs
│   │   ├── Migrations/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── FeedService.csproj
│   │
│   ├── ChatService/                       # Сервис чатов
│   │   ├── Controllers/
│   │   │   ├── ChatsController.cs        # CRUD чатов
│   │   │   └── MessagesController.cs      # CRUD сообщений
│   │   ├── Hubs/
│   │   │   └── ChatHub.cs                # SignalR Hub для WebSocket
│   │   ├── Data/
│   │   │   └── ChatDbContext.cs
│   │   ├── Services/
│   │   │   ├── IChatService.cs
│   │   │   ├── ChatService.cs
│   │   │   ├── IMessageService.cs
│   │   │   ├── MessageService.cs
│   │   │   ├── IRabbitMQService.cs
│   │   │   └── RabbitMQService.cs
│   │   ├── Migrations/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── ChatService.csproj
│   │
│   └── ReactApp/                          # React фронтенд (опционально)
│
├── shared/                                 # Общие библиотеки
│   ├── Common/                            # Общие утилиты
│   │   ├── JwtHelper.cs                   # JWT генерация/валидация
│   │   ├── CompanyIsolationMiddleware.cs  # Middleware для изоляции
│   │   └── Common.csproj
│   │
│   ├── Contracts/                         # Контракты для RabbitMQ
│   │   ├── UserCreatedEvent.cs
│   │   ├── PostCreatedEvent.cs
│   │   ├── MessageSentEvent.cs
│   │   └── Contracts.csproj
│   │
│   └── Models/                            # Общие модели данных
│       ├── User.cs
│       ├── Company.cs
│       ├── Post.cs
│       ├── Chat.cs
│       ├── Message.cs
│       └── Models.csproj
│
├── docker/                                 # Dockerfile для каждого сервиса
│   ├── Dockerfile.gateway
│   ├── Dockerfile.userservice
│   ├── Dockerfile.companyservice
│   ├── Dockerfile.feedservice
│   └── Dockerfile.chatservice
│
├── docker-compose.yml                      # Оркестрация всех сервисов
├── .env.example                           # Пример переменных окружения
├── .gitignore
├── README.md                              # Основная документация
├── ARCHITECTURE.md                        # Детальная архитектура
└── PROJECT_STRUCTURE.md                   # Этот файл
```

## Описание компонентов

### Микросервисы

1. **Gateway** - API Gateway, единая точка входа
2. **UserService** - Управление пользователями и аутентификация
3. **CompanyService** - Управление компаниями
4. **FeedService** - Лента новостей и посты
5. **ChatService** - Система чатов с WebSocket (SignalR)

### Shared библиотеки

- **Common** - Общие утилиты (JWT, middleware)
- **Contracts** - События для RabbitMQ
- **Models** - Общие модели данных

### Инфраструктура

- **PostgreSQL** - База данных с row-level security
- **RabbitMQ** - Message queue для асинхронной обработки
- **Redis** - Кэширование и pub/sub

## Изоляция компаний

Все компоненты системы обеспечивают изоляцию данных по компаниям:

1. **База данных**: Row-level security политики
2. **API**: Проверка CompanyId в JWT токене
3. **Redis**: Ключи с префиксом `company_{companyId}_`
4. **SignalR**: Группы с префиксом `company_{companyId}_chat_{chatId}`


