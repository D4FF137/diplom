import { useEffect, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { notificationWsService } from '../services/notificationWebSocket';
import { notificationAPI } from '../services/api';
import { useNotificationStore } from '../stores/notificationStore';
import { useChatStore } from '../stores/chatStore';
import { useAuthStore } from '../stores/authStore';
import type { NotificationCounters } from '../types';

export const useNotifications = () => {
  const { counters, setCounters, setConnected, getChatUnreadCount, getTotalChatUnread } =
    useNotificationStore();
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const authLoading = useAuthStore((state) => state.loading);
  const isConnected = useNotificationStore((state) => state.isConnected);
  const canUseNotifications = !authLoading && !!user && isAuthenticated;

  const { data: initialCounters, error: countersError } = useQuery({
    queryKey: ['notificationCounters'],
    queryFn: notificationAPI.getCounters,
    enabled: canUseNotifications,
    staleTime: 1000 * 60,
    refetchOnWindowFocus: false,
    retry: 2,
  });

  useEffect(() => {
    if (initialCounters) {
      setCounters(initialCounters);
    }

    if (countersError) {
      console.error('[Notifications] Error fetching initial counters:', countersError);
    }
  }, [initialCounters, countersError, setCounters]);

  useEffect(() => {
    if (!canUseNotifications) {
      setConnected(false);
      return;
    }

    const callback = (updatedCounters: NotificationCounters) => {
      const activeChatId = useChatStore.getState().selectedChat?.id;
      if (activeChatId && updatedCounters.chatUnread[String(activeChatId)] !== undefined) {
        updatedCounters = {
          ...updatedCounters,
          chatUnread: {
            ...updatedCounters.chatUnread,
            [String(activeChatId)]: 0,
          },
        };
      }

      setCounters(updatedCounters);
    };

    if (notificationWsService.isConnected()) {
      setConnected(true);
      notificationWsService.onNotificationCounters(callback);

      return () => {
        notificationWsService.offNotificationCounters(callback);
      };
    }

    let isCancelled = false;

    notificationWsService.connect()
      .then(() => {
        if (isCancelled) return;
        setConnected(true);
        notificationWsService.onNotificationCounters(callback);
      })
      .catch((error) => {
        if (isCancelled) return;
        console.error('[Notifications] Error connecting to notification WebSocket:', error);
        setConnected(false);
      });

    return () => {
      isCancelled = true;
      notificationWsService.offNotificationCounters(callback);
    };
  }, [canUseNotifications, setConnected, setCounters]);

  const markChatAsRead = useCallback(async (chatId: number | string) => {
    try {
      await notificationAPI.markChatAsRead(chatId);
    } catch (error) {
      console.error('[Notifications] Error marking chat as read:', error);
    }
  }, []);

  const markFeedAsRead = useCallback(async () => {
    try {
      await notificationAPI.markFeedAsRead();
    } catch (error) {
      console.error('[Notifications] Error marking feed as read:', error);
    }
  }, []);

  const feedUnread = useNotificationStore((state) => state.counters.feedUnread);
  const tasksUnread = useNotificationStore((state) => state.counters.tasksUnread);

  const markTasksAsRead = useCallback(async () => {
    try {
      await notificationAPI.markTasksAsRead();
    } catch (error) {
      console.error('[Notifications] Error marking tasks as read:', error);
    }
  }, []);

  return {
    counters,
    isConnected,
    markChatAsRead,
    markFeedAsRead,
    markTasksAsRead,
    getChatUnreadCount,
    getTotalChatUnread,
    feedUnread,
    tasksUnread,
  };
};
