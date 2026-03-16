import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15000, // 15 second timeout
});

// Request interceptor for logging
api.interceptors.request.use(
  (config) => {
    console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`);
    return config;
  },
  (error) => {
    console.error('[API] Request error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => {
    console.log(`[API] Response: ${response.status}`, response.data);
    return response;
  },
  (error) => {
    if (!error.response) {
      // Network error
      console.error('[API] Network error:', error.message);
      error.message = 'Server ei vasta. Palun kontrollige võrguühendust ja veenduge, et backend töötab.';
    } else if (error.response.status === 500) {
      console.error('[API] Server error:', error.response.data);
      error.message = 'Serveri viga. Palun proovige hiljem uuesti.';
    } else if (error.response.status === 409) {
      console.warn('[API] Conflict:', error.response.data);
    } else if (error.response.status === 400) {
      console.warn('[API] Bad request:', error.response.data);
    }
    return Promise.reject(error);
  }
);

export const vacationRequestsApi = {
  getAll: () => api.get('/VacationRequests'),
  getById: (id) => api.get(`/VacationRequests/${id}`),
  create: (data) => api.post('/VacationRequests', data),
  update: (id, data) => api.put(`/VacationRequests/${id}`, data),
  delete: (id) => api.delete(`/VacationRequests/${id}`),
};

export default api;
