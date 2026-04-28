import { useEffect, lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuthOptimized';
import { useNotifications } from './hooks/useNotificationsOptimized';
import { Login } from './components/auth/Login';
import { TelegramLayout } from './components/layout/TelegramLayout';

const AdminPage = lazy(() => import('./components/admin/AdminPage').then(module => ({ default: module.AdminPage })));
const ManageOrgPage = lazy(() => import('./components/manage-org/ManageOrgPage').then(module => ({ default: module.ManageOrgPage })));

function AuthPage() {
  const navigate = useNavigate();
  const { isAuthenticated, loading } = useAuth();

  if (!loading && isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-apple-gray dark:bg-gray-900">
      <Login onSuccess={() => navigate('/')} />
    </div>
  );
}

function Dashboard() {
  return <TelegramLayout />;
}

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { loading, isAuthenticated } = useAuth();

  // Инициализируем уведомления при входе в защищенную зону
  // Это гарантирует, что WebSocket подключится один раз
  useNotifications();

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/auth" replace />;
  }

  return (
    <Suspense fallback={
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div>
      </div>
    }>
      {children}
    </Suspense>
  );
}

function App() {
  // Инициализация темы при загрузке приложения
  useEffect(() => {
    const darkMode = sessionStorage.getItem('darkMode') === 'true';
    if (darkMode) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
  }, []);

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/auth" element={<AuthPage />} />
        <Route
          path="/admin"
          element={
            <Suspense fallback={<div className="min-h-screen flex items-center justify-center bg-apple-gray dark:bg-gray-900"><div className="animate-spin rounded-full h-12 w-12 border-b-2 border-apple-blue"></div></div>}>
              <AdminPage />
            </Suspense>
          }
        />
        <Route
          path="/manage-org"
          element={
            <ProtectedRoute>
              <ManageOrgPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
