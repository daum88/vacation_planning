import React, { useState, useEffect } from 'react';
import {
  vacationRequestsApi, usersApi, blackoutPeriodsApi,
  notificationsApi, departmentCapacityApi
} from '../../api/api';
import { formatDate } from '../../utils/dateUtils';
import { useToast } from '../Toast/Toast';
import CommentThread from '../CommentThread/CommentThread';
import CustomSelect from '../CustomSelect/CustomSelect';
import DatePicker from '../DatePicker/DatePicker';
import './AdminDashboard.css';

const TABS = [
  { id: 'requests',      label: 'Taotlused' },
  { id: 'team',          label: 'Meeskond' },
  { id: 'carryover',     label: 'Ülekanded' },
  { id: 'capacity',      label: 'Limiidid' },
  { id: 'blackouts',     label: 'Blokeeringud' },
  { id: 'notifications', label: 'Teavituste logi' },
];

const STATUS_META = {
  Pending:   { label: 'Ootel',           cls: 'st-pending' },
  Approved:  { label: 'Kinnitatud',      cls: 'st-approved' },
  Rejected:  { label: 'Tagasi lükatud', cls: 'st-rejected' },
  Withdrawn: { label: 'Tagasi võetud',  cls: 'st-withdrawn' },
};

const StatusBadge = ({ status }) => {
  const m = STATUS_META[status] || STATUS_META.Pending;
  return <span className={`a-status ${m.cls}`}>{m.label}</span>;
};

