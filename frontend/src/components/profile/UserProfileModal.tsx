import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { usersAPI, chatAPI } from '../../services/api';
import { getImageUrl } from '../../utils/imageUrl';
import { useAuth } from '../../hooks/useAuthOptimized';
import type { User } from '../../types';

interface UserProfileModalProps {
  userId: number;
  onClose: () => void;
  onMessageSent?: () => void;
}

export const UserProfileModal = ({ userId, onClose, onMessageSent }: UserProfileModalProps) => {
  const { user: currentUser } = useAuth();
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState('');
  const [sending, setSending] = useState(false);

  useEffect(() => {
    loadUser();
  }, [userId]);

  const loadUser = async () => {
    try {
      setLoading(true);
      const userData = await usersAPI.getUserById(userId);
      setUser(userData);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка загрузки профиля');
    } finally {
      setLoading(false);
    }
  };

  const handleSendMessage = async () => {
    if (!message.trim() || !currentUser) return;

    setSending(true);
    try {
      // Создаем или находим чат с пользователем
      const chats = await chatAPI.getChats();
      let chat = chats.find(c =>
        c.type === 'private' &&
        c.members?.length === 2 &&
        c.members?.some(m => m.id === userId) &&
        c.members?.some(m => m.id === currentUser.id)
      );

      if (!chat) {
        // Создаем новый приватный чат
        // Событие о создании чата будет отправлено через WebSocket автоматически
        chat = await chatAPI.createChat({
          name: `${currentUser.firstName || currentUser.email} - ${user?.firstName || user?.email}`,
          type: 'private',
          userIds: [userId],
        });

        // Обновляем список чатов после создания
        // Это будет сделано автоматически через WebSocket, но на всякий случай обновляем
        window.dispatchEvent(new Event('chatCreated'));
      }

      // Отправляем сообщение
      await chatAPI.sendMessage({
        chatId: chat.id,
        content: message.trim(),
      });

      setMessage('');
      if (onMessageSent) {
        onMessageSent();
      }
      onClose();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Ошибка отправки сообщения');
    } finally {
      setSending(false);
    }
  };

  if (loading) {
    return (
      <AnimatePresence>
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={onClose}>
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            onClick={(e) => e.stopPropagation()}
            className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6"
          >
            <div className="flex justify-center items-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-apple-blue"></div>
            </div>
          </motion.div>
        </div>
      </AnimatePresence>
    );
  }

  if (error || !user) {
    return (
      <AnimatePresence>
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={onClose}>
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            onClick={(e) => e.stopPropagation()}
            className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6"
          >
            <div className="text-center">
              <p className="text-red-600 mb-4">{error || 'Пользователь не найден'}</p>
              <button
                onClick={onClose}
                className="px-4 py-2 bg-apple-blue text-white rounded-lg hover:bg-blue-600 transition-colors"
              >
                Закрыть
              </button>
            </div>
          </motion.div>
        </div>
      </AnimatePresence>
    );
  }

  const userName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email;
  const userAvatar = getImageUrl(user.avatarUrl);

  return (
    <AnimatePresence>
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={onClose}>
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.9 }}
          onClick={(e) => e.stopPropagation()}
          className="bg-white rounded-2xl shadow-xl w-full max-w-md max-h-[90vh] overflow-hidden flex flex-col"
        >
          {/* Заголовок */}
          <div className="p-6 border-b border-gray-200">
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold">Профиль пользователя</h2>
              <button
                onClick={onClose}
                className="w-8 h-8 flex items-center justify-center hover:bg-gray-100 rounded-lg transition-colors"
              >
                ×
              </button>
            </div>
          </div>

          {/* Контент */}
          <div className="flex-1 overflow-y-auto p-6">
            {/* Аватар и имя */}
            <div className="flex flex-col items-center mb-6">
              <div className="w-24 h-24 rounded-full bg-apple-blue text-white flex items-center justify-center text-2xl font-medium mb-4">
                {userAvatar ? (
                  <img
                    src={userAvatar}
                    alt="Avatar"
                    className="w-full h-full rounded-full object-cover"
                    loading="lazy"
                  />
                ) : (
                  userName.charAt(0).toUpperCase()
                )}
              </div>
              <h3 className="text-lg font-semibold text-gray-900">{userName}</h3>
              <p className="text-sm text-gray-500 mt-1">{user.email}</p>
            </div>

            {/* Форма отправки сообщения */}
            {currentUser && currentUser.id !== user.id && (
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Написать сообщение</label>
                  <textarea
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    placeholder="Введите сообщение..."
                    className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-apple-blue focus:border-transparent resize-none"
                    rows={3}
                  />
                </div>
                <button
                  onClick={handleSendMessage}
                  disabled={!message.trim() || sending}
                  className="w-full px-4 py-2 bg-apple-blue text-white rounded-lg font-medium hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {sending ? 'Отправка...' : 'Отправить сообщение'}
                </button>
              </div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

