import { create } from 'zustand';

export interface AuthUser {
  id: string;
  username: string;
  fullName?: string;
  email?: string;
  roles: string[];
  permissions: string[];
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  isAuthenticated: () => boolean;
  hasPermission: (code: string) => boolean;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  token: localStorage.getItem('techpos_token'),
  user: (() => {
    try {
      const raw = localStorage.getItem('techpos_user');
      return raw ? (JSON.parse(raw) as AuthUser) : null;
    } catch { return null; }
  })(),


  login: async (username, password) => {
    try {
      const { http } = await import('../api/http');
      const res = await http.post('/auth/login', { username, password });
      const env = res.data as unknown as {
        success: boolean;
        data: { accessToken: string; refreshToken: string; user: AuthUser } | null;
      };
      if (!env.success || !env.data) return false;
      localStorage.setItem('techpos_token', env.data.accessToken);
      localStorage.setItem('techpos_refresh', env.data.refreshToken);
      localStorage.setItem('techpos_user', JSON.stringify(env.data.user));
      set({ token: env.data.accessToken, user: env.data.user });
      return true;
    } catch {
      return false;
    }
  },

  logout: () => {
    localStorage.removeItem('techpos_token');
    localStorage.removeItem('techpos_refresh');
    localStorage.removeItem('techpos_user');
    set({ token: null, user: null });
    window.location.href = '/login';
  },

  isAuthenticated: () => !!get().token,
  hasPermission: (code) => {
    const user = get().user;
    if (!user) return false;
    if (user.roles.includes('SUPER_ADMIN')) return true;
    return user.permissions.includes(code);
  }
}));