import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import './App.css';

// rootElement (elemento raiz): busca a <div id="root"> do index.html para iniciar o React dentro dela.
const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Root element (#root) was not found in index.html.');
}

// createRoot (criar raiz) + render (renderizar):
// StrictMode ajuda durante o desenvolvimento avisando sobre problemas comuns no React.
createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
