import { HubConnectionBuilder, HubConnectionState, HubConnection } from '@microsoft/signalr';
import type { NotificationCounters } from '../types';

// Use direct NotificationService URL for WebSocket (SignalR requires direct connection)
const NOTIFICATION_WS_BASE_URL = import.meta.env.VITE_NOTIFICATION_WS_URL || 'http://localhost:5005';

class NotificationWebSocketService {
  private connection: HubConnection | null = null;

  private connectionPromise: Promise<void> | null = null;
  private callbacks: Set<(counters: NotificationCounters) => void> = new Set();

  connect(): Promise<void> {
    // Если соединение уже существует и подключено, не создаем новое
    if (this.connection && this.isConnected()) {
      return Promise.resolve();
    }

    // Если уже идет процесс подключения, возвращаем существующий промис
    if (this.connectionPromise) {
      return this.connectionPromise;
    }

    // Если соединение существует, но не подключено, останавливаем его
    if (this.connection) {
      this.connection.stop().catch(console.error);
    }


    
    this.connection = new HubConnectionBuilder()
      .withUrl(`${NOTIFICATION_WS_BASE_URL}/notificationsHub`, {

        withCredentials: true,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Экспоненциальная задержка: 0, 2, 10, 30 секунд
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      })
      .build();

    // Добавляем обработчики событий для отладки
    this.connection.onclose((error) => {
      console.log('Notification WebSocket connection closed', error);
    });

    this.connection.onreconnecting((error) => {
      console.log('Notification WebSocket reconnecting', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('Notification WebSocket reconnected', connectionId);
    });

    this.connectionPromise = this.connection.start()
      .then(() => {
        console.log('Notification WebSocket connected');
        this.connectionPromise = null;
        
        // Переподписываем все колбэки после переподключения
        this.callbacks.forEach(callback => {
          this.connection?.on('notificationCounters', callback);
        });
      })
      .catch((error) => {
        console.error('Error starting Notification WebSocket:', error);
        this.connectionPromise = null;
        throw error;
      });
    
    return this.connectionPromise;
  }

  disconnect(): Promise<void> {
    if (this.connection) {
      return this.connection.stop();
    }
    return Promise.resolve();
  }

  onNotificationCounters(callback: (counters: NotificationCounters) => void): void {
    // Сохраняем колбэк для переподписки при переподключении
    this.callbacks.add(callback);
    
    if (this.connection && this.isConnected()) {
      this.connection.on('notificationCounters', callback);
    }
  }

  offNotificationCounters(callback?: (counters: NotificationCounters) => void): void {
    if (callback) {
      this.callbacks.delete(callback);
      if (this.connection) {
        this.connection.off('notificationCounters', callback);
      }
    } else {
      // Удаляем все колбэки
      this.callbacks.forEach(cb => {
        if (this.connection) {
          this.connection.off('notificationCounters', cb);
        }
      });
      this.callbacks.clear();
    }
  }

  off(event: string): void {
    if (this.connection) {
      this.connection.off(event);
    }
  }

  isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }
}

export const notificationWsService = new NotificationWebSocketService();
