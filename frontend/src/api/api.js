import axios from 'axios';

const API_BASE_URL = 'https://localhost:5001/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const vacationRequestsApi = {
  getAll: () => api.get('/VacationRequests'),
  getById: (id) => api.get(`/VacationRequests/${id}`),
  create: (data) => api.post('/VacationRequests', data),
  update: (id, data) => api.put(`/VacationRequests/${id}`, data),
  delete: (id) => api.delete(`/VacationRequests/${id}`),
};

export default api;
