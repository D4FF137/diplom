import React, { useState, useEffect } from 'react';
import { useTaskStore } from '../../stores/taskStore';
import { groupsAPI, usersAPI } from '../../services/api';
import type { CompanyGroup, User } from '../../types';

interface TaskModalProps {
  onClose: () => void;
}

export const TaskModal: React.FC<TaskModalProps> = ({ onClose }) => {
  const { createTask } = useTaskStore();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState<'Simple' | 'Checklist'>('Simple');
  const [priority, setPriority] = useState('Medium');
  const [targetType, setTargetType] = useState<'all' | 'group' | 'user'>('all');
  const [targetId, setTargetId] = useState<number | ''>('');
  const [dueDate, setDueDate] = useState('');
  const [checklistItems, setChecklistItems] = useState(['']);
  
  const [groups, setGroups] = useState<CompanyGroup[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [groupsData, usersData] = await Promise.all([
          groupsAPI.getAll(),
          usersAPI.getMembers()
        ]);
        setGroups(groupsData);
        setUsers(usersData);
      } catch (err) {
        console.error('Failed to fetch modal data', err);
      }
    };
    fetchData();
  }, []);

  const handleAddItem = () => setChecklistItems([...checklistItems, '']);
  const handleItemChange = (index: number, value: string) => {
    const newItems = [...checklistItems];
    newItems[index] = value;
    setChecklistItems(newItems);
  };
  const handleRemoveItem = (index: number) => {
    setChecklistItems(checklistItems.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      await createTask({
        title,
        description,
        type,
        priority,
        targetGroupId: targetType === 'group' ? targetId : undefined,
        targetUserId: targetType === 'user' ? targetId : undefined,
        dueDate: dueDate || undefined,
        checklistItems: type === 'Checklist' ? checklistItems.filter(i => i.trim()) : undefined
      });
      onClose();
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/40 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-white dark:bg-gray-800 w-full max-w-xl rounded-3xl shadow-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        <div className="px-6 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between">
          <h2 className="text-xl font-bold text-gray-900 dark:text-white">Новая задача</h2>
          <button onClick={onClose} className="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full transition-colors">
            <svg className="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 overflow-y-auto max-h-[80vh] no-scrollbar">
          <div className="space-y-5">
            <div>
              <label className="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Название</label>
              <input
                required
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Что нужно сделать?"
                className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-xl px-4 py-3 outline-none focus:ring-2 ring-apple-blue transition-all"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Описание</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Добавьте деталей..."
                rows={3}
                className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-xl px-4 py-3 outline-none focus:ring-2 ring-apple-blue transition-all resize-none"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Тип</label>
                <select 
                  value={type} 
                  onChange={(e) => setType(e.target.value as any)}
                  className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-xl px-4 py-3 outline-none focus:ring-2 ring-apple-blue transition-all"
                >
                  <option value="Simple">Обычная</option>
                  <option value="Checklist">Чек-лист</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Приоритет</label>
                <select 
                  value={priority} 
                  onChange={(e) => setPriority(e.target.value)}
                  className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-xl px-4 py-3 outline-none focus:ring-2 ring-apple-blue transition-all"
                >
                  <option value="Low">Низкий</option>
                  <option value="Medium">Средний</option>
                  <option value="High">Высокий</option>
                  <option value="Urgent">Срочно</option>
                </select>
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Кому назначить</label>
              <div className="flex gap-2 mb-3">
                <button
                  type="button"
                  onClick={() => setTargetType('all')}
                  className={`flex-1 py-2 rounded-lg text-xs font-bold transition-all ${targetType === 'all' ? 'bg-apple-blue text-white' : 'bg-gray-100 dark:bg-gray-700 text-gray-500'}`}
                >
                  Всем
                </button>
                <button
                  type="button"
                  onClick={() => setTargetType('group')}
                  className={`flex-1 py-2 rounded-lg text-xs font-bold transition-all ${targetType === 'group' ? 'bg-apple-blue text-white' : 'bg-gray-100 dark:bg-gray-700 text-gray-500'}`}
                >
                  Группе
                </button>
                <button
                  type="button"
                  onClick={() => setTargetType('user')}
                  className={`flex-1 py-2 rounded-lg text-xs font-bold transition-all ${targetType === 'user' ? 'bg-apple-blue text-white' : 'bg-gray-100 dark:bg-gray-700 text-gray-500'}`}
                >
                  Сотруднику
                </button>
              </div>
              
              {targetType !== 'all' && (
                <select 
                  required
                  value={targetId}
                  onChange={(e) => setTargetId(Number(e.target.value))}
                  className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-xl px-4 py-3 outline-none focus:ring-2 ring-apple-blue transition-all"
                >
                  <option value="">Выберите {targetType === 'group' ? 'группу' : 'сотрудника'}</option>
                  {targetType === 'group' ? groups.map(g => (
                    <option key={g.id} value={g.id}>{g.name}</option>
                  )) : users.map(u => (
                    <option key={u.id} value={u.id}>{u.firstName} {u.lastName} ({u.email})</option>
                  ))}
                </select>
              )}
            </div>

            <div>
              <label className="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Срок выполнения (опционально)</label>
              <input
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
                className="w-full bg-gray-50 dark:bg-gray-900 border-none rounded-xl px-4 py-3 outline-none focus:ring-2 ring-apple-blue transition-all"
              />
            </div>

            {type === 'Checklist' && (
              <div>
                <label className="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Пункты чек-листа</label>
                <div className="space-y-2">
                  {checklistItems.map((item, index) => (
                    <div key={index} className="flex gap-2">
                      <input
                        value={item}
                        onChange={(e) => handleItemChange(index, e.target.value)}
                        placeholder={`Пункт ${index + 1}`}
                        className="flex-1 bg-gray-50 dark:bg-gray-900 border-none rounded-xl px-4 py-2 outline-none focus:ring-2 ring-apple-blue transition-all"
                      />
                      <button 
                        type="button" 
                        onClick={() => handleRemoveItem(index)}
                        className="p-2 text-gray-400 hover:text-red-500 transition-colors"
                      >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </div>
                  ))}
                  <button
                    type="button"
                    onClick={handleAddItem}
                    className="w-full py-2 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-xl text-sm font-medium text-gray-500 hover:border-apple-blue hover:text-apple-blue transition-all"
                  >
                    + Добавить пункт
                  </button>
                </div>
              </div>
            )}
          </div>

          <div className="mt-8 flex gap-3">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-3 rounded-xl font-bold bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600 transition-all"
            >
              Отмена
            </button>
            <button
              disabled={isLoading}
              className="flex-1 py-3 rounded-xl font-bold bg-apple-blue text-white shadow-lg shadow-blue-500/30 hover:bg-blue-600 disabled:opacity-50 transition-all"
            >
              {isLoading ? 'Создание...' : 'Создать задачу'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
