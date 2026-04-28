import { useState, useEffect, useRef } from 'react';
import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import { adminAPI, companiesAPI } from '../../services/api';
import type { Company } from '../../types';

export function AdminPage() {
  const [secret, setSecret] = useState('');
  const [secretApplied, setSecretApplied] = useState(false);
  const appliedSecretRef = useRef<string>('');

  const [companyName, setCompanyName] = useState('');
  const [companyLoading, setCompanyLoading] = useState(false);
  const [companyError, setCompanyError] = useState<string | null>(null);
  const [companySuccess, setCompanySuccess] = useState<string | null>(null);

  const [companies, setCompanies] = useState<Company[]>([]);
  const [companiesLoading, setCompaniesLoading] = useState(false);
  const [selectedCompanyId, setSelectedCompanyId] = useState<number | ''>('');
  const [bossEmail, setBossEmail] = useState('');
  const [bossPassword, setBossPassword] = useState('');
  const [bossFirstName, setBossFirstName] = useState('');
  const [bossLastName, setBossLastName] = useState('');
  const [bossLoading, setBossLoading] = useState(false);
  const [bossError, setBossError] = useState<string | null>(null);
  const [bossSuccess, setBossSuccess] = useState<string | null>(null);

  const envSecret = (import.meta.env.VITE_ADMIN_SECRET as string) ?? '';
  const effectiveSecret = secretApplied ? appliedSecretRef.current : envSecret;
  const hasSecret = effectiveSecret.length > 0;

  useEffect(() => {
    loadCompanies();
  }, []);

  const loadCompanies = async () => {
    try {
      setCompaniesLoading(true);
      const list = await companiesAPI.getAll();
      setCompanies(list);
      if (list.length && selectedCompanyId === '') {
        setSelectedCompanyId(list[0].id);
      }
    } catch {
      setCompanies([]);
    } finally {
      setCompaniesLoading(false);
    }
  };

  const applySecret = () => {
    const s = secret.trim();
    if (s) {
      appliedSecretRef.current = s;
      setSecretApplied(true);
      setCompanyError(null);
      setBossError(null);
    }
  };

  const handleCreateCompany = async (e: React.FormEvent) => {
    e.preventDefault();
    setCompanyLoading(true);
    setCompanyError(null);
    setCompanySuccess(null);
    try {
      const created = await adminAPI.createCompany(
        { name: companyName.trim() },
        effectiveSecret
      );
      setCompanySuccess(`Организация «${created.name}» создана (ID: ${created.id}).`);
      setCompanyName('');
      await loadCompanies();
    } catch (err: any) {
      setCompanyError(err.response?.data?.message || err.response?.data?.errors || 'Ошибка создания организации');
    } finally {
      setCompanyLoading(false);
    }
  };

  const handleCreateBoss = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedCompanyId === '') return;
    setBossLoading(true);
    setBossError(null);
    setBossSuccess(null);
    try {
      const created = await adminAPI.createBoss(
        Number(selectedCompanyId),
        {
          email: bossEmail.trim(),
          password: bossPassword,
          firstName: bossFirstName.trim() || undefined,
          lastName: bossLastName.trim() || undefined,
        },
        effectiveSecret
      );
      const company = companies.find((c) => c.id === created.companyId);
      setBossSuccess(
        `Начальник создан: ${created.email} (${created.firstName} ${created.lastName}). ` +
          `Организация: ${company?.name ?? created.companyId}. Логин: ${created.email}, пароль — тот, что ввели.`
      );
      setBossEmail('');
      setBossPassword('');
      setBossFirstName('');
      setBossLastName('');
    } catch (err: any) {
      setBossError(err.response?.data?.message || 'Ошибка создания начальника');
    } finally {
      setBossLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-apple-gray p-6 dark:bg-gray-900">
      <div className="max-w-2xl mx-auto space-y-8">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold text-apple-dark dark:text-white">Админ-панель</h1>
          <Link
            to="/"
            className="text-apple-blue hover:underline text-sm"
          >
            ← На главную
          </Link>
        </div>

        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          className="card dark:bg-gray-800 dark:border-gray-700"
        >
          <h2 className="text-lg font-medium mb-3 dark:text-white">Секрет админки</h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-3">
            Задайте <code className="bg-gray-100 dark:bg-gray-700 px-1 rounded">VITE_ADMIN_SECRET</code> в <code className="bg-gray-100 dark:bg-gray-700 px-1 rounded">frontend/.env</code> или введите ниже (то же значение, что <code className="bg-gray-100 dark:bg-gray-700 px-1 rounded">ADMIN_SECRET</code> в backend).
          </p>
          <div className="flex gap-2">
            <input
              type="password"
              className="input-field flex-1"
              placeholder="Секрет (как ADMIN_SECRET в backend)"
              value={secret}
              onChange={(e) => {
                setSecret(e.target.value);
                setSecretApplied(false);
                appliedSecretRef.current = '';
              }}
            />
            <button
              type="button"
              className="btn-primary whitespace-nowrap"
              onClick={applySecret}
              disabled={!secret.trim()}
            >
              Применить
            </button>
          </div>
          {secretApplied && (
            <p className="mt-2 text-sm text-green-600 dark:text-green-400">Секрет применён. Можно создавать организации и начальников.</p>
          )}
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="card dark:bg-gray-800 dark:border-gray-700"
        >
          <h2 className="text-lg font-medium mb-4 dark:text-white">Добавить организацию</h2>
          {companyError && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-xl text-sm">
              {typeof companyError === 'string' ? companyError : JSON.stringify(companyError)}
            </div>
          )}
          {companySuccess && (
            <div className="mb-4 p-3 bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 rounded-xl text-sm">
              {companySuccess}
            </div>
          )}
          <form onSubmit={handleCreateCompany} className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2 dark:text-gray-300">Название</label>
              <input
                type="text"
                className="input-field"
                value={companyName}
                onChange={(e) => setCompanyName(e.target.value)}
                required
                disabled={!hasSecret}
              />
            </div>
            <button
              type="submit"
              className="btn-primary w-full"
              disabled={companyLoading || !hasSecret}
            >
              {companyLoading ? 'Создание...' : 'Создать'}
            </button>
          </form>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="card dark:bg-gray-800 dark:border-gray-700"
        >
          <h2 className="text-lg font-medium mb-4 dark:text-white">Создать начальника организации</h2>
          {bossError && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-xl text-sm">
              {bossError}
            </div>
          )}
          {bossSuccess && (
            <div className="mb-4 p-3 bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 rounded-xl text-sm">
              {bossSuccess}
            </div>
          )}
          <form onSubmit={handleCreateBoss} className="space-y-4">
            <div>
              <label className="block text-sm font-medium mb-2 dark:text-gray-300">Организация</label>
              <select
                className="input-field"
                value={selectedCompanyId}
                onChange={(e) => setSelectedCompanyId(e.target.value === '' ? '' : Number(e.target.value))}
                required
                disabled={!hasSecret || companiesLoading}
              >
                <option value="">— выбрать —</option>
                {companies.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium mb-2 dark:text-gray-300">Email (логин)</label>
              <input
                type="email"
                className="input-field"
                value={bossEmail}
                onChange={(e) => setBossEmail(e.target.value)}
                required
                disabled={!hasSecret}
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-2 dark:text-gray-300">Пароль</label>
              <input
                type="password"
                className="input-field"
                value={bossPassword}
                onChange={(e) => setBossPassword(e.target.value)}
                required
                minLength={6}
                disabled={!hasSecret}
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-2 dark:text-gray-300">Имя</label>
                <input
                  type="text"
                  className="input-field"
                  value={bossFirstName}
                  onChange={(e) => setBossFirstName(e.target.value)}
                  disabled={!hasSecret}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 dark:text-gray-300">Фамилия</label>
                <input
                  type="text"
                  className="input-field"
                  value={bossLastName}
                  onChange={(e) => setBossLastName(e.target.value)}
                  disabled={!hasSecret}
                />
              </div>
            </div>
            <button
              type="submit"
              className="btn-primary w-full"
              disabled={bossLoading || !hasSecret || selectedCompanyId === ''}
            >
              {bossLoading ? 'Создание...' : 'Создать начальника'}
            </button>
          </form>
        </motion.div>
      </div>
    </div>
  );
}
