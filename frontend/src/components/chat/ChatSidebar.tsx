import { useState, useMemo, useCallback, memo } from 'react';
import { motion } from 'framer-motion';
import { CreateChatModal } from './CreateChatModal';
import { DeleteChatModal } from './DeleteChatModal';
import { useAuthStore } from '../../stores/authStore';
import { useChatStore } from '../../stores/chatStore';
import { useNotificationStore } from '../../stores/notificationStore';
import { getImageUrl } from '../../utils/imageUrl';
import type { Chat } from '../../types';

interface ChatSidebarProps {
  onCreateChat: () => void;
}

const ChatSidebarComponent = ({ onCreateChat }: ChatSidebarProps) => {
  const user = useAuthStore((state) => state.user);
  const chats = useChatStore((state) => state.chats);
  const selectedChat = useChatStore((state) => state.selectedChat);
  const selectChat = useChatStore((state) => state.selectChat);
  const togglePinChat = useChatStore((state) => state.togglePinChat);
  const chatUnread = useNotificationStore((state) => state.counters.chatUnread);
  const pinnedChats = useChatStore((state) => state.pinnedChats);

  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [chatToDelete, setChatToDelete] = useState<Chat | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  // Мемоизированные функции
  const getChatPartner = useCallback((chat: Chat) => {
    if (chat.type === 'private' && chat.members && user) {
      return chat.members.find(m => String(m.id) !== String(user.id)) || null;
    }
    return null;
  }, [user]);

  const getChatAvatar = useCallback((chat: Chat) => {
    if (chat.type === 'private') {
      const partner = getChatPartner(chat);
      if (partner) {
        const avatarUrl = getImageUrl(partner.avatarUrl);
        const name = `${partner.firstName || ''} ${partner.lastName || ''}`.trim() || partner.email;
        return { avatarUrl, name };
      }
    }
    return { avatarUrl: null, name: chat.name };
  }, [getChatPartner]);

  const handlePinClick = useCallback((e: React.MouseEvent, chat: Chat) => {
    e.stopPropagation();
    togglePinChat(chat.id);
  }, [togglePinChat]);

  const handleDeleteClick = useCallback((e: React.MouseEvent, chat: Chat) => {
    e.stopPropagation();
    setChatToDelete(chat);
    setShowDeleteModal(true);
  }, []);

  const handleDeleteSuccess = useCallback(() => {
    onCreateChat();
    if (selectedChat && chatToDelete && String(selectedChat.id) === String(chatToDelete.id)) {
      selectChat(null);
    }
  }, [onCreateChat, selectedChat, chatToDelete, selectChat]);

  // Фильтрация чатов
  const filteredChats = useMemo(() => {
    if (!searchQuery.trim()) return chats;

    const query = searchQuery.toLowerCase();

    return chats.filter((chat) => {
      const partner = getChatPartner(chat);
      const displayName = chat.type === 'private' && partner
        ? `${partner.firstName || ''} ${partner.lastName || ''}`.trim() || partner.email
        : chat.name;

      return (
        displayName.toLowerCase().includes(query) ||
        (chat.lastMessage && chat.lastMessage.toLowerCase().includes(query))
      );
    });
  }, [chats, searchQuery, getChatAvatar, getChatPartner]);

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      {/* Кнопка создания чата и Поиск */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-700 space-y-3">
        <button
          onClick={() => setShowCreateModal(true)}
          className="w-full px-4 py-2 bg-apple-blue text-white rounded-lg font-medium hover:bg-blue-600 transition-colors flex items-center justify-center gap-2"
        >
          <span>+</span> Создать чат
        </button>

        <div className="relative">
          <input
            type="text"
            placeholder="Поиск чатов..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-4 py-2 bg-gray-100 dark:bg-gray-800 border-none rounded-lg text-sm focus:ring-2 focus:ring-apple-blue dark:text-white transition-all"
          />
          <svg
            className="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
      </div>

      {/* Список чатов */}
      <div className="flex-1 overflow-y-auto">
        {filteredChats.length === 0 ? (
          <div className="p-4 text-center text-gray-500 dark:text-gray-400 text-sm">
            {searchQuery ? 'Чаты не найдены' : 'Нет чатов'}
            {!searchQuery && <p className="mt-2">Создайте новый чат</p>}
          </div>
        ) : (
          <div className="divide-y divide-gray-100 dark:divide-gray-700">
            {filteredChats.map((chat) => {
              const { avatarUrl, name } = getChatAvatar(chat);
              const partner = getChatPartner(chat);
              const displayName = chat.type === 'private' && partner
                ? `${partner.firstName || ''} ${partner.lastName || ''}`.trim() || partner.email
                : chat.name;
              const unreadCount = chatUnread[String(chat.id)] || 0;
              const isPinned = pinnedChats.has(chat.id);
              const isSelected = Boolean(selectedChat && String(selectedChat.id) === String(chat.id));

              return (
                <ChatItem
                  key={chat.id}
                  displayName={displayName}
                  avatarUrl={avatarUrl}
                  name={name}
                  isSelected={isSelected}
                  isPinned={isPinned}
                  unreadCount={unreadCount}
                  lastMessage={chat.lastMessage}
                  partnerEmail={partner?.email}
                  onSelect={() => selectChat(chat)}
                  onPin={(e) => handlePinClick(e, chat)}
                  onDelete={(e) => handleDeleteClick(e, chat)}
                />
              );
            })}
          </div>
        )}
      </div>

      {showCreateModal && (
        <CreateChatModal
          onClose={() => setShowCreateModal(false)}
          onSuccess={(chat) => {
            onCreateChat();
            selectChat(chat);
            setShowCreateModal(false);
          }}
          existingChats={chats}
        />
      )}

      {showDeleteModal && chatToDelete && (
        <DeleteChatModal
          chat={chatToDelete}
          onClose={() => {
            setShowDeleteModal(false);
            setChatToDelete(null);
          }}
          onSuccess={handleDeleteSuccess}
        />
      )}
    </div>
  );
};

