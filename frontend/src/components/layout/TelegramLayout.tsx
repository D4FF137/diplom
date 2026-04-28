import { useState, useEffect, useMemo, useCallback, memo, lazy, Suspense } from 'react';
import { Link } from 'react-router-dom';
import { Helmet } from 'react-helmet-async';
import { useAuthStore } from '../../stores/authStore';
import { useChatStore } from '../../stores/chatStore';
import { useChats } from '../../hooks/queries/useChats';
import { useNotifications } from '../../hooks/useNotificationsOptimized';
import { ChatSidebar } from '../chat/ChatSidebar';
import { wsService } from '../../services/websocket';
import { getImageUrl } from '../../utils/imageUrl';
import type { Chat } from '../../types';

const ChatWindow = lazy(() => import('../chat/ChatWindow').then(m => ({ default: m.ChatWindow })));
const FeedCenter = lazy(() => import('../feed/FeedCenter').then(m => ({ default: m.FeedCenter })));
const StoragePage = lazy(() => import('../storage/StoragePage').then(m => ({ default: m.StoragePage })));
const ProfileModal = lazy(() => import('../profile/ProfileModal').then(m => ({ default: m.ProfileModal })));

const TelegramLayoutComponent = () => {
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);
  const refreshUser = useAuthStore((state) => state.refreshUser);

  const selectedChat = useChatStore((state) => state.selectedChat);
  const selectChat = useChatStore((state) => state.selectChat);

  const { refetch: loadChats } = useChats();
  // Инициализируем уведомления - это подключит WebSocket и загрузит счетчики
  const { getTotalChatUnread, feedUnread, isConnected } = useNotifications();

  const [showProfile, setShowProfile] = useState(false);
  const [currentView, setCurrentView] = useState<'chats' | 'feed' | 'files'>('chats');

  // Мемоизация вычислений
  const getUserDisplayName = useMemo(() => {
    if (user?.firstName || user?.lastName) {
      return `${user.firstName || ''} ${user.lastName || ''}`.trim();
    }
    return user?.email || 'User';
  }, [user]);

  useEffect(() => {
    // Подключаемся к WebSocket для обновления чатов в реальном времени
    if (user) {
      const setupWebSocket = async () => {
        try {
          if (!wsService.isConnected()) {
            await wsService.connect();
          }

          // Подписываемся на новые чаты
          wsService.onNewChat((newChat: Chat) => {
            useChatStore.getState().addChat(newChat);
          });

          // Подписываемся на обновление чатов
          wsService.onChatUpdated((data: { chatId: string | number; lastMessageAt: string }) => {
            useChatStore.getState().updateChat(data.chatId, {
              lastMessageAt: data.lastMessageAt,
            });
          });

          // Подписываемся на удаление чата (для всех)
          wsService.onChatDeleted((data: { chatId: string | number }) => {
            useChatStore.getState().removeChat(data.chatId);
            const { selectedChat, selectChat } = useChatStore.getState();
            if (selectedChat && String(selectedChat.id) === String(data.chatId)) {
              selectChat(null);
            }
          });

          // Подписываемся на удаление чата только для текущего пользователя
          wsService.onChatRemoved((data: { chatId: string | number }) => {
            useChatStore.getState().removeChat(data.chatId);
            const { selectedChat, selectChat } = useChatStore.getState();
            if (selectedChat && String(selectedChat.id) === String(data.chatId)) {
              selectChat(null);
            }
          });

          // Подписываемся на новые сообщения для локального обновления
          wsService.onMessageReceived((message: any) => {
            useChatStore.getState().updateChat(message.chatId, {
              lastMessage: message.content,
              lastMessageAt: message.createdAt,
            });
          });

          return () => {
            wsService.off('NewChat');
            wsService.off('ReceiveMessage');
            wsService.off('ChatUpdated');
            wsService.off('ChatDeleted');
            wsService.off('ChatRemoved');
          };
        } catch (error) {
          console.error('Error setting up WebSocket for chats:', error);
        }
      };

      setupWebSocket();
    }
  }, [user]);

  useEffect(() => {
    loadChats();

    // Слушаем событие создания чата
    const handleChatCreated = () => {
      loadChats();
    };
    window.addEventListener('chatCreated', handleChatCreated);

    return () => {
      window.removeEventListener('chatCreated', handleChatCreated);
    };
  }, [loadChats]);

  const handleProfileUpdate = useCallback(async () => {
    await refreshUser();
    loadChats();
    window.dispatchEvent(new Event('refreshFeed'));
  }, [refreshUser, loadChats]);

  const getPageTitle = useMemo(() => {
    if (selectedChat) return `Чат: ${selectedChat.name}`;
    if (currentView === 'files') return 'Файлы и Документы';
    if (currentView === 'feed') return 'Корпоративная Лента';
    return 'Мессенджер';
  }, [selectedChat, currentView]);

  return (
    <div className="h-screen flex bg-gray-50 dark:bg-gray-900">
      <Helmet>
        <title>{getPageTitle} | Корпоративная Сеть</title>
        <meta name="description" content="Единая платформа для общения и работы сотрудников" />
      </Helmet>
      {/* Левая панель - Чаты */}
      <div className={`
        ${selectedChat || currentView !== 'chats' ? 'hidden md:flex' : 'flex'} 
        w-full md:w-80 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex-col
      `}>
        <div className="h-16 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between px-4 shrink-0">
          <div className="flex items-center gap-2 sm:gap-4 overflow-x-auto no-scrollbar">
            <button
              onClick={() => { setCurrentView('chats'); selectChat(null); }}
              className={`text-base sm:text-lg font-semibold transition-colors shrink-0 ${currentView === 'chats' && !selectedChat ? 'text-apple-blue' : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-200'}`}
            >
              Чаты
            </button>
            <button
              onClick={() => { setCurrentView('feed'); selectChat(null); }}
              className={`text-base sm:text-lg font-semibold transition-colors shrink-0 ${currentView === 'feed' ? 'text-apple-blue' : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-200'}`}
            >
              Лента
            </button>
            <button
              onClick={() => { setCurrentView('files'); selectChat(null); }}
              className={`text-base sm:text-lg font-semibold transition-colors shrink-0 ${currentView === 'files' ? 'text-apple-blue' : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-200'}`}
            >
              Файлы
            </button>
            {!isConnected && (
              <span className="text-xs text-yellow-600 dark:text-yellow-400" title="Уведомления не подключены">
                ⚠
              </span>
            )}
            {getTotalChatUnread() > 0 && (
              <span className="bg-red-500 text-white text-xs font-semibold px-2 py-0.5 rounded-full">
                {getTotalChatUnread() > 99 ? '99+' : getTotalChatUnread()}
              </span>
            )}
          </div>
          <button
            onClick={() => setShowProfile(true)}
            className="w-8 h-8 rounded-full bg-apple-blue text-white flex items-center justify-center text-sm font-medium hover:bg-blue-600 transition-colors shrink-0"
          >
            {getImageUrl(user?.avatarUrl) ? (
              <img src={getImageUrl(user?.avatarUrl) || ''} alt="Avatar" className="w-full h-full rounded-full object-cover" loading="lazy" />
            ) : (
              getUserDisplayName.charAt(0).toUpperCase()
            )}
          </button>
        </div>

        {/* Список чатов */}
        <ChatSidebar
          onCreateChat={loadChats}
        />
      </div>

      {/* Центральная панель - Лента постов или Чат */}
      <div className={`
        ${selectedChat || currentView !== 'chats' ? 'flex' : 'hidden md:flex'} 
        flex-1 flex flex-col overflow-hidden
      `}>
        <Suspense fallback={
          <div className="flex-1 flex items-center justify-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
          </div>
        }>
          {selectedChat ? (
            <div className="flex-1 overflow-hidden flex flex-col">
              <ChatWindow
                chat={selectedChat}
                onBack={() => selectChat(null)}
              />
            </div>
          ) : currentView === 'files' ? (
            <div className="flex-1 flex flex-col overflow-hidden">
              {/* Заголовок хранилища */}
              <div className="h-16 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between px-4 sm:px-6 shrink-0">
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setCurrentView('chats')}
                    className="p-2 -ml-2 text-gray-400 hover:text-apple-blue md:hidden transition-colors"
                  >
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                    </svg>
                  </button>
                  <h2 className="text-lg font-semibold dark:text-white">Хранилище</h2>
                </div>
                <button
                  onClick={() => setCurrentView('chats')}
                  className="text-sm text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-1 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                >
                  К чатам
                </button>
              </div>
              <div className="flex-1 overflow-hidden">
                <StoragePage />
              </div>
            </div>
          ) : (
            <div className="flex-1 flex flex-col overflow-hidden">
              {/* Заголовок ленты */}
              <div className="h-16 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between px-4 sm:px-6 shrink-0">
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setCurrentView('chats')}
                    className="p-2 -ml-2 text-gray-400 hover:text-apple-blue md:hidden transition-colors"
                  >
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                    </svg>
                  </button>
                  <h2 className="text-lg font-semibold dark:text-white">Лента</h2>
                  {feedUnread > 0 && (
                    <span className="bg-blue-500 text-white text-xs font-semibold px-2 py-0.5 rounded-full">
                      {feedUnread > 99 ? '99+' : feedUnread}
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {user?.role === 'Boss' && (
                    <Link
                      to="/manage-org"
                      className="hidden sm:inline-block text-sm text-apple-blue hover:underline px-3 py-1 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                    >
                      Управление
                    </Link>
                  )}
                  <button
                    onClick={() => logout()}
                    className="text-sm text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white px-3 py-1 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                  >
                    Выйти
                  </button>
                </div>
              </div>
              {/* Контент ленты */}
              <div className="flex-1 overflow-y-auto">
                <FeedCenter />
              </div>
            </div>
          )}
        </Suspense>
      </div>

      {showProfile && (
        <ProfileModal
          user={user}
          onClose={() => setShowProfile(false)}
          onUpdate={handleProfileUpdate}
        />
      )}
    </div>
  );
};

export const TelegramLayout = memo(TelegramLayoutComponent);
