import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';

export interface ApiEnvelope<T> {
  success: boolean;
  data: T;
  message: string | null;
  errors: { field: string; code: string; message: string }[];
  traceId: string;
}

export const http = axios.create({ baseURL: '/api', timeout: 30000 });

http.interceptors.request.use((config) => {
  const token = localStorage.getItem('techpos_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// ---------- Silent refresh: 401 ⇒ đổi refresh token lấy access mới ⇒ retry 1 lần ----------

let refreshing: Promise<boolean> | null = null;

/** Gọi /auth/refresh bằng axios gốc (không qua interceptor — tránh vòng lặp), lưu cặp token mới. */
async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = localStorage.getItem('techpos_refresh');
  if (!refreshToken) return false;
  try {
    const res = await axios.post<ApiEnvelope<{ accessToken: string; refreshToken: string }>>(
      '/api/auth/refresh', { refreshToken }, { timeout: 15000 });
    const env = res.data;
    if (!env.success || !env.data?.accessToken) return false;
    localStorage.setItem('techpos_token', env.data.accessToken);
    localStorage.setItem('techpos_refresh', env.data.refreshToken);
    return true;
  } catch {
    return false;
  }
}

function forceLogout(): void {
  localStorage.removeItem('techpos_token');
  localStorage.removeItem('techpos_refresh');
  localStorage.removeItem('techpos_user');
  window.location.href = '/login';
}

http.interceptors.response.use(
  (res) => res,
  async (err: AxiosError) => {
    const original = err.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined;
    const url = original?.url ?? '';
    const is401 = err.response?.status === 401;

    // 401 do token hết hạn: thử refresh 1 lần rồi gọi lại request gốc.
    // Chỉ logout thật sự khi refresh thất bại (hết hạn 7 ngày / bị thu hồi).
    if (is401 && original && !original._retried && !url.includes('/auth/refresh') && localStorage.getItem('techpos_refresh')) {
      original._retried = true;
      refreshing ??= refreshAccessToken().finally(() => { refreshing = null; });
      const ok = await refreshing;
      if (ok) {
        original.headers.Authorization = `Bearer ${localStorage.getItem('techpos_token')}`;
        return http(original);
      }
    }

    if (is401 && !url.includes('/auth/refresh') && !localStorage.getItem('techpos_refresh')) {
      forceLogout();
    } else if (is401 && original?._retried) {
      forceLogout();
    } else if (is401 && url.includes('/auth/refresh')) {
      forceLogout();
    }

    return Promise.reject(err.response?.data ?? err);
  }
);

export const unwrap = <T,>(env: ApiEnvelope<T>): T => env.data;
