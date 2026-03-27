import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (!error.response) {
      error.message = 'Server ei vasta. Palun kontrollige võrguühendust.';
    } else if (error.response.status === 401) {
      // Unauthorized - redirect to login
      localStorage.clear();
      window.location.href = '/login';
    } else if (error.response.status === 500) {
      error.message = 'Serveri viga. Palun proovige hiljem uuesti.';
    } else if (error.response.status === 403) {
      error.message = 'Sul ei ole õigusi selle toimingu tegemiseks.';
    }
    return Promise.reject(error);
  }
);

export const vacationRequestsApi = {
  getAll:        (filters) => api.get('/VacationRequests', { params: filters }),
  getById:       (id)      => api.get(`/VacationRequests/${id}`),
  create:        (data)    => api.post('/VacationRequests', data),
  update:        (id, data)=> api.put(`/VacationRequests/${id}`, data),
  delete:        (id)      => api.delete(`/VacationRequests/${id}`),
  withdraw:      (id)      => api.post(`/VacationRequests/${id}/withdraw`),

  getAllAdmin:    (filters) => api.get('/VacationRequests/admin/all', { params: filters }),
  getPending:    ()        => api.get('/VacationRequests/admin/pending'),
  approve:       (id, data)=> api.post(`/VacationRequests/admin/approve/${id}`, data),
  bulkApprove:   (items)   => api.post('/VacationRequests/admin/bulk-approve', items),
  deleteAdmin:   (id)      => api.delete(`/VacationRequests/admin/${id}`),

  getStatistics: ()        => api.get('/VacationRequests/statistics'),
  exportCsv:     ()        => api.get('/VacationRequests/export/csv', { responseType: 'blob' }),
  exportIcal:    ()        => api.get('/VacationRequests/export/ical', { responseType: 'blob' }),
  getICalFeedUrl:(userId)  => `${window.location.origin}/api/VacationRequests/ical/user/${userId}`,

  getAuditLogs:  (id)      => api.get(`/VacationRequests/${id}/audit`),
  getComments:   (id)      => api.get(`/VacationRequests/${id}/comments`),
  postComment:   (id, text)=> api.post(`/VacationRequests/${id}/comments`, { text }),

  uploadAttachment: (id, file) => {
    const form = new FormData();
    form.append('file', file);
    return api.post(`/VacationRequests/${id}/attachments`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  downloadAttachment: (requestId, attachmentId) =>
    api.get(`/VacationRequests/${requestId}/attachments/${attachmentId}`, { responseType: 'blob' }),
  deleteAttachment: (requestId, attachmentId) =>
    api.delete(`/VacationRequests/${requestId}/attachments/${attachmentId}`),
};

export const usersApi = {
  getAll:         (includeInactive = false) => api.get('/Users', { params: { includeInactive } }),
  getById:        (id)  => api.get(`/Users/${id}`),
  getCurrent:     ()    => api.get('/Users/current'),
  getBalance:     (id)  => api.get(`/Users/${id}/balance`),
  updateCarryOver:(id, carryOverDays) => api.put(`/Users/${id}/carryover`, { carryOverDays }),
  annualReset:    (maxCarryOverDays = 5) =>
    api.post('/Users/annual-reset', null, { params: { maxCarryOverDays } }),
};

export const leaveTypesApi = {
  getAll:   (includeInactive = false) => api.get('/LeaveTypes', { params: { includeInactive } }),
  getById:  (id)    => api.get(`/LeaveTypes/${id}`),
  create:   (dto)   => api.post('/LeaveTypes', dto),
  update:   (id, dto) => api.put(`/LeaveTypes/${id}`, dto),
  delete:   (id)    => api.delete(`/LeaveTypes/${id}`),
};

export const calendarApi = {
  getTeamCalendar: (startDate, endDate, department) =>
    api.get('/Calendar/team', { params: { startDate, endDate, department } }),
  checkConflicts: (startDate, endDate, excludeRequestId, department) =>
    api.get('/Calendar/conflicts', { params: { startDate, endDate, excludeRequestId, department } }),
  getDepartments:    () => api.get('/Calendar/departments'),
  getPublicHolidays: (year) => api.get('/Calendar/holidays', { params: { year } }),
  getBlackouts:      () => api.get('/Calendar/blackouts'),
};

export const blackoutPeriodsApi = {
  getAll:  (activeOnly = true) => api.get('/BlackoutPeriods', { params: { activeOnly } }),
  create:  (data) => api.post('/BlackoutPeriods', data),
  delete:  (id)   => api.delete(`/BlackoutPeriods/${id}`),
};

export const departmentCapacityApi = {
  getAll:  () => api.get('/DepartmentCapacity'),
  check:   (department, startDate, endDate, excludeUserId) =>
    api.get('/DepartmentCapacity/check', { params: { department, startDate, endDate, excludeUserId } }),
  create:  (data) => api.post('/DepartmentCapacity', data),
  update:  (id, data) => api.put(`/DepartmentCapacity/${id}`, data),
  delete:  (id) => api.delete(`/DepartmentCapacity/${id}`),
};

export const notificationsApi = {
  getAll:  (limit = 50) => api.get('/Notifications', { params: { limit } }),
  getMy:   (since)      => api.get('/Notifications/my', since ? { params: { since } } : {}),
};

export const authApi = {
  login:           (email, password) => api.post('/Auth/login', { email, password }),
  logout:          ()                => api.post('/Auth/logout'),
  changePassword:  (currentPassword, newPassword) =>
    api.post('/Auth/change-password', { currentPassword, newPassword }),
};

export const registrationApi = {
  register:         (data) => api.post('/Registration/register', data),
  completeProfile:  (data) => api.post('/Registration/complete-profile', data),
};

export const organizationsApi = {
  getAll:   ()       => api.get('/Organizations'),
  getById:  (id)     => api.get(`/Organizations/${id}`),
  create:   (data)   => api.post('/Organizations', data),
  update:   (id, data) => api.put(`/Organizations/${id}`, data),
  delete:   (id)     => api.delete(`/Organizations/${id}`),
};

export const joinRequestsApi = {
  getMy:    ()       => api.get('/JoinRequests/my'),
  getAll:   ()       => api.get('/JoinRequests'),
  review:   (id, data) => api.post(`/JoinRequests/${id}/review`, data),
  cancel:   (id)     => api.post(`/JoinRequests/${id}/cancel`),
};

export const adminUserManagementApi = {
  getAll:           ()         => api.get('/AdminUserManagement'),
  invite:           (data)     => api.post('/AdminUserManagement/invite', data),
  resetPassword:    (id)       => api.post(`/AdminUserManagement/${id}/reset-password`),
  toggleActivation: (id)       => api.post(`/AdminUserManagement/${id}/toggle-activation`),
  toggleAdmin:      (id)       => api.post(`/AdminUserManagement/${id}/toggle-admin`),
  delete:           (id)       => api.delete(`/AdminUserManagement/${id}`),
};

export const auditLogsApi = {
  getLogs:    (params) => api.get('/AuditLogs', { params }),
  getSummary: ()       => api.get('/AuditLogs/summary'),
};

export const publicHolidaysApi = {
  getByYear:  (year)        => api.get('/PublicHolidays', { params: { year } }),
  getInRange: (start, end)  => api.get('/PublicHolidays/range', { params: { start, end } }),
  getAllRaw:   ()            => api.get('/PublicHolidays/all'),
  create:     (dto)         => api.post('/PublicHolidays', dto),
  update:     (id, dto)     => api.put(`/PublicHolidays/${id}`, dto),
  delete:     (id)          => api.delete(`/PublicHolidays/${id}`),
};

export const delegationsApi = {
  getMy:   ()    => api.get('/ManagerDelegations/my'),
  getAll:  ()    => api.get('/ManagerDelegations'),
  create:  (dto) => api.post('/ManagerDelegations', dto),
  cancel:  (id)  => api.delete(`/ManagerDelegations/${id}`),
};

export const requestHistoryApi = {
  get: (requestId) => api.get(`/VacationRequests/${requestId}/history`),
};

// Helper to get current user ID from localStorage
export const getCurrentUserId = () => {
  const userId = localStorage.getItem('userId');
  return userId ? parseInt(userId) : null;
};

export const setUserId = (id) => localStorage.setItem('userId', String(id));

export default api;
