import { useEffect, useState, useRef, useMemo, useCallback, memo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useQueryClient } from '@tanstack/react-query';
import { wsService } from '../../services/websocket';
import { useMessages } from '../../hooks/queries/useMessages';
import { useNotifications } from '../../hooks/useNotificationsOptimized';
import { useDebounce } from '../../hooks/useDebounce';
import { useAuthStore } from '../../stores/authStore';
import { chatAPI, usersAPI } from '../../services/api';
import { getImageUrl } from '../../utils/imageUrl';
import type { User, Message, Chat } from '../../types';

interface ChatWindowProps {
  chat: Chat;
  onBack?: () => void;
}

const getUserColor = (userId: number | string) => {
  const colors = [
    'text-red-500', 'text-blue-500', 'text-green-500', 'text-purple-500',
    'text-pink-500', 'text-indigo-500', 'text-orange-500', 'text-teal-500'
  ];
  const idValue = typeof userId === 'number' ? userId : userId.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
  return colors[idValue % colors.length];
};

const ChatWindowComponent = ({ chat, onBack }: ChatWindowProps) => {
  const user = useAuthStore((state) => state.user);
  const { markChatAsRead } = useNotifications();
  const queryClient = useQueryClient();
  const { messages: initialMessages = [], isLoading } = useMessages(chat.id);
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [sending, setSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const chatIdRef = useRef<number | string>(chat.id);
  const messageHandlerRef = useRef<((message: Message) => void) | null>(null);
  const reconnectHandlerRef = useRef<(() => void) | null>(null);
  const updateHandlerRef = useRef<((data: { id: string; chatId: string; content: string }) => void) | null>(null);
  const deleteHandlerRef = useRef<((data: { id: string; chatId: string }) => void) | null>(null);
  const memberAddedHandlerRef = useRef<((data: { chatId: string; user: any }) => void) | null>(null);
  const memberRemovedHandlerRef = useRef<((data: { chatId: string; userId: number }) => void) | null>(null);
  const [currentMembers, setCurrentMembers] = useState<User[]>(chat.members || []);
  const [showAddMember, setShowAddMember] = useState(false);
  const [showMembers, setShowMembers] = useState(false);

  const [editingMessage, setEditingMessage] = useState<Message | null>(null);

  const [showSearch, setShowSearch] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<Message[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  const [selectedImageUrl, setSelectedImageUrl] = useState<string | null>(null);
  const debouncedSearchQuery = useDebounce(searchQuery, 500);

  // Presence and Typing state
  const [typingUsers, setTypingUsers] = useState<Record<number, boolean>>({});
  const [onlineUsers, setOnlineUsers] = useState<Set<number>>(new Set());
  const [lastSeenMap, setLastSeenMap] = useState<Record<number, string>>({});
  const [showPollDialog, setShowPollDialog] = useState(false);
  const [selectedPollForVoters, setSelectedPollForVoters] = useState<any | null>(null);
  const typingHandlerRef = useRef<((data: any) => void) | null>(null);
  const onlineHandlerRef = useRef<((userId: number) => void) | null>(null);
  const offlineHandlerRef = useRef<((userId: number, lastSeen: string) => void) | null>(null);
  const pollUpdateHandlerRef = useRef<((data: any) => void) | null>(null);
  const reactionUpdatedHandlerRef = useRef<((data: any) => void) | null>(null);
  const isTypingRef = useRef(false);

  // Мемоизированные значения
  const sortedMessages = useMemo(() => {
    return [...messages].sort((a, b) =>
      new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
    );
  }, [messages]);

  const displayName = useMemo(() => {
    if (chat.type === 'private' && chat.members && user) {
      const partner = chat.members.find(m => String(m.id) !== String(user.id));
      if (partner) {
        return `${partner.firstName || ''} ${partner.lastName || ''}`.trim() || partner.email;
      }
    }
    return chat.name;
  }, [chat, user]);

  const partner = useMemo(() => {
    if (chat.type === 'private' && chat.members && user) {
      return chat.members.find(m => String(m.id) !== String(user.id));
    }
    return null;
  }, [chat, user]);

  const formatLastSeen = useCallback((dateString?: string) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const diff = now.getTime() - date.getTime();

    if (diff < 60000) return 'только что';
    if (diff < 3600000) return `${Math.floor(diff / 60000)} мин. назад`;
    if (diff < 86400000) return `${Math.floor(diff / 3600000)} ч. назад`;
    return date.toLocaleDateString();
  }, []);

  const partnerStatus = useMemo(() => {
    if (!partner) return null;
    if (onlineUsers.has(partner.id)) return 'в сети';

    const lastSeen = lastSeenMap[partner.id] || partner.lastSeen;
    if (lastSeen) {
      return `был(а) в сети ${formatLastSeen(lastSeen)}`;
    }
    return 'не в сети';
  }, [partner, onlineUsers, lastSeenMap, formatLastSeen]);

  const isAnyPartnerTyping = useMemo(() => {
    return Object.entries(typingUsers).some(([uid, typing]) => typing && String(uid) !== String(user?.id));
  }, [typingUsers, user?.id]);

  // Auto-search effect
  useEffect(() => {
    const performSearch = async () => {
      if (!debouncedSearchQuery.trim()) {
        setSearchResults([]);
        return;
      }

      setIsSearching(true);
      try {
        const results = await chatAPI.searchMessages(chat.id, debouncedSearchQuery);
        setSearchResults(results);
      } catch (err) {
        console.error('Error searching messages:', err);
      } finally {
        setIsSearching(false);
      }
    };

    if (showSearch) {
      performSearch();
    }
  }, [debouncedSearchQuery, chat.id, showSearch]);

  const clearSearch = () => {
    setShowSearch(false);
    setSearchQuery('');
    setSearchResults([]);
  };

  // Helper to highlight text
  const HighlightedText = ({ text, highlight }: { text: string; highlight: string }) => {
    if (!highlight.trim()) {
      return <span>{text}</span>;
    }
    const parts = text.split(new RegExp(`(${highlight})`, 'gi'));
    return (
      <span>
        {parts.map((part, i) =>
          part.toLowerCase() === highlight.toLowerCase() ? (
            <span key={i} className="bg-yellow-200 dark:bg-yellow-800 dark:text-white rounded px-0.5">{part}</span>
          ) : (
            part
          )
        )}
      </span>
    );
  };

  // Сбрасываем состояние при смене чата
  useEffect(() => {
    chatIdRef.current = chat.id;
    // Очищаем сообщения сразу при смене чата
    setMessages([]);
    // Сбрасываем поиск
    setSearchQuery('');
    setShowSearch(false);
    setSearchResults([]);
    setIsSearching(false);
  }, [chat.id]);

  // Синхронизируем messages из React Query с локальным состоянием
  useEffect(() => {
    const sorted = [...initialMessages].sort((a, b) =>
      new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
    );
    setMessages(sorted);
  }, [initialMessages]);

  // Помечаем чат как прочитанный после загрузки сообщений
  useEffect(() => {
    if (!isLoading) {
      // Небольшая задержка, чтобы убедиться, что все инициализировано
      const timer = setTimeout(() => {
        console.log('[ChatWindow] Marking chat as read:', chat.id);
        markChatAsRead(chat.id).catch((error) => {
          console.error('[ChatWindow] Error marking chat as read:', error);
        });
      }, 500);
      return () => clearTimeout(timer);
    }
  }, [chat.id, isLoading, markChatAsRead]);

  useEffect(() => {
    // Небольшая задержка для подключения WebSocket
    const timer = setTimeout(() => {
      connectWebSocket();
    }, 100);

    return () => {
      clearTimeout(timer);
      try {
        if (wsService.isConnected()) {
          if (messageHandlerRef.current) wsService.off('ReceiveMessage', messageHandlerRef.current);
          if (updateHandlerRef.current) wsService.off('ReceiveMessageUpdate', updateHandlerRef.current);
          if (deleteHandlerRef.current) wsService.off('ReceiveMessageDelete', deleteHandlerRef.current);
          if (onlineHandlerRef.current) wsService.off('UserOnline', onlineHandlerRef.current);
          if (offlineHandlerRef.current) wsService.off('UserOffline', offlineHandlerRef.current);
          if (typingHandlerRef.current) wsService.off('UserTyping', typingHandlerRef.current);
          if (pollUpdateHandlerRef.current) wsService.off('ReceivePollUpdate', pollUpdateHandlerRef.current);
          if (reactionUpdatedHandlerRef.current) wsService.off('ReceiveReactionUpdate', reactionUpdatedHandlerRef.current);
          wsService.leaveChat(chat.id).catch(console.error);
        }
      } catch (err) {
        console.error('WebSocket cleanup error:', err);
      }
      // Reset refs
      messageHandlerRef.current = null;
      updateHandlerRef.current = null;
      deleteHandlerRef.current = null;
      typingHandlerRef.current = null;
      onlineHandlerRef.current = null;
      offlineHandlerRef.current = null;
      pollUpdateHandlerRef.current = null;
    };
  }, [chat.id]);

  // Typing effect: send typing status to backend
  useEffect(() => {
    if (!newMessage.trim() || !wsService.isConnected()) {
      if (isTypingRef.current) {
        wsService.sendTyping(chat.id, false);
        isTypingRef.current = false;
      }
      return;
    }

    if (!isTypingRef.current) {
      wsService.sendTyping(chat.id, true);
      isTypingRef.current = true;
    }

    const timer = setTimeout(() => {
      wsService.sendTyping(chat.id, false);
      isTypingRef.current = false;
    }, 2000);

    return () => clearTimeout(timer);
  }, [newMessage, chat.id]);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const scrollToBottom = useCallback(() => {
    // Используем requestAnimationFrame для синхронизации со следующим кадром отрисовки
    requestAnimationFrame(() => {
      messagesEndRef.current?.scrollIntoView({ behavior: 'auto', block: 'end' });
    });
  }, []);

  const connectWebSocket = async () => {
    try {
      // Удаляем старый обработчик, если он есть
      if (messageHandlerRef.current) {
        wsService.offMessageReceived(messageHandlerRef.current);
        messageHandlerRef.current = null;
      }

      // Если соединение уже установлено, просто присоединяемся к новому чату
      if (!wsService.isConnected()) {
        await wsService.connect();
      }

      // Настраиваем обработчик переподключения для повторного присоединения к чату
      // Используем ref, чтобы всегда использовать актуальный chatId
      if (!reconnectHandlerRef.current) {
        const reconnectHandler = async () => {
          try {
            if (chatIdRef.current) {
              await wsService.joinChat(chatIdRef.current);
            }
          } catch (err) {
            console.error('Error rejoining chat after reconnect:', err);
          }
        };
        reconnectHandlerRef.current = reconnectHandler;
        wsService.onReconnected(reconnectHandler);
      }

      // Создаем новый обработчик сообщений для текущего чата
      const messageHandler = (message: Message) => {
        // Проверяем, что сообщение для текущего активного чата
        if (message.chatId === chatIdRef.current) {
          // Обновляем React Query кэш
          queryClient.setQueryData<Message[]>(['messages', chatIdRef.current], (old) => {
            if (!old) return [message];

            // Проверяем, нет ли уже такого сообщения (по ID)
            const existsById = old.some((m) => m.id === message.id);
            if (existsById) return old;

            // Проверяем, есть ли временное сообщение с таким же содержимым от того же пользователя
            const tempMessageIndex = old.findIndex((m) =>
              typeof m.id === 'number' && m.id > 1000000000 && // Временное сообщение (ID больше миллиарда)
              m.content === message.content &&
              m.userId === message.userId &&
              Math.abs(new Date(m.createdAt).getTime() - new Date(message.createdAt).getTime()) < 5000
            );

            let updated = [...old];

            // Если нашли временное сообщение, заменяем его на реальное
            if (tempMessageIndex !== -1) {
              updated[tempMessageIndex] = message;
              return updated;
            } else {
              // Иначе просто добавляем новое сообщение
              updated.push(message);
              // Сортируем по времени только для новых сообщений
              return updated.sort((a, b) =>
                new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
              );
            }
          });

          // Обновляем локальное состояние
          setMessages((prev) => {
            const existsById = prev.some((m) => m.id === message.id);
            if (existsById) return prev;

            const tempMessageIndex = prev.findIndex((m) =>
              String(m.id) === String(message.id) ||
              (typeof m.id === 'number' && m.id > 1000000000 &&
                m.content === message.content &&
                m.userId === message.userId &&
                Math.abs(new Date(m.createdAt).getTime() - new Date(message.createdAt).getTime()) < 5000)
            );

            let updated = [...prev];
            if (tempMessageIndex !== -1) {
              updated[tempMessageIndex] = message;
              return updated;
            } else {
              updated.push(message);
              return updated.sort((a, b) =>
                new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
              );
            }
          });

          // ВАЖНО: Если мы находимся в этом чате, помечаем его как прочитанный сразу же
          // Используем API напрямую, чтобы избежать проблем с замыканиями хуков
          import('../../services/api').then(({ notificationAPI }) => {
            notificationAPI.markChatAsRead(chatIdRef.current).catch(console.error);
          });
        }
      };

      // Регистрируем обработчик
      wsService.onMessageReceived(messageHandler);
      messageHandlerRef.current = messageHandler;

      // Обработчик обновления сообщений
      const updateHandler = (data: { id: string; chatId: string; content: string; isEdited?: boolean }) => {
        if (String(data.chatId) === String(chatIdRef.current)) {
          const updateFn = (old: Message[] | undefined) =>
            old?.map(m => String(m.id) === String(data.id) ? { ...m, content: data.content, isEdited: data.isEdited } : m);

          queryClient.setQueryData<Message[]>(['messages', chatIdRef.current], updateFn);
          setMessages(prev => prev.map(m => String(m.id) === String(data.id) ? { ...m, content: data.content, isEdited: data.isEdited } : m));
        }
      };
      updateHandlerRef.current = updateHandler;
      wsService.onMessageUpdated(updateHandler);

      // Обработчик удаления сообщений
      const deleteHandler = (data: { id: string; chatId: string }) => {
        if (String(data.chatId) === String(chatIdRef.current)) {
          const deleteFn = (old: Message[] | undefined) =>
            old?.filter(m => String(m.id) !== String(data.id));

          queryClient.setQueryData<Message[]>(['messages', chatIdRef.current], deleteFn);
          setMessages(prev => prev.filter(m => String(m.id) !== String(data.id)));
        }
      };
      deleteHandlerRef.current = deleteHandler;
      wsService.onMessageDeleted(deleteHandler);

      // Presence handlers
      const onlineHandler = (userId: number) => {
        setOnlineUsers(prev => {
          const next = new Set(prev);
          next.add(userId);
          return next;
        });
      };
      onlineHandlerRef.current = onlineHandler;
      wsService.onUserOnline(onlineHandler);

      const offlineHandler = (userId: number, lastSeen: string) => {
        setOnlineUsers(prev => {
          const next = new Set(prev);
          next.delete(userId);
          return next;
        });
        setLastSeenMap(prev => ({ ...prev, [userId]: lastSeen }));
      };
      offlineHandlerRef.current = offlineHandler;
      wsService.onUserOffline(offlineHandler);

      const typingHandler = (data: { chatId: string; userId: number; isTyping: boolean }) => {
        if (String(data.chatId) === String(chatIdRef.current)) {
          setTypingUsers(prev => ({ ...prev, [data.userId]: data.isTyping }));
        }
      };
      typingHandlerRef.current = typingHandler;
      wsService.onUserTyping(typingHandler);

      // Poll update handler
      const pollUpdateHandler = (data: { messageId: string; chatId: string; poll: any }) => {
        if (String(data.chatId) === String(chatIdRef.current)) {
          const updateFn = (old: Message[] | undefined) =>
            old?.map(m => String(m.id) === String(data.messageId) ? { ...m, poll: data.poll } : m);

          queryClient.setQueryData<Message[]>(['messages', chatIdRef.current], updateFn);
          setMessages(prev => prev.map(m => String(m.id) === String(data.messageId) ? { ...m, poll: data.poll } : m));
        }
      };
      pollUpdateHandlerRef.current = pollUpdateHandler;
      wsService.onPollUpdate(pollUpdateHandler);

      // Reaction update handler
      if (!reactionUpdatedHandlerRef.current) {
        const handler = (data: { messageId: string; chatId: string; reactions: any[] }) => {
          if (String(data.chatId) === String(chatIdRef.current)) {
            setMessages(prev => prev.map(m =>
              String(m.id) === String(data.messageId)
                ? { ...m, reactions: data.reactions }
                : m
            ));

            // Update React Query cache
            queryClient.setQueryData<Message[]>(['messages', chatIdRef.current], (old) => {
              return old?.map(m =>
                String(m.id) === String(data.messageId)
                  ? { ...m, reactions: data.reactions }
                  : m
              );
            });
          }
        };
        reactionUpdatedHandlerRef.current = handler;
        wsService.onReactionUpdated(handler);
      }

      // Member added handler
      const memberAddedHandler = (data: { chatId: string; user: any }) => {
        if (String(data.chatId) === String(chatIdRef.current)) {
          setCurrentMembers(prev => {
            if (!data.user || !data.user.id) return prev;
            if (prev.some(m => m.id === data.user.id)) return prev;
            return [...prev, data.user];
          });
        }
      };
      memberAddedHandlerRef.current = memberAddedHandler;
      wsService.onMemberAdded(memberAddedHandler);

      // Member removed handler
      const memberRemovedHandler = (data: { chatId: string; userId: number }) => {
        if (String(data.chatId) === String(chatIdRef.current)) {
          setCurrentMembers(prev => prev.filter(m => m.id !== data.userId));
        }
      };
      memberRemovedHandlerRef.current = memberRemovedHandler;
      wsService.onMemberRemoved(memberRemovedHandler);

      // Присоединяемся к чату
      await wsService.joinChat(chat.id);

      // Получаем список всех онлайн-пользователей компании
      try {
        const onlineIds = await wsService.getOnlineUsers();
        setOnlineUsers(new Set(onlineIds));
      } catch (err) {
        console.error('Error fetching online users:', err);
      }
    } catch (err) {
      console.error('WebSocket connection error:', err);
    }
  };

  // File upload state
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const dragCounter = useRef(0);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      setSelectedFile(file);
      const url = URL.createObjectURL(file);
      setPreviewUrl(url);
    }
  };

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    dragCounter.current++;
    if (dragCounter.current === 1) {
      setIsDragging(true);
    }
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    dragCounter.current--;
    if (dragCounter.current === 0) {
      setIsDragging(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    dragCounter.current = 0;
    setIsDragging(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      const file = e.dataTransfer.files[0];
      setSelectedFile(file);
      const url = URL.createObjectURL(file);
      setPreviewUrl(url);
    }
  }, []);

  const clearFile = () => {
    setSelectedFile(null);
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
      setPreviewUrl(null);
    }
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  // Cleanup preview URL on unmount
  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  const sendMessage = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    if ((!newMessage.trim() && !selectedFile) || sending) return;

    const messageContent = newMessage.trim();
    const fileToSend = selectedFile;

    // Clear input immediately
    setNewMessage('');
    clearFile();
    setSending(true);

    // Оптимистичное обновление - добавляем сообщение сразу
    const now = new Date();
    const tempId = Date.now();

    const tempMessage: Message = {
      id: tempId,
      chatId: chat.id,
      userId: user?.id || 0,
      content: messageContent,
      attachmentUrl: fileToSend ? URL.createObjectURL(fileToSend) : undefined, // Temporary preview
      createdAt: now.toISOString(),
    };

    // Обновляем React Query кэш
    queryClient.setQueryData<Message[]>(['messages', chat.id], (old) => {
      const updated = old ? [...old, tempMessage] : [tempMessage];
      setTimeout(() => scrollToBottom(), 0);
      return updated;
    });

    // Обновляем локальное состояние
    setMessages((prev) => {
      const updated = [...prev, tempMessage];
      setTimeout(() => scrollToBottom(), 0);
      return updated;
    });

    try {
      // NOTE: We change logic here. If we have a file, we MUST use HTTP API because WebSocket currently doesn't support binary uploads easily in this implementation.
      // Even for text, we can use HTTP API to be consistent, or mix.
      // Since user wants Telegram-style, reliable file upload is key.
      // Let's use HTTP for everything if there is a file, or if we want to be safe.
      // Existing logic used WS for text. Let's keep WS for text-only for speed, but use API for files.

      let createdMessage: Message;

      if (!fileToSend && wsService.isConnected()) {
        await wsService.sendMessage(chat.id, messageContent);
        // WS will send back the message via event
        // Note: The optimistic message will be replaced when the event arrives
      } else {
        const { chatAPI } = await import('../../services/api');
        createdMessage = await chatAPI.sendMessage({
          chatId: chat.id,
          content: messageContent,
          file: fileToSend || undefined
        });

        // Заменяем временное сообщение на реальное в React Query
        queryClient.setQueryData<Message[]>(['messages', chat.id], (old) => {
          if (!old) return [createdMessage];
          const filtered = old.filter((m) => m.id !== tempId);
          return [...filtered, createdMessage].sort((a, b) =>
            new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
          );
        });

        // Заменяем в локальном состоянии
        setMessages((prev) => {
          const filtered = prev.filter((m) => m.id !== tempId);
          return [...filtered, createdMessage].sort((a, b) =>
            new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
          );
        });
      }

      // Помечаем чат как прочитанный после отправки сообщения
      markChatAsRead(chat.id).catch((error) => {
        console.error('[ChatWindow] Error marking chat as read after sending message:', error);
      });
    } catch (err) {
      console.error('Error sending message:', err);
      // Удаляем временное сообщение при ошибке
      queryClient.setQueryData<Message[]>(['messages', chat.id], (old) =>
        old ? old.filter((m) => m.id !== tempId) : []
      );
      setMessages((prev) => prev.filter((m) => m.id !== tempId));
      setNewMessage(messageContent);
      // Restore file if failed (optional, but good UX)
    } finally {
      setSending(false);
    }
  }, [newMessage, selectedFile, sending, chat.id, user?.id, queryClient, scrollToBottom, markChatAsRead, previewUrl]);

  const handleUpdateMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingMessage || !newMessage.trim() || sending) return;

    const content = newMessage.trim();
    setSending(true);

    try {
      await chatAPI.updateMessage(editingMessage.id, content);
      setEditingMessage(null);
      setNewMessage('');
    } catch (err) {
      console.error('Error updating message:', err);
    } finally {
      setSending(false);
    }
  };

  const handleDeleteMessage = async (messageId: string | number) => {
    if (!window.confirm('Удалить это сообщение?')) return;

    try {
      await chatAPI.deleteMessage(messageId);
    } catch (err) {
      console.error('Error deleting message:', err);
    }
  };

  const startEditMessage = (message: Message) => {
    setEditingMessage(message);
    setNewMessage(message.content);
  };

  const cancelEdit = () => {
    setEditingMessage(null);
    setNewMessage('');
  };

  // ... (existing helper functions: sortedMessages, handleJumpToMessage, etc.)

  const handleJumpToMessage = (messageId: number | string) => {
    setShowSearch(false);
    setTimeout(() => {
      const element = document.getElementById(`msg-${messageId}`);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        element.classList.add('bg-yellow-100', 'dark:bg-yellow-900', 'transition-colors', 'duration-1000');
        setTimeout(() => {
          element.classList.remove('bg-yellow-100', 'dark:bg-yellow-900', 'transition-colors', 'duration-1000');
        }, 2000);
      } else {
        console.warn('Message not found in view');
      }
    }, 100);
  };

  const handleToggleReaction = (messageId: string | number, emoji: string) => {
    const msg = messages.find(m => String(m.id) === String(messageId));
    if (!msg || !user) return;

    const reaction = msg.reactions?.find(r => r.emoji === emoji);
    const hasReacted = reaction?.userIds.includes(user.id);

    if (hasReacted) {
      wsService.removeReaction(chat.id, messageId, emoji).catch(console.error);
    } else {
      wsService.addReaction(chat.id, messageId, emoji).catch(console.error);
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
      </div>
    );
  }

  return (
    <div
      className="flex flex-col h-full bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-700 relative"
      onDragEnter={handleDragEnter}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      {/* Drag Overlay */}
      {isDragging && (
        <div className="absolute inset-0 bg-apple-blue/10 dark:bg-apple-blue/20 backdrop-blur-sm z-50 flex items-center justify-center rounded-2xl border-2 border-dashed border-apple-blue pointer-events-none">
          <div className="text-xl font-bold text-apple-blue bg-white dark:bg-gray-800 px-6 py-4 rounded-xl shadow-lg">
            Перетащите файлы сюда
          </div>
        </div>
      )}

      {/* Header */}
      <div className="px-4 py-2 border-b border-gray-100 dark:border-gray-700 flex items-center h-[73px] shrink-0 gap-3">
        {onBack && (
          <button
            onClick={onBack}
            className="p-2 -ml-2 text-gray-400 hover:text-apple-blue md:hidden transition-colors"
            title="Назад"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
          </button>
        )}

        {showSearch ? (
          <div className="flex-1 flex gap-2 items-center">
            <input
              autoFocus
              type="text"
              className="px-3 py-1.5 rounded-lg bg-gray-100 dark:bg-gray-700 border-none focus:ring-2 focus:ring-apple-blue flex-1 dark:text-white transition-all"
              placeholder="Поиск в чате..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            {isSearching && (
              <span className="text-gray-500 animate-spin">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
              </span>
            )}
            <button
              type="button"
              onClick={clearSearch}
              className="p-2 text-gray-500 hover:text-red-500 dark:text-gray-400"
              title="Закрыть поиск"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        ) : (
          <>
            <div className="flex-1 min-w-0">
              <h3 className="text-lg font-bold text-gray-900 dark:text-white truncate">
                {displayName}
              </h3>
              {chat.type === 'private' && (
                <p className={`text-xs ${isAnyPartnerTyping ? 'text-apple-blue font-medium animate-pulse' : 'text-gray-500'}`}>
                  {isAnyPartnerTyping ? 'печатает...' : partnerStatus}
                </p>
              )}
            </div>

            <div className="flex items-center gap-1">
              <button
                onClick={() => setShowSearch(true)}
                className="p-2 text-gray-400 hover:text-apple-blue transition-colors hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full"
                title="Поиск сообщений"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </button>

              {chat.type === 'group' && Number(chat.creatorId) === Number(user?.id) && (
                <button
                  onClick={() => setShowAddMember(true)}
                  className="p-2 text-gray-400 hover:text-green-500 transition-colors hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full"
                  title="Добавить участника"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </button>
              )}

              {chat.type === 'group' && (
                <button
                  onClick={() => setShowMembers(true)}
                  className="p-2 text-gray-400 hover:text-apple-blue transition-colors hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full"
                  title="Участники"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                  </svg>
                </button>
              )}

              {onBack && (
                <button
                  onClick={onBack}
                  className="hidden lg:flex items-center gap-2 px-3 py-1.5 text-sm text-gray-500 hover:text-apple-blue hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
                >
                  Закрыть
                </button>
              )}
            </div>
          </>
        )}
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4 relative custom-scrollbar">
        {showSearch && searchQuery.trim() ? (
          <div className="space-y-4">
            {isSearching && searchResults.length === 0 ? (
              <div className="text-center text-gray-500 mt-10">Поиск...</div>
            ) : searchResults.length > 0 ? (
              searchResults.map((msg) => (
                <div
                  key={`search-result-${msg.id}`}
                  onClick={() => handleJumpToMessage(msg.id)}
                  className="cursor-pointer bg-gray-50 dark:bg-gray-700/50 p-3 rounded-xl border border-gray-100 dark:border-gray-700 hover:bg-white dark:hover:bg-gray-600 hover:shadow-md transition-all active:scale-[0.99]"
                >
                  <div className="flex justify-between items-start mb-1">
                    <span className="text-xs font-medium text-apple-blue">
                      {msg.userId === user?.id ? 'Вы' : 'Собеседник'}
                    </span>
                    <span className="text-xs text-gray-400">
                      {new Date(msg.createdAt).toLocaleString()}
                    </span>
                  </div>
                  <div className="text-sm text-gray-800 dark:text-gray-200">
                    <HighlightedText text={msg.content} highlight={searchQuery} />
                  </div>
                </div>
              ))
            ) : (
              <div className="text-center text-gray-500 mt-10">Ничего не найдено</div>
            )}
          </div>
        ) : (
          <>
            <AnimatePresence initial={false}>
              {sortedMessages.map((message) => {
                const isOwnMessage = message.userId === user?.id;

                let senderName = undefined;
                let senderAvatarUrl = undefined;

                if (!isOwnMessage && (chat.type === 'group' || chat.type === 'channel')) {
                  const member = currentMembers.find(m => String(m.id) === String(message.userId));
                  senderName = member?.firstName ? `${member.firstName} ${member.lastName || ''}`.trim() : member?.email;

                  if (member?.avatarUrl) {
                    senderAvatarUrl = getImageUrl(member.avatarUrl) || undefined;
                  }
                }

                return (
                  <MessageItem
                    key={message.id}
                    message={message}
                    isOwnMessage={isOwnMessage}
                    currentUserId={user?.id || 0}
                    senderName={senderName}
                    senderAvatarUrl={senderAvatarUrl}
                    onImageClick={setSelectedImageUrl}
                    onEdit={startEditMessage}
                    onDelete={handleDeleteMessage}
                    onShowVoters={setSelectedPollForVoters}
                    onToggleReaction={handleToggleReaction}
                    members={currentMembers}
                  />
                );
              })}
            </AnimatePresence>
            <div ref={messagesEndRef} />
          </>
        )}
      </div>

      {/* Lightbox Modal */}
      <AnimatePresence>
        {selectedImageUrl && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={() => setSelectedImageUrl(null)}
            className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/90 backdrop-blur-sm cursor-zoom-out"
          >
            <motion.div
              initial={{ scale: 0.9, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.9, opacity: 0 }}
              onClick={(e) => e.stopPropagation()}
              className="relative max-w-full max-h-full"
            >
              <img
                src={selectedImageUrl}
                alt="Full size"
                className="max-w-[95vw] max-h-[95vh] rounded-xl shadow-2xl object-contain border border-white/10"
              />
              <button
                onClick={() => setSelectedImageUrl(null)}
                className="absolute -top-4 -right-4 bg-white/10 hover:bg-white/20 text-white rounded-full p-2 transition-colors border border-white/20"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

      {!showSearch && (
        <div className="p-4 border-t border-gray-100 dark:border-gray-700 shrink-0 bg-white dark:bg-gray-800">
          {/* Image Preview */}
          {previewUrl && (
            <div className="mb-2 relative inline-block group">
              <img src={previewUrl} alt="Preview" className="h-20 w-auto rounded-lg border border-gray-200 dark:border-gray-600 object-cover" />
              <button
                onClick={clearFile}
                className="absolute -top-2 -right-2 bg-gray-800 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs shadow-md hover:bg-black transition-colors"
              >
                <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          )}

          <form onSubmit={editingMessage ? handleUpdateMessage : sendMessage} className="flex gap-2 items-end">
            <input
              type="file"
              ref={fileInputRef}
              className="hidden"
              accept="image/*, .pdf, .doc, .docx, .txt" // Add proper accepts
              onChange={handleFileSelect}
            />
            {!editingMessage && (
              <div className="flex gap-1">
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="p-2.5 text-gray-500 hover:text-apple-blue hover:bg-blue-50 dark:hover:bg-gray-700 rounded-xl transition-colors mb-[1px]"
                  title="Прикрепить файл"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" />
                  </svg>
                </button>
                <button
                  type="button"
                  onClick={() => setShowPollDialog(true)}
                  className="p-2.5 text-gray-500 hover:text-orange-500 hover:bg-orange-50 dark:hover:bg-gray-700 rounded-xl transition-colors mb-[1px]"
                  title="Создать опрос"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                  </svg>
                </button>
              </div>
            )}

            <div className="flex-1 flex flex-col gap-1">
              {editingMessage && (
                <div className="flex justify-between items-center px-3 py-1 bg-blue-50 dark:bg-blue-900/30 rounded-t-lg border-x border-t border-blue-100 dark:border-blue-800">
                  <span className="text-xs text-apple-blue font-medium flex items-center gap-1">
                    <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                    </svg>
                    Редактирование сообщения
                  </span>
                  <button type="button" onClick={cancelEdit} className="text-gray-400 hover:text-red-500">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              )}
              <input
                type="text"
                className={`input-field w-full dark:bg-gray-700 dark:text-white dark:border-gray-600 dark:placeholder-gray-400 min-h-[44px] ${editingMessage ? 'rounded-t-none' : ''}`}
                value={newMessage}
                onChange={(e) => setNewMessage(e.target.value)}
                placeholder={editingMessage ? "Редактировать..." : "Введите сообщение..."}
                autoComplete="off"
              />
            </div>

            <button
              type="submit"
              className="btn-primary px-6 h-[44px] flex items-center justify-center"
              disabled={sending || (!newMessage.trim() && !selectedFile)}
            >
              {sending ? (
                <svg className="animate-spin h-5 w-5 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              ) : editingMessage ? 'Сохранить' : 'Отправить'}
            </button>
          </form>
        </div>
      )}

      {showPollDialog && (
        <PollDialog
          onClose={() => setShowPollDialog(false)}
          onSubmit={async (question, options, isAnonymous) => {
            await wsService.sendPoll(chat.id, question, options, isAnonymous);
            setShowPollDialog(false);
          }}
        />
      )}

      <AnimatePresence>
        {showAddMember && (
          <AddMemberDialog
            onClose={() => setShowAddMember(false)}
            onSubmit={async (targetUserId) => {
              await wsService.addMember(chat.id, targetUserId);
              setShowAddMember(false);
            }}
          />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {showMembers && (
          <MembersModal
            chat={chat}
            members={currentMembers}
            onClose={() => setShowMembers(false)}
            onRemove={async (userId) => {
              if (confirm('Вы уверены, что хотите удалить этого участника?')) {
                await wsService.removeMember(chat.id, userId);
              }
            }}
            currentUserId={user?.id || 0}
          />
        )}
      </AnimatePresence>

      <AnimatePresence>
        {selectedPollForVoters && currentMembers && (
          <PollVotersModal
            poll={selectedPollForVoters}
            users={currentMembers}
            onClose={() => setSelectedPollForVoters(null)}
          />
        )}
      </AnimatePresence>
    </div>
  );
};

// --- Poll Components ---

const PollDialog = ({ onClose, onSubmit }: { onClose: () => void; onSubmit: (q: string, o: string[], a: boolean) => void }) => {
  const [question, setQuestion] = useState('');
  const [options, setOptions] = useState(['', '']);
  const [isAnonymous, setIsAnonymous] = useState(true);

  const addOption = () => {
    if (options.length < 10) {
      setOptions([...options, '']);
    }
  };

  const removeOption = (index: number) => {
    if (options.length > 2) {
      setOptions(options.filter((_, i) => i !== index));
    }
  };

  const updateOption = (index: number, text: string) => {
    const newOptions = [...options];
    newOptions[index] = text;
    setOptions(newOptions);
  };

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="absolute inset-0 bg-gray-900/60 backdrop-blur-md"
      />
      <motion.div
        initial={{ opacity: 0, scale: 0.9, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.9, y: 20 }}
        transition={{ type: "spring", damping: 25, stiffness: 300 }}
        className="relative bg-white dark:bg-gray-800 rounded-[32px] w-full max-w-lg shadow-[0_32px_64px_-12px_rgba(0,0,0,0.3)] overflow-hidden border border-white/20 dark:border-gray-700"
      >
        <div className="p-8 pb-6 flex justify-between items-center bg-gradient-to-r from-orange-500/10 to-transparent">
          <div>
            <h3 className="text-2xl font-black tracking-tight text-gray-900 dark:text-white">Создать опрос</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Организуйте голосование в чате за секунды</p>
          </div>
          <button
            onClick={onClose}
            className="w-10 h-10 flex items-center justify-center rounded-full bg-gray-100 dark:bg-gray-700 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="p-8 pt-2 space-y-6 max-h-[65vh] overflow-y-auto custom-scrollbar">
          <div className="space-y-2">
            <label className="text-xs font-black text-orange-500 uppercase tracking-widest px-1">Ваш вопрос</label>
            <textarea
              className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-2xl p-4 text-[16px] font-medium focus:ring-2 focus:ring-orange-500 transition-all resize-none dark:text-white dark:placeholder-gray-600"
              placeholder="Введите суть вопроса..."
              rows={2}
              value={question}
              onChange={e => setQuestion(e.target.value)}
            />
          </div>

          <div className="space-y-4">
            <div className="flex justify-between items-center px-1">
              <label className="text-xs font-black text-orange-500 uppercase tracking-widest">Варианты ответов</label>
              <span className="text-[10px] font-bold text-gray-400 bg-gray-100 dark:bg-gray-700 px-2 py-0.5 rounded-full">
                {options.length}/10
              </span>
            </div>

            <div className="space-y-3">
              <AnimatePresence mode="popLayout">
                {options.map((opt, i) => (
                  <motion.div
                    key={i}
                    layout
                    initial={{ opacity: 0, x: -10 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, scale: 0.95 }}
                    className="flex gap-3 group"
                  >
                    <div className="flex-1 relative">
                      <div className="absolute left-4 top-1/2 -translate-y-1/2 w-6 h-6 rounded-lg bg-orange-100 dark:bg-orange-900/40 flex items-center justify-center text-[11px] font-black text-orange-600 dark:text-orange-400">
                        {i + 1}
                      </div>
                      <input
                        type="text"
                        className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-2xl py-3.5 pl-12 pr-4 text-[15px] font-medium focus:ring-2 focus:ring-orange-500 transition-all dark:text-white dark:placeholder-gray-700 line-clamp-1"
                        placeholder={`Вариант ${i + 1}...`}
                        value={opt}
                        onChange={e => updateOption(i, e.target.value)}
                      />
                    </div>
                    {options.length > 2 && (
                      <button
                        onClick={() => removeOption(i)}
                        className="w-12 h-12 flex items-center justify-center rounded-2xl bg-red-50 dark:bg-red-900/20 text-red-400 hover:bg-red-100 hover:text-red-600 dark:hover:bg-red-900/40 transition-all group-hover:scale-105"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    )}
                  </motion.div>
                ))}
              </AnimatePresence>

              {options.length < 10 && (
                <button
                  onClick={addOption}
                  className="w-full py-4 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-2xl text-gray-400 hover:border-orange-500/50 hover:text-orange-500 hover:bg-orange-50/30 dark:hover:bg-orange-900/10 transition-all flex items-center justify-center gap-2 font-bold text-sm"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                  </svg>
                  Добавить вариант
                </button>
              )}
            </div>
          </div>

          <div className="pt-4 border-t border-gray-100 dark:border-gray-700">
            <div
              onClick={() => setIsAnonymous(!isAnonymous)}
              className="flex items-center justify-between p-4 rounded-2xl bg-gray-50 dark:bg-gray-900 border border-transparent hover:border-orange-500/30 transition-all cursor-pointer group"
            >
              <div className="flex items-center gap-3">
                <div className={`p-2 rounded-xl transition-colors ${isAnonymous ? 'bg-orange-500 text-white' : 'bg-gray-200 dark:bg-gray-800 text-gray-500'}`}>
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.046m4.596-4.596A9.964 9.964 0 0112 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-1.447 0-2.811-.378-3.991-1.042m2.414-2.414l7.071-7.071" />
                  </svg>
                </div>
                <div>
                  <p className="text-[15px] font-bold dark:text-white">Анонимный опрос</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Скрыть имена проголосовавших</p>
                </div>
              </div>
              <div className={`w-12 h-6 rounded-full transition-colors relative ${isAnonymous ? 'bg-orange-500' : 'bg-gray-300 dark:bg-gray-700'}`}>
                <motion.div
                  animate={{ x: isAnonymous ? 24 : 4 }}
                  className="absolute top-1 w-4 h-4 bg-white rounded-full shadow-sm"
                />
              </div>
            </div>
          </div>
        </div>

        <div className="p-8 bg-gray-50 dark:bg-gray-900/50 flex gap-4 border-t border-gray-100 dark:border-gray-800">
          <button
            onClick={onClose}
            className="flex-1 px-6 py-4 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl font-black text-gray-500 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-700 transition-all uppercase tracking-widest text-xs"
          >
            Отмена
          </button>
          <button
            onClick={() => onSubmit(question, options.filter(o => o.trim()), isAnonymous)}
            disabled={!question.trim() || options.filter(o => o.trim()).length < 2}
            className="flex-1 px-6 py-4 bg-gradient-to-br from-orange-400 to-orange-600 hover:from-orange-500 hover:to-orange-700 disabled:opacity-30 disabled:grayscale text-white rounded-2xl font-black shadow-[0_8px_24px_-4px_rgba(249,115,22,0.4)] transition-all uppercase tracking-widest text-xs"
          >
            Запустить
          </button>
        </div>
      </motion.div>
    </div>
  );
};

const MembersModal = ({
  chat,
  members,
  onClose,
  onRemove,
  currentUserId
}: {
  chat: Chat;
  members: User[];
  onClose: () => void;
  onRemove: (userId: number) => void;
  currentUserId: number;
}) => {
  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="absolute inset-0 bg-gray-900/60 backdrop-blur-sm"
      />
      <motion.div
        initial={{ opacity: 0, scale: 0.9, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.9, y: 20 }}
        className="relative bg-white dark:bg-gray-800 rounded-[28px] w-full max-w-md shadow-2xl overflow-hidden border border-white/20 dark:border-gray-700 flex flex-col max-h-[80vh]"
      >
        <div className="p-6 border-b border-gray-100 dark:border-gray-700 flex justify-between items-center">
          <div>
            <h3 className="text-xl font-bold dark:text-white">Участники</h3>
            <p className="text-sm text-gray-500">{members.length} участников</p>
          </div>
          <button onClick={onClose} className="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-400">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="flex-1 overflow-y-auto custom-scrollbar p-6 space-y-4">
          {members.map((u) => (
            <div key={u.id} className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-apple-blue/10 flex items-center justify-center text-apple-blue font-bold uppercase shrink-0">
                {u.firstName?.charAt(0) || u.email.charAt(0)}
              </div>
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-gray-900 dark:text-white truncate">
                  {u.firstName ? `${u.firstName} ${u.lastName || ''}` : u.email}
                  {chat.type === 'group' && Number(u.id) === Number(chat.creatorId) && (
                    <span className="ml-2 text-[10px] bg-apple-blue/10 text-apple-blue px-2 py-0.5 rounded-full uppercase font-black tracking-widest text-center">Создатель</span>
                  )}
                </p>
                <p className="text-xs text-gray-500 truncate">{u.email}</p>
              </div>

              {chat.type === 'group' && Number(chat.creatorId) === Number(currentUserId) && Number(u.id) !== Number(currentUserId) && (
                <button
                  onClick={() => onRemove(u.id)}
                  className="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-colors"
                  title="Удалить участника"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              )}
            </div>
          ))}
        </div>
      </motion.div>
    </div>
  );
};

const AddMemberDialog = ({ onClose, onSubmit }: { onClose: () => void; onSubmit: (userId: number) => void }) => {
  const [query, setQuery] = useState('');
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const searchUsers = async () => {
      if (!query.trim() || query.length < 2) {
        setUsers([]);
        return;
      }
      setLoading(true);
      try {
        const results = await usersAPI.search(query);
        setUsers(results);
      } catch (err) {
        console.error('Search error:', err);
      } finally {
        setLoading(false);
      }
    };

    const timer = setTimeout(searchUsers, 300);
    return () => clearTimeout(timer);
  }, [query]);

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="absolute inset-0 bg-gray-900/60 backdrop-blur-sm"
      />
      <motion.div
        initial={{ opacity: 0, scale: 0.9, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.9, y: 20 }}
        className="relative bg-white dark:bg-gray-800 rounded-[28px] w-full max-w-md shadow-2xl overflow-hidden border border-white/20 dark:border-gray-700"
      >
        <div className="p-6 border-b border-gray-100 dark:border-gray-700">
          <h3 className="text-xl font-bold dark:text-white">Добавить участника</h3>
          <p className="text-sm text-gray-500">Найдите пользователя по почте или имени</p>
        </div>

        <div className="p-6 space-y-4">
          <input
            autoFocus
            type="text"
            className="input-field w-full dark:bg-gray-700 dark:text-white dark:border-gray-600"
            placeholder="Поиск..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />

          <div className="max-h-[300px] overflow-y-auto custom-scrollbar space-y-2">
            {loading ? (
              <div className="text-center py-4 text-gray-500">Поиск...</div>
            ) : users.length > 0 ? (
              users.map((u) => (
                <button
                  key={u.id}
                  onClick={() => onSubmit(u.id)}
                  className="w-full flex items-center gap-3 p-3 rounded-xl hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-left group"
                >
                  <div className="w-10 h-10 rounded-full bg-apple-blue/10 flex items-center justify-center text-apple-blue font-bold uppercase">
                    {u.firstName?.charAt(0) || u.email.charAt(0)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-semibold text-gray-900 dark:text-white truncate">
                      {u.firstName ? `${u.firstName} ${u.lastName || ''}` : u.email}
                    </p>
                    <p className="text-xs text-gray-500 truncate">{u.email}</p>
                  </div>
                  <div className="opacity-0 group-hover:opacity-100 transition-opacity">
                    <svg className="w-5 h-5 text-apple-blue" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M12 4v16m8-8H4" />
                    </svg>
                  </div>
                </button>
              ))
            ) : query.length >= 2 ? (
              <div className="text-center py-4 text-gray-500">Пользователи не найдены</div>
            ) : null}
          </div>
        </div>

        <div className="p-6 bg-gray-50 dark:bg-gray-900/50 flex justify-end">
          <button onClick={onClose} className="btn-secondary px-6">Отмена</button>
        </div>
      </motion.div>
    </div>
  );
};

