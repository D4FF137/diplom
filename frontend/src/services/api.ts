import axios from 'axios';
import type {
  User,
  Company,
  Post,
  Chat,
  Message,
  LoginRequest,
  AuthResponse,
  Like,
  Comment,
  NotificationCounters,
  FileMetadata
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Auth API (register removed; only boss adds members)
export const authAPI = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  logout: async (): Promise<void> => {
    await api.post('/auth/logout');
  },
};

// Companies API
export const companiesAPI = {
  getAll: async (): Promise<Company[]> => {
    const response = await api.get<Company[]>('/companies');
    return response.data;
  },

  getById: async (id: number): Promise<Company> => {
    const response = await api.get<Company>(`/companies/${id}`);
    return response.data;
  },

  create: async (data: { name: string }): Promise<Company> => {
    const response = await api.post<Company>('/companies', data);
    return response.data;
  },

  update: async (id: number, data: { name: string }): Promise<Company> => {
    const response = await api.put<Company>(`/companies/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await api.delete(`/companies/${id}`);
  },
};

// Feed API
export const feedAPI = {
  getPosts: async (params?: { skip?: number; take?: number }): Promise<Post[]> => {
    const response = await api.get<Post[]>('/feed/posts', { params });
    return response.data;
  },

  getPostById: async (id: number): Promise<Post> => {
    const response = await api.get<Post>(`/feed/posts/${id}`);
    return response.data;
  },

  createPost: async (data: FormData | { content: string; image?: File }): Promise<Post> => {
    const formData = data instanceof FormData ? data : new FormData();
    if (!(data instanceof FormData)) {
      formData.append('content', data.content);
      if (data.image) {
        formData.append('image', data.image);
      }
    }
    const response = await api.post<Post>('/feed/posts', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  updatePost: async (id: number, data: { content: string }): Promise<Post> => {
    const response = await api.put<Post>(`/feed/posts/${id}`, data);
    return response.data;
  },

  deletePost: async (id: number): Promise<void> => {
    await api.delete(`/feed/posts/${id}`);
  },

  likePost: async (postId: number): Promise<Like> => {
    const response = await api.post<Like>(`/feed/posts/${postId}/like`);
    return response.data;
  },

  unlikePost: async (postId: number): Promise<void> => {
    await api.delete(`/feed/posts/${postId}/like`);
  },

  getComments: async (postId: number): Promise<Comment[]> => {
    const response = await api.get<Comment[]>(`/feed/posts/${postId}/comments`);
    return response.data;
  },

  createComment: async (postId: number, data: { content: string }): Promise<Comment> => {
    const response = await api.post<Comment>(`/feed/posts/${postId}/comments`, data);
    return response.data;
  },
};

// Chat API
export const chatAPI = {
  getChats: async (): Promise<Chat[]> => {
    const response = await api.get<Chat[]>('/chat/chats');
    return response.data;
  },

  getChatById: async (id: number): Promise<Chat> => {
    const response = await api.get<Chat>(`/chat/chats/${id}`);
    return response.data;
  },

  createChat: async (data: { name: string; type?: string; userIds?: number[] }): Promise<Chat> => {
    const response = await api.post<Chat>('/chat/chats', data);
    return response.data;
  },

  getMessages: async (chatId: string | number): Promise<Message[]> => {
    const response = await api.get<Message[]>(`/chat/messages?chatId=${chatId}`);
    return response.data;
  },

  searchMessages: async (chatId: string | number, query: string): Promise<Message[]> => {
    const response = await api.get<Message[]>(`/chat/chats/${chatId}/messages/search?query=${encodeURIComponent(query)}`);
    return response.data;
  },

  sendMessage: async (data: { chatId: string | number; content: string; file?: File }): Promise<Message> => {
    const formData = new FormData();
    formData.append('chatId', data.chatId.toString());
    if (data.content) formData.append('content', data.content);
    if (data.file) formData.append('file', data.file);

    // Use raw axios to avoid default Content-Type: application/json from 'api' instance
    // which causes 415 Unsupported Media Type if not cleared correctly
    const response = await axios.post<Message>(`${API_BASE_URL}/chat/messages`, formData, {
      withCredentials: true,
      headers: {
        'Content-Type': 'multipart/form-data',
      }
    });
    return response.data;
  },

  deleteChat: async (chatId: string | number): Promise<void> => {
    await api.delete(`/chat/chats/${chatId}`);
  },

  leaveChat: async (chatId: string | number): Promise<void> => {
    await api.delete(`/chat/chats/${chatId}/leave`);
  },

  updateMessage: async (messageId: string | number, content: string): Promise<Message> => {
    const response = await api.put<Message>(`/chat/messages/${messageId}`, { content });
    return response.data;
  },

  deleteMessage: async (messageId: string | number): Promise<void> => {
    await api.delete(`/chat/messages/${messageId}`);
  },
};

// Notification API
export const notificationAPI = {
  getCounters: async (): Promise<NotificationCounters> => {
    const response = await api.get<NotificationCounters>('/notifications/counters');
    return response.data;
  },

  markChatAsRead: async (chatId: string | number): Promise<void> => {
    await api.post(`/notifications/chats/${chatId}/read`);
  },

  markFeedAsRead: async (): Promise<void> => {
    await api.post('/notifications/feed/read');
  },
};

// Users API
export const usersAPI = {
  getMembers: async (): Promise<User[]> => {
    const response = await api.get<User[]>('/users');
    return response.data;
  },

  search: async (query: string): Promise<User[]> => {
    const response = await api.get<User[]>(`/users/search?q=${encodeURIComponent(query)}`);
    return response.data;
  },

  getUserById: async (id: number): Promise<User> => {
    const response = await api.get<User>(`/users/${id}`);
    return response.data;
  },

  getProfile: async (): Promise<User> => {
    const response = await api.get<User>('/users/me');
    return response.data;
  },

  updateProfile: async (data: FormData): Promise<User> => {
    const response = await api.put<User>('/users/me', data, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  changePassword: async (data: { oldPassword: string; newPassword: string }): Promise<void> => {
    await api.post('/users/me/password', data);
  },

  createMember: async (data: { email: string; password: string; firstName?: string; lastName?: string }): Promise<User> => {
    const response = await api.post<User>('/users/members', data);
    return response.data;
  },

  block: async (userId: number): Promise<void> => {
    await api.post(`/users/${userId}/block`);
  },

  unblock: async (userId: number): Promise<void> => {
    await api.post(`/users/${userId}/unblock`);
  },

  setPassword: async (userId: number, newPassword: string): Promise<void> => {
    await api.post(`/users/${userId}/password`, { newPassword });
  },

  importMembers: async (data: { email: string, firstName?: string, lastName?: string }[]): Promise<{ email: string, password?: string, error?: string, success: boolean }[]> => {
    const response = await api.post('/users/import', data);
    return response.data;
  },
};

// Storage API
export const storageAPI = {
  getFiles: async (params?: { category?: string; type?: string; search?: string; skip?: number; take?: number }): Promise<FileMetadata[]> => {
    const response = await api.get<FileMetadata[]>('/storage/files', { params });
    return response.data;
  },

  uploadFile: async (file: File, isPrivate: boolean = false): Promise<FileMetadata> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('isPrivate', isPrivate.toString());
    const response = await api.post<FileMetadata>('/storage/files/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  toggleImportant: async (id: string, isImportant: boolean): Promise<void> => {
    await api.put(`/storage/files/${id}/important`, isImportant);
  },

  deleteFile: async (id: string): Promise<void> => {
    await api.delete(`/storage/files/${id}`);
  },
};

export default api;

// Admin API (X-Admin-Secret). Pass secret from admin page (form or env).
const adminOrigin = (import.meta.env.VITE_API_URL as string)?.replace(/\/api\/?$/, '') || 'http://localhost:5000';

function adminRequest<T>(path: string, method: 'post', data: object, secret: string) {
  const url = path.startsWith('/') ? `${adminOrigin}/api${path}` : `${adminOrigin}/api/${path}`;
  return axios.request<T>({
    url,
    method,
    headers: {
      'Content-Type': 'application/json',
      'X-Admin-Secret': secret,
    },
    data,
  });
}

export const adminAPI = {
  createCompany: async (data: { name: string }, secret: string) => {
    const res = await adminRequest<{ id: number; name: string; createdAt: string }>(
      '/admin/companies',
      'post',
      data,
      secret
    );
    return res.data;
  },

  createBoss: async (
    companyId: number,
    data: { email: string; password: string; firstName?: string; lastName?: string },
    secret: string
  ) => {
    const res = await adminRequest<{
      id: number;
      companyId: number;
      email: string;
      firstName: string;
      lastName: string;
    }>(`/admin/companies/${companyId}/boss`, 'post', data, secret);
    return res.data;
  },
};
