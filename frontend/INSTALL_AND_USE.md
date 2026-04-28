# 🚀 Установка и использование оптимизаций

## 📦 Шаг 1: Установка зависимостей

```bash
cd frontend
npm install zustand @tanstack/react-query
```

## ✅ Шаг 2: Проверка установки

После установки проверьте, что в `package.json` появились:
- `zustand: ^5.0.2`
- `@tanstack/react-query: ^5.56.2`

## 🎯 Шаг 3: Использование

### Вариант A: Постепенная миграция (рекомендуется)

Используйте новые stores и хуки там, где нужно. Старые компоненты продолжают работать:

```tsx
// В любом компоненте
import { useAuthStore } from '../stores/authStore';
const user = useAuthStore((state) => state.user);

import { useChats } from '../hooks/queries/useChats';
const { chats, isLoading } = useChats();
```

### Вариант B: Использование оптимизированных компонентов

Замените импорты на оптимизированные версии:

```tsx
// В App.tsx или TelegramLayout
import { TelegramLayout } from './components/layout/TelegramLayoutOptimized';
```

## 📝 Примеры кода

### Пример 1: Использование Zustand store

```tsx
import { useAuthStore } from '../stores/authStore';

function MyComponent() {
  // Селектор - ререндер только при изменении user
  const user = useAuthStore((state) => state.user);
  const login = useAuthStore((state) => state.login);
  
  return <div>Hello, {user?.email}</div>;
}
```

### Пример 2: Использование React Query

```tsx
import { useChats } from '../hooks/queries/useChats';

function ChatsList() {
  const { chats, isLoading, createChat } = useChats();
  
  if (isLoading) return <div>Loading...</div>;
  
  const handleCreate = async () => {
    await createChat({ name: 'New Chat', type: 'group' });
  };
  
  return (
    <div>
      <button onClick={handleCreate}>Create Chat</button>
      {chats.map(chat => (
        <div key={chat.id}>{chat.name}</div>
      ))}
    </div>
  );
}
```

### Пример 3: Использование notification store

```tsx
import { useNotificationStore } from '../stores/notificationStore';

function ChatBadge({ chatId }: { chatId: number }) {
  const unreadCount = useNotificationStore(
    (state) => state.getChatUnreadCount(chatId)
  );
  
  if (unreadCount === 0) return null;
  
  return (
    <span className="badge">
      {unreadCount > 99 ? '99+' : unreadCount}
    </span>
  );
}
```

## 🔄 Миграция существующих компонентов

### Замена useAuth

**Было:**
```tsx
import { useAuth } from '../../hooks/useAuth';
const { user, login } = useAuth();
```

**Стало (вариант 1 - через store):**
```tsx
import { useAuthStore } from '../../stores/authStore';
const user = useAuthStore((state) => state.user);
const login = useAuthStore((state) => state.login);
```

**Стало (вариант 2 - через обертку):**
```tsx
import { useAuth } from '../../hooks/useAuthOptimized';
const { user, login } = useAuth(); // API остается тем же
```

### Замена локального состояния чатов

**Было:**
```tsx
const [chats, setChats] = useState<Chat[]>([]);
const [selectedChat, setSelectedChat] = useState<Chat | null>(null);
```

**Стало:**
```tsx
import { useChatStore } from '../../stores/chatStore';
const chats = useChatStore((state) => state.chats);
const selectedChat = useChatStore((state) => state.selectedChat);
const selectChat = useChatStore((state) => state.selectChat);
```

### Использование React Query для данных

**Было:**
```tsx
const [posts, setPosts] = useState<Post[]>([]);
useEffect(() => {
  loadPosts();
}, []);
```

**Стало:**
```tsx
import { usePosts } from '../../hooks/queries/usePosts';
const { posts, isLoading, createPost } = usePosts();
// Данные автоматически кэшируются и обновляются
```

## 🎨 Best Practices

### ✅ Делайте так:

1. **Используйте селекторы Zustand:**
```tsx
// ✅ Хорошо - ререндер только при изменении user
const user = useAuthStore((state) => state.user);
```

2. **Мемоизируйте вычисления:**
```tsx
const sortedChats = useMemo(() => {
  return chats.sort((a, b) => ...);
}, [chats]);
```

3. **Используйте useCallback:**
```tsx
const handleClick = useCallback(() => {
  // ...
}, [dependencies]);
```

### ❌ Не делайте так:

1. **Не используйте весь store:**
```tsx
// ❌ Плохо - ререндер при любом изменении
const store = useAuthStore();
```

2. **Не создавайте объекты в зависимостях:**
```tsx
// ❌ Плохо
useEffect(() => {...}, [object]); // объект создается заново
```

## 🐛 Отладка

### React DevTools

Установите React DevTools для проверки ререндеров:
- Chrome: [React Developer Tools](https://chrome.google.com/webstore/detail/react-developer-tools/fmkadmapgofadopljbjfkapdkoienihi)
- Firefox: [React Developer Tools](https://addons.mozilla.org/en-US/firefox/addon/react-devtools/)

### React Query DevTools (опционально)

```bash
npm install @tanstack/react-query-devtools
```

Добавьте в `main.tsx`:
```tsx
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

<QueryClientProvider client={queryClient}>
  <App />
  <ReactQueryDevtools initialIsOpen={false} />
</QueryClientProvider>
```

## 📊 Проверка результатов

### Bundle size

```bash
npm run build
```

Проверьте размер bundle - должен быть меньше после lazy loading.

### Производительность

1. Откройте React DevTools Profiler
2. Запишите сессию
3. Проверьте количество ререндеров - должно быть меньше

### Кэширование

1. Откройте Network tab в DevTools
2. Перейдите на страницу, затем вернитесь
3. Повторные запросы не должны выполняться (React Query кэширует)

## 🎉 Готово!

Теперь ваш frontend оптимизирован и готов к использованию!

Для подробной информации см.:
- `OPTIMIZATION_GUIDE.md` - подробное руководство
- `MIGRATION_GUIDE.md` - пошаговая миграция
- `OPTIMIZATIONS_SUMMARY.md` - резюме всех оптимизаций




