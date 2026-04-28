import { useEffect, useMemo, useCallback, memo } from 'react';
import { motion } from 'framer-motion';
import { useQueryClient } from '@tanstack/react-query';
import { feedWsService } from '../../services/feedWebSocket';
import { usePosts } from '../../hooks/queries/usePosts';
import { useNotifications } from '../../hooks/useNotificationsOptimized';
import { useAuthStore } from '../../stores/authStore';
import { PostCard } from './PostCard';
import { CreatePostCard } from './CreatePostCard';
import type { Post, Comment } from '../../types';

const FeedCenterComponent = () => {
  const user = useAuthStore((state) => state.user);
  const { feedUnread, markFeedAsRead } = useNotifications();
  const queryClient = useQueryClient();
  const {
    posts = [],
    isLoading,
    error,
    refetch,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage
  } = usePosts();

  useEffect(() => {
    // Помечаем ленту как прочитанную при загрузке
    markFeedAsRead();

    // Слушаем событие обновления ленты
    const handleRefresh = () => {
      refetch();
    };
    window.addEventListener('refreshFeed', handleRefresh);

    return () => {
      window.removeEventListener('refreshFeed', handleRefresh);
    };
  }, [markFeedAsRead, refetch]);

  useEffect(() => {
    // Подключаемся к WebSocket для реалтайм обновлений
    let pollingInterval: any = null;
    let isWebSocketConnected = false;

    if (user) {
      const setupWebSocket = async () => {
        try {
          await feedWsService.connect();
          isWebSocketConnected = feedWsService.isConnected();

          if (isWebSocketConnected) {
            // Слушаем новые посты
            feedWsService.onNewPost((newPost: Post) => {
              queryClient.setQueryData<Post[]>(['posts'], (old) => {
                if (!old) return [newPost];
                if (old.some((p) => p.id === newPost.id)) {
                  return old;
                }
                return [newPost, ...old];
              });
            });

            // Слушаем обновления лайков
            feedWsService.onPostLiked((data: { PostId: number; LikesCount: number }) => {
              queryClient.setQueryData<Post[]>(['posts'], (old) => {
                if (!old) return old;
                return old.map((post) =>
                  post.id === data.PostId
                    ? { ...post, likesCount: data.LikesCount, isLiked: post.userId === user?.id ? true : post.isLiked }
                    : post
                );
              });
            });

            feedWsService.onPostUnliked((data: { PostId: number; LikesCount: number }) => {
              queryClient.setQueryData<Post[]>(['posts'], (old) => {
                if (!old) return old;
                return old.map((post) =>
                  post.id === data.PostId
                    ? { ...post, likesCount: data.LikesCount, isLiked: post.userId === user?.id ? false : post.isLiked }
                    : post
                );
              });
            });

            // Слушаем новые комментарии
            feedWsService.onNewComment((data: { PostId: number; Comment: Comment; CommentsCount: number }) => {
              queryClient.setQueryData<Post[]>(['posts'], (old) => {
                if (!old) return old;
                return old.map((post) =>
                  post.id === data.PostId
                    ? { ...post, commentsCount: data.CommentsCount }
                    : post
                );
              });
            });
          }
        } catch (error) {
          console.error('Error setting up WebSocket:', error);
          isWebSocketConnected = false;
        }
      };

      setupWebSocket();

      // Если WebSocket не подключен, используем polling как fallback
      const checkConnectionAndSetupPolling = () => {
        if (!feedWsService.isConnected() && !isWebSocketConnected) {
          // Запускаем polling каждые 3 секунды
          if (!pollingInterval) {
            console.log('WebSocket not connected, starting polling...');
            pollingInterval = setInterval(() => {
              refetch(); // Используем refetch из React Query
            }, 3000);
          }
        } else {
          // Если WebSocket подключен, очищаем polling
          if (pollingInterval) {
            console.log('WebSocket connected, stopping polling...');
            clearInterval(pollingInterval);
            pollingInterval = null;
          }
        }
      };

      // Проверяем соединение через 2 секунды после попытки подключения
      const checkTimeout = setTimeout(() => {
        checkConnectionAndSetupPolling();
      }, 2000);

      // Также проверяем периодически
      const connectionCheckInterval = setInterval(() => {
        const connected = feedWsService.isConnected();
        if (connected) {
          isWebSocketConnected = true;
          if (pollingInterval) {
            clearInterval(pollingInterval);
            pollingInterval = null;
          }
        } else {
          isWebSocketConnected = false;
          checkConnectionAndSetupPolling();
        }
      }, 5000);

      return () => {
        clearTimeout(checkTimeout);
        clearInterval(connectionCheckInterval);
        if (pollingInterval) {
          clearInterval(pollingInterval);
        }
        feedWsService.disconnect().catch(console.error);
        feedWsService.off('NewPost');
        feedWsService.off('PostLiked');
        feedWsService.off('PostUnliked');
        feedWsService.off('NewComment');
      };
    }

    return () => {
      if (pollingInterval) {
        clearInterval(pollingInterval);
      }
    };
  }, [user, queryClient, refetch]);

  const handlePostUpdate = useCallback(() => {
    refetch();
  }, [refetch]);

  // Мемоизированный список постов - должен быть выше условных return
  const postsList = useMemo(() => {
    return posts.map((post, index) => (
      <PostItem
        key={post.id}
        post={post}
        index={index}
        onUpdate={handlePostUpdate}
      />
    ));
  }, [posts, handlePostUpdate]);

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-full">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex justify-center items-center h-full">
        <div className="text-red-600">{error.message || 'Ошибка загрузки постов'}</div>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto p-6 space-y-6">
      {feedUnread > 0 && (
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4 flex items-center justify-between">
          <span className="text-sm text-blue-700 dark:text-blue-300">
            Новых постов: <strong>{feedUnread}</strong>
          </span>
          <button
            onClick={markFeedAsRead}
            className="text-sm text-blue-600 dark:text-blue-400 hover:underline"
          >
            Отметить как прочитанное
          </button>
        </div>
      )}
      <CreatePostCard onSuccess={handlePostUpdate} />

      {posts.length === 0 ? (
        <div className="text-center text-gray-500 py-12">
          <p className="text-lg">Пока нет постов</p>
          <p className="text-sm mt-2">Создайте первый пост!</p>
        </div>
      ) : (
        <>
          {postsList}

          {/* Сентинель для бесконечного скролла */}
          <div
            className="h-10 flex justify-center items-center"
            ref={(el) => {
              if (el && hasNextPage && !isFetchingNextPage && !isLoading) {
                const observer = new IntersectionObserver((entries) => {
                  if (entries[0].isIntersecting) {
                    fetchNextPage();
                  }
                }, { threshold: 0.5 });
                observer.observe(el);
              }
            }}
          >
            {isFetchingNextPage && (
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-apple-blue"></div>
            )}
            {!hasNextPage && posts.length > 0 && (
              <p className="text-sm text-gray-400">Вы просмотрели все посты</p>
            )}
          </div>
        </>
      )}
    </div>
  );
};

// Мемоизированный компонент поста
const PostItem = memo(({ post, index, onUpdate }: { post: Post; index: number; onUpdate: () => void }) => (
  <motion.div
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ delay: index * 0.05 }}
  >
    <PostCard post={post} onUpdate={onUpdate} />
  </motion.div>
));

PostItem.displayName = 'PostItem';

export const FeedCenter = memo(FeedCenterComponent);
