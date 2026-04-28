import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import type { Chat } from '../types';

interface ChatState {
  chats: Chat[];
  selectedChat: Chat | null;
  pinnedChats: Set<number | string>;
  setChats: (chats: Chat[]) => void;
  addChat: (chat: Chat) => void;
  updateChat: (chatId: number | string, updates: Partial<Chat>) => void;
  removeChat: (chatId: number | string) => void;
  selectChat: (chat: Chat | null) => void;
  togglePinChat: (chatId: number | string) => void;
  sortChats: (chats: Chat[]) => Chat[];
  clearChats: () => void;
}

export const useChatStore = create<ChatState>()(
  persist(
    (set, get) => ({
      chats: [],
      selectedChat: null,
      pinnedChats: new Set<number | string>(),

      setChats: (chats) => {
        const sorted = get().sortChats(chats);
        const { selectedChat } = get();

        let newSelectedChat = selectedChat;
        if (selectedChat) {
          const updated = sorted.find(c => String(c.id) === String(selectedChat.id));
          if (updated) {
            newSelectedChat = { ...selectedChat, ...updated };
          }
        }

        set({ chats: sorted, selectedChat: newSelectedChat });
      },

      addChat: (chat) => {
        const { chats } = get();
        const exists = chats.some((c) => String(c.id) === String(chat.id));
        if (!exists) {
          const updated = [...chats, chat];
          const sorted = get().sortChats(updated);
          set({ chats: sorted });
        } else {
          // If it exists, update it instead of adding (handles metadata updates)
          get().updateChat(chat.id, chat);
        }
      },

      updateChat: (chatId, updates) => {
        const { chats, selectedChat } = get();
        const updated = chats.map((chat) =>
          String(chat.id) === String(chatId) ? { ...chat, ...updates } : chat
        );
        const sorted = get().sortChats(updated);

        let newSelectedChat = selectedChat;
        if (selectedChat && String(selectedChat.id) === String(chatId)) {
          newSelectedChat = { ...selectedChat, ...updates };
        }

        set({ chats: sorted, selectedChat: newSelectedChat });
      },

      removeChat: (chatId) => {
        const { chats, selectedChat } = get();
        const filtered = chats.filter((chat) => String(chat.id) !== String(chatId));
        set({
          chats: filtered,
          selectedChat: selectedChat && String(selectedChat.id) === String(chatId) ? null : selectedChat,
        });
      },

      selectChat: (chat) => {
        set({ selectedChat: chat });
        if (chat) {
          sessionStorage.setItem('selectedChatId', chat.id.toString());
          sessionStorage.setItem('isOnFeed', 'false');
        } else {
          sessionStorage.removeItem('selectedChatId');
          sessionStorage.setItem('isOnFeed', 'true');
        }
      },

      togglePinChat: (chatId) => {
        const { pinnedChats } = get();
        const updated = new Set(pinnedChats);
        if (updated.has(chatId)) {
          updated.delete(chatId);
        } else {
          updated.add(chatId);
        }
        set({ pinnedChats: updated });
        const { chats } = get();
        const sorted = get().sortChats(chats);
        set({ chats: sorted });
      },

      sortChats: (chatsToSort) => {
        const { pinnedChats } = get();
        return [...chatsToSort].sort((a, b) => {
          const aPinned = pinnedChats.has(a.id);
          const bPinned = pinnedChats.has(b.id);

          if (aPinned && !bPinned) return -1;
          if (!aPinned && bPinned) return 1;

          const aTime = a.lastMessageAt ? new Date(a.lastMessageAt).getTime() : 0;
          const bTime = b.lastMessageAt ? new Date(b.lastMessageAt).getTime() : 0;
          return bTime - aTime;
        });
      },
      clearChats: () => {
        set({
          chats: [],
          selectedChat: null,
          pinnedChats: new Set(),
        });
      },
    }),
    {
      name: 'chat-storage',
      storage: createJSONStorage(() => sessionStorage),
      partialize: (state) => ({
        selectedChat: state.selectedChat,
        pinnedChats: Array.from(state.pinnedChats),
      }),
      merge: (persistedState, currentState) => {
        const persisted = persistedState as any;
        return {
          ...currentState,
          selectedChat: persisted.selectedChat || currentState.selectedChat,
          pinnedChats: persisted.pinnedChats
            ? new Set(persisted.pinnedChats)
            : currentState.pinnedChats,
        };
      },
    }
  )
);
