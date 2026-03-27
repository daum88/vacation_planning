import React, { useState, useEffect, useCallback } from 'react';
import { joinRequestsApi } from '../../api/api';
import { useFlash } from '../../hooks/useAsync';
import { getErrorMessage } from '../../utils/errorUtils';
import { fmtShort } from '../../utils/dateUtils';
import { JOIN_STATUS, JOIN_STATUS_LABELS } from '../../constants/appConstants';
import './JoinRequestsManagement.css';

export default function JoinRequestsManagement() {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const { flash, showSuccess, showError } = useFlash();
  const [reviewTarget, setReviewTarget] = useState(null);
  const [reviewForm, setReviewForm] = useState({
    approve: true, note: '', department: '', position: '', annualLeaveDays: 25,
  });
  const [reviewLoading, setReviewLoading] = useState(false);

  const fetchRequests = useCallback(async () => {
    try {
      const res = await joinRequestsApi.getAll();
      setRequests(res.data || []);
    } catch {
      setError('Liitumise taotluste laadimine ebaõnnestus.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchRequests(); }, [fetchRequests]);

  const openReview = (req, approve) => {
    setReviewTarget(req);
    setReviewForm({ approve, note: '', department: '', position: '', annualLeaveDays: 25 });
  };

  const handleReview = async (e) => {
    e.preventDefault();
    if (!reviewTarget) return;
    setReviewLoading(true);
    try {
      await joinRequestsApi.review(reviewTarget.id, {
        approve: reviewForm.approve,
        note: reviewForm.note || undefined,
        department: reviewForm.department || undefined,
        position: reviewForm.position || undefined,
        annualLeaveDays: reviewForm.annualLeaveDays,
      });
      setReviewTarget(null);
      await fetchRequests();
      showSuccess(reviewForm.approve ? 'Taotlus kinnitatud.' : 'Taotlus tagasi lükatud.');
    } catch (err) {
      showError(getErrorMessage(err, 'Taotluse menetlemine ebaõnnestus.'));
    } finally {
      setReviewLoading(false);
    }
  };

  const pending = requests.filter(r => r.status === JOIN_STATUS.PENDING);
  const reviewed = requests.filter(r => r.status !== JOIN_STATUS.PENDING);

  if (loading) return <div className="jrm-loading">Laadin taotlusi...</div>;

  return (
    <div className="jrm-container">
      <div className="jrm-header">
        <h2 className="jrm-title">Liitumise taotlused</h2>
        {pending.length > 0 && (
          <span className="jrm-badge-pending">{pending.length} ootel</span>
        )}
      </div>

      {error && (
        <div className="jrm-alert jrm-alert--error">
          {error}
          <button onClick={() => setError('')}>×</button>
        </div>
      )}
      {flash && (
        <div className={`jrm-alert jrm-alert--${flash.type}`}>
          {flash.message}
        </div>
      )}

      {/* Review modal */}
      {reviewTarget && (
        <div className="jrm-overlay" onClick={() => setReviewTarget(null)}>
          <div className="jrm-modal" onClick={e => e.stopPropagation()}>
            <div className="jrm-modal-header">
              <h3>{reviewForm.approve ? 'Kinnita taotlus' : 'Lükka tagasi'}</h3>
              <button onClick={() => setReviewTarget(null)}>×</button>
            </div>

            <div className="jrm-modal-info">
              <span className="jrm-modal-user">{reviewTarget.userFullName}</span>
              <span className="jrm-modal-email">{reviewTarget.userEmail}</span>
              <span className="jrm-modal-org">{reviewTarget.organizationName}</span>
            </div>

            {reviewTarget.message && (
              <div className="jrm-modal-message">
                <label>Sõnum</label>
                <p>{reviewTarget.message}</p>
              </div>
            )}

            <form className="jrm-review-form" onSubmit={handleReview}>
              {reviewForm.approve && (
                <>
                  <div className="jrm-field">
                    <label>Osakond <span className="jrm-required">*</span></label>
                    <input
                      type="text"
                      value={reviewForm.department}
                      onChange={e => setReviewForm(p => ({ ...p, department: e.target.value }))}
                      placeholder="IT, HR, Müük..."
                      required
                    />
                  </div>
                  <div className="jrm-field">
                    <label>Ametikoht</label>
                    <input
                      type="text"
                      value={reviewForm.position}
                      onChange={e => setReviewForm(p => ({ ...p, position: e.target.value }))}
                      placeholder="Arendaja, analüütik..."
                    />
                  </div>
                  <div className="jrm-field">
                    <label>Aastane puhkuse norm (päeva)</label>
                    <input
                      type="number"
                      min="10" max="50"
                      value={reviewForm.annualLeaveDays}
                      onChange={e => setReviewForm(p => ({ ...p, annualLeaveDays: parseInt(e.target.value) }))}
                    />
                  </div>
                </>
              )}

              <div className="jrm-field">
                <label>{reviewForm.approve ? 'Märkus (valikuline)' : 'Põhjus (valikuline)'}</label>
                <textarea
                  rows={3}
                  value={reviewForm.note}
                  onChange={e => setReviewForm(p => ({ ...p, note: e.target.value }))}
                  placeholder={reviewForm.approve ? 'Tere tulemast meeskonda!' : 'Põhjenda otsust...'}
                />
              </div>

              <div className="jrm-modal-actions">
                <button type="button" className="jrm-btn jrm-btn--ghost" onClick={() => setReviewTarget(null)}>
                  Tühista
                </button>
                <button
                  type="submit"
                  className={`jrm-btn ${reviewForm.approve ? 'jrm-btn--approve' : 'jrm-btn--reject'}`}
                  disabled={reviewLoading}
                >
                  {reviewLoading ? 'Töötlen...' : reviewForm.approve ? 'Kinnita' : 'Lükka tagasi'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Pending requests */}
      {pending.length === 0 && reviewed.length === 0 && (
        <div className="jrm-empty">Liitumise taotlusi pole.</div>
      )}

      {pending.length > 0 && (
        <section className="jrm-section">
          <h3 className="jrm-section-title">Ootel taotlused ({pending.length})</h3>
          <div className="jrm-list">
            {pending.map(req => (
              <div key={req.id} className="jrm-card jrm-card--pending">
                <div className="jrm-card-body">
                  <div className="jrm-card-info">
                    <span className="jrm-user-name">{req.userFullName}</span>
                    <span className="jrm-user-email">{req.userEmail}</span>
                    <span className="jrm-org">{req.organizationName}</span>
                    {req.message && <span className="jrm-message">"{req.message}"</span>}
                  </div>
                  <div className="jrm-card-meta">
                    <span className="jrm-date">
                      {fmtShort(req.createdAt)}
                    </span>
                  </div>
                </div>
                <div className="jrm-card-actions">
                  <button className="jrm-btn jrm-btn--approve" onClick={() => openReview(req, true)}>
                    Kinnita
                  </button>
                  <button className="jrm-btn jrm-btn--reject" onClick={() => openReview(req, false)}>
                    Lükka tagasi
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {reviewed.length > 0 && (
        <section className="jrm-section">
          <h3 className="jrm-section-title">Menetletud taotlused</h3>
          <div className="jrm-table-wrap">
            <table className="jrm-table">
              <thead>
                <tr>
                  <th>Kasutaja</th>
                  <th>Organisatsioon</th>
                  <th>Olek</th>
                  <th>Menetleja</th>
                  <th>Märkus</th>
                  <th>Kuupäev</th>
                </tr>
              </thead>
              <tbody>
                {reviewed.map(req => (
                  <tr key={req.id}>
                    <td>
                      <div className="jrm-cell-user">
                        <span>{req.userFullName}</span>
                        <span className="jrm-cell-email">{req.userEmail}</span>
                      </div>
                    </td>
                    <td>{req.organizationName}</td>
                    <td>
                      <span className={`jrm-status jrm-status--${req.status.toLowerCase()}`}>
                        {JOIN_STATUS_LABELS[req.status] || req.status}
                      </span>
                    </td>
                    <td>{req.reviewedByName || '—'}</td>
                    <td>{req.reviewNote || '—'}</td>
                    <td>{req.reviewedAt ? fmtShort(req.reviewedAt) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </div>
  );
}
