import { HubConnectionBuilder, HubConnectionState, HubConnection } from '@microsoft/signalr';
import type { Post, Comment } from '../types';

// Use direct FeedService URL for WebSocket (SignalR requires direct connection)
const FEED_WS_BASE_URL = import.meta.env.VITE_FEED_WS_URL || 'http://localhost:5003';

class FeedWebSocketService {
  private connection: HubConnection | null = null;


  connect(): Promise<void> {
    // Если соединение уже существует и подключено, не создаем новое
    if (this.connection && this.isConnected()) {
      return Promise.resolve();
    }

    // Если соединение существует, но не подключено, останавливаем его
    if (this.connection) {
      this.connection.stop().catch(console.error);
    }


    
    this.connection = new HubConnectionBuilder()
      .withUrl(`${FEED_WS_BASE_URL}/feedHub`, {

        withCredentials: true, // Важно для CORS с credentials
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
      console.log('WebSocket connection closed', error);
    });

    this.connection.onreconnecting((error) => {
      console.log('WebSocket reconnecting', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('WebSocket reconnected', connectionId);
    });

    return this.connection.start().then(() => {
      console.log('WebSocket connected successfully');
    }).catch((error) => {
      console.error('WebSocket connection failed:', error);
      throw error;
    });
  }

  disconnect(): Promise<void> {
    if (this.connection) {
      return this.connection.stop();
    }
    return Promise.resolve();
  }

  onNewPost(callback: (post: Post) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('NewPost', callback);
  }

  onPostLiked(callback: (data: { PostId: number; LikesCount: number }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('PostLiked', callback);
  }

  onPostUnliked(callback: (data: { PostId: number; LikesCount: number }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('PostUnliked', callback);
  }

  onNewComment(callback: (data: { PostId: number; Comment: Comment; CommentsCount: number }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('NewComment', callback);
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

export const feedWsService = new FeedWebSocketService();
