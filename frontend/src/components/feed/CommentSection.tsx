import { useState, useEffect } from 'react';

import { feedAPI } from '../../services/api';
import { feedWsService } from '../../services/feedWebSocket';
import { getImageUrl } from '../../utils/imageUrl';
import type { Comment } from '../../types';

interface CommentSectionProps {
  postId: number;
  onUpdate: () => void;
}

export const CommentSection = ({ postId, onUpdate }: CommentSectionProps) => {
  // @ts-ignore
  onUpdate;
  const [comments, setComments] = useState<Comment[]>([]);
  const [newComment, setNewComment] = useState('');
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);

  useEffect(() => {
    loadComments();

    // Слушаем новые комментарии в реалтайме
    const handleNewComment = (data: { PostId: number; Comment: Comment; CommentsCount: number }) => {
      if (data.PostId === postId) {
        // Проверяем, нет ли уже такого комментария
        setComments((prev) => {
          if (prev.some((c) => c.id === data.Comment.id)) {
            return prev;
          }
          return [...prev, data.Comment];
        });
      }
    };

    let pollingInterval: ReturnType<typeof setInterval> | null = null;

    // Подписываемся на события после установки соединения
    const setupSubscription = () => {
      if (feedWsService.isConnected()) {
        feedWsService.onNewComment(handleNewComment);
        // Если WebSocket подключен, очищаем polling
        if (pollingInterval) {
          clearInterval(pollingInterval);
          pollingInterval = null;
        }
      } else {
        // Если WebSocket не подключен, используем polling
        if (!pollingInterval) {
          pollingInterval = setInterval(() => {
            loadComments(true); // silent mode для polling
          }, 3000);
        }
      }
    };

    // Пытаемся подписаться сразу
    setupSubscription();

    // Ждем установки соединения
    const checkConnection = setInterval(() => {
      setupSubscription();
    }, 1000);
    
    // Очищаем интервал через 5 секунд
    setTimeout(() => {
      clearInterval(checkConnection);
      setupSubscription();
    }, 5000);

    // Периодически проверяем соединение
    const connectionCheckInterval = setInterval(() => {
      setupSubscription();
    }, 5000);

    return () => {
      clearInterval(checkConnection);
      clearInterval(connectionCheckInterval);
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
      feedWsService.off('NewComment');
    };
  }, [postId]);

  const loadComments = async (silent = false) => {
    try {
      if (!silent) {
        setLoading(true);
      }
      const data = await feedAPI.getComments(postId);
      // Обновляем только если есть изменения
      setComments((prev) => {
        const prevIds = new Set(prev.map(c => c.id));
        const newIds = new Set(data.map(c => c.id));
        const hasChanges = prev.length !== data.length || 
          data.some(c => !prevIds.has(c.id)) ||
          prev.some(c => !newIds.has(c.id));
        
        if (hasChanges) {
          return data;
        }
        return prev;
      });
    } catch (err) {
      console.error('Error loading comments:', err);
    } finally {
      if (!silent) {
        setLoading(false);
      }
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    e.stopPropagation(); // Предотвращаем всплытие события
    if (!newComment.trim() || sending) return;

    const commentText = newComment.trim();
    setSending(true);
    setNewComment('');
    
    // Оптимистичное обновление - добавляем комментарий сразу
    const tempComment: Comment = {
      id: Date.now(), // Временный ID
      postId: postId,
      userId: 0,
      content: commentText,
      createdAt: new Date().toISOString(),
      author: undefined, // Будет обновлено после получения ответа
    };
    
    setComments((prev) => [...prev, tempComment]);
    
    try {
      // Отправляем комментарий на сервер
      const createdComment = await feedAPI.createComment(postId, { content: commentText });
      
      // Заменяем временный комментарий на реальный
      setComments((prev) => 
        prev.map(c => c.id === tempComment.id ? createdComment : c)
      );
      
      // Если WebSocket не работает, перезагружаем комментарии через 1 секунду
      if (!feedWsService.isConnected()) {
        setTimeout(() => {
          loadComments(true);
        }, 1000);
      }
    } catch (err) {
      console.error('Error creating comment:', err);
      // Удаляем временный комментарий при ошибке
      setComments((prev) => prev.filter(c => c.id !== tempComment.id));
      setNewComment(commentText); // Возвращаем текст обратно
    } finally {
      setSending(false);
    }
  };

  if (loading) {
    return <div className="p-4 text-center text-gray-500 dark:text-gray-400 text-sm">Загрузка комментариев...</div>;
  }

  return (
    <div className="p-4 space-y-4">
      {/* Список комментариев */}
      <div className="space-y-3 max-h-64 overflow-y-auto">
        {comments.length === 0 ? (
          <div className="text-center text-gray-500 dark:text-gray-400 text-sm py-4">Нет комментариев</div>
        ) : (
          comments.map((comment) => {
            const authorName = comment.author
              ? `${comment.author.firstName || ''} ${comment.author.lastName || ''}`.trim() || comment.author.email
              : 'Неизвестный пользователь';

            return (
              <div key={comment.id} className="flex gap-3">
                <div className="w-8 h-8 rounded-full bg-apple-blue text-white flex items-center justify-center text-xs font-medium flex-shrink-0">
                  {getImageUrl(comment.author?.avatarUrl) ? (
                    <img
                      src={getImageUrl(comment.author?.avatarUrl) || ''}
                      alt="Avatar"
                      className="w-full h-full rounded-full object-cover"
                    />
                  ) : (
                    authorName.charAt(0).toUpperCase()
                  )}
                </div>
                <div className="flex-1">
                  <div className="bg-gray-50 dark:bg-gray-700 rounded-2xl px-3 py-2">
                    <div className="font-medium text-sm text-gray-900 dark:text-white">{authorName}</div>
                    <div className="text-sm text-gray-700 dark:text-gray-300 mt-1">{comment.content}</div>
                  </div>
                  <div className="text-xs text-gray-500 dark:text-gray-400 mt-1 ml-3">
                    {new Date(comment.createdAt).toLocaleTimeString('ru-RU', {
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Форма добавления комментария */}
      <form onSubmit={handleSubmit} className="flex gap-2">
        <input
          type="text"
          className="flex-1 px-4 py-2 border border-gray-200 dark:border-gray-600 rounded-full focus:outline-none focus:ring-2 focus:ring-apple-blue focus:border-transparent bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400"
          placeholder="Написать комментарий..."
          value={newComment}
          onChange={(e) => setNewComment(e.target.value)}
        />
        <button
          type="submit"
          disabled={sending || !newComment.trim()}
          className="px-4 py-2 bg-apple-blue text-white rounded-full font-medium hover:bg-blue-600 transition-colors disabled:opacity-50"
        >
          Отправить
        </button>
      </form>
    </div>
  );
};

