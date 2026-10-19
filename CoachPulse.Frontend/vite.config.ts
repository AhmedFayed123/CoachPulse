import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: 'http://localhost:5006', changeOrigin: true },
      '/hubs': { target: 'http://localhost:5006', changeOrigin: true, ws: true },
      '/healthz': { target: 'http://localhost:5006', changeOrigin: true },
      '/readyz': { target: 'http://localhost:5006', changeOrigin: true },
    },
  },
})
