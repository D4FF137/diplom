import { useState, useEffect, useCallback } from 'react';
import { storageAPI } from '../../services/api';
import type { FileMetadata } from '../../types';
import { FileCard } from './FileCard';
import { AnimatePresence } from 'framer-motion';

export const StoragePage = () => {
    const [files, setFiles] = useState<FileMetadata[]>([]);
    const [loading, setLoading] = useState(true);
    const [category, setCategory] = useState<'shared' | 'private' | 'important'>('shared');
    const [typeFilter, setTypeFilter] = useState<string>('');
    const [search, setSearch] = useState('');
    const [isUploading, setIsUploading] = useState(false);

    const loadFiles = useCallback(async () => {
        setLoading(true);
        try {
            const data = await storageAPI.getFiles({
                category,
                type: typeFilter || undefined,
                search: search || undefined
            });
            setFiles(data);
        } catch (error) {
            console.error('Error loading files:', error);
        } finally {
            setLoading(false);
        }
    }, [category, typeFilter, search]);

    useEffect(() => {
        loadFiles();
    }, [loadFiles]);

    const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
        if (!e.target.files?.[0]) return;

        setIsUploading(true);
        try {
            const file = e.target.files[0];
            await storageAPI.uploadFile(file, category === 'private');
            loadFiles();
        } catch (error) {
            console.error('Error uploading file:', error);
            alert('Ошибка при загрузке файла');
        } finally {
            setIsUploading(false);
            e.target.value = '';
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Вы уверены, что хотите удалить этот файл?')) return;
        try {
            await storageAPI.deleteFile(id);
            setFiles(prev => prev.filter(f => f.id !== id));
        } catch (error) {
            console.error('Error deleting file:', error);
        }
    };

    const handleToggleImportant = async (id: string, currentStatus: boolean) => {
        try {
            await storageAPI.toggleImportant(id, !currentStatus);
            setFiles(prev => prev.map(f => f.id === id ? { ...f, isImportant: !currentStatus } : f));
        } catch (error) {
            console.error('Error toggling important status:', error);
        }
    };

    return (
        <div className="flex flex-col h-full bg-apple-gray dark:bg-gray-900 overflow-hidden">
            {/* Toolbar */}
            <div className="p-4 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 flex flex-wrap gap-4 items-center justify-between sticky top-0 z-10">
                <div className="flex bg-gray-100 dark:bg-gray-700 p-1 rounded-xl">
                    <button
                        onClick={() => setCategory('shared')}
                        className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-all ${category === 'shared' ? 'bg-white dark:bg-gray-600 shadow-sm text-apple-blue' : 'text-gray-500'}`}
                    >
                        Общие
                    </button>
                    <button
                        onClick={() => setCategory('private')}
                        className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-all ${category === 'private' ? 'bg-white dark:bg-gray-600 shadow-sm text-apple-blue' : 'text-gray-500'}`}
                    >
                        Личные
                    </button>
                    <button
                        onClick={() => setCategory('important')}
                        className={`px-4 py-1.5 rounded-lg text-sm font-medium transition-all ${category === 'important' ? 'bg-white dark:bg-gray-600 shadow-sm text-red-500' : 'text-gray-500'}`}
                    >
                        Важные
                    </button>
                </div>

                <div className="flex-1 min-w-[200px] relative">
                    <input
                        type="text"
                        placeholder="Поиск по названию..."
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        className="w-full pl-10 pr-4 py-2 bg-gray-100 dark:bg-gray-700 border-none rounded-xl focus:ring-2 focus:ring-apple-blue dark:text-white"
                    />
                    <svg className="w-5 h-5 absolute left-3 top-2.5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                </div>

                <div className="flex items-center gap-2">
                    <select
                        value={typeFilter}
                        onChange={(e) => setTypeFilter(e.target.value)}
                        className="bg-gray-100 dark:bg-gray-700 border-none rounded-xl px-4 py-2 text-sm dark:text-white focus:ring-2 focus:ring-apple-blue"
                    >
                        <option value="">Все типы</option>
                        <option value="image">Изображения</option>
                        <option value="document">Документы</option>
                    </select>

                    <label className={`flex items-center gap-2 bg-apple-blue text-white px-4 py-2 rounded-xl text-sm font-medium hover:bg-blue-600 cursor-pointer transition-colors ${isUploading ? 'opacity-50 cursor-not-allowed' : ''}`}>
                        {isUploading ? (
                            <div className="w-4 h-4 border-2 border-white border-t-transparent animate-spin rounded-full"></div>
                        ) : (
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                            </svg>
                        )}
                        Загрузить
                        <input type="file" className="hidden" onChange={handleUpload} disabled={isUploading} />
                    </label>
                </div>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-6">
                {loading ? (
                    <div className="flex justify-center items-center h-48">
                        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
                    </div>
                ) : files.length > 0 ? (
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                        <AnimatePresence>
                            {files.map((file) => (
                                <FileCard
                                    key={file.id}
                                    file={file}
                                    onDelete={() => handleDelete(file.id)}
                                    onToggleImportant={() => handleToggleImportant(file.id, file.isImportant)}
                                />
                            ))}
                        </AnimatePresence>
                    </div>
                ) : (
                    <div className="flex flex-col items-center justify-center h-full text-gray-500">
                        <svg className="w-16 h-16 mb-4 opacity-20" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 13h6m-3-3v6m-9-1h18a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                        </svg>
                        <p className="text-lg">Файлы не найдены</p>
                    </div>
                )}
            </div>
        </div>
    );
};