// Мемоизированный компонент элемента чата
const ChatItem = memo(({
  displayName,
  avatarUrl,
  name,
  isSelected,
  isPinned,
  unreadCount,
  lastMessage,
  partnerEmail,
  onSelect,
  onPin,
  onDelete,
}: {
  displayName: string;
  avatarUrl: string | null;
  name: string;
  isSelected: boolean;
  isPinned: boolean;
  unreadCount: number;
  lastMessage?: string;
  partnerEmail?: string;
  onSelect: () => void;
  onPin: (e: React.MouseEvent) => void;
  onDelete: (e: React.MouseEvent) => void;
}) => (
  <motion.div
    onClick={onSelect}
    className={`w-full text-left p-4 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors relative cursor-pointer ${isSelected ? 'bg-blue-50 dark:bg-blue-900 border-l-4 border-apple-blue' : ''
      } ${isPinned ? 'bg-yellow-50 dark:bg-yellow-900/20' : ''}`}
    whileHover={{ x: 2 }}
    transition={{ duration: 0.1 }}
  >
    <div className="flex items-center gap-3">
      <div className="w-12 h-12 rounded-full bg-apple-blue text-white flex items-center justify-center text-lg font-medium flex-shrink-0 relative">
        {avatarUrl ? (
          <img
            src={avatarUrl}
            alt={name}
            className="w-full h-full rounded-full object-cover"
          />
        ) : (
          name.charAt(0).toUpperCase()
        )}
        {isPinned && (
          <div className="absolute -top-1 -right-1 w-4 h-4 bg-yellow-400 rounded-full flex items-center justify-center">
            <svg className="w-2.5 h-2.5 text-yellow-900" fill="currentColor" viewBox="0 0 20 20">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            </svg>
          </div>
        )}
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <h3 className="font-medium text-gray-900 dark:text-white truncate">{displayName}</h3>
          {isPinned && (
            <svg className="w-4 h-4 text-yellow-500 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            </svg>
          )}
          {unreadCount > 0 && (
            <span className="flex-shrink-0 bg-apple-blue text-white text-xs font-semibold px-2 py-0.5 rounded-full min-w-[20px] text-center">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
        </div>
        {lastMessage ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 truncate">
            {lastMessage}
          </p>
        ) : partnerEmail ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 truncate">
            {partnerEmail}
          </p>
        ) : (
          <p className="text-sm text-gray-500 dark:text-gray-400 truncate">
            Нет сообщений
          </p>
        )}
      </div>
      <div className="flex items-center gap-1">
        <button
          onClick={onPin}
          className="p-2 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-lg transition-colors flex-shrink-0"
          title={isPinned ? 'Открепить чат' : 'Закрепить чат'}
        >
          <svg
            className={`w-5 h-5 ${isPinned ? 'text-yellow-500' : 'text-gray-400 dark:text-gray-500'}`}
            fill={isPinned ? 'currentColor' : 'none'}
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"
            />
          </svg>
        </button>
        <button
          onClick={onDelete}
          className="p-2 hover:bg-red-100 dark:hover:bg-red-900/20 rounded-lg transition-colors flex-shrink-0"
          title="Удалить чат"
        >
          <svg
            className="w-5 h-5 text-gray-400 dark:text-gray-500 hover:text-red-600 dark:hover:text-red-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            />
          </svg>
        </button>
      </div>
    </div>
  </motion.div>
));

ChatItem.displayName = 'ChatItem';

export const ChatSidebar = memo(ChatSidebarComponent);

