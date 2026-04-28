import { useEffect, useMemo, lazy, Suspense, useState } from 'react';
import { useAuthStore } from '../../stores/authStore';
import { useChatStore } from '../../stores/chatStore';
import { useChats } from '../../hooks/queries/useChats';
import { wsService } from '../../services/websocket';
import { getImageUrl } from '../../utils/imageUrl';

// Lazy loading компонентов
const ChatSidebar = lazy(() => import('../chat/ChatSidebarOptimized').then(m => ({ default: m.ChatSidebar })));
const ChatWindow = lazy(() => import('../chat/ChatWindow').then(m => ({ default: m.ChatWindow })));
const FeedCenter = lazy(() => import('../feed/FeedCenter').then(m => ({ default: m.FeedCenter })));
const ProfileModal = lazy(() => import('../profile/ProfileModal').then(m => ({ default: m.ProfileModal })));

const LoadingSpinner = () => (
  <div className="flex justify-center items-center h-full">
    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
  </div>
);

export const TelegramLayout = () => {
  const user = useAuthStore((state) => state.user);
  const selectedChat = useChatStore((state) => state.selectedChat);
  const selectChat = useChatStore((state) => state.selectChat);
  const [showProfile, setShowProfile] = useState(false);

  const { refetch: loadChats } = useChats();

  // Мемоизация вычислений
  const getUserDisplayName = useMemo(() => {
    if (!user) return '';
    return `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email;
  }, [user]);

  useEffect(() => {
    loadChats();

    if (user) {
      const setupWebSocket = async () => {
        try {
          if (!wsService.isConnected()) {
            await wsService.connect();
          }

          wsService.onNewChat((newChat) => {
            useChatStore.getState().addChat(newChat);
          });

          wsService.onChatUpdated((data) => {
            useChatStore.getState().updateChat(data.chatId, {
              lastMessage: data.lastMessage,
              lastMessageAt: data.lastMessageAt,
            });
          });

          wsService.onChatDeleted((data) => {
            useChatStore.getState().removeChat(data.chatId);
            if (selectedChat?.id === data.chatId) {
              selectChat(null);
            }
          });

          wsService.onChatRemoved((data) => {
            useChatStore.getState().removeChat(data.chatId);
            if (selectedChat?.id === data.chatId) {
              selectChat(null);
            }
          });

          wsService.onMessageReceived((message) => {
            useChatStore.getState().updateChat(message.chatId, {
              lastMessage: message.content,
              lastMessageAt: message.createdAt,
            });
          });
        } catch (error) {
          console.error('Error setting up WebSocket for chats:', error);
        }
      };

      setupWebSocket();
    }
  }, [user, selectedChat, selectChat]);


  return (
    <div className="h-screen flex bg-gray-50 dark:bg-gray-900">
      <div className="w-80 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col">
        <div className="h-16 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between px-4">
          <h1 className="text-lg font-semibold dark:text-white">Чаты</h1>
          <button
            onClick={() => setShowProfile(true)}
            className="w-8 h-8 rounded-full bg-apple-blue text-white flex items-center justify-center text-sm font-medium hover:bg-blue-600 transition-colors"
          >
            {getImageUrl(user?.avatarUrl) ? (
              <img src={getImageUrl(user?.avatarUrl) || ''} alt="Avatar" className="w-full h-full rounded-full object-cover" />
            ) : (
              getUserDisplayName.charAt(0).toUpperCase()
            )}
          </button>
        </div>

        <Suspense fallback={<LoadingSpinner />}>
          <ChatSidebar onCreateChat={loadChats} />
        </Suspense>
      </div>

      <div className="flex-1 flex flex-col overflow-hidden">
        {selectedChat ? (
          <Suspense fallback={<LoadingSpinner />}>
            <ChatWindow chat={selectedChat} />
          </Suspense>
        ) : (
          <Suspense fallback={<LoadingSpinner />}>
            <FeedCenter />
          </Suspense>
        )}
      </div>

      {showProfile && user && (
        <Suspense fallback={<LoadingSpinner />}>
          <ProfileModal
            user={user}
            onClose={() => setShowProfile(false)}
            onUpdate={loadChats}
          />
        </Suspense>
      )}
    </div>
  );
};
