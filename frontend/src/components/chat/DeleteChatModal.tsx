import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { chatAPI } from '../../services/api';
import { useAuthStore } from '../../stores/authStore';
import type { Chat } from '../../types';

interface DeleteChatModalProps {
  chat: Chat;
  onClose: () => void;
  onSuccess: () => void;
}

export const DeleteChatModal = ({ chat, onClose, onSuccess }: DeleteChatModalProps) => {
  const user = useAuthStore((state) => state.user);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteType, setDeleteType] = useState<'self' | 'all'>('self');
  const [error, setError] = useState<string | null>(null);

  const handleDelete = async () => {
    try {
      setIsDeleting(true);
      setError(null);

      if (deleteType === 'self') {
        await chatAPI.leaveChat(chat.id);
      } else {
        await chatAPI.deleteChat(chat.id);
      }

      onSuccess();
      onClose();
    } catch (err: any) {
      console.error('Error deleting chat:', err);
      setError(err.response?.data?.message || 'Ошибка при удалении чата');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <AnimatePresence>
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={onClose}>
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.9 }}
          onClick={(e) => e.stopPropagation()}
          className="bg-white dark:bg-gray-800 rounded-2xl shadow-xl p-6 w-full max-w-md mx-4"
        >
          <h2 className="text-xl font-semibold mb-4 dark:text-white">
            Удалить чат
          </h2>

          <p className="text-gray-600 dark:text-gray-400 mb-6">
            Выберите тип удаления для чата <strong className="dark:text-white">{chat.name}</strong>:
          </p>

          {/* Показываем выбор типа удаления только для создателя чата. 
              Для остальных - только возможность выйти (аналог "Только для меня") */}
          {chat.creatorId === user?.id ? (
            <div className="space-y-3 mb-6">
              <label className="flex items-start gap-3 p-3 border border-gray-200 dark:border-gray-700 rounded-lg cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
                <input
                  type="radio"
                  name="deleteType"
                  value="self"
                  checked={deleteType === 'self'}
                  onChange={(e) => setDeleteType(e.target.value as 'self' | 'all')}
                  className="mt-1"
                />
                <div className="flex-1">
                  <div className="font-medium dark:text-white">Только для меня (Выйти)</div>
                  <div className="text-sm text-gray-500 dark:text-gray-400">
                    Вы покинете чат. Другие участники продолжат общаться.
                  </div>
                </div>
              </label>

              <label className="flex items-start gap-3 p-3 border border-gray-200 dark:border-gray-700 rounded-lg cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
                <input
                  type="radio"
                  name="deleteType"
                  value="all"
                  checked={deleteType === 'all'}
                  onChange={(e) => setDeleteType(e.target.value as 'self' | 'all')}
                  className="mt-1"
                />
                <div className="flex-1">
                  <div className="font-medium dark:text-white">Для всех</div>
                  <div className="text-sm text-gray-500 dark:text-gray-400">
                    Чат будет полностью удален для всех участников. Это действие нельзя отменить.
                  </div>
                </div>
              </label>
            </div>
          ) : (
            <div className="mb-6 bg-blue-50 dark:bg-blue-900/20 p-4 rounded-lg border border-blue-100 dark:border-blue-800">
              <p className="text-sm text-blue-800 dark:text-blue-200">
                Вы собираетесь покинуть чат <strong>{chat.name}</strong>.
                <br />
                Чат останется доступен для других участников.
              </p>
            </div>
          )}

          {error && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
              <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
            </div>
          )}

          <div className="flex gap-3 justify-end">
            <button
              onClick={onClose}
              disabled={isDeleting}
              className="px-4 py-2 text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors disabled:opacity-50"
            >
              Отмена
            </button>
            <button
              onClick={handleDelete}
              disabled={isDeleting}
              className={`px-4 py-2 text-white rounded-lg transition-colors disabled:opacity-50 ${chat.creatorId === user?.id && deleteType === 'all'
                ? 'bg-red-600 hover:bg-red-700'
                : 'bg-apple-blue hover:bg-blue-600'
                }`}
            >
              {isDeleting ? 'Выполнение...' : (chat.creatorId === user?.id && deleteType === 'all' ? 'Удалить' : 'Выйти')}
            </button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};




