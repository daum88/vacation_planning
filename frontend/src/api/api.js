import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

// Get role and userId from localStorage
const getRole = () => localStorage.getItem('userRole') || 'employee';
const getUserId = () => localStorage.getItem('userId') || '1';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15000,
});

// Request interceptor
api.interceptors.request.use(
  (config) => {
    const role = getRole();
    const userId = getUserId();
    
    if (!config.params) {
      config.params = {};
    }
    config.params.role = role;
    config.params.userId = userId;
    
    console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`, { role, userId });
    return config;
  },
  (error) => {
    console.error('[API] Request error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor
api.interceptors.response.use(
  (response) => {
    console.log(`[API] Response: ${response.status}`, response.data);
    return response;
  },
  (error) => {
    if (!error.response) {
      error.message = 'Server ei vasta. Palun kontrollige võrguühendust.';
    } else if (error.response.status === 500) {
      console.error('[API] Server error:', error.response.data);
      error.message = 'Serveri viga. Palun proovige hiljem uuesti.';
    } else if (error.response.status === 409) {
      console.warn('[API] Conflict:', error.response.data);
    } else if (error.response.status === 400) {
      console.warn('[API] Bad request:', error.response.data);
    } else if (error.response.status === 403) {
      error.message = 'Sul ei ole õigusi selle toimingu tegemiseks.';
    }
    return Promise.reject(error);
  }
);

// Vacation Requests API
export const vacationRequestsApi = {
  // Employee endpoints
  getAll: (filters) => api.get('/VacationRequests', { params: filters }),
  getById: (id) => api.get(`/VacationRequests/${id}`),
  create: (data) => api.post('/VacationRequests', data),
  update: (id, data) => api.put(`/VacationRequests/${id}`, data),
  delete: (id) => api.delete(`/VacationRequests/${id}`),
  withdraw: (id) => api.post(`/VacationRequests/${id}/withdraw`),
  
  // Admin endpoints
  getAllAdmin: (filters) => api.get('/VacationRequests/admin/all', { params: filters }),
  getPending: () => api.get('/VacationRequests/admin/pending'),
  approve: (id, data) => api.post(`/VacationRequests/admin/approve/${id}`, data),
  deleteAdmin: (id) => api.delete(`/VacationRequests/admin/${id}`),
  
  // Statistics
  getStatistics: () => api.get('/VacationRequests/statistics'),
  
  // Export
  exportCsv: () => api.get('/VacationRequests/export/csv', { responseType: 'blob' }),
  exportIcal: () => api.get('/VacationRequests/export/ical', { responseType: 'blob' }),
  
  // Audit
  getAuditLogs: (id) => api.get(`/VacationRequests/${id}/audit`),
  
  // Attachments
  uploadAttachment: (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post(`/VacationRequests/${id}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },
  downloadAttachment: (requestId, attachmentId) => 
    api.get(`/VacationRequests/${requestId}/attachments/${attachmentId}`, { responseType: 'blob' }),
  deleteAttachment: (requestId, attachmentId) => 
    api.delete(`/VacationRequests/${requestId}/attachments/${attachmentId}`)
};

export const usersApi = {
  getAll: (includeInactive = false) => api.get('/Users', { params: { includeInactive } }),
  getById: (id) => api.get(`/Users/${id}`),
  getCurrent: () => api.get('/Users/current'),
  getBalance: (id) => api.get(`/Users/${id}/balance`),
  updateCarryOver: (id, carryOverDays) => api.put(`/Users/${id}/carryover`, { carryOverDays }),
};

// Leave Types API
export const leaveTypesApi = {
  getAll: (includeInactive = false) => api.get('/LeaveTypes', { params: { includeInactive } }),
  getById: (id) => api.get(`/LeaveTypes/${id}`)
};

// Calendar API
export const calendarApi = {
  getTeamCalendar: (startDate, endDate, department) =>
    api.get('/Calendar/team', { params: { startDate, endDate, department } }),
  checkConflicts: (startDate, endDate, excludeRequestId, department) =>
    api.get('/Calendar/conflicts', { params: { startDate, endDate, excludeRequestId, department } }),
  getDepartments: () => api.get('/Calendar/departments'),
  getPublicHolidays: (year) => api.get('/Calendar/holidays', { params: { year } }),
  getBlackouts: () => api.get('/Calendar/blackouts'),
};

// Blackout Periods API
export const blackoutPeriodsApi = {
  getAll: (activeOnly = true) => api.get('/BlackoutPeriods', { params: { activeOnly } }),
  create: (data) => api.post('/BlackoutPeriods', data),
  delete: (id) => api.delete(`/BlackoutPeriods/${id}`),
};

// Notifications API
export const notificationsApi = {
  getAll: (limit = 50) => api.get('/Notifications', { params: { limit } }),
};

// Helper functions
export const setUserRole = (role) => {
  localStorage.setItem('userRole', role);
};

export const setUserId = (userId) => {
  localStorage.setItem('userId', userId.toString());
};

export const getCurrentRole = getRole;
export const getCurrentUserId = getUserId;

export default api;
