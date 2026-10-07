import axios from 'axios';

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

http.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('techpos_token');
      window.location.href = '/login';
    }
    return Promise.reject(err.response?.data ?? err);
  }
);

export const unwrap = <T,>(env: ApiEnvelope<T>): T => env.data;