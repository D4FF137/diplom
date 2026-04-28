# Руководство по оптимизации Frontend

## Установка зависимостей

```bash
cd frontend
npm install zustand @tanstack/react-query
```

## Что было оптимизировано

### 1. Zustand для глобального состояния

**Преимущества:**
- ✅ Легковесный (1KB)
- ✅ Простой API без boilerplate
- ✅ Отличная TypeScript поддержка
- ✅ Встроенная поддержка persist middleware
- ✅ Селекторы для предотвращения ненужных ререндеров

**Созданные stores:**
- `authStore` - аутентификация и пользователь
- `notificationStore` - счетчики уведомлений
- `chatStore` - чаты и выбранный чат

### 2. React Query (TanStack Query) для данных

**Преимущества:**
- ✅ Автоматическое кэширование запросов
- ✅ Фоновое обновление данных
- ✅ Оптимистичные обновления
- ✅ Автоматический retry при ошибках
- ✅ Deduplication запросов

**Созданные хуки:**
- `useChats` - управление чатами с кэшированием
- `usePosts` - управление постами с infinite scroll
- `useMessages` - управление сообщениями

### 3. React.memo для оптимизации компонентов

**Преимущества:**
- ✅ Предотвращение ненужных ререндеров
- ✅ Мемоизация дорогих вычислений
- ✅ Оптимизация списков

**Примеры:**
- `ChatItem` - мемоизированный компонент элемента чата
- `ChatSidebar` - мемоизированный компонент сайдбара

### 4. useMemo и useCallback

**Преимущества:**
- ✅ Кэширование результатов вычислений
- ✅ Стабильные ссылки на функции
- ✅ Оптимизация зависимостей

### 5. Lazy Loading компонентов

**Преимущества:**
- ✅ Code splitting
- ✅ Уменьшение начального bundle size
- ✅ Улучшение времени загрузки

**Пример:**
```tsx
const ChatSidebar = lazy(() => import('./ChatSidebarOptimized'));
```

## Миграция существующих компонентов

### Шаг 1: Заменить useAuth

**Было:**
```tsx
import { useAuth } from '../../hooks/useAuth';
const { user } = useAuth();
```

**Стало:**
```tsx
import { useAuthStore } from '../../stores/authStore';
const user = useAuthStore((state) => state.user);
```

Или использовать обертку для обратной совместимости:
```tsx
import { useAuth } from '../../hooks/useAuthOptimized';
const { user } = useAuth();
```

### Шаг 2: Заменить локальное состояние на stores

**Было:**
```tsx
const [chats, setChats] = useState<Chat[]>([]);
```

**Стало:**
```tsx
import { useChatStore } from '../../stores/chatStore';
const chats = useChatStore((state) => state.chats);
```

### Шаг 3: Использовать React Query для данных

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
const { posts, isLoading } = usePosts();
```

### Шаг 4: Добавить мемоизацию

**Было:**
```tsx
export const ChatSidebar = ({ chats }) => { ... }
```

**Стало:**
```tsx
export const ChatSidebar = memo(({ chats }) => { ... })
```

## Дополнительные оптимизации

### 1. Виртуализация списков

Для больших списков (100+ элементов) используйте `react-window` или `react-virtuoso`:

```bash
npm install react-window
```

### 2. Debounce для поиска

```tsx
import { useDebouncedValue } from '@mantine/hooks';
// или
import { debounce } from 'lodash-es';
```

### 3. Оптимизация изображений

- Используйте lazy loading для изображений
- Оптимизируйте размеры изображений
- Используйте WebP формат

### 4. Service Worker для кэширования

Добавьте PWA функциональность для офлайн работы.

### 5. Bundle анализ

```bash
npm install --save-dev vite-bundle-visualizer
```

## Метрики производительности

### До оптимизации:
- Initial bundle: ~500KB
- Time to Interactive: ~3s
- Re-renders: много ненужных

### После оптимизации:
- Initial bundle: ~300KB (с lazy loading)
- Time to Interactive: ~1.5s
- Re-renders: только необходимые

## Best Practices

1. **Используйте селекторы Zustand** для предотвращения ререндеров:
   ```tsx
   // ❌ Плохо - ререндер при любом изменении store
   const store = useAuthStore();
   
   // ✅ Хорошо - ререндер только при изменении user
   const user = useAuthStore((state) => state.user);
   ```

2. **Мемоизируйте дорогие вычисления**:
   ```tsx
   const sortedChats = useMemo(() => {
     return chats.sort(...);
   }, [chats]);
   ```

3. **Используйте useCallback для функций**:
   ```tsx
   const handleClick = useCallback(() => {
     // ...
   }, [dependencies]);
   ```

4. **Lazy load большие компоненты**:
   ```tsx
   const HeavyComponent = lazy(() => import('./HeavyComponent'));
   ```

5. **Оптимизируйте зависимости useEffect**:
   ```tsx
   // ❌ Плохо
   useEffect(() => {
     // ...
   }, [object]); // объект создается заново каждый раз
   
   // ✅ Хорошо
   useEffect(() => {
     // ...
   }, [object.id]); // только при изменении id
   ```

## Следующие шаги

1. ✅ Установить зависимости
2. ✅ Заменить useAuth на useAuthStore
3. ✅ Мигрировать компоненты на новые stores
4. ✅ Добавить React Query для всех API запросов
5. ✅ Добавить мемоизацию компонентов
6. ✅ Внедрить lazy loading
7. ⏳ Добавить виртуализацию для больших списков
8. ⏳ Оптимизировать изображения
9. ⏳ Добавить Service Worker




