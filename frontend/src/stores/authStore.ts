import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { User } from '../types';
import { authAPI, usersAPI } from '../services/api';
import type { LoginRequest } from '../types';
import { useChatStore } from './chatStore';

interface AuthState {
  user: User | null;
  loading: boolean;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  updateUser: (user: User) => void;
  refreshUser: () => Promise<void>;
  setLoading: (loading: boolean) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      loading: true,
      isAuthenticated: false,

      setLoading: (loading) => set({ loading }),

      login: async (data) => {
        const response = await authAPI.login(data);
        try {
          const fullUser = await usersAPI.getProfile();
          set({ user: fullUser, isAuthenticated: true, loading: false });
        } catch (err) {
          console.error('Error loading full profile:', err);
          const user: User = {
            id: response.userId,
            email: response.email,
            firstName: response.firstName,
            lastName: response.lastName,
            companyId: response.companyId,
            role: response.role,
            createdAt: new Date().toISOString(),
          };
          set({ user, isAuthenticated: true, loading: false });
        }
      },

      logout: async () => { // Kept async because of WebSocket disconnection logic
        try {
          await authAPI.logout();
        } catch (err) {
          console.error('Error clearing auth cookie:', err);
        }

        set({ user: null, isAuthenticated: false });
        sessionStorage.removeItem('selectedChatId');
        useChatStore.getState().clearChats(); // Added this line
        sessionStorage.removeItem('isOnFeed');

        // Очищаем WebSocket соединения
        if (typeof window !== 'undefined') {
          try {
            const { feedWsService } = await import('../services/feedWebSocket');
            await feedWsService.disconnect();
          } catch (err) {
            console.error('Error disconnecting WebSocket:', err);
          }
          window.location.href = '/auth';
        }
      },

      updateUser: (user) => {
        set({ user });
      },

      refreshUser: async () => {
        try {
          const updatedUser = await usersAPI.getProfile();
          set({ user: updatedUser });
        } catch (err) {
          console.error('Error refreshing user:', err);
        }
      },
    }),
    {
      name: 'auth-storage',
      // Используем sessionStorage для изоляции между вкладками
      // Каждая вкладка будет иметь свой собственный пользователь
      storage: {
        getItem: (name) => {
          const value = sessionStorage.getItem(name);
          return value ? JSON.parse(value) : null;
        },
        setItem: (name, value) => {
          sessionStorage.setItem(name, JSON.stringify(value));
        },
        removeItem: (name) => {
          sessionStorage.removeItem(name);
        },
      },
      partialize: (state) => ({ user: state.user, isAuthenticated: state.isAuthenticated } as AuthState),
    }
  )
);
