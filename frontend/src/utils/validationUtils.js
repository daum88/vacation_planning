import { VALIDATION } from '../constants/appConstants';

/**
 * Common validation helpers used across multiple components
 */

export function validateEmail(email) {
  if (!email?.trim()) return 'Email on kohustuslik';
  if (!VALIDATION.EMAIL_REGEX.test(email)) return 'Vigane email formaat';
  return null;
}

export function validatePassword(password, label = 'Parool') {
  if (!password) return `${label} on kohustuslik`;
  if (password.length < VALIDATION.PASSWORD_MIN_LENGTH)
    return `${label} peab olema vähemalt ${VALIDATION.PASSWORD_MIN_LENGTH} tähemärki`;
  if (password.length > VALIDATION.PASSWORD_MAX_LENGTH)
    return `${label} on liiga pikk`;
  return null;
}

export function validatePasswordMatch(password, confirm) {
  if (password !== confirm) return 'Paroolid ei ühti';
  return null;
}

export function validateRequired(value, label = 'Väli') {
  if (!value || (typeof value === 'string' && !value.trim())) return `${label} on kohustuslik`;
  return null;
}

export function validateAnnualLeaveDays(days) {
  const n = parseInt(days);
  if (isNaN(n)) return 'Sisesta number';
  if (n < VALIDATION.ANNUAL_LEAVE_MIN || n > VALIDATION.ANNUAL_LEAVE_MAX)
    return `Peab olema vahemikus ${VALIDATION.ANNUAL_LEAVE_MIN}–${VALIDATION.ANNUAL_LEAVE_MAX}`;
  return null;
}

/**
 * Run multiple validators and return error map.
 * `rules` is an array of [fieldName, errorOrNull] tuples.
 * Returns { fieldName: errorString } for failing fields only.
 *
 * Example:
 *   const errors = collectErrors([
 *     ['email',    validateEmail(form.email)],
 *     ['password', validatePassword(form.password)],
 *   ]);
 */
export function collectErrors(rules) {
  return rules.reduce((acc, [field, err]) => {
    if (err) acc[field] = err;
    return acc;
  }, {});
}
