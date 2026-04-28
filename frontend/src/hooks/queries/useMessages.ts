import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { chatAPI } from '../../services/api';
import type { Message } from '../../types';

const EMPTY_MESSAGES: Message[] = [];

export const useMessages = (chatId: string | number | null) => {
  const queryClient = useQueryClient();

  const {
    data: messages = EMPTY_MESSAGES,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['messages', chatId],
    queryFn: async () => {
      if (!chatId) return [];
      return await chatAPI.getMessages(chatId);
    },
    enabled: !!chatId,
    staleTime: 1000 * 30, // 30 секунд для сообщений
  });

  const sendMessageMutation = useMutation({
    mutationFn: chatAPI.sendMessage,
    onSuccess: (newMessage) => {
      queryClient.setQueryData<Message[]>(['messages', chatId], (old) => {
        return old ? [...old, newMessage] : [newMessage];
      });
    },
  });

  return {
    messages,
    isLoading,
    error,
    refetch,
    sendMessage: sendMessageMutation.mutateAsync,
  };
};




