import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { usersAPI } from '../../services/api';
import { useAuth } from '../../hooks/useAuthOptimized';
import { getImageUrl } from '../../utils/imageUrl';
import type { User } from '../../types';

interface ProfileModalProps {
  user: User | null;
  onClose: () => void;
  onUpdate: () => void;
}

export const ProfileModal = ({ user: userProp, onClose, onUpdate }: ProfileModalProps) => {
  const { user, updateUser } = useAuth();
  // Используем user из useAuth, если он доступен, иначе используем prop
  const currentUser = user || userProp;

  const [activeTab, setActiveTab] = useState<'profile' | 'password' | 'settings'>('profile');
  const [darkMode, setDarkMode] = useState(() => {
    return sessionStorage.getItem('darkMode') === 'true';
  });
  const [firstName, setFirstName] = useState(currentUser?.firstName || '');
  const [lastName, setLastName] = useState(currentUser?.lastName || '');
  const [avatar, setAvatar] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(currentUser?.avatarUrl || null);

  // Обновляем состояние при изменении пользователя
  useEffect(() => {
    if (currentUser) {
      setFirstName(currentUser.firstName || '');
      setLastName(currentUser.lastName || '');
      setAvatarPreview(getImageUrl(currentUser.avatarUrl));
    }
  }, [currentUser]);
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Очищаем сообщения при переключении вкладок
  const handleTabChange = (tab: 'profile' | 'password' | 'settings') => {
    setActiveTab(tab);
    setError(null);
    setSuccess(null);
  };

  // Обработка переключения темы
  useEffect(() => {
    if (darkMode) {
      document.documentElement.classList.add('dark');
      sessionStorage.setItem('darkMode', 'true');
    } else {
      document.documentElement.classList.remove('dark');
      sessionStorage.setItem('darkMode', 'false');
    }
  }, [darkMode]);

  const handleAvatarSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.size > 2 * 1024 * 1024) {
        setError('Размер файла не должен превышать 2 МБ');
        return;
      }
      setAvatar(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setAvatarPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleProfileUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const formData = new FormData();
      // Всегда отправляем firstName и lastName, даже если они не изменились
      // (сервер обновит только если они переданы)
      if (firstName.trim()) {
        formData.append('firstName', firstName.trim());
      }
      if (lastName.trim()) {
        formData.append('lastName', lastName.trim());
      }
      if (avatar) {
        formData.append('avatar', avatar);
      }

      const updatedUser = await usersAPI.updateProfile(formData);
      // Обновляем данные пользователя в useAuth
      updateUser(updatedUser);
      // Обновляем превью аватара с новым URL из ответа сервера
      if (updatedUser.avatarUrl) {
        setAvatarPreview(getImageUrl(updatedUser.avatarUrl));
      } else {
        setAvatarPreview(null);
      }
      // Очищаем выбранный файл, так как он уже отправлен
      setAvatar(null);
      setSuccess('Профиль обновлен');
      // Автоматически очищаем сообщение об успехе через 3 секунды
      setTimeout(() => {
        setSuccess(null);
      }, 3000);
      // Обновляем данные в родительском компоненте
      await onUpdate();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка обновления профиля');
    } finally {
      setLoading(false);
    }
  };

  const handlePasswordChange = async (e: React.FormEvent) => {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      setError('Пароли не совпадают');
      return;
    }

    if (newPassword.length < 6) {
      setError('Пароль должен содержать минимум 6 символов');
      return;
    }

    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      await usersAPI.changePassword({
        oldPassword,
        newPassword,
      });
      setSuccess('Пароль изменен');
      // Автоматически очищаем сообщение об успехе через 3 секунды
      setTimeout(() => {
        setSuccess(null);
      }, 3000);
      setOldPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка смены пароля');
    } finally {
      setLoading(false);
    }
  };

  if (!currentUser) return null;

  return (
    <AnimatePresence>
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={onClose}>
        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.9 }}
          onClick={(e) => e.stopPropagation()}
          className="bg-white dark:bg-gray-800 rounded-2xl shadow-xl w-full max-w-md max-h-[90vh] overflow-hidden flex flex-col"
        >
          {/* Заголовок */}
          <div className="p-6 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold dark:text-white">Профиль</h2>
              <button
                onClick={onClose}
                className="w-8 h-8 flex items-center justify-center hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors text-gray-600 dark:text-gray-300"
              >
                ×
              </button>
            </div>
            <div className="flex gap-2 mt-4">
              <button
                onClick={() => handleTabChange('profile')}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${activeTab === 'profile'
                  ? 'bg-apple-blue text-white'
                  : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                  }`}
              >
                Профиль
              </button>
              <button
                onClick={() => handleTabChange('password')}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${activeTab === 'password'
                  ? 'bg-apple-blue text-white'
                  : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                  }`}
              >
                Пароль
              </button>
              <button
                onClick={() => handleTabChange('settings')}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${activeTab === 'settings'
                  ? 'bg-apple-blue text-white'
                  : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                  }`}
              >
                Настройки
              </button>
            </div>
          </div>

          {/* Контент */}
          <div className="flex-1 overflow-y-auto p-6">
            {activeTab === 'profile' && (
              <form onSubmit={handleProfileUpdate} className="space-y-6">
                {/* Аватар */}
                <div className="flex flex-col items-center">
                  <div className="relative">
                    <div className="w-24 h-24 rounded-full bg-apple-blue text-white flex items-center justify-center text-2xl font-medium">
                      {avatarPreview ? (
                        <img
                          src={avatarPreview}
                          alt="Avatar"
                          className="w-full h-full rounded-full object-cover"
                          loading="lazy"
                        />
                      ) : (
                        (currentUser.firstName || currentUser.email).charAt(0).toUpperCase()
                      )}
                    </div>
                    <label
                      htmlFor="avatar-upload"
                      className="absolute bottom-0 right-0 w-8 h-8 bg-apple-blue text-white rounded-full flex items-center justify-center cursor-pointer hover:bg-blue-600 transition-colors"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M12 4v16m8-8H4"
                        />
                      </svg>
                    </label>
                    <input
                      id="avatar-upload"
                      type="file"
                      accept="image/*"
                      onChange={handleAvatarSelect}
                      className="hidden"
                    />
                  </div>
                </div>

                {/* Имя */}
                <div>
                  <label className="block text-sm font-medium mb-2 dark:text-white">Имя</label>
                  <input
                    type="text"
                    className="input-field"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                  />
                </div>

                {/* Фамилия */}
                <div>
                  <label className="block text-sm font-medium mb-2 dark:text-white">Фамилия</label>
                  <input
                    type="text"
                    className="input-field"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                  />
                </div>

                {/* Email (только для отображения) */}
                <div>
                  <label className="block text-sm font-medium mb-2 dark:text-white">Email</label>
                  <input
                    type="email"
                    className="input-field bg-gray-50 dark:bg-gray-700"
                    value={currentUser.email}
                    disabled
                  />
                </div>

                {error && (
                  <div className="p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-lg text-sm">{error}</div>
                )}

                {success && (
                  <div className="p-3 bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400 rounded-lg text-sm">{success}</div>
                )}

                <button
                  type="submit"
                  disabled={loading}
                  className="w-full px-4 py-2 bg-apple-blue text-white rounded-lg font-medium hover:bg-blue-600 transition-colors disabled:opacity-50"
                >
                  {loading ? 'Сохранение...' : 'Сохранить'}
                </button>
              </form>
            )}

            {activeTab === 'password' && (
              <form onSubmit={handlePasswordChange} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium mb-2 dark:text-white">Текущий пароль</label>
                  <input
                    type="password"
                    className="input-field"
                    value={oldPassword}
                    onChange={(e) => setOldPassword(e.target.value)}
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2 dark:text-white">Новый пароль</label>
                  <input
                    type="password"
                    className="input-field"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                    minLength={6}
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2 dark:text-white">Подтвердите пароль</label>
                  <input
                    type="password"
                    className="input-field"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    minLength={6}
                  />
                </div>

                {error && (
                  <div className="p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-lg text-sm">{error}</div>
                )}

                {success && (
                  <div className="p-3 bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400 rounded-lg text-sm">{success}</div>
                )}

                <button
                  type="submit"
                  disabled={loading}
                  className="w-full px-4 py-2 bg-apple-blue text-white rounded-lg font-medium hover:bg-blue-600 transition-colors disabled:opacity-50"
                >
                  {loading ? 'Изменение...' : 'Изменить пароль'}
                </button>
              </form>
            )}

            {activeTab === 'settings' && (
              <div className="space-y-6">
                <div>
                  <h3 className="text-lg font-semibold mb-4 dark:text-white">Внешний вид</h3>
                  <div className="flex items-center justify-between p-4 border border-gray-200 dark:border-gray-700 rounded-lg">
                    <div>
                      <div className="font-medium text-gray-900 dark:text-white">Темная тема</div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">Включить темную тему интерфейса</div>
                    </div>
                    <button
                      onClick={() => setDarkMode(!darkMode)}
                      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${darkMode ? 'bg-apple-blue' : 'bg-gray-300'
                        }`}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${darkMode ? 'translate-x-6' : 'translate-x-1'
                          }`}
                      />
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
};