const AdminDashboard = ({ currentAdminUserId }) => {
  const [allRequests, setAllRequests]       = useState([]);
  const [pendingRequests, setPendingRequests] = useState([]);
  const [users, setUsers]                   = useState([]);
  const [blackouts, setBlackouts]           = useState([]);
  const [notifications, setNotifications]   = useState([]);
  const [deptCapacities, setDeptCapacities] = useState([]);
  const [loading, setLoading]               = useState(true);
  const [error, setError]                   = useState(null);
  const [view, setView]                     = useState('pending');
  const [approvingId, setApprovingId]       = useState(null);
  const [adminComment, setAdminComment]     = useState('');
  const [confirmDelete, setConfirmDelete]   = useState(null);
  const [expandedComments, setExpandedComments] = useState(null);
  const [editingCarryOver, setEditingCarryOver] = useState(null);
  const [carryOverValue, setCarryOverValue] = useState(0);
  const [newBlackout, setNewBlackout]       = useState({ name: '', description: '', startDate: '', endDate: '' });
  const [showBlackoutForm, setShowBlackoutForm] = useState(false);
  const [newCapacity, setNewCapacity]       = useState({ department: '', maxConcurrent: 2 });
  const [showCapacityForm, setShowCapacityForm] = useState(false);
  const [editingCapacity, setEditingCapacity] = useState(null);
  const [editCapacityValue, setEditCapacityValue] = useState(2);
  const [selectedRequests, setSelectedRequests] = useState(new Set());
  const [bulkComment, setBulkComment]       = useState('');
  const [resetMaxCarry, setResetMaxCarry]   = useState(5);
  const [resetResult, setResetResult]       = useState(null);
  const [showResetConfirm, setShowResetConfirm] = useState(false);
  const [activeTab, setActiveTab]           = useState('requests');
  const toast = useToast();

  useEffect(() => { fetchAll(); }, [view]); // eslint-disable-line

  const fetchAll = async () => {
    setLoading(true);
    setError(null);
    try {
      const [pendingRes, allRes, usersRes, blackoutsRes, deptRes] = await Promise.all([
        vacationRequestsApi.getPending(),
        vacationRequestsApi.getAllAdmin(),
        usersApi.getAll(),
        blackoutPeriodsApi.getAll(false),
        departmentCapacityApi.getAll(),
      ]);
      setPendingRequests(pendingRes.data);
      setAllRequests(allRes.data);
      setUsers(usersRes.data);
      setBlackouts(blackoutsRes.data);
      setDeptCapacities(deptRes.data);
    } catch (err) {
      setError(err.message || 'Viga andmete laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const fetchNotifications = async () => {
    try {
      const res = await notificationsApi.getAll(50);
      setNotifications(res.data);
    } catch (err) { console.error('Notifications error', err); }
  };

  useEffect(() => {
    if (activeTab === 'notifications') fetchNotifications();
  }, [activeTab]);

  // ── Week helpers ───────────────────────────────────────────────────
  const getWeekRange = (offsetWeeks = 0) => {
    const now = new Date();
    const day = now.getDay();
    const monday = new Date(now);
    monday.setDate(now.getDate() - (day === 0 ? 6 : day - 1) + offsetWeeks * 7);
    monday.setHours(0, 0, 0, 0);
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);
    return { start: monday, end: sunday };
  };

  const getAbsentees = (weekStart, weekEnd) =>
    allRequests.filter(r => {
      if (r.status !== 'Approved') return false;
      return new Date(r.startDate) <= weekEnd && new Date(r.endDate) >= weekStart;
    });

  const thisWeek   = getWeekRange(0);
  const nextWeek   = getWeekRange(1);
  const thisWeekAbsent = getAbsentees(thisWeek.start, thisWeek.end);
  const nextWeekAbsent = getAbsentees(nextWeek.start, nextWeek.end);

  // ── Approve ────────────────────────────────────────────────────────
  const handleApprove = async (id, approved) => {
    try {
      await vacationRequestsApi.approve(id, { approved, adminComment: adminComment || null });
      toast.success(approved ? 'Taotlus kinnitatud' : 'Taotlus tagasi lükatud');
      setAdminComment('');
      setApprovingId(null);
      setSelectedRequests(prev => { const n = new Set(prev); n.delete(id); return n; });
      fetchAll();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Viga kinnitamisel');
    }
  };

  // ── Bulk approve ───────────────────────────────────────────────────
  const handleBulkApprove = async (approved) => {
    if (selectedRequests.size === 0) return;
    const items = Array.from(selectedRequests).map(id => ({
      id, approved, adminComment: bulkComment || null
    }));
    try {
      const res = await vacationRequestsApi.bulkApprove(items);
      const { succeeded, failed, errors } = res.data;
      if (succeeded > 0) toast.success(`${succeeded} taotlust ${approved ? 'kinnitatud' : 'tagasi lükatud'}`);
      if (failed   > 0) toast.error(`${failed} ebaõnnestus: ${errors.join(', ')}`);
      setSelectedRequests(new Set());
      setBulkComment('');
      fetchAll();
    } catch { toast.error('Viga hulgi-töötlusel'); }
  };

  const toggleSelect = (id) => {
    setSelectedRequests(prev => {
      const n = new Set(prev);
      n.has(id) ? n.delete(id) : n.add(id);
      return n;
    });
  };

  const selectAllPending = () => {
    const allIds = pendingRequests.map(r => r.id);
    if (selectedRequests.size === allIds.length) setSelectedRequests(new Set());
    else setSelectedRequests(new Set(allIds));
  };

  // ── Delete ─────────────────────────────────────────────────────────
  const handleDelete = async (id) => {
    try {
      await vacationRequestsApi.deleteAdmin(id);
      toast.success('Taotlus kustutatud');
      setConfirmDelete(null);
      fetchAll();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Viga kustutamisel');
    }
  };

  // ── Carry-over ─────────────────────────────────────────────────────
  const handleCarryOverSave = async (userId) => {
    try {
      await usersApi.updateCarryOver(userId, carryOverValue);
      toast.success('Ülekanne uuendatud');
      setEditingCarryOver(null);
      fetchAll();
    } catch { toast.error('Viga ülekande uuendamisel'); }
  };

  // ── Annual reset ───────────────────────────────────────────────────
  const handleAnnualReset = async () => {
    try {
      const res = await usersApi.annualReset(resetMaxCarry);
      setResetResult(res.data);
      setShowResetConfirm(false);
      toast.success(`${res.data.usersReset} töötajat lähtestatud`);
      fetchAll();
    } catch { toast.error('Viga aastase lähtestamise tegemisel'); }
  };

  // ── Blackouts ──────────────────────────────────────────────────────
  const handleBlackoutCreate = async () => {
    if (!newBlackout.name || !newBlackout.startDate || !newBlackout.endDate) {
      toast.error('Täida kõik kohustuslikud väljad');
      return;
    }
    try {
      await blackoutPeriodsApi.create(newBlackout);
      toast.success('Blokeerimisperiood loodud');
      setNewBlackout({ name: '', description: '', startDate: '', endDate: '' });
      setShowBlackoutForm(false);
      fetchAll();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Viga loomisel');
    }
  };

  const handleBlackoutDelete = async (id) => {
    try {
      await blackoutPeriodsApi.delete(id);
      toast.success('Blokeerimisperiood kustutatud');
      fetchAll();
    } catch { toast.error('Viga kustutamisel'); }
  };

  // ── Dept Capacity ──────────────────────────────────────────────────
  const handleCapacityCreate = async () => {
    if (!newCapacity.department) { toast.error('Osakond on kohustuslik'); return; }
    try {
      await departmentCapacityApi.create(newCapacity);
      toast.success('Limiit salvestatud');
      setNewCapacity({ department: '', maxConcurrent: 2 });
      setShowCapacityForm(false);
      fetchAll();
    } catch { toast.error('Viga limiidi salvestamisel'); }
  };

  const handleCapacitySave = async (id) => {
    try {
      const cap = deptCapacities.find(c => c.id === id);
      await departmentCapacityApi.update(id, { department: cap.department, maxConcurrent: editCapacityValue });
      toast.success('Limiit uuendatud');
      setEditingCapacity(null);
      fetchAll();
    } catch { toast.error('Viga limiidi uuendamisel'); }
  };

  const handleCapacityDelete = async (id) => {
    try {
      await departmentCapacityApi.delete(id);
      toast.success('Limiit kustutatud');
      fetchAll();
    } catch { toast.error('Viga kustutamisel'); }
  };

  const iCalUrl = (userId) => vacationRequestsApi.getICalFeedUrl(userId);
  const requests = view === 'pending' ? pendingRequests : allRequests;

  // ─────────────────────────────────────────────────────────────────
  if (loading) return (
    <div className="admin-shell">
      <div className="admin-loading">Laadimine…</div>
    </div>
  );
  if (error) return (
    <div className="admin-shell">
      <div className="admin-error">{error}</div>
    </div>
  );

  return (
    <div className="admin-shell">

      {/* ── Page header ──────────────────────────────────────────────── */}
      <div className="admin-page-header">
        <div>
          <h1 className="admin-page-title">Administreerimine</h1>
          <p className="admin-page-sub">Halda taotlusi, meeskonda ja seadeid.</p>
        </div>
        <div className="admin-summary-pills">
          <span className="a-pill">
            <span className="a-pill-num">{pendingRequests.length}</span>
            ootel
          </span>
          <span className="a-pill">
            <span className="a-pill-num">{allRequests.filter(r => r.status === 'Approved').length}</span>
            kinnitatud
          </span>
          <span className="a-pill">
            <span className="a-pill-num">{users.length}</span>
            töötajat
          </span>
        </div>
      </div>

      {/* ── Tab bar ──────────────────────────────────────────────────── */}
      <div className="admin-tab-bar">
        {TABS.map(t => (
          <button
            key={t.id}
            className={`a-tab ${activeTab === t.id ? 'a-tab-active' : ''}`}
            onClick={() => setActiveTab(t.id)}
          >
            {t.label}
            {t.id === 'requests' && pendingRequests.length > 0 && (
              <span className="a-tab-count">{pendingRequests.length}</span>
            )}
          </button>
        ))}
      </div>

      {/* ── Tab content ──────────────────────────────────────────────── */}
      <div className="admin-tab-content">

        {/* ─── REQUESTS ──────────────────────────────────────────────── */}
        {activeTab === 'requests' && (
          <div className="a-section">
            {/* View sub-toggle */}
            <div className="a-view-toggle">
              <button
                className={`a-view-btn ${view === 'pending' ? 'active' : ''}`}
                onClick={() => setView('pending')}
              >
                Ootel
                <span className="a-view-count">{pendingRequests.length}</span>
              </button>
              <button
                className={`a-view-btn ${view === 'all' ? 'active' : ''}`}
                onClick={() => setView('all')}
              >
                Kõik
                <span className="a-view-count">{allRequests.length}</span>
              </button>
            </div>

            {/* Bulk bar */}
            {view === 'pending' && pendingRequests.length > 0 && (
              <div className="a-bulk-bar">
                <label className="a-bulk-check-label">
                  <input
                    type="checkbox"
                    checked={selectedRequests.size === pendingRequests.length && pendingRequests.length > 0}
                    onChange={selectAllPending}
                  />
                  <span>Vali kõik ({pendingRequests.length})</span>
                </label>

                {selectedRequests.size > 0 && (
                  <div className="a-bulk-actions">
                    <span className="a-bulk-count">{selectedRequests.size} valitud</span>
                    <input
                      type="text"
                      className="a-bulk-comment"
                      placeholder="Valikuline kommentaar valitutele…"
                      value={bulkComment}
                      onChange={e => setBulkComment(e.target.value)}
                    />
                    <button className="a-btn a-btn-approve" onClick={() => handleBulkApprove(true)}>
                      Kinnita valitud
                    </button>
                    <button className="a-btn a-btn-reject" onClick={() => handleBulkApprove(false)}>
                      Lükka tagasi
                    </button>
                    <button className="a-btn a-btn-ghost" onClick={() => setSelectedRequests(new Set())}>
                      Tühista valik
                    </button>
                  </div>
                )}
              </div>
            )}

            {requests.length === 0 ? (
              <div className="a-empty">
                Ühtegi {view === 'pending' ? 'ootel ' : ''}taotlust ei leitud.
              </div>
            ) : (
              <div className="a-request-list">
                {requests.map(req => (
                  <div
                    key={req.id}
                    className={`a-request-row ${req.status.toLowerCase()} ${selectedRequests.has(req.id) ? 'row-selected' : ''}`}
                  >
                    {/* Checkbox */}
                    {view === 'pending' && (
                      <input
                        type="checkbox"
                        className="a-row-check"
                        checked={selectedRequests.has(req.id)}
                        onChange={() => toggleSelect(req.id)}
                      />
                    )}

                    {/* Status stripe */}
                    <div className={`a-status-stripe stripe-${req.status.toLowerCase()}`} />

                    {/* Main content */}
                    <div className="a-row-main">
                      <div className="a-row-top">
                        <div className="a-row-who">
                          <span className="a-row-name">{req.userName || `Töötaja #${req.userId}`}</span>
                          {req.department && <span className="a-row-dept">{req.department}</span>}
                        </div>
                        <div className="a-row-meta">
                          <span className="a-row-dates">
                            {formatDate(req.startDate)} – {formatDate(req.endDate)}
                          </span>
                          <span className="a-row-days">{req.daysCount} tööpäeva</span>
                          {req.leaveTypeName && (
                            <span
                              className="a-row-leavetype"
                              style={{ borderLeftColor: req.leaveTypeColor }}
                            >
                              {req.leaveTypeName}
                            </span>
                          )}
                        </div>
                        <StatusBadge status={req.status} />
                      </div>

                      {/* Secondary info */}
                      {(req.comment || req.substituteName || req.adminComment) && (
                        <div className="a-row-details">
                          {req.substituteName && (
                            <span className="a-row-sub">Asendaja: {req.substituteName}</span>
                          )}
                          {req.comment && (
                            <span className="a-row-comment">"{req.comment}"</span>
                          )}
                          {req.adminComment && (
                            <span className="a-row-admin-comment">Admin: {req.adminComment}</span>
                          )}
                        </div>
                      )}

                      {/* Approve form */}
                      {req.status === 'Pending' && req.userId === currentAdminUserId && (
                        <div className="a-own-request-note">
                          Oma taotlust ei saa kinnitada — peab kinnitama teine administraator.
                        </div>
                      )}

                      {req.status === 'Pending' && req.userId !== currentAdminUserId && approvingId === req.id && (
                        <div className="a-approve-form">
                          <textarea
                            className="a-approve-textarea"
                            value={adminComment}
                            onChange={e => setAdminComment(e.target.value)}
                            placeholder="Valikuline kommentaar töötajale…"
                            rows={2}
                            maxLength={500}
                          />
                          <div className="a-approve-actions">
                            <button className="a-btn a-btn-approve" onClick={() => handleApprove(req.id, true)}>
                              Kinnita
                            </button>
                            <button className="a-btn a-btn-reject" onClick={() => handleApprove(req.id, false)}>
                              Lükka tagasi
                            </button>
                            <button
                              className="a-btn a-btn-ghost"
                              onClick={() => { setApprovingId(null); setAdminComment(''); }}
                            >
                              Tühista
                            </button>
                          </div>
                        </div>
                      )}

                      {/* Row actions */}
                      <div className="a-row-actions">
                        {req.status === 'Pending' && approvingId !== req.id && req.userId !== currentAdminUserId && (
                          <button
                            className="a-action-btn"
                            onClick={() => setApprovingId(req.id)}
                          >
                            Vaata üle
                          </button>
                        )}
                        <button
                          className="a-action-btn"
                          onClick={() => setExpandedComments(expandedComments === req.id ? null : req.id)}
                        >
                          {expandedComments === req.id ? 'Peida sõnumid' : 'Sõnumid'}
                        </button>
                        {confirmDelete === req.id ? (
                          <span className="a-confirm-delete">
                            Kustutada?
                            <button className="a-action-btn a-action-danger" onClick={() => handleDelete(req.id)}>
                              Jah
                            </button>
                            <button className="a-action-btn" onClick={() => setConfirmDelete(null)}>
                              Ei
                            </button>
                          </span>
                        ) : (
                          <button
                            className="a-action-btn a-action-danger"
                            onClick={() => setConfirmDelete(req.id)}
                          >
                            Kustuta
                          </button>
                        )}
                      </div>

                      {/* Comment thread */}
                      {expandedComments === req.id && (
                        <div className="a-comment-thread">
                          <CommentThread requestId={req.id} isAdmin={true} />
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* ─── TEAM ──────────────────────────────────────────────────── */}
        {activeTab === 'team' && (
          <div className="a-section">
            <div className="a-two-col">
              <WeekPanel
                title="See nädal"
                range={thisWeek}
                absences={thisWeekAbsent}
              />
              <WeekPanel
                title="Järgmine nädal"
                range={nextWeek}
                absences={nextWeekAbsent}
              />
            </div>

            {/* Full upcoming list */}
            <div className="a-subsection">
              <div className="a-subsection-title">Kõik kinnitatud puhkused (tulevased)</div>
              {(() => {
                const today = new Date();
                today.setHours(0,0,0,0);
                const upcoming = allRequests
                  .filter(r => r.status === 'Approved' && new Date(r.endDate) >= today)
                  .sort((a, b) => new Date(a.startDate) - new Date(b.startDate));
                if (upcoming.length === 0) {
                  return <div className="a-empty">Tulevasi kinnitatud puhkusi ei leitud.</div>;
                }
                return (
                  <table className="a-table">
                    <thead>
                      <tr>
                        <th>Töötaja</th>
                        <th>Osakond</th>
                        <th>Algus</th>
                        <th>Lõpp</th>
                        <th>Päevi</th>
                        <th>Tüüp</th>
                      </tr>
                    </thead>
                    <tbody>
                      {upcoming.map(r => (
                        <tr key={r.id}>
                          <td><strong>{r.userName}</strong></td>
                          <td>{r.department || '—'}</td>
                          <td>{formatDate(r.startDate)}</td>
                          <td>{formatDate(r.endDate)}</td>
                          <td>{r.daysCount}</td>
                          <td>
                            <span className="a-ltype-cell" style={{ borderLeftColor: r.leaveTypeColor }}>
                              {r.leaveTypeName}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                );
              })()}
            </div>
          </div>
        )}

        {/* ─── CARRY-OVER ────────────────────────────────────────────── */}
        {activeTab === 'carryover' && (
          <div className="a-section">
            <div className="a-section-header">
              <div>
                <div className="a-section-title">Puhkusepäevade ülekanded</div>
                <div className="a-section-desc">
                  Halda üle kantud päevade arvu ja tee aastane lähtestamine.
                </div>
              </div>
              {!showResetConfirm ? (
                <button className="a-btn a-btn-secondary" onClick={() => setShowResetConfirm(true)}>
                  Aastane lähtestamine
                </button>
              ) : (
                <div className="a-inline-form">
                  <label className="a-form-label">Max ülekanne päevades</label>
                  <input
                    type="number" min="0" max="30"
                    value={resetMaxCarry}
                    onChange={e => setResetMaxCarry(parseInt(e.target.value) || 0)}
                    className="a-num-input"
                  />
                  <button className="a-btn a-btn-reject" onClick={handleAnnualReset}>
                    Käivita lähtestamine
                  </button>
                  <button className="a-btn a-btn-ghost" onClick={() => setShowResetConfirm(false)}>
                    Tühista
                  </button>
                </div>
              )}
            </div>

            {resetResult && (
              <div className="a-success-banner">
                <strong>{resetResult.usersReset} töötajat lähtestatud</strong> — kasutatud päevad nullitud,
                max {resetResult.maxCarryOverDays} päeva kantud üle.
                <button className="a-banner-close" onClick={() => setResetResult(null)}>×</button>
                <div className="a-banner-details">
                  {resetResult.details.map(d => (
                    <span key={d.userId} className="a-banner-chip">
                      {d.userName}: {d.previousUsedDays}→0 kasutatud, +{d.newCarryOver} üle
                    </span>
                  ))}
                </div>
              </div>
            )}

            <table className="a-table">
              <thead>
                <tr>
                  <th>Töötaja</th>
                  <th>Osakond</th>
                  <th>Aastane norm</th>
                  <th>Kasutatud</th>
                  <th>Alles</th>
                  <th>Üle kantud</th>
                  <th>Toiming</th>
                </tr>
              </thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id}>
                    <td><strong>{u.fullName}</strong></td>
                    <td>{u.department || '—'}</td>
                    <td>{u.annualLeaveDays} p</td>
                    <td>{u.usedLeaveDays} p</td>
                    <td>
                      <strong style={{ color: u.remainingLeaveDays < 5 ? '#c0392b' : 'inherit' }}>
                        {u.remainingLeaveDays} p
                      </strong>
                    </td>
                    <td>
                      {editingCarryOver === u.id ? (
                        <div className="a-inline-edit">
                          <input
                            type="number" min="0" max="30"
                            value={carryOverValue}
                            onChange={e => setCarryOverValue(parseInt(e.target.value) || 0)}
                            className="a-num-input"
                          />
                          <button className="a-action-btn a-action-ok" onClick={() => handleCarryOverSave(u.id)}>
                            Salvesta
                          </button>
                          <button className="a-action-btn" onClick={() => setEditingCarryOver(null)}>
                            Tühista
                          </button>
                        </div>
                      ) : (
                        <span className="a-carry-val">+{u.carryOverDays} p</span>
                      )}
                    </td>
                    <td>
                      {editingCarryOver !== u.id && (
                        <button
                          className="a-action-btn"
                          onClick={() => { setEditingCarryOver(u.id); setCarryOverValue(u.carryOverDays); }}
                        >
                          Muuda
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* iCal feeds */}
            <div className="a-subsection">
              <div className="a-subsection-title">Kalendri tellimislingid</div>
              <p className="a-section-desc">
                Kopeeri link Google Calendari, Outlooki vm, et puhkused automaatselt sünkida.
              </p>
              <table className="a-table">
                <thead>
                  <tr>
                    <th>Töötaja</th>
                    <th>Tellimislink</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {users.map(u => (
                    <tr key={u.id}>
                      <td><strong>{u.fullName}</strong></td>
                      <td>
                        <input
                          type="text"
                          readOnly
                          className="a-ical-input"
                          value={iCalUrl(u.id)}
                          onFocus={e => e.target.select()}
                        />
                      </td>
                      <td>
                        <button
                          className="a-action-btn"
                          onClick={() => { navigator.clipboard.writeText(iCalUrl(u.id)); toast.success('Link kopeeritud'); }}
                        >
                          Kopeeri
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* ─── CAPACITY ──────────────────────────────────────────────── */}
        {activeTab === 'capacity' && (
          <div className="a-section">
            <div className="a-section-header">
              <div>
                <div className="a-section-title">Osakondade korraga-limiidid</div>
                <div className="a-section-desc">
                  Mitu töötajat võib korraga samast osakonnast puhkusel olla.
                  Ületamine blokeerib uue taotluse esitamise.
                </div>
              </div>
              <button
                className="a-btn a-btn-secondary"
                onClick={() => setShowCapacityForm(f => !f)}
              >
                {showCapacityForm ? 'Tühista' : 'Lisa osakond'}
              </button>
            </div>

            {showCapacityForm && (
              <div className="a-inline-form a-form-panel">
                <input
                  type="text"
                  placeholder="Osakonna nimi *"
                  value={newCapacity.department}
                  onChange={e => setNewCapacity(p => ({ ...p, department: e.target.value }))}
                  className="a-text-input"
                />
                <div className="a-num-group">
                  <label className="a-form-label">Max korraga</label>
                  <input
                    type="number" min="1" max="50"
                    value={newCapacity.maxConcurrent}
                    onChange={e => setNewCapacity(p => ({ ...p, maxConcurrent: parseInt(e.target.value) || 1 }))}
                    className="a-num-input"
                  />
                  <span className="a-form-unit">inimest</span>
                </div>
                <button className="a-btn a-btn-primary" onClick={handleCapacityCreate}>
                  Salvesta
                </button>
              </div>
            )}

            {deptCapacities.length === 0 ? (
              <div className="a-empty">Osakondade limiite pole veel määratud.</div>
            ) : (
              <table className="a-table">
                <thead>
                  <tr>
                    <th>Osakond</th>
                    <th>Max korraga</th>
                    <th>Staatus</th>
                    <th>Toiming</th>
                  </tr>
                </thead>
                <tbody>
                  {deptCapacities.map(cap => (
                    <tr key={cap.id}>
                      <td><strong>{cap.department}</strong></td>
                      <td>
                        {editingCapacity === cap.id ? (
                          <div className="a-inline-edit">
                            <input
                              type="number" min="1" max="50"
                              value={editCapacityValue}
                              onChange={e => setEditCapacityValue(parseInt(e.target.value) || 1)}
                              className="a-num-input"
                            />
                            <span className="a-form-unit">inimest</span>
                            <button className="a-action-btn a-action-ok" onClick={() => handleCapacitySave(cap.id)}>Salvesta</button>
                            <button className="a-action-btn" onClick={() => setEditingCapacity(null)}>Tühista</button>
                          </div>
                        ) : (
                          <span className="a-cap-num">{cap.maxConcurrent} inimest</span>
                        )}
                      </td>
                      <td>
                        {cap.isActive
                          ? <span className="a-status st-approved">Aktiivne</span>
                          : <span className="a-status st-withdrawn">Mitteaktiivne</span>
                        }
                      </td>
                      <td>
                        {editingCapacity !== cap.id && (
                          <div className="a-action-group">
                            <button className="a-action-btn" onClick={() => { setEditingCapacity(cap.id); setEditCapacityValue(cap.maxConcurrent); }}>Muuda</button>
                            <button className="a-action-btn a-action-danger" onClick={() => handleCapacityDelete(cap.id)}>Kustuta</button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

        {/* ─── BLACKOUTS ─────────────────────────────────────────────── */}
        {activeTab === 'blackouts' && (
          <div className="a-section">
            <div className="a-section-header">
              <div>
                <div className="a-section-title">Blokeeritud perioodid</div>
                <div className="a-section-desc">
                  Ajavahemikud, mil uute puhkusetaotluste esitamine ei ole lubatud.
                </div>
              </div>
              <button
                className="a-btn a-btn-secondary"
                onClick={() => setShowBlackoutForm(f => !f)}
              >
                {showBlackoutForm ? 'Tühista' : 'Lisa periood'}
              </button>
            </div>

            {showBlackoutForm && (
              <div className="a-form-panel">
                <div className="a-form-grid">
                  <div className="a-form-field">
                    <label className="a-form-label">Nimi *</label>
                    <input
                      type="text"
                      placeholder="nt. Suvine sulgemine"
                      value={newBlackout.name}
                      onChange={e => setNewBlackout(p => ({ ...p, name: e.target.value }))}
                      className="a-text-input"
                    />
                  </div>
                  <div className="a-form-field">
                    <label className="a-form-label">Kirjeldus</label>
                    <input
                      type="text"
                      placeholder="Valikuline selgitus"
                      value={newBlackout.description}
                      onChange={e => setNewBlackout(p => ({ ...p, description: e.target.value }))}
                      className="a-text-input"
                    />
                  </div>
                  <div className="a-form-field">
                    <label className="a-form-label">Alguskuupäev *</label>
                    <DatePicker
                      value={newBlackout.startDate}
                      onChange={v => setNewBlackout(p => ({ ...p, startDate: v }))}
                      placeholder="Vali algus"
                    />
                  </div>
                  <div className="a-form-field">
                    <label className="a-form-label">Lõppkuupäev *</label>
                    <DatePicker
                      value={newBlackout.endDate}
                      onChange={v => setNewBlackout(p => ({ ...p, endDate: v }))}
                      minDate={newBlackout.startDate}
                      placeholder="Vali lõpp"
                    />
                  </div>
                </div>
                <div className="a-form-actions">
                  <button className="a-btn a-btn-primary" onClick={handleBlackoutCreate}>
                    Loo periood
                  </button>
                </div>
              </div>
            )}

            {blackouts.length === 0 ? (
              <div className="a-empty">Blokeerimisperioode pole lisatud.</div>
            ) : (
              <table className="a-table">
                <thead>
                  <tr>
                    <th>Nimi</th>
                    <th>Kirjeldus</th>
                    <th>Algus</th>
                    <th>Lõpp</th>
                    <th>Staatus</th>
                    <th>Toiming</th>
                  </tr>
                </thead>
                <tbody>
                  {blackouts.map(b => (
                    <tr key={b.id} className={!b.isActive ? 'a-row-inactive' : ''}>
                      <td><strong>{b.name}</strong></td>
                      <td>{b.description || '—'}</td>
                      <td>{formatDate(b.startDate)}</td>
                      <td>{formatDate(b.endDate)}</td>
                      <td>
                        {b.isActive
                          ? <span className="a-status st-approved">Aktiivne</span>
                          : <span className="a-status st-withdrawn">Mitteaktiivne</span>
                        }
                      </td>
                      <td>
                        <button className="a-action-btn a-action-danger" onClick={() => handleBlackoutDelete(b.id)}>
                          Kustuta
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

        {/* ─── NOTIFICATIONS ─────────────────────────────────────────── */}
        {activeTab === 'notifications' && (
          <div className="a-section">
            <div className="a-section-header">
              <div>
                <div className="a-section-title">Teavituste logi</div>
                <div className="a-section-desc">
                  Logitud e-kirjad — arendusrežiimis ei saadeta päriselt välja.
                </div>
              </div>
              <button className="a-btn a-btn-ghost" onClick={fetchNotifications}>
                Uuenda
              </button>
            </div>

            {notifications.length === 0 ? (
              <div className="a-empty">Teavitusi pole veel logitud.</div>
            ) : (
              <table className="a-table a-table-fixed">
                <thead>
                  <tr>
                    <th style={{ width: 110 }}>Tüüp</th>
                    <th style={{ width: 180 }}>Saaja</th>
                    <th>Teema</th>
                    <th style={{ width: 130 }}>Aeg</th>
                    <th style={{ width: 50 }}></th>
                  </tr>
                </thead>
                <tbody>
                  {notifications.map(n => (
                    <tr key={n.id}>
                      <td>
                        <span className="a-notif-type">{n.type}</span>
                      </td>
                      <td className="a-cell-truncate">{n.toEmail}</td>
                      <td className="a-cell-truncate">{n.subject}</td>
                      <td className="a-cell-time">
                        {new Date(n.sentAt).toLocaleDateString('et-EE')}{' '}
                        {new Date(n.sentAt).toLocaleTimeString('et-EE', { hour: '2-digit', minute: '2-digit' })}
                      </td>
                      <td>
                        {n.isMock && <span className="a-mock-badge">mock</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

      </div>
    </div>
  );
};

// ── WeekPanel sub-component ────────────────────────────────────────────
const WeekPanel = ({ title, range, absences }) => (
  <div className="a-week-panel">
    <div className="a-week-header">
      <span className="a-week-title">{title}</span>
      <span className="a-week-range">
        {range.start.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}–{range.end.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}
      </span>
      {absences.length > 0 && (
        <span className="a-week-count">{absences.length} puhkusel</span>
      )}
    </div>
    {absences.length === 0 ? (
      <div className="a-week-empty">Keegi pole puhkusel.</div>
    ) : (
      <div className="a-week-list">
        {absences.map(r => (
          <div key={r.id} className="a-week-row">
            <span
              className="a-week-dot"
              style={{ background: r.leaveTypeColor || '#0071E3' }}
            />
            <span className="a-week-who">
              <strong>{r.userName}</strong>
              {r.department && <span className="a-week-dept">{r.department}</span>}
            </span>
            <span className="a-week-dates">
              {new Date(r.startDate).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}–{new Date(r.endDate).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}
            </span>
          </div>
        ))}
      </div>
    )}
  </div>
);

export default AdminDashboard;
