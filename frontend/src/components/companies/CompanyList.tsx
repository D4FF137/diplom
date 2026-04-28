import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { companiesAPI } from '../../services/api';
import type { Company } from '../../types';

interface CompanyListProps {
  refreshTrigger?: number;
}

export const CompanyList = ({ refreshTrigger }: CompanyListProps) => {
  const [companies, setCompanies] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadCompanies();
  }, [refreshTrigger]);

  const loadCompanies = async () => {
    try {
      setLoading(true);
      const data = await companiesAPI.getAll();
      setCompanies(data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Ошибка загрузки компаний');
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
    <div className="space-y-4">
      {companies.map((company, index) => (
        <motion.div
          key={company.id}
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: index * 0.1 }}
          className="card"
        >
          <h3 className="text-xl font-semibold mb-2">{company.name}</h3>
          <p className="text-sm text-gray-500">
            Создана: {new Date(company.createdAt).toLocaleDateString('ru-RU')}
          </p>
        </motion.div>
      ))}
    </div>
  );
};

