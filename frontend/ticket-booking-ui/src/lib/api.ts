import axios from 'axios';

// In production (GKE): VITE_API_URL="" → relative URLs so the Ingress routes
// /api/* to the gateway and /* to the frontend SPA from the same origin.
// In local dev: VITE_API_URL=http://localhost:5000 (set in .env)
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

export default api;
