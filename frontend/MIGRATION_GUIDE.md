# Руководство по миграции на оптимизированную версию

## Быстрый старт

### 1. Установите зависимости

```bash
cd frontend
npm install
```

### 2. Обновите импорты в компонентах

#### Замена useAuth

**Старый способ:**
```tsx
import { useAuth } from '../../hooks/useAuth';
const { user, login, logout } = useAuth();
```

**Новый способ (вариант 1 - через store):**
```tsx
import { useAuthStore } from '../../stores/authStore';
const user = useAuthStore((state) => state.user);
const login = useAuthStore((state) => state.login);
const logout = useAuthStore((state) => state.logout);
```

**Новый способ (вариант 2 - через обертку):**
```tsx
import { useAuth } from '../../hooks/useAuthOptimized';
const { user, login, logout } = useAuth(); // API остается тем же
```

#### Замена локального состояния чатов

**Старый способ:**
```tsx
const [chats, setChats] = useState<Chat[]>([]);
const [selectedChat, setSelectedChat] = useState<Chat | null>(null);
```

**Новый способ:**
```tsx
import { useChatStore } from '../../stores/chatStore';
const chats = useChatStore((state) => state.chats);
const selectedChat = useChatStore((state) => state.selectedChat);
const selectChat = useChatStore((state) => state.selectChat);
```

#### Использование React Query для данных

**Старый способ:**
```tsx
const [posts, setPosts] = useState<Post[]>([]);
useEffect(() => {
  loadPosts();
}, []);
```

**Новый способ:**
```tsx
import { usePosts } from '../../hooks/queries/usePosts';
const { posts, isLoading, createPost } = usePosts();
```

### 3. Обновите TelegramLayout

**Старый способ:**
```tsx
import { TelegramLayout } from './components/layout/TelegramLayout';
```

**Новый способ:**
```tsx
import { TelegramLayout } from './components/layout/TelegramLayoutOptimized';
```

Или постепенно мигрируйте существующий компонент, используя новые stores.

## Пошаговая миграция

### Шаг 1: Обновить App.tsx

Добавьте QueryClientProvider (уже добавлен в main.tsx).

### Шаг 2: Мигрировать TelegramLayout

1. Замените локальное состояние на stores
2. Используйте useChats вместо ручной загрузки
3. Добавьте lazy loading для компонентов

### Шаг 3: Мигрировать ChatSidebar

1. Используйте ChatSidebarOptimized или обновите существующий
2. Замените пропсы на store селекторы
3. Добавьте React.memo

### Шаг 4: Мигрировать другие компоненты

Аналогично обновите:
- ChatWindow
- FeedCenter
- PostCard
- И другие компоненты

## Проверка оптимизаций

### 1. Проверьте bundle size

```bash
npm run build
```

Должен быть меньше после lazy loading.

### 2. Проверьте ререндеры

Используйте React DevTools Profiler для проверки ререндеров.

### 3. Проверьте кэширование

React Query автоматически кэширует запросы. Проверьте Network tab - повторные запросы не должны выполняться.

## Откат изменений

Если что-то пошло не так, можно вернуться к старой версии:

1. Используйте старые импорты
2. Удалите новые stores (или просто не используйте их)
3. Старые хуки продолжают работать

## Дополнительные улучшения

После базовой миграции можно добавить:

1. **Виртуализацию списков** для больших списков
2. **Service Worker** для офлайн работы
3. **Оптимизацию изображений** с lazy loading
4. **Bundle анализ** для дальнейшей оптимизации




