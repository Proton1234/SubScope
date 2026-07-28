/*
 * main.tsx
 *
 * Browser entry point that mounts the root App component and loads global styles.
 */
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './styles/app.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
