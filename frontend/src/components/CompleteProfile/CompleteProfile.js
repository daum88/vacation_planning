import React, { useState } from 'react';
import { registrationApi } from '../../api/api';
import { collectErrors, validatePassword, validatePasswordMatch, validateRequired } from '../../utils/validationUtils';
import { getErrorMessage } from '../../utils/errorUtils';
import './CompleteProfile.css';

/**
 * Shown to users who logged in with a temporary password (admin invitation flow).
 * They must set a real password + fill in their profile before using the app.
 */
export default function CompleteProfile({ onComplete }) {
  const [form, setForm] = useState({
    firstName:   '',
    lastName:    '',
    department:  '',
    position:    '',
    hireDate:    '',
    newPassword: '',
    confirmPwd:  '',
  });
  const [errors, setErrors]   = useState({});
  const [loading, setLoading] = useState(false);
  const [apiError, setApiError] = useState('');

  const set = (field) => (e) => setForm(f => ({ ...f, [field]: e.target.value }));

  const validate = () => collectErrors([
    ['firstName',   validateRequired(form.firstName,  'Eesnimi')],
    ['lastName',    validateRequired(form.lastName,   'Perekonnanimi')],
    ['department',  validateRequired(form.department, 'Osakond')],
    ['hireDate',    validateRequired(form.hireDate,   'Tööle asumise kuupäev')],
    ['newPassword', validatePassword(form.newPassword)],
    ['confirmPwd',  validatePasswordMatch(form.newPassword, form.confirmPwd)],
  ]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setApiError('');
    const errs = validate();
    setErrors(errs);
    if (Object.keys(errs).length > 0) return;

    setLoading(true);
    try {
      const res = await registrationApi.completeProfile({
        firstName:   form.firstName.trim(),
        lastName:    form.lastName.trim(),
        department:  form.department.trim(),
        position:    form.position.trim() || undefined,
        hireDate:    form.hireDate,
        newPassword: form.newPassword,
      });

      // Update session with new token + mark profile complete
      const newToken = res.data?.token;
      if (newToken) {
        localStorage.setItem('token', newToken);
        localStorage.setItem('isTemporaryPassword', 'false');
        localStorage.setItem('isProfileComplete', 'true');
        localStorage.setItem('userName', `${form.firstName} ${form.lastName}`);
      }

      onComplete();
    } catch (err) {
      setApiError(getErrorMessage(err, 'Viga profiili täitmisel. Proovi uuesti.'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="cp-backdrop">
      <div className="cp-card">
        <div className="cp-brand">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <rect x="3" y="4" width="18" height="18" rx="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8"  y1="2" x2="8"  y2="6"/>
            <line x1="3"  y1="10" x2="21" y2="10"/>
          </svg>
          <span>Puhkusetaotlused</span>
        </div>

        <div className="cp-hero">
          <h1>Tere tulemast!</h1>
          <p>
            Administraator on loonud sulle konto ajutise parooliga.
            Enne jätkamist sea püsiv parool ja täida oma profiil.
          </p>
        </div>

        {apiError && <div className="cp-api-error">{apiError}</div>}

        <form className="cp-form" onSubmit={handleSubmit} noValidate>

          {/* Name row */}
          <div className="cp-row-2">
            <div className={`cp-field ${errors.firstName ? 'cp-field-error' : ''}`}>
              <label>Eesnimi</label>
              <input
                type="text"
                value={form.firstName}
                onChange={set('firstName')}
                placeholder="Mari"
                autoFocus
                disabled={loading}
              />
              {errors.firstName && <span className="cp-err">{errors.firstName}</span>}
            </div>
            <div className={`cp-field ${errors.lastName ? 'cp-field-error' : ''}`}>
              <label>Perekonnanimi</label>
              <input
                type="text"
                value={form.lastName}
                onChange={set('lastName')}
                placeholder="Maasikas"
                disabled={loading}
              />
              {errors.lastName && <span className="cp-err">{errors.lastName}</span>}
            </div>
          </div>

          {/* Department + Position */}
          <div className="cp-row-2">
            <div className={`cp-field ${errors.department ? 'cp-field-error' : ''}`}>
              <label>Osakond</label>
              <input
                type="text"
                value={form.department}
                onChange={set('department')}
                placeholder="IT, HR, Müük..."
                disabled={loading}
              />
              {errors.department && <span className="cp-err">{errors.department}</span>}
            </div>
            <div className="cp-field">
              <label>Ametinimetus <span className="cp-optional">(valikuline)</span></label>
              <input
                type="text"
                value={form.position}
                onChange={set('position')}
                placeholder="Arendaja, analüütik..."
                disabled={loading}
              />
            </div>
          </div>

          {/* Hire date */}
          <div className={`cp-field ${errors.hireDate ? 'cp-field-error' : ''}`}>
            <label>Tööle asumise kuupäev</label>
            <input
              type="date"
              value={form.hireDate}
              onChange={set('hireDate')}
              max={new Date().toISOString().split('T')[0]}
              disabled={loading}
            />
            {errors.hireDate && <span className="cp-err">{errors.hireDate}</span>}
          </div>

          <div className="cp-divider">
            <span>Uus parool</span>
          </div>

          {/* Passwords */}
          <div className="cp-row-2">
            <div className={`cp-field ${errors.newPassword ? 'cp-field-error' : ''}`}>
              <label>Uus parool</label>
              <input
                type="password"
                value={form.newPassword}
                onChange={set('newPassword')}
                placeholder="Vähemalt 8 tähemärki"
                autoComplete="new-password"
                disabled={loading}
              />
              {errors.newPassword && <span className="cp-err">{errors.newPassword}</span>}
            </div>
            <div className={`cp-field ${errors.confirmPwd ? 'cp-field-error' : ''}`}>
              <label>Kinnita parool</label>
              <input
                type="password"
                value={form.confirmPwd}
                onChange={set('confirmPwd')}
                placeholder="Korda parooli"
                autoComplete="new-password"
                disabled={loading}
              />
              {errors.confirmPwd && <span className="cp-err">{errors.confirmPwd}</span>}
            </div>
          </div>

          <div className="cp-pwd-hint">
            Parool peab sisaldama vähemalt 8 tähemärki, üht suurtähte ja üht numbrit.
          </div>

          <button type="submit" className="cp-submit" disabled={loading}>
            {loading ? 'Salvestamine...' : 'Aktiveeri konto'}
          </button>

        </form>
      </div>
    </div>
  );
}