const PollVotersModal = ({ poll, users, onClose }: { poll: any; users: User[]; onClose: () => void }) => {
  return (
    <div className="fixed inset-0 z-[80] flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        onClick={onClose}
        className="absolute inset-0 bg-gray-900/60 backdrop-blur-sm"
      />
      <motion.div
        initial={{ opacity: 0, scale: 0.9, y: 20 }}
        animate={{ opacity: 1, scale: 1, y: 0 }}
        exit={{ opacity: 0, scale: 0.9, y: 20 }}
        transition={{ type: "spring", damping: 25, stiffness: 300 }}
        className="relative bg-white dark:bg-gray-800 rounded-[28px] w-full max-w-md shadow-[0_32px_64px_-12px_rgba(0,0,0,0.3)] overflow-hidden border border-white/20 dark:border-gray-700"
      >
        <div className="p-6 border-b border-gray-100 dark:border-gray-700 flex justify-between items-center bg-gradient-to-r from-apple-blue/5 to-transparent">
          <div>
            <h3 className="text-xl font-bold dark:text-white">Результаты</h3>
            <p className="text-xs text-gray-500 dark:text-gray-400 line-clamp-1">{poll.question}</p>
          </div>
          <button onClick={onClose} className="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 dark:bg-gray-700 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="p-6 max-h-[60vh] overflow-y-auto custom-scrollbar space-y-6">
          {poll.options.map((opt: any) => (
            <div key={opt.id} className="space-y-3">
              <div className="flex justify-between items-center px-1">
                <span className="font-bold text-[14px] dark:text-white">{opt.text}</span>
                <span className="text-[11px] font-black text-apple-blue bg-apple-blue/10 px-2 py-0.5 rounded-full uppercase tracking-wider">
                  {opt.voterIds?.length || 0}
                </span>
              </div>
              <div className="space-y-1.5 px-0.5">
                {opt.voterIds && opt.voterIds.length > 0 ? (
                  opt.voterIds.map((userId: number) => {
                    const voter = users.find(u => u.id === userId);
                    return (
                      <div key={userId} className="flex items-center gap-3 p-2 rounded-xl bg-gray-50 dark:bg-white/5 border border-transparent hover:border-apple-blue/20 transition-all">
                        <div className="w-7 h-7 rounded-full bg-apple-blue/10 border border-apple-blue/20 flex items-center justify-center text-[10px] font-black text-apple-blue shrink-0">
                          {voter?.firstName?.charAt(0) || voter?.email?.charAt(0) || '?'}
                        </div>
                        <div className="min-w-0">
                          <p className="text-sm font-semibold truncate dark:text-white">
                            {voter ? `${voter.firstName || ''} ${voter.lastName || ''}`.trim() || voter.email : `User ${userId}`}
                          </p>
                        </div>
                      </div>
                    );
                  })
                ) : (
                  <p className="text-[11px] text-gray-400 italic px-2 font-medium">Голосов пока нет</p>
                )}
              </div>
            </div>
          ))}
        </div>
      </motion.div>
    </div>
  );
};

