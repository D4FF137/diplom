import { useState, useEffect } from 'react';
import { authAPI, usersAPI } from '../services/api';
import type { User, LoginRequest } from '../types';

interface UseAuthReturn {
  user: User | null;
  loading: boolean;
  login: (data: LoginRequest) => Promise<void>;
// register removed
  logout: () => void;
  updateUser: (user: User) => void;
  refreshUser: () => Promise<void>;
  isAuthenticated: boolean;
}

export const useAuth = (): UseAuthReturn => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const storedUser = sessionStorage.getItem('user');
    if (storedUser) {
      try {
        setUser(JSON.parse(storedUser));
      } catch (e) {
        sessionStorage.removeItem('user');
      }
    }
    setLoading(false);
  }, []);

  const login = async (data: LoginRequest) => {
    const response = await authAPI.login(data);
    // Загружаем полный профиль пользователя после логина
    try {
      const fullUser = await usersAPI.getProfile();
      setUser(fullUser);
      sessionStorage.setItem('user', JSON.stringify(fullUser));
    } catch (err) {
      // Если не удалось загрузить полный профиль, используем данные из ответа
      console.error('Error loading full profile:', err);
      const user: User = {
        id: response.userId,
        email: response.email,
        firstName: response.firstName,
        lastName: response.lastName,
        companyId: response.companyId,
        avatarUrl: undefined,
        createdAt: new Date().toISOString(),
      };
      setUser(user);
      sessionStorage.setItem('user', JSON.stringify(user));
    }
  };



  const logout = async () => {
    authAPI.logout();
    setUser(null);
    sessionStorage.removeItem('user');
    sessionStorage.removeItem('selectedChatId');
    sessionStorage.removeItem('isOnFeed');
    // Очищаем WebSocket соединения
    if (typeof window !== 'undefined') {
      // Импортируем динамически, чтобы избежать циклических зависимостей
      try {
        const { feedWsService } = await import('../services/feedWebSocket');
        await feedWsService.disconnect();
      } catch (err) {
        console.error('Error disconnecting WebSocket:', err);
      }
      // Редирект на страницу авторизации
      window.location.href = '/auth';
    }
  };

  const updateUser = (updatedUser: User) => {
    setUser(updatedUser);
    sessionStorage.setItem('user', JSON.stringify(updatedUser));
  };

  const refreshUser = async () => {
    try {
      const updatedUser = await usersAPI.getProfile();
      setUser(updatedUser);
      sessionStorage.setItem('user', JSON.stringify(updatedUser));
    } catch (err) {
      console.error('Error refreshing user:', err);
    }
  };

  return {
    user,
    loading,
    login,

    logout,
    updateUser,
    refreshUser,
    isAuthenticated: !!user,
  };
};

