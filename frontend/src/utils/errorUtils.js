/**
 * Extract user-friendly error message from an Axios error.
 * Falls back through: response.data.message → response.data.title → fallback
 */
export function getErrorMessage(err, fallback = 'Midagi läks valesti. Palun proovi uuesti.') {
  return (
    err?.response?.data?.message ||
    err?.response?.data?.title  ||
    err?.message               ||
    fallback
  );
}

/**
 * Returns true if error is a 401 Unauthorized
 */
export function isUnauthorized(err) {
  return err?.response?.status === 401;
}

/**
 * Returns true if error is a 403 Forbidden
 */
export function isForbidden(err) {
  return err?.response?.status === 403;
}

/**
 * Returns true if error is a 404 Not Found
 */
export function isNotFound(err) {
  return err?.response?.status === 404;
}
