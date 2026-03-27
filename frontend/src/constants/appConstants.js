// ── Vacation request statuses ───────────────────────────────────────────────
export const STATUS = {
  PENDING:   'Pending',
  APPROVED:  'Approved',
  REJECTED:  'Rejected',
  WITHDRAWN: 'Withdrawn',
};

export const STATUS_LABELS = {
  [STATUS.PENDING]:   'Ootel',
  [STATUS.APPROVED]:  'Kinnitatud',
  [STATUS.REJECTED]:  'Tagasi lükatud',
  [STATUS.WITHDRAWN]: 'Tagasi võetud',
};

// ── Join request statuses ───────────────────────────────────────────────────
export const JOIN_STATUS = {
  PENDING:  'Pending',
  APPROVED: 'Approved',
  REJECTED: 'Rejected',
};

export const JOIN_STATUS_LABELS = {
  [JOIN_STATUS.PENDING]:  'Ootel',
  [JOIN_STATUS.APPROVED]: 'Kinnitatud',
  [JOIN_STATUS.REJECTED]: 'Tagasi lükatud',
};

// ── Audit event types ───────────────────────────────────────────────────────
export const AUDIT_EVENT_LABELS = {
  LoginSuccess:            'Sisselogimine',
  LoginFailed:             'Ebaõnnestunud login',
  Logout:                  'Väljalogimine',
  PasswordChanged:         'Parool muudetud',
  PasswordReset:           'Parool lähtestatud',
  UserCreated:             'Kasutaja loodud',
  UserInvited:             'Kasutaja kutsutud',
  UserDeleted:             'Kasutaja kustutatud',
  UserActivated:           'Kasutaja aktiveeritud',
  UserDeactivated:         'Kasutaja deaktiveeritud',
  AdminGranted:            'Admin õigused antud',
  AdminRevoked:            'Admin õigused eemaldatud',
  RegistrationCompleted:   'Registreerimine lõpetatud',
  ProfileCompleted:        'Profiil lõpetatud',
  JoinRequestSubmitted:    'Liitumise taotlus esitatud',
  JoinRequestApproved:     'Liitumise taotlus kinnitatud',
  JoinRequestRejected:     'Liitumise taotlus tagasi lükatud',
  VacationRequestCreated:  'Puhkusetaotlus loodud',
  VacationRequestApproved: 'Puhkusetaotlus kinnitatud',
  VacationRequestRejected: 'Puhkusetaotlus tagasi lükatud',
};

// ── Validation ──────────────────────────────────────────────────────────────
export const VALIDATION = {
  PASSWORD_MIN_LENGTH: 8,
  PASSWORD_MAX_LENGTH: 100,
  EMAIL_REGEX: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  ANNUAL_LEAVE_MIN: 10,
  ANNUAL_LEAVE_MAX: 50,
  ANNUAL_LEAVE_DEFAULT: 25,
};

// ── Pagination ──────────────────────────────────────────────────────────────
export const PAGINATION = {
  DEFAULT_PAGE_SIZE: 20,
  AUDIT_PAGE_SIZE: 50,
};

// ── Flash message duration (ms) ─────────────────────────────────────────────
export const FLASH_DURATION = 4000;

// ── LocalStorage keys ───────────────────────────────────────────────────────
export const STORAGE_KEYS = {
  TOKEN:                'token',
  USER_ID:              'userId',
  USER_NAME:            'userName',
  IS_ADMIN:             'isAdmin',
  DEPARTMENT:           'department',
  IS_TEMP_PASSWORD:     'isTemporaryPassword',
  IS_PROFILE_COMPLETE:  'isProfileComplete',
  NOTIF_LAST_SEEN:      'notifLastSeen',
};
