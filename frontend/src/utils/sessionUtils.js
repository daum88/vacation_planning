import { STORAGE_KEYS } from '../constants/appConstants';

/**
 * Session utilities — single source of truth for localStorage auth data
 */

export const session = {
  /** Save auth data after login */
  save({ token, userId, fullName, isAdmin, department, isTemporaryPassword, isProfileComplete }) {
    localStorage.setItem(STORAGE_KEYS.TOKEN,               token);
    localStorage.setItem(STORAGE_KEYS.USER_ID,             String(userId));
    localStorage.setItem(STORAGE_KEYS.USER_NAME,           fullName);
    localStorage.setItem(STORAGE_KEYS.IS_ADMIN,            String(isAdmin));
    localStorage.setItem(STORAGE_KEYS.IS_TEMP_PASSWORD,    String(isTemporaryPassword ?? false));
    localStorage.setItem(STORAGE_KEYS.IS_PROFILE_COMPLETE, String(isProfileComplete ?? true));
    if (department) localStorage.setItem(STORAGE_KEYS.DEPARTMENT, department);
  },

  /** Clear all auth data on logout */
  clear() {
    localStorage.clear();
  },

  /** Check if a valid session exists */
  isAuthenticated() {
    return !!(localStorage.getItem(STORAGE_KEYS.TOKEN) && localStorage.getItem(STORAGE_KEYS.USER_ID));
  },

  getToken()      { return localStorage.getItem(STORAGE_KEYS.TOKEN); },
  getUserId()     {
    const id = localStorage.getItem(STORAGE_KEYS.USER_ID);
    return id ? parseInt(id) : null;
  },
  getUserName()          { return localStorage.getItem(STORAGE_KEYS.USER_NAME) || ''; },
  isAdmin()              { return localStorage.getItem(STORAGE_KEYS.IS_ADMIN) === 'true'; },
  getDepartment()        { return localStorage.getItem(STORAGE_KEYS.DEPARTMENT) || ''; },
  isTempPassword()       { return localStorage.getItem(STORAGE_KEYS.IS_TEMP_PASSWORD) === 'true'; },
  isProfileComplete()    { return localStorage.getItem(STORAGE_KEYS.IS_PROFILE_COMPLETE) !== 'false'; },

  /** Notification last-seen timestamp */
  getNotifLastSeen()  { return localStorage.getItem(STORAGE_KEYS.NOTIF_LAST_SEEN) || null; },
  setNotifLastSeen(ts) { localStorage.setItem(STORAGE_KEYS.NOTIF_LAST_SEEN, ts); },
};

export default session;
