import { useState, useCallback } from 'react';
import { getErrorMessage } from '../utils/errorUtils';

/**
 * useAsync — manages loading/error/data state for any async operation.
 *
 * Usage:
 *   const { data, loading, error, execute } = useAsync(someApiCall);
 *   useEffect(() => { execute(param1, param2); }, [execute]);
 */
export function useAsync(asyncFn) {
  const [state, setState] = useState({ data: null, loading: false, error: null });

  const execute = useCallback(async (...args) => {
    setState({ data: null, loading: true, error: null });
    try {
      const result = await asyncFn(...args);
      const data = result?.data ?? result;
      setState({ data, loading: false, error: null });
      return data;
    } catch (err) {
      const error = getErrorMessage(err);
      setState({ data: null, loading: false, error });
      throw err;
    }
  }, [asyncFn]);

  return { ...state, execute };
}

/**
 * useFlash — shows a success/error message that auto-dismisses.
 *
 * Usage:
 *   const { flash, showSuccess, showError, clearFlash } = useFlash(4000);
 *   showSuccess('Salvesta õnnestus!');
 */
export function useFlash(duration = 4000) {
  const [flash, setFlash] = useState(null); // { type: 'success'|'error', message }

  const show = useCallback((type, message) => {
    setFlash({ type, message });
    setTimeout(() => setFlash(null), duration);
  }, [duration]);

  return {
    flash,
    showSuccess: (msg) => show('success', msg),
    showError:   (msg) => show('error', msg),
    clearFlash:  ()    => setFlash(null),
  };
}

/**
 * useConfirm — inline confirmation state (replaces window.confirm).
 *
 * Usage:
 *   const { confirmTarget, ask, cancel } = useConfirm();
 *   // Render: {confirmTarget && <ConfirmDialog onConfirm={() => { doThing(confirmTarget); cancel(); }} onCancel={cancel} />}
 *   // Trigger: ask(item) — stores item as confirmTarget
 */
export function useConfirm() {
  const [confirmTarget, setConfirmTarget] = useState(null);

  return {
    confirmTarget,
    ask:    (target) => setConfirmTarget(target),
    cancel: ()       => setConfirmTarget(null),
  };
}

/**
 * useFormField — manages a single form field with value + error.
 *
 * Usage:
 *   const email = useFormField('', validateEmail);
 *   <input value={email.value} onChange={email.onChange} />
 *   {email.error && <span>{email.error}</span>}
 */
export function useFormField(initialValue = '', validator = null) {
  const [value, setValue]   = useState(initialValue);
  const [error, setError]   = useState(null);
  const [touched, setTouched] = useState(false);

  const onChange = useCallback((e) => {
    const newValue = typeof e === 'string' ? e : e.target.value;
    setValue(newValue);
    if (touched && validator) setError(validator(newValue));
  }, [touched, validator]);

  const onBlur = useCallback(() => {
    setTouched(true);
    if (validator) setError(validator(value));
  }, [value, validator]);

  const reset = useCallback(() => {
    setValue(initialValue);
    setError(null);
    setTouched(false);
  }, [initialValue]);

  return { value, error, touched, onChange, onBlur, reset, setError };
}
