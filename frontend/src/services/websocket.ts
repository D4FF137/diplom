import { HubConnectionBuilder, HubConnectionState, HubConnection } from '@microsoft/signalr';
import type { Message } from '../types';

// Use direct ChatService URL for WebSocket (SignalR requires direct connection)
// CORS is configured in ChatService to allow this
const WS_BASE_URL = import.meta.env.VITE_WS_URL || 'http://localhost:5004';

class WebSocketService {
  private connection: HubConnection | null = null;

  private onReconnectedCallback: (() => void) | null = null;

  connect(): Promise<void> {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${WS_BASE_URL}/chatHub`, {
        
        // Явно указываем, что не используем credentials
        // Токен передается через accessTokenFactory в query string
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

    // Обработка переподключения
    this.connection.onreconnected((connectionId) => {
      console.log('WebSocket reconnected', connectionId);
      if (this.onReconnectedCallback) {
        this.onReconnectedCallback();
      }
    });

    this.connection.onclose((error) => {
      console.log('WebSocket connection closed', error);
    });

    this.connection.onreconnecting((error) => {
      console.log('WebSocket reconnecting', error);
    });

    return this.connection.start();
  }

  onReconnected(callback: () => void): void {
    this.onReconnectedCallback = callback;
  }

  disconnect(): Promise<void> {
    if (this.connection) {
      return this.connection.stop();
    }
    return Promise.resolve();
  }

  joinChat(chatId: string | number): Promise<void> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    return this.connection.invoke('JoinChat', String(chatId));
  }

  leaveChat(chatId: string | number): Promise<void> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    return this.connection.invoke('LeaveChat', String(chatId));
  }

  sendMessage(chatId: string | number, content: string): Promise<void> {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    return this.connection.invoke('SendMessage', String(chatId), content);
  }

  sendTyping(chatId: string | number, isTyping: boolean): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve();
    }
    return this.connection.invoke('SendTyping', String(chatId), isTyping);
  }

  sendPoll(chatId: string | number, question: string, options: string[], isAnonymous: boolean): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve();
    }
    return this.connection.invoke('SendPoll', String(chatId), question, options, isAnonymous);
  }

  vote(messageId: string | number, optionId: number): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve();
    }
    return this.connection.invoke('Vote', String(messageId), optionId);
  }

  getOnlineUsers(): Promise<number[]> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve([]);
    }
    return this.connection.invoke('GetOnlineUsers');
  }

  addMember(chatId: string | number, userId: number): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve();
    }
    return this.connection.invoke('AddMember', String(chatId), userId);
  }

  removeMember(chatId: string | number, userId: number): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve();
    }
    return this.connection.invoke('RemoveMember', String(chatId), userId);
  }

  addReaction(chatId: string | number, messageId: string | number, emoji: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve();
    }
    return this.connection.invoke('AddReaction', String(chatId), String(messageId), emoji);
  }

  removeReaction(chatId: string | number, messageId: string | number, emoji: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return Promise.resolve();
    }
    return this.connection.invoke('RemoveReaction', String(chatId), String(messageId), emoji);
  }

  onMessageReceived(callback: (message: Message) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('ReceiveMessage', callback);
  }

  onMessageUpdated(callback: (data: { id: string; chatId: string; content: string }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('ReceiveMessageUpdate', callback);
  }

  onMessageDeleted(callback: (data: { id: string; chatId: string }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('ReceiveMessageDelete', callback);
  }

  offMessageReceived(callback?: (message: Message) => void): void {
    if (this.connection) {
      if (callback) {
        this.connection.off('ReceiveMessage', callback);
      } else {
        this.connection.off('ReceiveMessage');
      }
    }
  }

  offMessageUpdated(): void {
    if (this.connection) {
      this.connection.off('ReceiveMessageUpdate');
    }
  }

  offMessageDeleted(): void {
    if (this.connection) {
      this.connection.off('ReceiveMessageDelete');
    }
  }

  onUserJoined(callback: (userId: number | string, chatId: number | string) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('UserJoined', callback);
  }

  onUserLeft(callback: (userId: number | string, chatId: number | string) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('UserLeft', callback);
  }

  onNewChat(callback: (chat: any) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('NewChat', callback);
  }

  onUserOnline(callback: (userId: number) => void): void {
    if (!this.connection) return;
    this.connection.on('UserOnline', callback);
  }

  onUserOffline(callback: (userId: number, lastSeen: string) => void): void {
    if (!this.connection) return;
    this.connection.on('UserOffline', callback);
  }

  onUserTyping(callback: (data: { chatId: string; userId: number; isTyping: boolean }) => void): void {
    if (!this.connection) return;
    this.connection.on('UserTyping', callback);
  }

  onPollUpdate(callback: (data: { messageId: string; chatId: string; poll: any }) => void): void {
    if (!this.connection) return;
    this.connection.on('ReceivePollUpdate', callback);
  }

  onMemberAdded(callback: (data: { chatId: string; user: any }) => void): void {
    if (!this.connection) return;
    this.connection.on('MemberAdded', callback);
  }

  onMemberRemoved(callback: (data: { chatId: string; userId: number }) => void): void {
    if (!this.connection) return;
    this.connection.on('MemberRemoved', callback);
  }

  onReactionUpdated(callback: (data: { messageId: string; chatId: string; reactions: any[] }) => void): void {
    if (!this.connection) return;
    this.connection.on('ReceiveReactionUpdate', callback);
  }

  onChatUpdated(callback: (data: { chatId: number | string; lastMessageAt: string; lastMessage?: string }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('ChatUpdated', callback);
  }

  off(methodName: string, callback?: (...args: any[]) => any): void {
    if (this.connection) {
      if (callback) {
        this.connection.off(methodName, callback);
      } else {
        this.connection.off(methodName);
      }
    }
  }

  onChatDeleted(callback: (data: { chatId: number | string }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('ChatDeleted', callback);
  }

  onChatRemoved(callback: (data: { chatId: number | string }) => void): void {
    if (!this.connection) {
      throw new Error('Connection not established');
    }
    this.connection.on('ChatRemoved', callback);
  }

  isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }
}

export const wsService = new WebSocketService();
