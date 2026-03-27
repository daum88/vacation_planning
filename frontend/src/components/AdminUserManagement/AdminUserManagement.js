import React, { useState, useEffect, useCallback } from 'react';
import { adminUserManagementApi, organizationsApi } from '../../api/api';
import { useFlash } from '../../hooks/useAsync';
import { useConfirm } from '../../hooks/useAsync';
import { getErrorMessage } from '../../utils/errorUtils';
import './AdminUserManagement.css';

export default function AdminUserManagement() {
  const [users, setUsers] = useState([]);
  const [organizations, setOrganizations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const { flash, showSuccess, showError } = useFlash();
  const { confirmTarget, ask: askDelete, cancel: cancelDelete } = useConfirm();

  // Invite modal
  const [showInvite, setShowInvite] = useState(false);
  const [inviteForm, setInviteForm] = useState({
    email: '', organizationId: '', isAdmin: false, annualLeaveDays: 25,
  });
  const [inviteResult, setInviteResult] = useState(null);
  const [inviteLoading, setInviteLoading] = useState(false);
  const [inviteError, setInviteError] = useState('');

  // confirmDelete now comes from useConfirm hook (confirmTarget, askDelete, cancelDelete)

  const fetchUsers = useCallback(async () => {
    try {
      const res = await adminUserManagementApi.getAll();
      setUsers(res.data || []);
    } catch {
      setError('Kasutajate laadimine ebaõnnestus.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers();
    organizationsApi.getAll()
      .then(res => setOrganizations(res.data || []))
      .catch(() => {});
  }, [fetchUsers]);

  const handleResetPassword = async (userId) => {
    try {
      const res = await adminUserManagementApi.resetPassword(userId);
      showSuccess(`Ajutine parool: ${res.data.temporaryPassword || res.data}`);
    } catch (err) {
      showError(getErrorMessage(err, 'Parooli lähtestamine ebaõnnestus.'));
    }
  };

  const handleToggleActivation = async (userId) => {
    try {
      await adminUserManagementApi.toggleActivation(userId);
      await fetchUsers();
      showSuccess('Kasutaja olek uuendatud.');
    } catch (err) {
      showError(getErrorMessage(err, 'Oleku muutmine ebaõnnestus.'));
    }
  };

  const handleToggleAdmin = async (userId) => {
    try {
      await adminUserManagementApi.toggleAdmin(userId);
      await fetchUsers();
      showSuccess('Admini õigused muudetud.');
    } catch (err) {
      showError(getErrorMessage(err, 'Admini õiguste muutmine ebaõnnestus.'));
    }
  };

  const handleDelete = async (userId) => {
    try {
      await adminUserManagementApi.delete(userId);
      cancelDelete();
      await fetchUsers();
      showSuccess('Kasutaja kustutatud.');
    } catch (err) {
      showError(getErrorMessage(err, 'Kustutamine ebaõnnestus.'));
      cancelDelete();
    }
  };

  const handleInvite = async (e) => {
    e.preventDefault();
    if (!inviteForm.email || !inviteForm.organizationId) {
      setInviteError('Email ja organisatsioon on kohustuslikud.');
      return;
    }
    setInviteLoading(true);
    setInviteError('');
    try {
      const res = await adminUserManagementApi.invite({
        email: inviteForm.email,
        organizationId: parseInt(inviteForm.organizationId),
        isAdmin: inviteForm.isAdmin,
        annualLeaveDays: parseInt(inviteForm.annualLeaveDays),
      });
      setInviteResult(res.data);
      setInviteForm({ email: '', organizationId: '', isAdmin: false, annualLeaveDays: 25 });
      await fetchUsers();
    } catch (err) {
      setInviteError(getErrorMessage(err, 'Kutsumine ebaõnnestus.'));
    } finally {
      setInviteLoading(false);
    }
  };

  if (loading) return <div className="aum-loading">Laadin kasutajaid...</div>;

  return (
    <div className="aum-container">
      <div className="aum-header">
        <div>
          <h2 className="aum-title">Kasutajate haldus</h2>
          <span className="aum-count">{users.length} kasutajat</span>
        </div>
        <button className="aum-btn aum-btn--primary" onClick={() => { setShowInvite(true); setInviteResult(null); }}>
          + Kutsu kasutaja
        </button>
      </div>

      {error && (
        <div className="aum-alert aum-alert--error">
          {error}
          <button className="aum-alert-close" onClick={() => setError('')}>×</button>
        </div>
      )}
      {flash && (
        <div className={`aum-alert aum-alert--${flash.type === 'success' ? 'success' : 'error'}`}>
          {flash.message}
        </div>
      )}

      {/* Invite Modal */}
      {showInvite && (
        <div className="aum-modal-overlay" onClick={() => setShowInvite(false)}>
          <div className="aum-modal" onClick={e => e.stopPropagation()}>
            <div className="aum-modal-header">
              <h3>Kutsu uus kasutaja</h3>
              <button className="aum-modal-close" onClick={() => setShowInvite(false)}>×</button>
            </div>

            {inviteResult ? (
              <div className="aum-invite-result">
                <div className="aum-invite-result__icon">✓</div>
                <p>Kasutaja <strong>{inviteResult.email}</strong> on edukalt kutsutud.</p>
                <p className="aum-invite-result__pw">
                  Ajutine parool: <code>{inviteResult.temporaryPassword}</code>
                </p>
                <p className="aum-invite-result__note">
                  Saada see parool kasutajale. Sisselogimisel peab ta parooli muutma.
                </p>
                <button className="aum-btn aum-btn--primary" onClick={() => { setShowInvite(false); setInviteResult(null); }}>
                  Sulge
                </button>
              </div>
            ) : (
              <form className="aum-invite-form" onSubmit={handleInvite}>
                {inviteError && <div className="aum-alert aum-alert--error">{inviteError}</div>}

                <div className="aum-field">
                  <label>Email</label>
                  <input
                    type="email"
                    value={inviteForm.email}
                    onChange={e => setInviteForm(p => ({ ...p, email: e.target.value }))}
                    placeholder="kasutaja@example.com"
                    required
                  />
                </div>

                <div className="aum-field">
                  <label>Organisatsioon</label>
                  <select
                    value={inviteForm.organizationId}
                    onChange={e => setInviteForm(p => ({ ...p, organizationId: e.target.value }))}
                    required
                  >
                    <option value="">-- Vali --</option>
                    {organizations.map(org => (
                      <option key={org.id} value={org.id}>{org.name}</option>
                    ))}
                  </select>
                </div>

                <div className="aum-field">
                  <label>Aastane puhkuse norm (päeva)</label>
                  <input
                    type="number"
                    min="10" max="50"
                    value={inviteForm.annualLeaveDays}
                    onChange={e => setInviteForm(p => ({ ...p, annualLeaveDays: e.target.value }))}
                  />
                </div>

                <label className="aum-checkbox">
                  <input
                    type="checkbox"
                    checked={inviteForm.isAdmin}
                    onChange={e => setInviteForm(p => ({ ...p, isAdmin: e.target.checked }))}
                  />
                  <span>Administraator</span>
                </label>

                <div className="aum-modal-actions">
                  <button type="button" className="aum-btn aum-btn--ghost" onClick={() => setShowInvite(false)}>
                    Tühista
                  </button>
                  <button type="submit" className="aum-btn aum-btn--primary" disabled={inviteLoading}>
                    {inviteLoading ? 'Kutsun...' : 'Kutsu'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}

      {/* Delete Confirm */}
      {confirmTarget && (
        <div className="aum-modal-overlay" onClick={cancelDelete}>
          <div className="aum-modal aum-modal--confirm" onClick={e => e.stopPropagation()}>
            <h3>Kustuta kasutaja</h3>
            <p>Oled kindel, et soovid kustutada kasutaja <strong>{confirmTarget.email}</strong>?</p>
            <p className="aum-confirm-warning">See toiming on pöördumatu.</p>
            <div className="aum-modal-actions">
              <button className="aum-btn aum-btn--ghost" onClick={cancelDelete}>Tühista</button>
              <button className="aum-btn aum-btn--danger" onClick={() => handleDelete(confirmTarget.id)}>
                Kustuta
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Users Table */}
      <div className="aum-table-wrap">
        <table className="aum-table">
          <thead>
            <tr>
              <th>Kasutaja</th>
              <th>Osakond</th>
              <th>Roll</th>
              <th>Olek</th>
              <th>Puhkus</th>
              <th>Toimingud</th>
            </tr>
          </thead>
          <tbody>
            {users.map(user => (
              <tr key={user.id} className={!user.isActive ? 'aum-row--inactive' : ''}>
                <td>
                  <div className="aum-user-cell">
                    <span className="aum-user-name">{user.firstName} {user.lastName}</span>
                    <span className="aum-user-email">{user.email}</span>
                  </div>
                </td>
                <td><span className="aum-dept">{user.department || '—'}</span></td>
                <td>
                  <span className={`aum-badge aum-badge--${user.isAdmin ? 'admin' : 'user'}`}>
                    {user.isAdmin ? 'Admin' : 'Kasutaja'}
                  </span>
                </td>
                <td>
                  <span className={`aum-badge aum-badge--${user.isActive ? 'active' : 'inactive'}`}>
                    {user.isActive ? 'Aktiivne' : 'Passiivne'}
                  </span>
                </td>
                <td>
                  <span className="aum-leave">
                    {user.remainingLeaveDays ?? user.annualLeaveDays}/{user.annualLeaveDays}p
                  </span>
                </td>
                <td>
                  <div className="aum-actions">
                    <button
                      className="aum-action-btn"
                      title="Lähtesta parool"
                      onClick={() => handleResetPassword(user.id)}
                    >
                      Parool
                    </button>
                    <button
                      className="aum-action-btn"
                      title={user.isActive ? 'Deaktiveeri' : 'Aktiveeri'}
                      onClick={() => handleToggleActivation(user.id)}
                    >
                      {user.isActive ? 'Deaktiveeri' : 'Aktiveeri'}
                    </button>
                    <button
                      className="aum-action-btn"
                      title={user.isAdmin ? 'Eemalda admin' : 'Tee adminiks'}
                      onClick={() => handleToggleAdmin(user.id)}
                    >
                      {user.isAdmin ? '-Admin' : '+Admin'}
                    </button>
                    <button
                      className="aum-action-btn aum-action-btn--danger"
                      title="Kustuta"
                      onClick={() => askDelete({ id: user.id, email: user.email })}
                    >
                      Kustuta
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
