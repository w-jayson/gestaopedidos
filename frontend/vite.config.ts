import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Configuração do Vite (ferramenta de desenvolvimento e empacotamento do React).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // proxy (procuração/redirecionamento): em desenvolvimento local, toda chamada para /api/...
    // é encaminhada para a API .NET rodando na porta 5084, removendo o prefixo "/api".
    // Exemplo: fetch('/api/pedidos') -> http://localhost:5084/pedidos
    proxy: {
      '/api': {
        target: 'http://localhost:5084',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
});
