import { useState } from 'react';
import { motion } from 'framer-motion';
import { chatAPI } from '../../services/api';
import type { Chat } from '../../types';

interface CreateChatProps {
  onCreateSuccess?: (chat: Chat) => void;
  onSuccess?: (chat: Chat) => void;
}

export const CreateChat = ({ onCreateSuccess, onSuccess }: CreateChatProps) => {
  const [name, setName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const chat = await chatAPI.createChat({ name });
      setName('');
      onCreateSuccess?.(chat);
      onSuccess?.(chat);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка создания чата');
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
      <h2 className="text-xl font-semibold mb-4">Создать чат</h2>

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
          <label className="block text-sm font-medium mb-2">Название чата</label>
          <input
            type="text"
            className="input-field"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>

        <button
          type="submit"
          className="btn-primary w-full"
          disabled={loading}
        >
          {loading ? 'Создание...' : 'Создать'}
        </button>
      </form>
    </motion.div>
  );
};

