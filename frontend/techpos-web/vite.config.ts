import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Proxy /api sang ASP.NET Core API khi dev (mục 35: frontend không truy cập MySQL trực tiếp)
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5005',
        changeOrigin: true
      },
      '/hubs': {
        target: 'http://localhost:5005',
        changeOrigin: true,
        ws: true
      }
    }
  },
  build: { outDir: 'dist', sourcemap: false }
});