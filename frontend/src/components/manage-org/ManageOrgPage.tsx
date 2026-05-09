import { useState, useEffect } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { useAuthStore } from '../../stores/authStore';
import { groupsAPI, usersAPI } from '../../services/api';
import type { CompanyGroup, User } from '../../types';

export function ManageOrgPage() {
  const user = useAuthStore((s) => s.user);

  const [members, setMembers] = useState<User[]>([]);
  const [groups, setGroups] = useState<CompanyGroup[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addEmail, setAddEmail] = useState('');
  const [addPassword, setAddPassword] = useState('');
  const [addFirstName, setAddFirstName] = useState('');
  const [addLastName, setAddLastName] = useState('');
  const [addRole, setAddRole] = useState<'Worker' | 'Boss'>('Worker');
  const [addGroupIds, setAddGroupIds] = useState<number[]>([]);
  const [addLoading, setAddLoading] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  const [addSuccess, setAddSuccess] = useState<string | null>(null);

  const [importResults, setImportResults] = useState<{ email: string, password?: string, error?: string, success: boolean }[] | null>(null);
  const [importLoading, setImportLoading] = useState(false);

  const [selected, setSelected] = useState<User | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [passwordLoading, setPasswordLoading] = useState(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [groupName, setGroupName] = useState('');
  const [groupLeaderId, setGroupLeaderId] = useState<number | ''>('');
  const [groupMemberIds, setGroupMemberIds] = useState<number[]>([]);
  const [groupLoading, setGroupLoading] = useState(false);
  const [groupError, setGroupError] = useState<string | null>(null);
  const [groupSuccess, setGroupSuccess] = useState<string | null>(null);

  const isBoss = user?.role === 'Boss';

  async function load() {
    try {
      setLoading(true);
      setError(null);
      const [list, groupList] = await Promise.all([
        usersAPI.getMembers(),
        groupsAPI.getAll(),
      ]);
      setMembers(list);
      setGroups(groupList);
    } catch (e: unknown) {
      setError((e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? 'Ошибка загрузки');
    } finally {
      setLoading(false);
    }
  }

  async function loadSearch() {
    if (!search.trim()) {
      load();
      return;
    }
    try {
      setLoading(true);
      setError(null);
      const [list, groupList] = await Promise.all([
        usersAPI.search(search.trim()),
        groupsAPI.getAll(),
      ]);
      setMembers(list);
      setGroups(groupList);
    } catch (e: unknown) {
      setError((e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? 'Ошибка поиска');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!isBoss) return;
    if (!search.trim()) {
      load();
      return;
    }
    const t = setTimeout(() => loadSearch(), 300);
    return () => clearTimeout(t);
  }, [isBoss, search]);

  async function handleAdd(e: React.FormEvent) {
    e.preventDefault();
    setAddLoading(true);
    setAddError(null);
    setAddSuccess(null);
    try {
      await usersAPI.createMember({
        email: addEmail.trim(),
        password: addPassword,
        firstName: addFirstName.trim() || undefined,
        lastName: addLastName.trim() || undefined,
        role: addRole,
        groupIds: addGroupIds,
      });
      setAddSuccess('Участник добавлен.');
      setAddEmail('');
      setAddPassword('');
      setAddFirstName('');
      setAddLastName('');
      setAddRole('Worker');
      setAddGroupIds([]);
      await load();
    } catch (e: unknown) {
      setAddError((e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? 'Ошибка');
    } finally {
      setAddLoading(false);
    }
  }

  async function handleImportCSV(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setImportLoading(true);
    setImportResults(null);
    setAddError(null);

    const reader = new FileReader();
    reader.onload = async (event) => {
      try {
        const text = event.target?.result as string;
        const lines = text.split(/\r?\n/).filter(line => line.trim() !== '');

        // Skip header if exits (heuristic: if first line contains 'email')
        let startIndex = 0;
        if (lines[0].toLowerCase().includes('email')) {
          startIndex = 1;
        }

        const data = lines.slice(startIndex).map(line => {
          const [email, firstName, lastName] = line.split(/[,;]/).map(s => s.trim());
          return { email, firstName, lastName };
        }).filter(item => item.email);

        if (data.length === 0) {
          setAddError('Файл пуст или имеет неверный формат (ожидается: email, имя, фамилия)');
          setImportLoading(false);
          return;
        }

        const results = await usersAPI.importMembers(data);
        setImportResults(results);
        await load();
      } catch (err: any) {
        setAddError(err.response?.data?.message || 'Ошибка при импорте CSV');
      } finally {
        setImportLoading(false);
        // Reset file input
        e.target.value = '';
      }
    };
    reader.readAsText(file);
  }

  async function handleBlock(u: User) {
    try {
      await usersAPI.block(u.id);
      setSelected((s) => (s?.id === u.id ? { ...s, isBlocked: true } : s));
      await load();
    } catch {
      // ignore
    }
  }

  async function handleUnblock(u: User) {
    try {
      await usersAPI.unblock(u.id);
      setSelected((s) => (s?.id === u.id ? { ...s, isBlocked: false } : s));
      await load();
    } catch {
      // ignore
    }
  }

  async function handleSetPassword(e: React.FormEvent) {
    e.preventDefault();
    if (!selected || !newPassword.trim()) return;
    setPasswordLoading(true);
    setPasswordError(null);
    try {
      await usersAPI.setPassword(selected.id, newPassword);
      setNewPassword('');
      setPasswordError(null);
    } catch (e: unknown) {
      setPasswordError((e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? 'Ошибка');
    } finally {
      setPasswordLoading(false);
    }
  }

  async function handleCreateGroup(e: React.FormEvent) {
    e.preventDefault();
    if (!groupLeaderId) return;

    setGroupLoading(true);
    setGroupError(null);
    setGroupSuccess(null);
    try {
      await groupsAPI.create({
        name: groupName.trim(),
        leaderUserId: Number(groupLeaderId),
        memberIds: groupMemberIds,
      });
      setGroupSuccess('Группа создана, системный чат добавлен.');
      setGroupName('');
      setGroupLeaderId('');
      setGroupMemberIds([]);
      await load();
    } catch (e: unknown) {
      setGroupError((e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? 'Ошибка создания группы');
    } finally {
      setGroupLoading(false);
    }
  }

  function toggleNumber(list: number[], id: number) {
    return list.includes(id) ? list.filter((x) => x !== id) : [...list, id];
  }

  if (!user) return null;
  if (!isBoss) return <Navigate to="/" replace />;

  return (
    <div className="min-h-screen bg-apple-gray dark:bg-gray-900 p-4 sm:p-6">
      <div className="max-w-3xl mx-auto space-y-4 sm:space-y-6">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 sm:gap-4">
          <h1 className="text-xl sm:text-2xl font-bold dark:text-white">Управление организацией</h1>
          <Link to="/" className="text-apple-blue hover:underline text-sm font-medium">
            ← Вернуться в чат
          </Link>
        </div>

        <motion.section
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 p-6 shadow-sm"
        >
          <h2 className="text-lg font-medium dark:text-white mb-4">Добавить участника</h2>
          {addError && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-xl text-sm">
              {addError}
            </div>
          )}
          {addSuccess && (
            <div className="mb-4 p-3 bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 rounded-xl text-sm">
              {addSuccess}
            </div>
          )}
          <form onSubmit={handleAdd} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Email</label>
              <input
                type="email"
                className="input-field"
                value={addEmail}
                onChange={(e) => setAddEmail(e.target.value)}
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Пароль</label>
              <input
                type="password"
                className="input-field"
                value={addPassword}
                onChange={(e) => setAddPassword(e.target.value)}
                required
                minLength={6}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Имя</label>
              <input
                type="text"
                className="input-field"
                value={addFirstName}
                onChange={(e) => setAddFirstName(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Фамилия</label>
              <input
                type="text"
                className="input-field"
                value={addLastName}
                onChange={(e) => setAddLastName(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Роль</label>
              <select
                className="input-field"
                value={addRole}
                onChange={(e) => setAddRole(e.target.value as 'Worker' | 'Boss')}
              >
                <option value="Worker">Сотрудник</option>
                <option value="Boss">Boss / админ</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Группы</label>
              <div className="min-h-[42px] rounded-xl border border-gray-200 dark:border-gray-700 px-3 py-2 bg-white dark:bg-gray-900">
                {groups.length === 0 ? (
                  <span className="text-sm text-gray-400">Групп пока нет</span>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    {groups.map((group) => (
                      <label key={group.id} className="inline-flex items-center gap-2 text-sm dark:text-gray-200">
                        <input
                          type="checkbox"
                          checked={addGroupIds.includes(group.id)}
                          onChange={() => setAddGroupIds((ids) => toggleNumber(ids, group.id))}
                        />
                        {group.name}
                      </label>
                    ))}
                  </div>
                )}
              </div>
            </div>
            <div className="sm:col-span-2 flex flex-col sm:flex-row gap-4">
              <button type="submit" className="btn-primary flex-1" disabled={addLoading || importLoading}>
                {addLoading ? 'Добавление...' : 'Добавить'}
              </button>
              <label className={`btn-secondary flex-1 flex items-center justify-center cursor-pointer ${importLoading ? 'opacity-50 cursor-not-allowed' : ''}`}>
                <input
                  type="file"
                  accept=".csv,text/csv"
                  className="hidden"
                  onChange={handleImportCSV}
                  disabled={importLoading}
                />
                {importLoading ? 'Импорт...' : 'Импорт из CSV'}
              </label>
            </div>
          </form>

          <AnimatePresence>
            {importResults && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                className="mt-6 overflow-hidden"
              >
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-sm font-semibold dark:text-white uppercase tracking-wider">Результаты импорта</h3>
                  <button
                    onClick={() => setImportResults(null)}
                    className="text-xs text-gray-500 hover:text-apple-blue transition-colors"
                  >
                    Скрыть
                  </button>
                </div>
                <div className="overflow-x-auto rounded-xl border border-gray-100 dark:border-gray-700">
                  <table className="w-full text-sm text-left">
                    <thead className="bg-gray-50 dark:bg-gray-700/50 text-gray-600 dark:text-gray-300">
                      <tr>
                        <th className="px-4 py-2 font-medium">Email</th>
                        <th className="px-4 py-2 font-medium">Пароль</th>
                        <th className="px-4 py-2 font-medium">Статус</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
                      {importResults.map((res, i) => (
                        <tr key={res.email + i} className="dark:text-gray-200">
                          <td className="px-4 py-2 font-mono text-xs">{res.email}</td>
                          <td className="px-4 py-2">
                            {res.password ? (
                              <span className="bg-blue-50 dark:bg-blue-900/30 text-apple-blue dark:text-blue-300 px-2 py-0.5 rounded font-mono select-all">
                                {res.password}
                              </span>
                            ) : '—'}
                          </td>
                          <td className="px-4 py-2">
                            {res.success ? (
                              <span className="text-green-600 dark:text-green-400">Успех</span>
                            ) : (
                              <span className="text-red-600 dark:text-red-400 text-xs" title={res.error}>Ошибка</span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <p className="mt-3 text-xs text-gray-500 dark:text-gray-400">
                  ⚠️ Обязательно скопируйте пароли сейчас. Они не будут показаны повторно.
                </p>
              </motion.div>
            )}
          </AnimatePresence>
        </motion.section>

        <motion.section
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.03 }}
          className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 p-6 shadow-sm"
        >
          <h2 className="text-lg font-medium dark:text-white mb-4">Группы и отделы</h2>
          {groupError && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-xl text-sm">
              {groupError}
            </div>
          )}
          {groupSuccess && (
            <div className="mb-4 p-3 bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 rounded-xl text-sm">
              {groupSuccess}
            </div>
          )}
          <form onSubmit={handleCreateGroup} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Название</label>
              <input
                type="text"
                className="input-field"
                value={groupName}
                onChange={(e) => setGroupName(e.target.value)}
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Начальник группы</label>
              <select
                className="input-field"
                value={groupLeaderId}
                onChange={(e) => setGroupLeaderId(e.target.value ? Number(e.target.value) : '')}
                required
              >
                <option value="">Выберите сотрудника</option>
                {members.map((member) => (
                  <option key={member.id} value={member.id}>
                    {[member.firstName, member.lastName].filter(Boolean).join(' ') || member.email}
                  </option>
                ))}
              </select>
            </div>
            <div className="sm:col-span-2">
              <label className="block text-sm font-medium mb-1 dark:text-gray-300">Участники</label>
              <div className="max-h-40 overflow-y-auto rounded-xl border border-gray-200 dark:border-gray-700 p-3 bg-white dark:bg-gray-900">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {members.map((member) => (
                    <label key={member.id} className="inline-flex items-center gap-2 text-sm dark:text-gray-200">
                      <input
                        type="checkbox"
                        checked={groupMemberIds.includes(member.id)}
                        onChange={() => setGroupMemberIds((ids) => toggleNumber(ids, member.id))}
                      />
                      <span className="truncate">
                        {[member.firstName, member.lastName].filter(Boolean).join(' ') || member.email}
                      </span>
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <button type="submit" className="btn-primary sm:col-span-2" disabled={groupLoading || !groupName.trim() || !groupLeaderId}>
              {groupLoading ? 'Создание...' : 'Создать группу'}
            </button>
          </form>

          {groups.length > 0 && (
            <div className="mt-6 space-y-2">
              {groups.map((group) => {
                const leader = members.find((member) => member.id === group.leaderUserId);
                return (
                  <div key={group.id} className="rounded-xl border border-gray-100 dark:border-gray-700 p-3">
                    <div className="font-medium dark:text-white">{group.name}</div>
                    <div className="text-sm text-gray-500 dark:text-gray-400">
                      Начальник: {[leader?.firstName, leader?.lastName].filter(Boolean).join(' ') || leader?.email || group.leaderUserId}
                    </div>
                    <div className="text-xs text-gray-400 mt-1">
                      {group.memberIds.length} участников · чат {group.chatId}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </motion.section>

        <motion.section
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.05 }}
          className="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 p-6 shadow-sm"
        >
          <h2 className="text-lg font-medium dark:text-white mb-4">Члены организации</h2>
          <div className="mb-4">
            <input
              type="text"
              placeholder="Поиск по имени или email..."
              className="input-field"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          {error && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-xl text-sm">
              {error}
            </div>
          )}
          {loading ? (
            <div className="flex justify-center py-8">
              <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-apple-blue" />
            </div>
          ) : (
            <ul className="space-y-2">
              {members.map((u) => (
                <li
                  key={u.id}
                  className="flex items-center justify-between p-3 rounded-xl border border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50"
                >
                  <button
                    type="button"
                    className="text-left flex-1 min-w-0"
                    onClick={() => setSelected(u)}
                  >
                    <div className="font-medium dark:text-white truncate">
                      {[u.firstName, u.lastName].filter(Boolean).join(' ') || u.email}
                    </div>
                    <div className="text-sm text-gray-500 dark:text-gray-400 truncate">{u.email}</div>
                    <div className="flex gap-2 mt-1">
                      {u.role && (
                        <span className="text-xs px-2 py-0.5 rounded bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300">
                          {u.role}
                        </span>
                      )}
                      {u.isBlocked && (
                        <span className="text-xs px-2 py-0.5 rounded bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400">
                          Заблокирован
                        </span>
                      )}
                    </div>
                  </button>
                  <div className="flex gap-2 shrink-0">
                    <button
                      type="button"
                      className="text-sm text-apple-blue hover:underline"
                      onClick={() => setSelected(u)}
                    >
                      Редактировать
                    </button>
                    {u.id !== user.id && (
                      u.isBlocked ? (
                        <button
                          type="button"
                          className="text-sm text-green-600 dark:text-green-400 hover:underline"
                          onClick={() => handleUnblock(u)}
                        >
                          Разблокировать
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="text-sm text-red-600 dark:text-red-400 hover:underline"
                          onClick={() => handleBlock(u)}
                        >
                          Заблокировать
                        </button>
                      )
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </motion.section>
      </div>

      <AnimatePresence>
        {selected && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4"
            onClick={() => setSelected(null)}
          >
            <motion.div
              initial={{ scale: 0.95, opacity: 0 }}
              animate={{ scale: 1, opacity: 1 }}
              exit={{ scale: 0.95, opacity: 0 }}
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-xl max-w-md w-full p-6"
              onClick={(e) => e.stopPropagation()}
            >
              <h3 className="text-lg font-semibold dark:text-white mb-4">
                {[selected.firstName, selected.lastName].filter(Boolean).join(' ') || selected.email}
              </h3>
              <div className="space-y-2 text-sm text-gray-600 dark:text-gray-400 mb-6">
                <p>Email: {selected.email}</p>
                <p>Роль: {selected.role ?? '—'}</p>
                <p>Статус: {selected.isBlocked ? 'Заблокирован' : 'Активен'}</p>
                {selected.createdAt && (
                  <p>Добавлен: {new Date(selected.createdAt).toLocaleDateString('ru-RU')}</p>
                )}
              </div>

              {selected.id !== user.id && (
                <form onSubmit={handleSetPassword} className="space-y-3 mb-6">
                  <label className="block text-sm font-medium dark:text-gray-300">Изменить пароль</label>
                  <input
                    type="password"
                    placeholder="Новый пароль"
                    className="input-field"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    minLength={6}
                  />
                  {passwordError && (
                    <p className="text-sm text-red-600 dark:text-red-400">{passwordError}</p>
                  )}
                  <button type="submit" className="btn-primary text-sm" disabled={passwordLoading || !newPassword.trim()}>
                    {passwordLoading ? 'Сохранение...' : 'Сохранить пароль'}
                  </button>
                </form>
              )}

              <div className="flex gap-2">
                {selected.id !== user.id && (
                  selected.isBlocked ? (
                    <button
                      type="button"
                      className="btn-secondary flex-1 text-sm"
                      onClick={() => handleUnblock(selected)}
                    >
                      Разблокировать
                    </button>
                  ) : (
                    <button
                      type="button"
                      className="px-4 py-2 rounded-full border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 text-sm"
                      onClick={() => handleBlock(selected)}
                    >
                      Заблокировать
                    </button>
                  )
                )}
                <button
                  type="button"
                  className="btn-secondary flex-1 text-sm"
                  onClick={() => setSelected(null)}
                >
                  Закрыть
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
