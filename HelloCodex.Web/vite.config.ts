import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    host: 'localhost',
    port: 8081,
    strictPort: true,
    proxy: {
      '/api': 'http://localhost:8080',
    },
  },
});
