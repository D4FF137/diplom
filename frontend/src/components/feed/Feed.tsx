import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { feedAPI } from '../../services/api';
import { CreatePost } from './CreatePost';
import type { Post } from '../../types';

export const Feed = () => {
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadPosts();
  }, []);

  const loadPosts = async () => {
    try {
      setLoading(true);
      const data = await feedAPI.getPosts();
      setPosts(data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка загрузки постов');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="card">
        <p className="text-red-600">{error}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <CreatePost 
        onSuccess={() => {
          // Перезагружаем посты после создания
          loadPosts();
        }} 
      />
      {posts.length === 0 && !loading && (
        <div className="card text-center text-gray-500">
          <p>Пока нет постов. Создайте первый пост!</p>
        </div>
      )}
      {posts.map((post, index) => (
        <motion.div
          key={post.id}
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: index * 0.1 }}
          className="card"
        >
          <p className="text-gray-600 mb-4">{post.content}</p>
          <p className="text-sm text-gray-500">
            {new Date(post.createdAt).toLocaleDateString('ru-RU')}
          </p>
        </motion.div>
      ))}
    </div>
  );
};

