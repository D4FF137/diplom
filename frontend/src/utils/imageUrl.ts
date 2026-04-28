/**
 * Преобразует относительный URL изображения в полный URL
 */
export const getImageUrl = (url: string | null | undefined, baseUrl?: string): string | null => {
  if (!url) return null;

  // Если URL уже полный, возвращаем как есть
  if (url.startsWith('http://') || url.startsWith('https://')) {
    return url;
  }

  // Если URL начинается с /, используем базовый URL API (через Gateway)
  if (url.startsWith('/')) {
    const apiUrl = baseUrl || import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
    return `${apiUrl}${url}`;
  }


  return url;
};

