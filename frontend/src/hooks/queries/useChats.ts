import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { chatAPI } from '../../services/api';
import { useChatStore } from '../../stores/chatStore';
import type { Chat } from '../../types';

export const useChats = () => {
  const queryClient = useQueryClient();
  const { setChats, addChat, removeChat } = useChatStore();

  const {
    data: chats,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['chats'],
    queryFn: async () => {
      const data = await chatAPI.getChats();
      setChats(data);
      return data;
    },
  });

  const createChatMutation = useMutation({
    mutationFn: chatAPI.createChat,
    onSuccess: (newChat) => {
      queryClient.setQueryData<Chat[]>(['chats'], (old) => {
        const updated = old ? [...old, newChat] : [newChat];
        setChats(updated);
        return updated;
      });
      addChat(newChat);
    },
  });

  const deleteChatMutation = useMutation({
    mutationFn: chatAPI.deleteChat,
    onSuccess: (_, chatId) => {
      queryClient.setQueryData<Chat[]>(['chats'], (old) => {
        const updated = old?.filter((c) => c.id !== chatId) || [];
        setChats(updated);
        return updated;
      });
      removeChat(chatId);
    },
  });

  const leaveChatMutation = useMutation({
    mutationFn: chatAPI.leaveChat,
    onSuccess: (_, chatId) => {
      queryClient.setQueryData<Chat[]>(['chats'], (old) => {
        const updated = old?.filter((c) => c.id !== chatId) || [];
        setChats(updated);
        return updated;
      });
      removeChat(chatId);
    },
  });

  return {
    chats: chats || [],
    isLoading,
    error,
    refetch,
    createChat: createChatMutation.mutateAsync,
    deleteChat: deleteChatMutation.mutateAsync,
    leaveChat: leaveChatMutation.mutateAsync,
  };
};




