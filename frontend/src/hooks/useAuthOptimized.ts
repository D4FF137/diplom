import { useEffect } from 'react';
import { usersAPI } from '../services/api';
import { useAuthStore } from '../stores/authStore';

let authSessionChecked = false;

export const useAuth = () => {
  const store = useAuthStore();

  useEffect(() => {
    if (authSessionChecked) {
      return;
    }

    authSessionChecked = true;
    let isMounted = true;

    store.setLoading(true);
    usersAPI.getProfile()
      .then((user) => {
        if (!isMounted) return;
        useAuthStore.setState({ user, isAuthenticated: true, loading: false });
      })
      .catch((err) => {
        if (!isMounted) return;
        console.error('Error restoring authenticated session:', err);
        useAuthStore.setState({ user: null, isAuthenticated: false, loading: false });
      });

    return () => {
      isMounted = false;
    };
  }, []);

  return {
    user: store.user,
    loading: store.loading,
    login: store.login,
    logout: store.logout,
    updateUser: store.updateUser,
    refreshUser: store.refreshUser,
    isAuthenticated: store.isAuthenticated,
  };
};
