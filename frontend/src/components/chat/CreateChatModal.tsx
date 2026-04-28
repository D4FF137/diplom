import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { chatAPI, usersAPI } from '../../services/api';
import { getImageUrl } from '../../utils/imageUrl';
import { useAuth } from '../../hooks/useAuthOptimized';
import type { Chat, User } from '../../types';

interface CreateChatModalProps {
  onClose: () => void;
  onSuccess: (chat: Chat) => void;
  existingChats?: Chat[];
}

export const CreateChatModal = ({ onClose, onSuccess, existingChats = [] }: CreateChatModalProps) => {
  const { user: currentUser } = useAuth();
  const [step, setStep] = useState<'type' | 'name' | 'users'>('type');
  const [chatType, setChatType] = useState<'group' | 'private'>('group');
  const [chatName, setChatName] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [users, setUsers] = useState<User[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const searchUsers = async (query: string) => {
    if (query.length < 2) {
      setUsers([]);
      return;
    }

    try {
      const results = await usersAPI.search(query);
      setUsers(results);
    } catch (err) {
      console.error('Error searching users:', err);
    }
  };

  const handleCreateChat = async () => {
    if (chatType === 'group' && !chatName.trim()) {
      setError('Введите название чата');
      return;
    }

    if (chatType === 'private' && selectedUsers.length === 0) {
      setError('Выберите пользователя');
      return;
    }

    // Для приватных чатов проверяем, не существует ли уже чат с этим пользователем
    if (chatType === 'private' && selectedUsers.length === 1 && currentUser) {
      const partnerId = selectedUsers[0].id;
      const existingChat = existingChats.find(chat => {
        if (chat.type === 'private' && chat.members) {
          // Проверяем, что в чате ровно 2 участника: текущий пользователь и выбранный
          return chat.members.length === 2 && 
                 chat.members.some(m => m.id === partnerId) &&
                 chat.members.some(m => m.id === currentUser.id);
        }
        return false;
      });

      if (existingChat) {
        // Если чат уже существует, просто переключаемся на него
        onSuccess(existingChat);
        return;
      }
    }

    setLoading(true);
    setError(null);

    try {
      const chat = await chatAPI.createChat({
        name: chatName || selectedUsers[0]?.email || 'Private Chat',
        type: chatType,
        userIds: selectedUsers.map(u => u.id),
      });
      onSuccess(chat);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка создания чата');
    } finally {
      setLoading(false);
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
          className="bg-gray-50 dark:bg-gray-800 rounded-2xl shadow-xl w-full max-w-md max-h-[90vh] overflow-hidden flex flex-col border border-gray-200 dark:border-gray-700"
        >
          {/* Заголовок */}
          <div className="p-6 border-b border-gray-200 dark:border-gray-700">
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Создать чат</h2>
          </div>

          {/* Контент */}
          <div className="flex-1 overflow-y-auto p-6">
            {step === 'type' && (
              <div className="space-y-4">
                <p className="text-gray-600 dark:text-gray-400 mb-4">Выберите тип чата:</p>
                <button
                  onClick={() => {
                    setChatType('group');
                    setStep('name');
                  }}
                  className="w-full p-4 border-2 border-gray-200 dark:border-gray-600 rounded-xl hover:border-apple-blue hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-all text-left bg-gray-100 dark:bg-gray-700/50 text-gray-900 dark:text-white"
                >
                  <div className="font-medium">Групповой чат</div>
                  <div className="text-sm text-gray-500 dark:text-gray-400 mt-1">Создать чат с несколькими участниками</div>
                </button>
                <button
                  onClick={() => {
                    setChatType('private');
                    setStep('users');
                  }}
                  className="w-full p-4 border-2 border-gray-200 dark:border-gray-600 rounded-xl hover:border-apple-blue hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-all text-left bg-gray-100 dark:bg-gray-700/50 text-gray-900 dark:text-white"
                >
                  <div className="font-medium">Личный чат</div>
                  <div className="text-sm text-gray-500 dark:text-gray-400 mt-1">Начать переписку с коллегой</div>
                </button>
              </div>
            )}

            {step === 'name' && (
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2 text-gray-700 dark:text-gray-300">Название чата</label>
                  <input
                    type="text"
                    className="input-field"
                    value={chatName}
                    onChange={(e) => setChatName(e.target.value)}
                    placeholder="Введите название"
                    autoFocus
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-2 text-gray-700 dark:text-gray-300">Добавить участников</label>
                  <input
                    type="text"
                    className="input-field"
                    placeholder="Поиск по имени или email..."
                    value={searchQuery}
                    onChange={(e) => {
                      setSearchQuery(e.target.value);
                      searchUsers(e.target.value);
                    }}
                  />
                  {users.length > 0 && (
                    <div className="mt-2 space-y-2 max-h-48 overflow-y-auto">
                      {users.map((user) => {
                        const isSelected = selectedUsers.some(u => u.id === user.id);
                        return (
                          <button
                            key={user.id}
                            onClick={() => {
                              if (isSelected) {
                                setSelectedUsers(selectedUsers.filter(u => u.id !== user.id));
                              } else {
                                setSelectedUsers([...selectedUsers, user]);
                              }
                            }}
                            className={`w-full p-3 rounded-lg text-left transition-colors text-gray-900 dark:text-white ${
                              isSelected ? 'bg-blue-100 dark:bg-blue-900/30 border-2 border-apple-blue' : 'bg-gray-100 dark:bg-gray-700/50 hover:bg-gray-200 dark:hover:bg-gray-700'
                            }`}
                          >
                            <div className="flex items-center gap-3">
                              <div className="w-10 h-10 rounded-full bg-apple-blue text-white flex items-center justify-center text-sm font-medium">
                                {getImageUrl(user.avatarUrl) ? (
                                  <img src={getImageUrl(user.avatarUrl) || ''} alt="Avatar" className="w-full h-full rounded-full object-cover" />
                                ) : (
                                  (user.firstName || user.email).charAt(0).toUpperCase()
                                )}
                              </div>
                              <div>
                                <div className="font-medium">
                                  {user.firstName || user.lastName
                                    ? `${user.firstName || ''} ${user.lastName || ''}`.trim()
                                    : user.email}
                                </div>
                                {user.firstName && <div className="text-sm text-gray-500 dark:text-gray-400">{user.email}</div>}
                              </div>
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  )}
                </div>
                {selectedUsers.length > 0 && (
                  <div className="mt-4">
                    <p className="text-sm font-medium mb-2 text-gray-700 dark:text-gray-300">Выбранные участники:</p>
                    <div className="flex flex-wrap gap-2">
                      {selectedUsers.map((user) => (
                        <div
                          key={user.id}
                          className="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-200 rounded-full text-sm flex items-center gap-2"
                        >
                          {user.firstName || user.email}
                          <button
                            onClick={() => setSelectedUsers(selectedUsers.filter(u => u.id !== user.id))}
                            className="hover:text-blue-600"
                          >
                            ×
                          </button>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}

            {step === 'users' && (
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2 text-gray-700 dark:text-gray-300">Поиск коллег</label>
                  <input
                    type="text"
                    className="input-field"
                    placeholder="Поиск по имени или email..."
                    value={searchQuery}
                    onChange={(e) => {
                      setSearchQuery(e.target.value);
                      searchUsers(e.target.value);
                    }}
                    autoFocus
                  />
                </div>
                {users.length > 0 && (
                  <div className="space-y-2 max-h-64 overflow-y-auto">
                    {users.map((user) => (
                      <button
                        key={user.id}
                        onClick={() => {
                          setSelectedUsers([user]);
                          handleCreateChat();
                        }}
                        className="w-full p-3 rounded-lg bg-gray-100 dark:bg-gray-700/50 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors text-left text-gray-900 dark:text-white"
                      >
                        <div className="flex items-center gap-3">
                          <div className="w-10 h-10 rounded-full bg-apple-blue text-white flex items-center justify-center text-sm font-medium">
                            {getImageUrl(user.avatarUrl) ? (
                              <img src={getImageUrl(user.avatarUrl) || ''} alt="Avatar" className="w-full h-full rounded-full object-cover" />
                            ) : (
                              (user.firstName || user.email).charAt(0).toUpperCase()
                            )}
                          </div>
                          <div>
                            <div className="font-medium">
                              {user.firstName || user.lastName
                                ? `${user.firstName || ''} ${user.lastName || ''}`.trim()
                                : user.email}
                            </div>
                            {user.firstName && <div className="text-sm text-gray-500 dark:text-gray-400">{user.email}</div>}
                          </div>
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}

            {error && (
              <div className="mt-4 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-lg text-sm">{error}</div>
            )}
          </div>

          {/* Кнопки */}
          <div className="p-6 border-t border-gray-200 dark:border-gray-700 flex gap-3">
            <button
              onClick={onClose}
              className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-900 dark:text-white"
            >
              Отмена
            </button>
            {(step === 'name' || (step === 'users' && selectedUsers.length > 0)) && (
              <button
                onClick={handleCreateChat}
                disabled={loading}
                className="flex-1 px-4 py-2 bg-apple-blue text-white rounded-lg hover:bg-blue-600 transition-colors disabled:opacity-50"
              >
                {loading ? 'Создание...' : 'Создать'}
              </button>
            )}
            {step === 'name' && (
              <button
                onClick={() => setStep('type')}
                className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-900 dark:text-white"
              >
                Назад
              </button>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};


