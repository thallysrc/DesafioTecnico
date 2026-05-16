import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'node:path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
    proxy: {
      // D-06 / D-07 (LOCKED): forward /api/** to the backend service over Docker DNS.
      // Target is the literal compose service name "backend". No host.docker.internal,
      // no env-var indirection, no fallback. The canonical run path is `docker compose up`.
      '/api': {
        target: 'http://backend:8080',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
