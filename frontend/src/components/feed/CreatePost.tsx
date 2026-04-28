import { useState } from 'react';
import { motion } from 'framer-motion';
import { feedAPI } from '../../services/api';

interface CreatePostProps {
  onSuccess?: () => void;
}

export const CreatePost = ({ onSuccess }: CreatePostProps) => {
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim() || loading) return;

    const postContent = content.trim();
    setContent('');
    setLoading(true);
    setError(null);

    try {
      await feedAPI.createPost({ content: postContent });
      // Вызываем callback с новым постом для оптимистичного обновления
      onSuccess?.();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка создания поста');
      setContent(postContent); // Возвращаем текст при ошибке
    } finally {
      setLoading(false);
    }
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="card"
    >
      <h2 className="text-2xl font-semibold mb-4">Создать пост</h2>

      {error && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          className="mb-4 p-3 bg-red-50 text-red-600 rounded-xl text-sm"
        >
          {error}
        </motion.div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-2">Содержание поста</label>
          <textarea
            className="input-field min-h-[120px] resize-none"
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Что у вас нового?"
            required
          />
        </div>

        <button
          type="submit"
          className="btn-primary w-full"
          disabled={loading}
        >
          {loading ? 'Создание...' : 'Опубликовать'}
        </button>
      </form>
    </motion.div>
  );
};

