import React, { useState } from 'react';
import { authApi } from '../../api/api';
import { collectErrors, validatePassword, validatePasswordMatch } from '../../utils/validationUtils';
import { getErrorMessage } from '../../utils/errorUtils';
import './ChangePassword.css';

export default function ChangePassword({ onSuccess }) {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [serverError, setServerError] = useState('');
  const [success, setSuccess] = useState(false);

  const validate = () => collectErrors([
    ['currentPassword', !form.currentPassword ? 'Praegune parool on kohustuslik' : null],
    ['newPassword',     validatePassword(form.newPassword, 'Uus parool')],
    ['newPassword',     form.newPassword === form.currentPassword ? 'Uus parool ei saa olla sama mis praegune' : null],
    ['confirmPassword', validatePasswordMatch(form.newPassword, form.confirmPassword)],
  ]);

  const handleChange = e => {
    const { name, value } = e.target;
    setForm(p => ({ ...p, [name]: value }));
    if (errors[name]) setErrors(p => ({ ...p, [name]: '' }));
  };

  const handleSubmit = async e => {
    e.preventDefault();
    const validationErrors = validate();
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }
    setLoading(true);
    setServerError('');
    try {
      await authApi.changePassword(form.currentPassword, form.newPassword);
      setSuccess(true);
      onSuccess?.();
    } catch (err) {
      setServerError(getErrorMessage(err, 'Parooli muutmine ebaõnnestus.'));
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="cp-success">
        <div className="cp-success-icon">✓</div>
        <p>Parool edukalt muudetud!</p>
      </div>
    );
  }

  return (
    <form className="cp-form" onSubmit={handleSubmit} noValidate>
      {serverError && <div className="cp-error-banner">{serverError}</div>}

      <div className="cp-field">
        <label htmlFor="currentPassword">Praegune parool</label>
        <input
          id="currentPassword"
          name="currentPassword"
          type="password"
          value={form.currentPassword}
          onChange={handleChange}
          className={errors.currentPassword ? 'cp-input--error' : ''}
          autoComplete="current-password"
        />
        {errors.currentPassword && <span className="cp-field-error">{errors.currentPassword}</span>}
      </div>

      <div className="cp-field">
        <label htmlFor="newPassword">Uus parool</label>
        <input
          id="newPassword"
          name="newPassword"
          type="password"
          value={form.newPassword}
          onChange={handleChange}
          className={errors.newPassword ? 'cp-input--error' : ''}
          autoComplete="new-password"
        />
        {errors.newPassword && <span className="cp-field-error">{errors.newPassword}</span>}
      </div>

      <div className="cp-field">
        <label htmlFor="confirmPassword">Korda uut parooli</label>
        <input
          id="confirmPassword"
          name="confirmPassword"
          type="password"
          value={form.confirmPassword}
          onChange={handleChange}
          className={errors.confirmPassword ? 'cp-input--error' : ''}
          autoComplete="new-password"
        />
        {errors.confirmPassword && <span className="cp-field-error">{errors.confirmPassword}</span>}
      </div>

      <button type="submit" className="cp-submit" disabled={loading}>
        {loading ? 'Muudan...' : 'Muuda parool'}
      </button>
    </form>
  );
}
