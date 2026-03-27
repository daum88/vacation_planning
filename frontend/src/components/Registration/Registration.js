import React, { useState, useEffect } from 'react';
import { registrationApi, organizationsApi } from '../../api/api';
import { collectErrors, validateEmail, validatePassword, validatePasswordMatch, validateRequired } from '../../utils/validationUtils';
import { getErrorMessage } from '../../utils/errorUtils';
import './Registration.css';

export default function Registration({ onLoginClick }) {
  const [organizations, setOrganizations] = useState([]);
  const [form, setForm] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    organizationId: '',
    joinMessage: '',
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    organizationsApi.getAll()
      .then(res => setOrganizations(res.data || []))
      .catch(() => setOrganizations([]));
  }, []);

  const validate = () => collectErrors([
    ['firstName',       validateRequired(form.firstName, 'Eesnimi')],
    ['lastName',        validateRequired(form.lastName, 'Perenimi')],
    ['email',           validateEmail(form.email)],
    ['password',        validatePassword(form.password)],
    ['confirmPassword', validatePasswordMatch(form.password, form.confirmPassword)],
    ['organizationId',  !form.organizationId ? 'Palun vali organisatsioon' : null],
  ]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm(prev => ({ ...prev, [name]: value }));
    if (errors[name]) setErrors(prev => ({ ...prev, [name]: '' }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const validationErrors = validate();
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setLoading(true);
    setServerError('');

    try {
      await registrationApi.register({
        email: form.email,
        password: form.password,
        firstName: form.firstName,
        lastName: form.lastName,
        organizationId: parseInt(form.organizationId),
        joinMessage: form.joinMessage || undefined,
      });
      setSuccess(true);
    } catch (err) {
      setServerError(getErrorMessage(err, 'Registreerimine ebaõnnestus. Palun proovi uuesti.'));
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="reg-page">
        <div className="reg-card reg-card--success">
          <div className="reg-success-icon">✓</div>
          <h2>Registreerimine õnnestus!</h2>
          <p>
            Sinu liitumise taotlus on edukalt esitatud. Administraator vaatab
            taotluse üle ja saadab sulle kinnituse e-postile.
          </p>
          <button className="reg-btn reg-btn--primary" onClick={onLoginClick}>
            Mine sisselogimisele
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="reg-page">
      <div className="reg-card">
        <div className="reg-header">
          <h1 className="reg-title">Registreerimine</h1>
          <p className="reg-subtitle">Loo konto ja liitu organisatsiooniga</p>
        </div>

        {serverError && (
          <div className="reg-alert reg-alert--error">{serverError}</div>
        )}

        <form className="reg-form" onSubmit={handleSubmit} noValidate>
          <div className="reg-row">
            <div className="reg-field">
              <label className="reg-label" htmlFor="firstName">Eesnimi</label>
              <input
                id="firstName"
                name="firstName"
                type="text"
                className={`reg-input${errors.firstName ? ' reg-input--error' : ''}`}
                value={form.firstName}
                onChange={handleChange}
                placeholder="Mari"
                autoComplete="given-name"
              />
              {errors.firstName && <span className="reg-error">{errors.firstName}</span>}
            </div>

            <div className="reg-field">
              <label className="reg-label" htmlFor="lastName">Perenimi</label>
              <input
                id="lastName"
                name="lastName"
                type="text"
                className={`reg-input${errors.lastName ? ' reg-input--error' : ''}`}
                value={form.lastName}
                onChange={handleChange}
                placeholder="Maasikas"
                autoComplete="family-name"
              />
              {errors.lastName && <span className="reg-error">{errors.lastName}</span>}
            </div>
          </div>

          <div className="reg-field">
            <label className="reg-label" htmlFor="email">Email</label>
            <input
              id="email"
              name="email"
              type="email"
              className={`reg-input${errors.email ? ' reg-input--error' : ''}`}
              value={form.email}
              onChange={handleChange}
              placeholder="mari@example.com"
              autoComplete="email"
            />
            {errors.email && <span className="reg-error">{errors.email}</span>}
          </div>

          <div className="reg-row">
            <div className="reg-field">
              <label className="reg-label" htmlFor="password">Parool</label>
              <input
                id="password"
                name="password"
                type="password"
                className={`reg-input${errors.password ? ' reg-input--error' : ''}`}
                value={form.password}
                onChange={handleChange}
                placeholder="Vähemalt 8 tähemärki"
                autoComplete="new-password"
              />
              {errors.password && <span className="reg-error">{errors.password}</span>}
            </div>

            <div className="reg-field">
              <label className="reg-label" htmlFor="confirmPassword">Korda parooli</label>
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                className={`reg-input${errors.confirmPassword ? ' reg-input--error' : ''}`}
                value={form.confirmPassword}
                onChange={handleChange}
                placeholder="Korda parooli"
                autoComplete="new-password"
              />
              {errors.confirmPassword && <span className="reg-error">{errors.confirmPassword}</span>}
            </div>
          </div>

          <div className="reg-field">
            <label className="reg-label" htmlFor="organizationId">Organisatsioon</label>
            <select
              id="organizationId"
              name="organizationId"
              className={`reg-select${errors.organizationId ? ' reg-input--error' : ''}`}
              value={form.organizationId}
              onChange={handleChange}
            >
              <option value="">-- Vali organisatsioon --</option>
              {organizations.map(org => (
                <option key={org.id} value={org.id}>{org.name}</option>
              ))}
            </select>
            {errors.organizationId && <span className="reg-error">{errors.organizationId}</span>}
          </div>

          <div className="reg-field">
            <label className="reg-label" htmlFor="joinMessage">
              Liitumise sõnum <span className="reg-optional">(valikuline)</span>
            </label>
            <textarea
              id="joinMessage"
              name="joinMessage"
              className="reg-textarea"
              value={form.joinMessage}
              onChange={handleChange}
              placeholder="Tutvusta end administraatorile..."
              rows={3}
              maxLength={500}
            />
          </div>

          <button
            type="submit"
            className="reg-btn reg-btn--primary reg-btn--full"
            disabled={loading}
          >
            {loading ? 'Registreerin...' : 'Registreeri'}
          </button>
        </form>

        <div className="reg-footer">
          <span>Juba on konto?</span>
          <button className="reg-link" onClick={onLoginClick}>Logi sisse</button>
        </div>
      </div>
    </div>
  );
}
