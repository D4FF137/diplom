import { motion } from 'framer-motion';
import type { FileMetadata } from '../../types';
import { useAuthStore } from '../../stores/authStore';

interface FileCardProps {
    file: FileMetadata;
    onDelete: () => void;
    onToggleImportant: () => void;
}

export const FileCard = ({ file, onDelete, onToggleImportant }: FileCardProps) => {
    const user = useAuthStore(state => state.user);
    const isOwner = user?.id === file.ownerId;
    const isBoss = user?.role === 'Boss';

    const getFileIcon = () => {
        if (file.contentType.startsWith('image/')) {
            return (
                <div className="w-full h-32 bg-gray-100 dark:bg-gray-700 rounded-lg mb-3 overflow-hidden">
                    <img
                        src={`${import.meta.env.VITE_API_URL}${file.path}`}
                        alt={file.fileName}
                        className="w-full h-full object-cover"
                        loading="lazy"
                        onError={(e) => {
                            (e.target as HTMLImageElement).src = '/file-placeholder.png';
                        }}
                    />
                </div>
            );
        }

        // Generic icon for documents
        return (
            <div className="w-full h-32 bg-gray-50 dark:bg-gray-700/50 rounded-lg mb-3 flex items-center justify-center text-apple-blue">
                <svg className="w-12 h-12" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                </svg>
            </div>
        );
    };

    const formatSize = (bytes: number) => {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    };

    return (
        <motion.div
            layout
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            className="bg-white dark:bg-gray-800 p-4 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-700 hover:shadow-md transition-shadow relative group"
        >
            {file.isImportant && (
                <div className="absolute -top-2 -right-2 bg-red-500 text-white p-1.5 rounded-full shadow-lg z-10">
                    <svg className="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                        <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                    </svg>
                </div>
            )}

            {getFileIcon()}

            <div className="flex flex-col">
                <h4 className="text-sm font-semibold dark:text-white truncate" title={file.fileName}>
                    {file.fileName}
                </h4>
                <div className="flex justify-between items-center mt-1">
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                        {formatSize(file.fileSize)}
                    </span>
                    <span className="text-[10px] text-gray-400">
                        {new Date(file.createdAt).toLocaleDateString()}
                    </span>
                </div>

                <div className="flex items-center gap-2 mt-4 opacity-0 group-hover:opacity-100 transition-opacity">
                    <a
                        href={`${import.meta.env.VITE_API_URL}${file.path}`}
                        download={file.fileName}
                        target="_blank"
                        rel="noreferrer"
                        className="flex-1 bg-gray-100 dark:bg-gray-700 text-apple-blue px-3 py-1.5 rounded-lg text-xs font-medium hover:bg-apple-blue hover:text-white transition-all text-center"
                    >
                        Скачать
                    </a>

                    {isBoss && (
                        <button
                            onClick={onToggleImportant}
                            title={file.isImportant ? "Снять отметку важного" : "Отметить как важное"}
                            className={`p-1.5 rounded-lg transition-colors ${file.isImportant ? 'text-red-500 bg-red-50' : 'text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'}`}
                        >
                            <svg className="w-4 h-4" fill={file.isImportant ? "currentColor" : "none"} stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.921-.755 1.688-1.54 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.784.57-1.838-.197-1.539-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                            </svg>
                        </button>
                    )}

                    {(isOwner || isBoss) && (
                        <button
                            onClick={onDelete}
                            className="p-1.5 rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 transition-colors"
                            title="Удалить"
                        >
                            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                        </button>
                    )}
                </div>
            </div>
        </motion.div>
    );
};
