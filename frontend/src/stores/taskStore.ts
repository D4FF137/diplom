import { create } from 'zustand';
import { tasksAPI } from '../services/api';
import type { UserTask, TaskStatus } from '../types';

interface TaskState {
  tasks: UserTask[];
  isLoading: boolean;
  error: string | null;
  fetchTasks: () => Promise<void>;
  createTask: (data: any) => Promise<void>;
  updateTaskStatus: (id: number, status: TaskStatus) => Promise<void>;
  toggleChecklistItem: (taskId: number, itemId: number) => Promise<void>;
  deleteTask: (id: number) => Promise<void>;
}

export const useTaskStore = create<TaskState>((set, get) => ({
  tasks: [],
  isLoading: false,
  error: null,

  fetchTasks: async () => {
    set({ isLoading: true, error: null });
    try {
      const tasks = await tasksAPI.getTasks();
      set({ tasks, isLoading: false });
    } catch (error: any) {
      set({ error: error.message, isLoading: false });
    }
  },

  createTask: async (data) => {
    try {
      const newTask = await tasksAPI.createTask(data);
      set({ tasks: [newTask, ...get().tasks] });
    } catch (error: any) {
      set({ error: error.message });
      throw error;
    }
  },

  updateTaskStatus: async (id, status) => {
    try {
      await tasksAPI.updateStatus(id, status);
      set({
        tasks: get().tasks.map((t) => (t.id === id ? { ...t, status } : t)),
      });
    } catch (error: any) {
      set({ error: error.message });
    }
  },

  toggleChecklistItem: async (taskId, itemId) => {
    try {
      await tasksAPI.toggleItem(itemId);
      set({
        tasks: get().tasks.map((t) => {
          if (t.id === taskId && t.checklistItems) {
            return {
              ...t,
              checklistItems: t.checklistItems.map((i) =>
                i.id === itemId ? { ...i, isCompleted: !i.isCompleted } : i
              ),
            };
          }
          return t;
        }),
      });
    } catch (error: any) {
      set({ error: error.message });
    }
  },

  deleteTask: async (id) => {
    try {
      await tasksAPI.deleteTask(id);
      set({ tasks: get().tasks.filter((t) => t.id !== id) });
    } catch (error: any) {
      set({ error: error.message });
    }
  },
}));
