import { useEffect, useState } from 'react';
import { useTaskStore } from '../../stores/taskStore';
import { useAuthStore } from '../../stores/authStore';
import { TaskModal } from './TaskModal';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import { notificationAPI } from '../../services/api';

export const TasksPage = () => {
  const { tasks, fetchTasks, updateTaskStatus, toggleChecklistItem, deleteTask, isLoading } = useTaskStore();
  const user = useAuthStore((state) => state.user);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [filter, setFilter] = useState<'all' | 'my' | 'assigned'>('all');

  useEffect(() => {
    fetchTasks();
    // Reset unread tasks counter
    notificationAPI.markTasksAsRead();
  }, [fetchTasks]);

  const filteredTasks = tasks.filter(task => {
    if (filter === 'my') return task.creatorId === user?.id;
    if (filter === 'assigned') return task.targetUserId === user?.id || task.targetGroupId;
    return true;
  });

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'Urgent': return 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400';
      case 'High': return 'bg-orange-100 text-orange-600 dark:bg-orange-900/30 dark:text-orange-400';
      case 'Medium': return 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400';
      default: return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400';
    }
  };

  return (
    <div className="p-4 sm:p-6 max-w-5xl mx-auto h-full flex flex-col">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-1">Задачи</h1>
          <p className="text-gray-500 dark:text-gray-400">Управляйте командными задачами и чек-листами</p>
        </div>
        
        <div className="flex items-center gap-3">
          {(user?.role === 'Boss' || user?.role === 'Leader') && (
            <button
              onClick={() => setIsModalOpen(true)}
              className="bg-apple-blue hover:bg-blue-600 text-white px-5 py-2.5 rounded-xl font-medium transition-all shadow-lg shadow-blue-500/20 flex items-center gap-2"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              Новая задача
            </button>
          )}
        </div>
      </div>

      <div className="flex bg-gray-100 dark:bg-gray-800 p-1 rounded-xl w-fit mb-6">
        <button
          onClick={() => setFilter('all')}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${filter === 'all' ? 'bg-white dark:bg-gray-700 shadow-sm text-apple-blue' : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'}`}
        >
          Все
        </button>
        <button
          onClick={() => setFilter('my')}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${filter === 'my' ? 'bg-white dark:bg-gray-700 shadow-sm text-apple-blue' : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'}`}
        >
          Созданные мной
        </button>
        <button
          onClick={() => setFilter('assigned')}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${filter === 'assigned' ? 'bg-white dark:bg-gray-700 shadow-sm text-apple-blue' : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'}`}
        >
          Назначенные мне
        </button>
      </div>

      {isLoading ? (
        <div className="flex-1 flex items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
        </div>
      ) : filteredTasks.length === 0 ? (
        <div className="flex-1 flex flex-col items-center justify-center text-center opacity-60">
          <div className="w-20 h-20 bg-gray-200 dark:bg-gray-800 rounded-full flex items-center justify-center mb-4">
             <svg className="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
               <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
             </svg>
          </div>
          <h3 className="text-xl font-semibold mb-1">Задач пока нет</h3>
          <p>Когда появятся задачи, они отобразятся здесь</p>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto pr-2 space-y-4 no-scrollbar">
          {filteredTasks.map((task) => (
            <div key={task.id} className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 p-5 shadow-sm hover:shadow-md transition-all group">
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <span className={`text-[10px] uppercase tracking-wider font-bold px-2 py-0.5 rounded-full ${getPriorityColor(task.priority)}`}>
                      {task.priority === 'Urgent' ? 'Срочно' : 
                       task.priority === 'High' ? 'Высокий' : 
                       task.priority === 'Medium' ? 'Средний' : 'Низкий'}
                    </span>
                    <span className="text-[10px] uppercase tracking-wider font-bold bg-purple-100 text-purple-600 dark:bg-purple-900/30 dark:text-purple-400 px-2 py-0.5 rounded-full">
                      {task.type === 'Checklist' ? 'Чек-лист' : 'Задача'}
                    </span>
                  </div>
                  <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-2 leading-tight">{task.title}</h3>
                  <p className="text-gray-600 dark:text-gray-400 text-sm mb-4 line-clamp-2">{task.description}</p>
                  
                  {task.checklistItems && task.checklistItems.length > 0 && (
                    <div className="bg-gray-50 dark:bg-gray-900/50 rounded-xl p-3 mb-4 space-y-2">
                      {task.checklistItems.map((item) => (
                        <div 
                          key={item.id} 
                          className="flex items-center gap-3 cursor-pointer group/item"
                          onClick={() => toggleChecklistItem(task.id, item.id)}
                        >
                          <div className={`w-5 h-5 rounded border flex items-center justify-center transition-all ${item.isCompleted ? 'bg-green-500 border-green-500' : 'border-gray-300 dark:border-gray-600'}`}>
                            {item.isCompleted && (
                              <svg className="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                              </svg>
                            )}
                          </div>
                          <span className={`text-sm transition-all ${item.isCompleted ? 'text-gray-400 line-through' : 'text-gray-700 dark:text-gray-300'}`}>
                            {item.content}
                          </span>
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-xs text-gray-500 dark:text-gray-400">
                    <div className="flex items-center gap-1.5">
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      {format(new Date(task.createdAt), 'd MMMM, HH:mm', { locale: ru })}
                    </div>
                    {task.dueDate && (
                      <div className={`flex items-center gap-1.5 ${new Date(task.dueDate) < new Date() && task.status !== 'Done' ? 'text-red-500 font-semibold' : ''}`}>
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        До {format(new Date(task.dueDate), 'd MMMM', { locale: ru })}
                      </div>
                    )}
                  </div>
                </div>

                <div className="flex flex-col items-end gap-2">
                   <select 
                    value={task.status}
                    onChange={(e) => updateTaskStatus(task.id, e.target.value as any)}
                    className="text-xs font-bold bg-gray-50 dark:bg-gray-900 border-none rounded-lg px-3 py-2 outline-none focus:ring-1 ring-apple-blue transition-all"
                   >
                     <option value="Todo">К выполнению</option>
                     <option value="InProgress">В процессе</option>
                     <option value="Done">Завершено</option>
                     <option value="Cancelled">Отменено</option>
                   </select>

                   {(user?.role === 'Boss' || task.creatorId === user?.id) && (
                     <button
                       onClick={() => deleteTask(task.id)}
                       className="p-2 text-gray-400 hover:text-red-500 transition-colors opacity-0 group-hover:opacity-100"
                     >
                       <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                         <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                       </svg>
                     </button>
                   )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {isModalOpen && (
        <TaskModal onClose={() => setIsModalOpen(false)} />
      )}
    </div>
  );
};
