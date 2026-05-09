export interface User {
  id: number;
  email: string;
  firstName?: string;
  lastName?: string;
  companyId?: number;
  avatarUrl?: string;
  role?: string;
  isBlocked?: boolean;
  lastSeen?: string;
  createdAt?: string;
}

export interface Company {
  id: number;
  name: string;
  createdAt: string;
}

export interface Post {
  id: number;
  content: string;
  userId: number;
  companyId: number;
  imageUrl?: string;
  createdAt: string;
  author?: User;
  likesCount?: number;
  commentsCount?: number;
  isLiked?: boolean;
}

export interface Like {
  id: number;
  postId: number;
  userId: number;
  createdAt: string;
}

export interface Comment {
  id: number;
  postId: number;
  userId: number;
  content: string;
  createdAt: string;
  author?: User;
}

export interface Chat {
  id: number | string;
  name: string;
  companyId: number;
  type: string;
  createdAt: string;
  members?: User[];
  lastMessageAt?: string;
  lastMessage?: string;
  isPinned?: boolean;
  creatorId?: number;
  companyGroupId?: number;
  isSystem?: boolean;
}

export interface CompanyGroup {
  id: number;
  companyId: number;
  name: string;
  leaderUserId: number;
  chatId: string;
  createdByUserId: number;
  createdAt: string;
  memberIds: number[];
}

export interface Reaction {
  emoji: string;
  userIds: number[];
}

export interface Message {
  id: number | string;
  chatId: number | string;
  userId: number;
  content: string;
  attachmentUrl?: string;
  type?: 'text' | 'poll' | 'system';
  poll?: PollData;
  createdAt: string;
  isEdited?: boolean;
  reactions?: Reaction[];
}

export interface PollData {
  question: string;
  options: PollOption[];
  isAnonymous: boolean;
  isMultipleChoice: boolean;
  closedAt?: string;
}

export interface PollOption {
  id: number;
  text: string;
  voterIds: number[];
  voteCount?: number; // Frontend helper
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  companyId?: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: number;
  companyId?: number;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: string;
}

export interface NotificationCounters {
  chatUnread: Record<string, number>; // chatId -> count
  feedUnread: number;
  tasksUnread: number;
}

export interface FileMetadata {
  id: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  path: string;
  ownerId: number;
  companyId: number;
  isImportant: boolean;
  isPrivate: boolean;
  createdAt: string;
}

export type TaskStatus = 'Todo' | 'InProgress' | 'Done' | 'Cancelled';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent';
export type TaskType = 'Simple' | 'Checklist';

export interface ChecklistItem {
  id: number;
  taskId: number;
  content: string;
  isCompleted: boolean;
  completedByUserId?: number;
  completedAt?: string;
}

export interface UserTask {
  id: number;
  companyId: number;
  creatorId: number;
  targetGroupId?: number;
  targetUserId?: number;
  title: string;
  description: string;
  type: TaskType;
  priority: TaskPriority;
  status: TaskStatus;
  dueDate?: string;
  createdAt: string;
  checklistItems?: ChecklistItem[];
}
