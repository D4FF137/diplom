import { create } from 'zustand';
import type { NotificationCounters } from '../types';

interface NotificationState {
  counters: NotificationCounters;
  isConnected: boolean;
  setCounters: (counters: NotificationCounters) => void;
  setConnected: (connected: boolean) => void;
  getChatUnreadCount: (chatId: number | string) => number;
  getTotalChatUnread: () => number;
}

export const useNotificationStore = create<NotificationState>((set, get) => ({
  counters: {
    chatUnread: {},
    feedUnread: 0,
  },
  isConnected: false,

  setCounters: (counters) => {
    console.log('[NotificationStore] Setting counters:', counters);
    console.log('[NotificationStore] Chat unread keys:', Object.keys(counters.chatUnread));
    console.log('[NotificationStore] Chat unread values:', Object.values(counters.chatUnread));
    set({ counters });
  },
  setConnected: (connected) => set({ isConnected: connected }),

  getChatUnreadCount: (chatId) => {
    const { counters } = get();
    return counters.chatUnread[chatId.toString()] || 0;
  },

  getTotalChatUnread: () => {
    const { counters } = get();
    return Object.values(counters.chatUnread).reduce((sum, count) => sum + count, 0);
  },
}));


