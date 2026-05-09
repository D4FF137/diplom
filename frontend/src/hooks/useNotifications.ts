import { useState, useEffect, useCallback } from 'react';
import { notificationWsService } from '../services/notificationWebSocket';
import { notificationAPI } from '../services/api';
import type { NotificationCounters } from '../types';

export const useNotifications = () => {
  const [counters, setCounters] = useState<NotificationCounters>({
    chatUnread: {},
    feedUnread: 0,
    tasksUnread: 0,
  });
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const connect = async () => {
      try {
        await notificationWsService.connect();
        setIsConnected(true);

        // Загружаем начальные счетчики
        const initialCounters = await notificationAPI.getCounters();
        setCounters(initialCounters);

        // Подписываемся на обновления счетчиков
        notificationWsService.onNotificationCounters((updatedCounters) => {
          setCounters(updatedCounters);
        });
      } catch (error) {
        console.error('Error connecting to notification WebSocket:', error);
        setIsConnected(false);
      }
    };

    connect();

    return () => {
      notificationWsService.disconnect().catch(console.error);
      notificationWsService.offNotificationCounters();
    };
  }, []);

  const markChatAsRead = useCallback(async (chatId: number) => {
    try {
      await notificationAPI.markChatAsRead(chatId);
      // Счетчики обновятся автоматически через WebSocket
    } catch (error) {
      console.error('Error marking chat as read:', error);
    }
  }, []);

  const markFeedAsRead = useCallback(async () => {
    try {
      await notificationAPI.markFeedAsRead();
      // Счетчики обновятся автоматически через WebSocket
    } catch (error) {
      console.error('Error marking feed as read:', error);
    }
  }, []);

  const getChatUnreadCount = useCallback((chatId: number): number => {
    return counters.chatUnread[chatId.toString()] || 0;
  }, [counters]);

  const getTotalChatUnread = useCallback((): number => {
    return Object.values(counters.chatUnread).reduce((sum, count) => sum + count, 0);
  }, [counters]);

  return {
    counters,
    isConnected,
    markChatAsRead,
    markFeedAsRead,
    getChatUnreadCount,
    getTotalChatUnread,
    feedUnread: counters.feedUnread,
  };
};




