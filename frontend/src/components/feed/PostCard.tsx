import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { feedAPI } from '../../services/api';
import { feedWsService } from '../../services/feedWebSocket';
import { useAuth } from '../../hooks/useAuthOptimized';
import { CommentSection } from './CommentSection';
import { UserProfileModal } from '../profile/UserProfileModal';
import { getImageUrl } from '../../utils/imageUrl';
import type { Post } from '../../types';

interface PostCardProps {
  post: Post;
  onUpdate: () => void;
}

export const PostCard = ({ post, onUpdate }: PostCardProps) => {
  const { user } = useAuth();
  const [showComments, setShowComments] = useState(false);
  const [isLiked, setIsLiked] = useState(post.isLiked || false);
  const [likesCount, setLikesCount] = useState(post.likesCount || 0);
  const [commentsCount, setCommentsCount] = useState(post.commentsCount || 0);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showUserProfile, setShowUserProfile] = useState(false);

  // Обновляем состояние при изменении поста (реалтайм обновления)
  useEffect(() => {
    setIsLiked(post.isLiked || false);
    setLikesCount(post.likesCount || 0);
    setCommentsCount(post.commentsCount || 0);
  }, [post.isLiked, post.likesCount, post.commentsCount]);

  // Слушаем реалтайм обновления для этого поста
  useEffect(() => {
    const handlePostLiked = (data: { PostId: number; LikesCount: number }) => {
      if (data.PostId === post.id) {
        setLikesCount(data.LikesCount);
        setIsLiked(true);
      }
    };

    const handlePostUnliked = (data: { PostId: number; LikesCount: number }) => {
      if (data.PostId === post.id) {
        setLikesCount(data.LikesCount);
        setIsLiked(false);
      }
    };

    const handleNewComment = (data: { PostId: number; Comment: any; CommentsCount: number }) => {
      if (data.PostId === post.id) {
        setCommentsCount(data.CommentsCount);
      }
    };

    // Подписываемся только если соединение установлено
    if (feedWsService.isConnected()) {
      feedWsService.onPostLiked(handlePostLiked);
      feedWsService.onPostUnliked(handlePostUnliked);
      feedWsService.onNewComment(handleNewComment);
    } else {
      // Если WebSocket не подключен, проверяем периодически
      const checkConnection = setInterval(() => {
        if (feedWsService.isConnected()) {
          feedWsService.onPostLiked(handlePostLiked);
          feedWsService.onPostUnliked(handlePostUnliked);
          feedWsService.onNewComment(handleNewComment);
          clearInterval(checkConnection);
        }
      }, 1000);

      setTimeout(() => clearInterval(checkConnection), 10000);
    }

    return () => {
      feedWsService.off('PostLiked');
      feedWsService.off('PostUnliked');
      feedWsService.off('NewComment');
    };
  }, [post.id]);

  const handleLike = async () => {
    // Оптимистичное обновление
    const previousLiked = isLiked;
    const previousCount = likesCount;

    if (isLiked) {
      setIsLiked(false);
      setLikesCount(prev => Math.max(0, prev - 1));
    } else {
      setIsLiked(true);
      setLikesCount(prev => prev + 1);
    }

    try {
      if (previousLiked) {
        await feedAPI.unlikePost(post.id);
      } else {
        await feedAPI.likePost(post.id);
      }

      // Если WebSocket не работает, обновляем через 1 секунду
      if (!feedWsService.isConnected()) {
        setTimeout(() => {
          // Перезагружаем данные поста
          onUpdate();
        }, 1000);
      }
    } catch (err) {
      console.error('Error toggling like:', err);
      // Откатываем изменения при ошибке
      setIsLiked(previousLiked);
      setLikesCount(previousCount);
    }
  };

  const authorName = post.author
    ? `${post.author.firstName || ''} ${post.author.lastName || ''}`.trim() || post.author.email
    : 'Неизвестный пользователь';

  const authorAvatar = getImageUrl(post.author?.avatarUrl);
  const isAuthor = user?.id === post.userId;

  const handleDelete = async () => {
    if (!confirm('Вы уверены, что хотите удалить этот пост?')) {
      return;
    }

    setIsDeleting(true);
    try {
      await feedAPI.deletePost(post.id);
      onUpdate(); // Обновляем список постов
    } catch (err) {
      console.error('Error deleting post:', err);
      alert('Ошибка при удалении поста');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      {/* Заголовок поста */}
      <div className="p-4 border-b border-gray-100 dark:border-gray-700">
        <div className="flex items-center gap-3">
          <button
            onClick={() => setShowUserProfile(true)}
            className="w-10 h-10 rounded-full bg-apple-blue text-white flex items-center justify-center text-sm font-medium flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
          >
            {authorAvatar ? (
              <img src={authorAvatar} alt="Avatar" className="w-full h-full rounded-full object-cover" loading="lazy" />
            ) : (
              authorName.charAt(0).toUpperCase()
            )}
          </button>
          <div className="flex-1">
            <button
              onClick={() => setShowUserProfile(true)}
              className="font-medium text-gray-900 dark:text-white hover:text-apple-blue transition-colors text-left"
            >
              {authorName}
            </button>
            <div className="text-xs text-gray-500 dark:text-gray-400">
              {new Date(post.createdAt).toLocaleDateString('ru-RU', {
                day: 'numeric',
                month: 'long',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </div>
          </div>
          {isAuthor && (
            <button
              onClick={handleDelete}
              disabled={isDeleting}
              className="text-gray-400 hover:text-red-500 transition-colors p-2"
              title="Удалить пост"
            >
              {isDeleting ? (
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-red-500"></div>
              ) : (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              )}
            </button>
          )}
        </div>
      </div>

      {/* Контент поста */}
      <div className="p-4">
        {post.content && (
          <p className="text-gray-900 dark:text-white mb-4 whitespace-pre-wrap">{post.content}</p>
        )}
        {post.imageUrl && (
          <div className="mb-4 rounded-xl overflow-hidden">
            <img
              src={getImageUrl(post.imageUrl) || ''}
              alt="Post"
              className="w-full h-auto object-cover"
              loading="lazy"
            />
          </div>
        )}
      </div>

      {/* Действия */}
      <div className="px-4 py-3 border-t border-gray-100 dark:border-gray-700 flex items-center gap-6">
        <button
          onClick={handleLike}
          className={`flex items-center gap-2 transition-colors ${isLiked ? 'text-red-500' : 'text-gray-500 dark:text-gray-400 hover:text-red-500'
            }`}
        >
          <svg
            className="w-5 h-5"
            fill={isLiked ? 'currentColor' : 'none'}
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z"
            />
          </svg>
          <span className="text-sm font-medium">{likesCount}</span>
        </button>

        <button
          onClick={() => setShowComments(!showComments)}
          className="flex items-center gap-2 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
            />
          </svg>
          <span className="text-sm font-medium">{commentsCount}</span>
        </button>
      </div>

      {/* Комментарии */}
      {showComments && (
        <motion.div
          initial={{ height: 0, opacity: 0 }}
          animate={{ height: 'auto', opacity: 1 }}
          exit={{ height: 0, opacity: 0 }}
          className="border-t border-gray-100"
        >
          <CommentSection postId={post.id} onUpdate={onUpdate} />
        </motion.div>
      )}

      {/* Модальное окно профиля пользователя */}
      {showUserProfile && post.author && (
        <UserProfileModal
          userId={post.author.id}
          onClose={() => setShowUserProfile(false)}
        />
      )}
    </div>
  );
};

