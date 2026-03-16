import React, { useState, useEffect } from 'react';
import {
  vacationRequestsApi, usersApi, blackoutPeriodsApi,
  notificationsApi, departmentCapacityApi
} from '../../api/api';
import { formatDate } from '../../utils/dateUtils';
import { useToast } from '../Toast/Toast';
import CommentThread from '../CommentThread/CommentThread';
import './AdminDashboard.css';

const AdminDashboard = () => {
  const [allRequests, setAllRequests] = useState([]);
  const [pendingRequests, setPendingRequests] = useState([]);
  const [users, setUsers] = useState([]);
  const [blackouts, setBlackouts] = useState([]);
  const [notifications, setNotifications] = useState([]);
  const [deptCapacities, setDeptCapacities] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [view, setView] = useState('pending');
  const [approvingId, setApprovingId] = useState(null);
  const [adminComment, setAdminComment] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(null);
  const [expandedComments, setExpandedComments] = useState(null);
  const [editingCarryOver, setEditingCarryOver] = useState(null);
  const [carryOverValue, setCarryOverValue] = useState(0);
  const [newBlackout, setNewBlackout] = useState({ name: '', description: '', startDate: '', endDate: '' });
  const [showBlackoutForm, setShowBlackoutForm] = useState(false);
  const [newCapacity, setNewCapacity] = useState({ department: '', maxConcurrent: 2 });
  const [showCapacityForm, setShowCapacityForm] = useState(false);
  const [editingCapacity, setEditingCapacity] = useState(null);
  const [editCapacityValue, setEditCapacityValue] = useState(2);
  const [selectedRequests, setSelectedRequests] = useState(new Set());
  const [bulkComment, setBulkComment] = useState('');
  const [resetMaxCarry, setResetMaxCarry] = useState(5);
  const [resetResult, setResetResult] = useState(null);
  const [showResetConfirm, setShowResetConfirm] = useState(false);
  const [activeTab, setActiveTab] = useState('requests');
  const toast = useToast();

  useEffect(() => {
    fetchAll();
  }, [view]);

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
    } catch (err) {
      console.error('Notifications error', err);
    }
  };

  useEffect(() => {
    if (activeTab === 'notifications') fetchNotifications();
  }, [activeTab]);

  // ── Team capacity helpers ──────────────────────────────────────────
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

  const thisWeek = getWeekRange(0);
  const nextWeek = getWeekRange(1);
  const thisWeekAbsent = getAbsentees(thisWeek.start, thisWeek.end);
  const nextWeekAbsent = getAbsentees(nextWeek.start, nextWeek.end);

  // ── Single approve ─────────────────────────────────────────────────
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
      if (failed > 0) toast.error(`${failed} ebaõnnestus: ${errors.join(', ')}`);
      setSelectedRequests(new Set());
      setBulkComment('');
      fetchAll();
    } catch (err) {
      toast.error('Viga hulgi-töötlusel');
    }
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
    if (selectedRequests.size === allIds.length) {
      setSelectedRequests(new Set());
    } else {
      setSelectedRequests(new Set(allIds));
    }
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
    } catch {
      toast.error('Viga ülekande uuendamisel');
    }
  };

  // ── Annual reset ───────────────────────────────────────────────────
  const handleAnnualReset = async () => {
    try {
      const res = await usersApi.annualReset(resetMaxCarry);
      setResetResult(res.data);
      setShowResetConfirm(false);
      toast.success(`${res.data.usersReset} töötajat lähtestatud`);
      fetchAll();
    } catch {
      toast.error('Viga aastase lähtestamise tegemisel');
    }
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
    } catch {
      toast.error('Viga kustutamisel');
    }
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
    } catch {
      toast.error('Viga limiidi salvestamisel');
    }
  };

  const handleCapacitySave = async (id) => {
    try {
      const cap = deptCapacities.find(c => c.id === id);
      await departmentCapacityApi.update(id, { department: cap.department, maxConcurrent: editCapacityValue });
      toast.success('Limiit uuendatud');
      setEditingCapacity(null);
      fetchAll();
    } catch {
      toast.error('Viga limiidi uuendamisel');
    }
  };

  const handleCapacityDelete = async (id) => {
    try {
      await departmentCapacityApi.delete(id);
      toast.success('Limiit kustutatud');
      fetchAll();
    } catch {
      toast.error('Viga kustutamisel');
    }
  };

  const getStatusBadge = (status) => {
    const map = {
      'Pending': { text: 'Ootel', cls: 'pending' },
      'Approved': { text: 'Kinnitatud', cls: 'approved' },
      'Rejected': { text: 'Tagasi lükatud', cls: 'rejected' },
    };
    const s = map[status] || map['Pending'];
    return <span className={`status-badge ${s.cls}`}>{s.text}</span>;
  };

  const iCalUrl = (userId) => vacationRequestsApi.getICalFeedUrl(userId);

  const requests = view === 'pending' ? pendingRequests : allRequests;

  if (loading) return <div className="admin-dashboard"><div className="loading">Laadimine...</div></div>;
  if (error) return <div className="admin-dashboard"><div className="error-box">{error}</div></div>;

  return (
    <div className="admin-dashboard">
      <div className="admin-header">
        <h2>Admin juhtpaneel</h2>
        <div className="admin-tab-nav">
          <button className={`admin-tab-btn ${activeTab === 'requests' ? 'active' : ''}`} onClick={() => setActiveTab('requests')}>
            Taotlused {pendingRequests.length > 0 && <span className="tab-badge">{pendingRequests.length}</span>}
          </button>
          <button className={`admin-tab-btn ${activeTab === 'team' ? 'active' : ''}`} onClick={() => setActiveTab('team')}>Meeskond</button>
          <button className={`admin-tab-btn ${activeTab === 'carryover' ? 'active' : ''}`} onClick={() => setActiveTab('carryover')}>Ülekanded</button>
          <button className={`admin-tab-btn ${activeTab === 'capacity' ? 'active' : ''}`} onClick={() => setActiveTab('capacity')}>Limiidid</button>
          <button className={`admin-tab-btn ${activeTab === 'blackouts' ? 'active' : ''}`} onClick={() => setActiveTab('blackouts')}>Blokeeringud</button>
          <button className={`admin-tab-btn ${activeTab === 'notifications' ? 'active' : ''}`} onClick={() => setActiveTab('notifications')}>Teavitused</button>
        </div>
      </div>

      {/* ─── REQUESTS TAB ─────────────────────────────────────────────── */}
      {activeTab === 'requests' && (
        <div>
          <div className="view-switcher" style={{ marginBottom: 12 }}>
            <button className={`view-btn ${view === 'pending' ? 'active' : ''}`} onClick={() => setView('pending')}>
              Ootel ({pendingRequests.length})
            </button>
            <button className={`view-btn ${view === 'all' ? 'active' : ''}`} onClick={() => setView('all')}>
              Kõik ({allRequests.length})
            </button>
          </div>

          {/* Bulk actions bar — only shown when items selected or on pending view */}
          {view === 'pending' && pendingRequests.length > 0 && (
            <div className="bulk-bar">
              <label className="bulk-select-all">
                <input
                  type="checkbox"
                  checked={selectedRequests.size === pendingRequests.length && pendingRequests.length > 0}
                  onChange={selectAllPending}
                />
                <span>Vali kõik ({pendingRequests.length})</span>
              </label>
              {selectedRequests.size > 0 && (
                <>
                  <span className="bulk-selected-count">{selectedRequests.size} valitud</span>
                  <input
                    type="text"
                    className="bulk-comment-input"
                    placeholder="Valikuline kommentaar valitutele..."
                    value={bulkComment}
                    onChange={e => setBulkComment(e.target.value)}
                  />
                  <button className="btn btn-approve" onClick={() => handleBulkApprove(true)}>
                    Kinnita valitud
                  </button>
                  <button className="btn btn-reject" onClick={() => handleBulkApprove(false)}>
                    Lükka tagasi
                  </button>
                  <button className="btn btn-cancel" onClick={() => setSelectedRequests(new Set())}>Tühista</button>
                </>
              )}
            </div>
          )}

          {requests.length === 0 ? (
            <div className="empty-state-admin">Ühtegi {view === 'pending' ? 'ootel ' : ''}taotlust ei leitud.</div>
          ) : (
            <div className="admin-requests-list">
              {requests.map((request) => (
                <div
                  key={request.id}
                  className={`admin-request-card ${selectedRequests.has(request.id) ? 'selected' : ''}`}
                >
                  <div className="request-top">
                    {view === 'pending' && (
                      <input
                        type="checkbox"
                        className="request-checkbox"
                        checked={selectedRequests.has(request.id)}
                        onChange={() => toggleSelect(request.id)}
                      />
                    )}
                    <div className="request-info">
                      <div className="request-user">
                        <strong>{request.userName || `Töötaja #${request.userId}`}</strong>
                        {request.department && <span className="dept-tag">{request.department}</span>}
                      </div>
                      <div className="request-dates-inline">
                        {formatDate(request.startDate)} → {formatDate(request.endDate)}
                        <span className="days-text">{request.daysCount} tööpäeva</span>
                        {request.leaveTypeName && (
                          <span className="leave-type-tag" style={{ borderColor: request.leaveTypeColor }}>
                            {request.leaveTypeName}
                          </span>
                        )}
                      </div>
                      {request.substituteName && (
                        <div className="request-substitute-admin">Asendaja: {request.substituteName}</div>
                      )}
                    </div>
                    {getStatusBadge(request.status)}
                  </div>

                  {request.comment && (
                    <div className="request-comment-box">
                      <strong>Kommentaar:</strong>
                      <p>{request.comment}</p>
                    </div>
                  )}
                  {request.adminComment && (
                    <div className="admin-comment-box">
                      <strong>Admin kommentaar:</strong>
                      <p>{request.adminComment}</p>
                    </div>
                  )}

                  {request.status === 'Pending' && !selectedRequests.has(request.id) && (
                    <div className="approval-section">
                      {approvingId === request.id ? (
                        <div className="approval-form">
                          <textarea value={adminComment}
                            onChange={(e) => setAdminComment(e.target.value)}
                            placeholder="Valikuline kommentaar töötajale..." rows="2" maxLength="500" />
                          <div className="approval-actions">
                            <button className="btn btn-approve" onClick={() => handleApprove(request.id, true)}>Kinnita</button>
                            <button className="btn btn-reject" onClick={() => handleApprove(request.id, false)}>Lükka tagasi</button>
                            <button className="btn btn-cancel" onClick={() => { setApprovingId(null); setAdminComment(''); }}>Tühista</button>
                          </div>
                        </div>
                      ) : (
                        <button className="btn btn-review" onClick={() => setApprovingId(request.id)}>Vaata üle</button>
                      )}
                    </div>
                  )}

                  <div className="admin-actions">
                    <button
                      className="btn-link"
                      onClick={() => setExpandedComments(expandedComments === request.id ? null : request.id)}
                    >
                      {expandedComments === request.id ? 'Peida sõnumid' : 'Sõnumid'}
                    </button>
                    {confirmDelete === request.id ? (
                      <div className="confirm-delete-row">
                        <span>Kustutada?</span>
                        <button className="btn btn-delete-confirm" onClick={() => handleDelete(request.id)}>Jah</button>
                        <button className="btn btn-cancel" onClick={() => setConfirmDelete(null)}>Ei</button>
                      </div>
                    ) : (
                      <button className="btn-delete-admin" onClick={() => setConfirmDelete(request.id)}>Kustuta</button>
                    )}
                  </div>

                  {expandedComments === request.id && (
                    <div className="admin-comment-thread">
                      <CommentThread requestId={request.id} isAdmin={true} />
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* ─── TEAM CAPACITY TAB ────────────────────────────────────────── */}
      {activeTab === 'team' && (
        <div className="team-capacity">
          <div className="capacity-week">
            <div className="capacity-week-title">
              See nädal
              <span className="capacity-range">
                {thisWeek.start.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })} – {thisWeek.end.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}
              </span>
            </div>
            {thisWeekAbsent.length === 0 ? <p className="capacity-empty">Keegi pole puhkusel.</p> : (
              <div className="capacity-list">
                {thisWeekAbsent.map(r => (
                  <div key={r.id} className="capacity-row">
                    <div className="capacity-dot" style={{ background: r.leaveTypeColor || '#0071E3' }} />
                    <div><strong>{r.userName}</strong><span className="capacity-dept">{r.department}</span></div>
                    <div className="capacity-dates">{formatDate(r.startDate)} – {formatDate(r.endDate)}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
          <div className="capacity-week">
            <div className="capacity-week-title">
              Järgmine nädal
              <span className="capacity-range">
                {nextWeek.start.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })} – {nextWeek.end.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}
              </span>
            </div>
            {nextWeekAbsent.length === 0 ? <p className="capacity-empty">Keegi pole puhkusel.</p> : (
              <div className="capacity-list">
                {nextWeekAbsent.map(r => (
                  <div key={r.id} className="capacity-row">
                    <div className="capacity-dot" style={{ background: r.leaveTypeColor || '#0071E3' }} />
                    <div><strong>{r.userName}</strong><span className="capacity-dept">{r.department}</span></div>
                    <div className="capacity-dates">{formatDate(r.startDate)} – {formatDate(r.endDate)}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* ─── CARRY-OVER TAB ──────────────────────────────────────────── */}
      {activeTab === 'carryover' && (
        <div className="carryover-panel">
          <div className="carryover-header">
            <p className="carryover-desc">Halda töötajate üle kantud puhkusepäevi.</p>
            <div className="annual-reset-section">
              {showResetConfirm ? (
                <div className="reset-confirm-row">
                  <span>Max ülekanne:</span>
                  <input type="number" min="0" max="30" value={resetMaxCarry}
                    onChange={e => setResetMaxCarry(parseInt(e.target.value) || 0)}
                    className="carryover-input" />
                  <span>päeva</span>
                  <button className="btn btn-reject" onClick={handleAnnualReset}>Kinnita lähtestamine</button>
                  <button className="btn btn-cancel" onClick={() => setShowResetConfirm(false)}>Tühista</button>
                </div>
              ) : (
                <button className="btn btn-review" onClick={() => setShowResetConfirm(true)}>
                  Aastane lähtestamine
                </button>
              )}
            </div>
          </div>

          {resetResult && (
            <div className="reset-result">
              <strong>{resetResult.usersReset} töötajat lähtestatud</strong> — kasutatud päevad nullitud,
              max {resetResult.maxCarryOverDays} päeva kantud üle.
              <button className="btn-link" style={{ marginLeft: 8 }} onClick={() => setResetResult(null)}>×</button>
              <div className="reset-details">
                {resetResult.details.map(d => (
                  <span key={d.userId}>{d.userName}: {d.previousUsedDays} → 0 kasutatud, +{d.newCarryOver} üle</span>
                ))}
              </div>
            </div>
          )}

          <div className="carryover-list">
            {users.map(user => (
              <div key={user.id} className="carryover-row">
                <div className="carryover-user">
                  <strong>{user.fullName}</strong>
                  <span className="carryover-dept">{user.department}</span>
                </div>
                <div className="carryover-balance">
                  <span>Aastane: {user.annualLeaveDays}</span>
                  <span>Kasutatud: {user.usedLeaveDays}</span>
                  <span>Alles: {user.remainingLeaveDays}</span>
                </div>
                <div className="carryover-edit">
                  {editingCarryOver === user.id ? (
                    <>
                      <span>Ülekanne:</span>
                      <input type="number" min="0" max="30" value={carryOverValue}
                        onChange={e => setCarryOverValue(parseInt(e.target.value) || 0)}
                        className="carryover-input" />
                      <button className="btn btn-approve" onClick={() => handleCarryOverSave(user.id)}>Salvesta</button>
                      <button className="btn btn-cancel" onClick={() => setEditingCarryOver(null)}>Tühista</button>
                    </>
                  ) : (
                    <>
                      <span className="carryover-current">+{user.carryOverDays} üle kantud</span>
                      <button className="btn btn-review" onClick={() => { setEditingCarryOver(user.id); setCarryOverValue(user.carryOverDays); }}>Muuda</button>
                    </>
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* iCal feed URLs */}
          <div className="ical-section">
            <div className="ical-title">Kalendri tellimislingid</div>
            <p className="carryover-desc">Kasuta neid linke Google Calendari, Outlooki vm rakenduses puhkuste automaatseks sünkimiseks.</p>
            <div className="ical-list">
              {users.map(u => (
                <div key={u.id} className="ical-row">
                  <span className="ical-name">{u.fullName}</span>
                  <input
                    type="text"
                    readOnly
                    className="ical-url"
                    value={iCalUrl(u.id)}
                    onFocus={e => e.target.select()}
                  />
                  <button className="btn btn-review" onClick={() => {
                    navigator.clipboard.writeText(iCalUrl(u.id));
                    toast.success('Link kopeeritud');
                  }}>Kopeeri</button>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* ─── DEPARTMENT CAPACITY TAB ─────────────────────────────────── */}
      {activeTab === 'capacity' && (
        <div className="capacity-panel">
          <div className="capacity-panel-header">
            <p className="carryover-desc">
              Määra maksimaalne arv töötajaid, kes võivad korraga puhkusel olla sama osakonnast.
              Ületamine blokeerib taotluse esitamise.
            </p>
            <button className="btn btn-review" onClick={() => setShowCapacityForm(f => !f)}>
              {showCapacityForm ? 'Tühista' : 'Lisa uus'}
            </button>
          </div>

          {showCapacityForm && (
            <div className="blackout-form">
              <div className="blackout-form-row">
                <input
                  type="text"
                  placeholder="Osakond *"
                  value={newCapacity.department}
                  onChange={e => setNewCapacity(p => ({ ...p, department: e.target.value }))}
                  className="blackout-input"
                />
                <div className="capacity-input-group">
                  <span>Max korraga:</span>
                  <input
                    type="number" min="1" max="50"
                    value={newCapacity.maxConcurrent}
                    onChange={e => setNewCapacity(p => ({ ...p, maxConcurrent: parseInt(e.target.value) || 1 }))}
                    className="carryover-input"
                  />
                  <span>inimest</span>
                </div>
                <button className="btn btn-approve" onClick={handleCapacityCreate}>Salvesta</button>
              </div>
            </div>
          )}

          <div className="capacity-limits-list">
            {deptCapacities.length === 0 ? (
              <p className="capacity-empty">Osakondade limiite pole määratud.</p>
            ) : (
              deptCapacities.map(cap => (
                <div key={cap.id} className="capacity-limit-row">
                  <div className="capacity-limit-dept">
                    <strong>{cap.department}</strong>
                    {!cap.isActive && <span className="inactive-badge">mitteaktiivne</span>}
                  </div>
                  <div className="capacity-limit-value">
                    {editingCapacity === cap.id ? (
                      <>
                        <span>Max:</span>
                        <input type="number" min="1" max="50"
                          value={editCapacityValue}
                          onChange={e => setEditCapacityValue(parseInt(e.target.value) || 1)}
                          className="carryover-input" />
                        <span>inimest</span>
                        <button className="btn btn-approve" onClick={() => handleCapacitySave(cap.id)}>Salvesta</button>
                        <button className="btn btn-cancel" onClick={() => setEditingCapacity(null)}>Tühista</button>
                      </>
                    ) : (
                      <>
                        <span className="capacity-number">max {cap.maxConcurrent} korraga</span>
                        <button className="btn btn-review" onClick={() => { setEditingCapacity(cap.id); setEditCapacityValue(cap.maxConcurrent); }}>Muuda</button>
                        <button className="btn-delete-admin" onClick={() => handleCapacityDelete(cap.id)}>Kustuta</button>
                      </>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* ─── BLACKOUTS TAB ───────────────────────────────────────────── */}
      {activeTab === 'blackouts' && (
        <div className="blackouts-panel">
          <div className="blackouts-header">
            <p className="carryover-desc">Perioodid, mil puhkuse võtmine ei ole lubatud.</p>
            <button className="btn btn-review" onClick={() => setShowBlackoutForm(f => !f)}>
              {showBlackoutForm ? 'Tühista' : 'Lisa uus'}
            </button>
          </div>
          {showBlackoutForm && (
            <div className="blackout-form">
              <div className="blackout-form-row">
                <input type="text" placeholder="Nimi *" value={newBlackout.name}
                  onChange={e => setNewBlackout(p => ({ ...p, name: e.target.value }))}
                  className="blackout-input" />
                <input type="text" placeholder="Kirjeldus" value={newBlackout.description}
                  onChange={e => setNewBlackout(p => ({ ...p, description: e.target.value }))}
                  className="blackout-input" />
                <input type="date" value={newBlackout.startDate}
                  onChange={e => setNewBlackout(p => ({ ...p, startDate: e.target.value }))}
                  className="blackout-input" />
                <input type="date" value={newBlackout.endDate}
                  onChange={e => setNewBlackout(p => ({ ...p, endDate: e.target.value }))}
                  className="blackout-input" />
                <button className="btn btn-approve" onClick={handleBlackoutCreate}>Loo</button>
              </div>
            </div>
          )}
          <div className="blackouts-list">
            {blackouts.length === 0 ? (
              <p className="capacity-empty">Blokeerimisperioode pole lisatud.</p>
            ) : (
              blackouts.map(b => (
                <div key={b.id} className={`blackout-row ${!b.isActive ? 'inactive' : ''}`}>
                  <div>
                    <strong>{b.name}</strong>
                    {b.description && <span className="blackout-desc"> — {b.description}</span>}
                  </div>
                  <div className="blackout-dates">{formatDate(b.startDate)} – {formatDate(b.endDate)}</div>
                  <button className="btn-delete-admin" onClick={() => handleBlackoutDelete(b.id)}>Kustuta</button>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* ─── NOTIFICATIONS TAB ───────────────────────────────────────── */}
      {activeTab === 'notifications' && (
        <div className="notifications-panel">
          <p className="carryover-desc">Viimased saadetud teavitused (logitud, mitte päriselt saadetud arendusrežiimis).</p>
          {notifications.length === 0 ? (
            <p className="capacity-empty">Teavitusi pole veel saadetud.</p>
          ) : (
            <div className="notifications-list">
              {notifications.map(n => (
                <div key={n.id} className="notification-row">
                  <div className="notification-type">{n.type}</div>
                  <div className="notification-to">{n.toEmail}</div>
                  <div className="notification-subject">{n.subject}</div>
                  <div className="notification-time">
                    {new Date(n.sentAt).toLocaleDateString('et-EE')} {new Date(n.sentAt).toLocaleTimeString('et-EE', { hour: '2-digit', minute: '2-digit' })}
                  </div>
                  {n.isMock && <span className="notification-mock">mock</span>}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default AdminDashboard;