const PollMessage = ({
  poll,
  onVote,
  currentUserId,
  isOwnMessage,
  members = [],
  onShowDetails
}: {
  poll: any;
  onVote: (id: number) => void;
  currentUserId: number;
  isOwnMessage: boolean;
  members?: User[];
  onShowDetails?: () => void;
}) => {
  const totalVotes = useMemo(() => {
    return poll.options.reduce((acc: number, opt: any) => acc + (opt.voterIds?.length || 0), 0);
  }, [poll.options]);

  const hasVoted = useMemo(() => {
    return poll.options.some((opt: any) => opt.voterIds?.includes(currentUserId));
  }, [poll.options, currentUserId]);

  const getVoterNames = (voterIds: number[]) => {
    if (!voterIds || voterIds.length === 0) return '';
    const names = voterIds
      .map(id => {
        const u = members.find(m => m.id === id);
        return u ? (u.firstName || u.email) : `User ${id}`;
      })
      .filter(Boolean);

    if (names.length === 0) return '';
    if (names.length <= 3) return names.join(', ');
    return `${names.slice(0, 3).join(', ')} +${names.length - 3}`;
  };

  return (
    <div className="space-y-3 py-1 min-w-[220px]">
      <h4 className={`font-bold text-[15px] leading-snug ${isOwnMessage ? 'text-white' : 'text-gray-900 dark:text-white'}`}>
        {poll.question}
      </h4>

      <div className="space-y-1.5 mt-2">
        {poll.options.map((opt: any) => {
          const votes = opt.voterIds?.length || 0;
          const percentage = totalVotes > 0 ? Math.round((votes / totalVotes) * 100) : 0;
          const isSelected = opt.voterIds?.includes(currentUserId);

          return (
            <button
              key={opt.id}
              onClick={(e) => {
                e.stopPropagation();
                onVote(opt.id);
              }}
              className={`w-full relative overflow-hidden rounded-xl border p-2.5 text-left transition-all duration-200 ${isSelected
                ? (isOwnMessage ? 'border-white/40 bg-white/10' : 'border-apple-blue/50 bg-apple-blue/5 dark:bg-apple-blue/10')
                : 'border-transparent bg-black/5 dark:bg-white/5 hover:bg-black/10 dark:hover:bg-white/10'
                }`}
            >
              {/* Progress Bar (Subtle) */}
              {hasVoted && (
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${percentage}%` }}
                  transition={{ duration: 0.8, ease: "easeOut" }}
                  className={`absolute left-0 top-0 bottom-0 opacity-10 pointer-events-none ${isOwnMessage ? 'bg-white' : 'bg-apple-blue'
                    }`}
                />
              )}

              <div className="relative flex justify-between items-center z-10">
                <div className="flex items-center gap-2">
                  {hasVoted && isSelected && (
                    <div className={`w-1 h-1 rounded-full shrink-0 ${isOwnMessage ? 'bg-white' : 'bg-apple-blue'}`} />
                  )}
                  <span className={`text-[13px] font-medium ${isOwnMessage ? 'text-white' : (isSelected ? 'text-apple-blue dark:text-blue-400' : 'text-gray-700 dark:text-gray-200')
                    }`}>
                    {opt.text}
                  </span>
                </div>

                {hasVoted && (
                  <span className={`text-[12px] font-bold tabular-nums ${isOwnMessage ? 'text-white/80' : 'text-gray-500 dark:text-gray-400'}`}>
                    {percentage}%
                  </span>
                )}
              </div>

              {/* Voter Names (Public Poll Only) */}
              {hasVoted && !poll.isAnonymous && votes > 0 && (
                <div className="relative mt-0.5 z-10">
                  <p className={`text-[9px] font-medium leading-tight opacity-70 ${isOwnMessage ? 'text-white' : 'text-gray-500 dark:text-gray-400'}`}>
                    {getVoterNames(opt.voterIds)}
                  </p>
                </div>
              )}
            </button>
          );
        })}
      </div>

      <div className="flex items-center justify-between mt-1 pt-1 group/poll-footer">
        <div className="flex items-center gap-1.5 opacity-60">
          <span className={`text-[9px] font-bold uppercase tracking-wider ${isOwnMessage ? 'text-white' : 'text-gray-500'}`}>
            {totalVotes} {totalVotes === 1 ? 'голос' : 'голосов'}
          </span>
          <span className="w-0.5 h-0.5 rounded-full bg-current" />
          <span className={`text-[9px] font-bold uppercase tracking-wider ${isOwnMessage ? 'text-white' : 'text-gray-500'}`}>
            {poll.isAnonymous ? 'Анонимно' : 'Публично'}
          </span>
        </div>

        {!poll.isAnonymous && onShowDetails && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              onShowDetails();
            }}
            className={`text-[9px] font-black uppercase tracking-widest px-2 py-1 rounded-lg transition-all opacity-0 group-hover/msg:opacity-100 ${isOwnMessage ? 'bg-white/10 text-white hover:bg-white/20' : 'bg-apple-blue/10 text-apple-blue hover:bg-apple-blue/20'
              }`}
          >
            Подробнее
          </button>
        )}
      </div>
    </div>
  );
};

// --- Reactions Components ---

const ReactionsList = ({
  reactions,
  currentUserId,
  onToggleReaction
}: {
  reactions: any[];
  currentUserId: number;
  onToggleReaction: (emoji: string) => void;
}) => {
  if (!reactions || reactions.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-1 mt-1 -mb-1">
      {reactions.map((r) => {
        const hasReacted = r.userIds.includes(currentUserId);
        return (
          <button
            key={r.emoji}
            onClick={(e) => {
              e.stopPropagation();
              onToggleReaction(r.emoji);
            }}
            className={`flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[11px] font-medium transition-all ${hasReacted
              ? 'bg-apple-blue/20 text-apple-blue border border-apple-blue/30'
              : 'bg-black/5 dark:bg-white/5 text-gray-500 hover:bg-black/10 dark:hover:bg-white/10 border border-transparent'
              }`}
          >
            <span>{r.emoji}</span>
            {r.userIds.length > 1 && <span>{r.userIds.length}</span>}
          </button>
        );
      })}
    </div>
  );
};

const ReactionPicker = ({
  onSelect,
  onClose
}: {
  onSelect: (emoji: string) => void;
  onClose: () => void;
}) => {
  const emojis = ['👍', '❤️', '😂', '😮', '😢', '🔥', '✅', '❌', '👏', '🎉', '👀', '🤔'];

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.8, y: 10 }}
      animate={{ opacity: 1, scale: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.8, y: 10 }}
      className="absolute bottom-full mb-2 left-0 z-50 p-2 bg-white dark:bg-gray-800 rounded-2xl shadow-xl border border-gray-100 dark:border-gray-700 flex flex-wrap gap-1 max-w-[200px]"
    >
      {emojis.map((emoji) => (
        <button
          key={emoji}
          onClick={(e) => {
            e.stopPropagation();
            onSelect(emoji);
            onClose();
          }}
          className="w-8 h-8 flex items-center justify-center text-lg hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
        >
          {emoji}
        </button>
      ))}
      <div
        className="fixed inset-0 z-[-1]"
        onClick={(e) => {
          e.stopPropagation();
          onClose();
        }}
      />
    </motion.div>
  );
};

// Мемоизированный компонент сообщения
const MessageItem = memo(({
  message,
  isOwnMessage,
  currentUserId,
  senderName,
  senderAvatarUrl,
  onImageClick,
  onEdit,
  onDelete,
  onShowVoters,
  onToggleReaction,
  members
}: {
  message: Message;
  isOwnMessage: boolean;
  currentUserId: number;
  senderName?: string;
  senderAvatarUrl?: string;
  onImageClick: (url: string) => void;
  onEdit: (message: Message) => void;
  onDelete: (id: string | number) => void;
  onShowVoters?: (poll: any) => void;
  onToggleReaction?: (id: string | number, emoji: string) => void;
  members?: User[];
}) => {
  const timeString = useMemo(() => {
    return new Date(message.createdAt).toLocaleTimeString('ru-RU', {
      hour: '2-digit',
      minute: '2-digit',
    });
  }, [message.createdAt]);

  const [showPicker, setShowPicker] = useState(false);

  const attachment = useMemo(() => {
    if (!message.attachmentUrl) return null;

    // Check if it's a blob URL (preview) or relative URL (server)
    const isBlob = message.attachmentUrl.startsWith('blob:');
    const url = isBlob
      ? message.attachmentUrl
      : `${import.meta.env.VITE_API_URL}/chat${message.attachmentUrl}`;

    // Simple check for image extension
    const isImage = /\.(jpg|jpeg|png|gif|webp)$/i.test(message.attachmentUrl) || isBlob;

    if (isImage) {
      return (
        <div className="mb-2 rounded-lg overflow-hidden max-w-full">
          <img
            src={url}
            alt="Attachment"
            className="max-w-full h-auto max-h-[300px] object-contain block cursor-pointer hover:opacity-95 transition-opacity"
            onClick={() => onImageClick(url)}
            onError={() => {
              console.error('Image load error:', url);
              // If it's a blob error, maybe the URL was revoked too early
            }}
          />
        </div>
      );
    }

    return (
      <a
        href={url}
        target="_blank"
        rel="noopener noreferrer"
        className="flex items-center gap-3 p-3 mb-2 bg-gray-50 dark:bg-gray-600/50 rounded-xl hover:bg-gray-100 dark:hover:bg-gray-600 border border-gray-200 dark:border-gray-600 transition-all group"
      >
        <div className="p-2 bg-white dark:bg-gray-700 rounded-lg text-apple-blue dark:text-blue-400 group-hover:scale-110 transition-transform">
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-900 dark:text-white truncate">
            {message.attachmentUrl.split('/').pop()}
          </p>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Файл
          </p>
        </div>
      </a>
    );
  }, [message.attachmentUrl, onImageClick]);

  return (
    <div
      id={`msg-${message.id}`}
      className={`flex w-full mb-2 group/msg ${isOwnMessage ? 'justify-end' : 'justify-start items-end gap-2'}`}
    >
      {/* Аватарка только для чужих сообщений в групповом чате (если есть имя отправителя) */}
      {!isOwnMessage && senderName && (
        <div className="flex-shrink-0 w-8 h-8 rounded-full bg-gray-200 dark:bg-gray-600 overflow-hidden self-end mb-1 shadow-sm">
          {senderAvatarUrl ? (
            <img
              src={senderAvatarUrl}
              alt={senderName}
              className="w-full h-full object-cover"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-xs font-bold text-gray-500 dark:text-gray-300">
              {senderName.charAt(0).toUpperCase()}
            </div>
          )}
        </div>
      )}

      {!isOwnMessage && message.type !== 'system' && (
        <div className="flex flex-col gap-1 opacity-0 group-hover/msg:opacity-100 transition-opacity">
          <div className="relative">
            <button
              onClick={(e) => {
                e.stopPropagation();
                setShowPicker(!showPicker);
              }}
              className="p-1 text-gray-400 hover:text-apple-blue dark:text-gray-500 transition-colors"
              title="Добавить реакцию"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </button>
            <AnimatePresence>
              {showPicker && (
                <ReactionPicker
                  onSelect={(emoji) => onToggleReaction?.(message.id, emoji)}
                  onClose={() => setShowPicker(false)}
                />
              )}
            </AnimatePresence>
          </div>
        </div>
      )}

      <div
        className={`relative w-fit min-w-[80px] max-w-[85%] lg:max-w-[70%] px-3 py-2 shadow-sm ${message.type === 'system'
          ? 'bg-transparent text-gray-500 text-center mx-auto text-xs'
          : isOwnMessage
            ? 'bg-[#007AFF] dark:bg-[#2b5278] text-white rounded-2xl rounded-br-sm'
            : 'bg-white dark:bg-[#182533] text-gray-800 dark:text-white rounded-2xl rounded-bl-sm border border-gray-100 dark:border-gray-800/50'
          }`}
      >
        {message.type !== 'system' && !isOwnMessage && senderName && (
          <p className={`text-xs font-bold mb-0.5 ${getUserColor(message.userId)}`}>
            {senderName}
          </p>
        )}

        {/* Attachment rendering */}
        {attachment}

        {message.type === 'poll' && message.poll ? (
          <PollMessage
            poll={message.poll}
            isOwnMessage={isOwnMessage}
            onVote={(optionId) => {
              wsService.vote(message.id, optionId);
            }}
            currentUserId={currentUserId}
            members={members}
            onShowDetails={() => onShowVoters?.(message.poll)}
          />
        ) : (
          message.content && (
            <div className="relative group/text">
              <p className="text-[15px] leading-[1.35] break-words whitespace-pre-wrap">
                {message.content}
                {/* Невидимый спейсер, чтобы время не накладывалось на текст в коротких строках */}
                <span className="inline-block w-[48px]" />
              </p>
              <div className={`absolute bottom-0.5 right-0.5 flex items-center gap-1 text-[10px] select-none opacity-80 ${isOwnMessage ? 'text-blue-100' : 'text-gray-400'}`}>
                {message.isEdited && <span className="text-[9px] italic opacity-70">ред.</span>}
                {timeString}
              </div>
            </div>
          )
        )}

        {/* Reactions List */}
        {message.reactions && message.reactions.length > 0 && (
          <ReactionsList
            reactions={message.reactions}
            currentUserId={currentUserId}
            onToggleReaction={(emoji) => onToggleReaction?.(message.id, emoji)}
          />
        )}

        {message.type !== 'system' && isOwnMessage && (
          <div className="absolute -left-10 top-1/2 -translate-y-1/2 flex flex-col gap-1 opacity-0 group-hover/msg:opacity-100 transition-opacity">
            <button
              onClick={() => onEdit(message)}
              className="text-gray-400 hover:text-apple-blue p-1 bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700"
              title="Редактировать"
            >
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
              </svg>
            </button>
            <button
              onClick={() => onDelete(message.id)}
              className="text-gray-400 hover:text-red-500 p-1 bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-100 dark:border-gray-700"
              title="Удалить"
            >
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        )}
      </div>
    </div>
  );
});

MessageItem.displayName = 'MessageItem';

export const ChatWindow = memo(ChatWindowComponent);
