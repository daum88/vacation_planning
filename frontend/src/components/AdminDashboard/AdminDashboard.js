import React, { useState, useEffect } from 'react';
import { vacationRequestsApi, usersApi, blackoutPeriodsApi, notificationsApi } from '../../api/api';
import { formatDate } from '../../utils/dateUtils';
import { useToast } from '../Toast/Toast';
import './AdminDashboard.css';

const AdminDashboard = () => {
  const [allRequests, setAllRequests] = useState([]);
  const [pendingRequests, setPendingRequests] = useState([]);
  const [users, setUsers] = useState([]);
  const [blackouts, setBlackouts] = useState([]);
  const [notifications, setNotifications] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [view, setView] = useState('pending');
  const [approvingId, setApprovingId] = useState(null);
  const [adminComment, setAdminComment] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(null);
  const [editingCarryOver, setEditingCarryOver] = useState(null);
  const [carryOverValue, setCarryOverValue] = useState(0);
  const [newBlackout, setNewBlackout] = useState({ name: '', description: '', startDate: '', endDate: '' });
  const [showBlackoutForm, setShowBlackoutForm] = useState(false);
  const [activeTab, setActiveTab] = useState('requests');
  const toast = useToast();

  useEffect(() => {
    fetchAll();
  }, [view]);

  const fetchAll = async () => {
    setLoading(true);
    setError(null);
    try {
      const [pendingRes, allRes, usersRes, blackoutsRes] = await Promise.all([
        vacationRequestsApi.getPending(),
        vacationRequestsApi.getAllAdmin(),
        usersApi.getAll(),
        blackoutPeriodsApi.getAll(false),
      ]);
      setPendingRequests(pendingRes.data);
      setAllRequests(allRes.data);
      setUsers(usersRes.data);
      setBlackouts(blackoutsRes.data);
    } catch (err) {
      setError(err.message || 'Viga andmete laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const fetchNotifications = async () => {
    try {
      const res = await notificationsApi.getAll(30);
      setNotifications(res.data);
    } catch (err) {
      console.error('Notifications error', err);
    }
  };

  useEffect(() => {
    if (activeTab === 'notifications') fetchNotifications();
  }, [activeTab]);

  // Team capacity: who's off this week and next week
  const getWeekRange = (offsetWeeks = 0) => {
    const now = new Date();
    const day = now.getDay();
    const monday = new Date(now);
    monday.setDate(now.getDate() - (day === 0 ? 6 : day - 1) + offsetWeeks * 7);
    const friday = new Date(monday);
    friday.setDate(monday.getDate() + 6);
    return { start: monday, end: friday };
  };

  const getAbsentees = (weekStart, weekEnd) => {
    return allRequests.filter(r => {
      if (r.status !== 'Approved') return false;
      const rs = new Date(r.startDate);
      const re = new Date(r.endDate);
      return rs <= weekEnd && re >= weekStart;
    });
  };

  const thisWeek = getWeekRange(0);
  const nextWeek = getWeekRange(1);
  const thisWeekAbsent = getAbsentees(thisWeek.start, thisWeek.end);
  const nextWeekAbsent = getAbsentees(nextWeek.start, nextWeek.end);

  const handleApprove = async (id, approved) => {
    try {
      await vacationRequestsApi.approve(id, { approved, adminComment: adminComment || null });
      toast.success(approved ? 'Taotlus kinnitatud' : 'Taotlus tagasi lükatud');
      setAdminComment('');
      setApprovingId(null);
      fetchAll();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Viga kinnitamisel');
    }
  };

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

  const handleCarryOverSave = async (userId) => {
    try {
      await usersApi.updateCarryOver(userId, carryOverValue);
      toast.success('Ülekanne uuendatud');
      setEditingCarryOver(null);
      fetchAll();
    } catch (err) {
      toast.error('Viga ülekande uuendamisel');
    }
  };

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
    } catch (err) {
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
          <button className={`admin-tab-btn ${activeTab === 'team' ? 'active' : ''}`} onClick={() => setActiveTab('team')}>
            Meeskond
          </button>
          <button className={`admin-tab-btn ${activeTab === 'carryover' ? 'active' : ''}`} onClick={() => setActiveTab('carryover')}>
            Ülekanded
          </button>
          <button className={`admin-tab-btn ${activeTab === 'blackouts' ? 'active' : ''}`} onClick={() => setActiveTab('blackouts')}>
            Blokeeringud
          </button>
          <button className={`admin-tab-btn ${activeTab === 'notifications' ? 'active' : ''}`} onClick={() => setActiveTab('notifications')}>
            Teavitused
          </button>
        </div>
      </div>

      {/* REQUESTS TAB */}
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

          {requests.length === 0 ? (
            <div className="empty-state-admin">Ühtegi {view === 'pending' ? 'ootel ' : ''}taotlust ei leitud.</div>
          ) : (
            <div className="admin-requests-list">
              {requests.map((request) => (
                <div key={request.id} className="admin-request-card">
                  <div className="request-top">
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
                      <strong>Töötaja kommentaar:</strong>
                      <p>{request.comment}</p>
                    </div>
                  )}

                  {request.adminComment && (
                    <div className="admin-comment-box">
                      <strong>Admin kommentaar:</strong>
                      <p>{request.adminComment}</p>
                    </div>
                  )}

                  {request.status === 'Pending' && (
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
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* TEAM CAPACITY TAB */}
      {activeTab === 'team' && (
        <div className="team-capacity">
          <div className="capacity-week">
            <div className="capacity-week-title">
              See nädal
              <span className="capacity-range">
                {thisWeek.start.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })} – {thisWeek.end.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}
              </span>
            </div>
            {thisWeekAbsent.length === 0 ? (
              <p className="capacity-empty">Keegi pole puhkusel.</p>
            ) : (
              <div className="capacity-list">
                {thisWeekAbsent.map(r => (
                  <div key={r.id} className="capacity-row">
                    <div className="capacity-dot" style={{ background: r.leaveTypeColor || '#0071E3' }} />
                    <div>
                      <strong>{r.userName}</strong>
                      <span className="capacity-dept">{r.department}</span>
                    </div>
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
            {nextWeekAbsent.length === 0 ? (
              <p className="capacity-empty">Keegi pole puhkusel.</p>
            ) : (
              <div className="capacity-list">
                {nextWeekAbsent.map(r => (
                  <div key={r.id} className="capacity-row">
                    <div className="capacity-dot" style={{ background: r.leaveTypeColor || '#0071E3' }} />
                    <div>
                      <strong>{r.userName}</strong>
                      <span className="capacity-dept">{r.department}</span>
                    </div>
                    <div className="capacity-dates">{formatDate(r.startDate)} – {formatDate(r.endDate)}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* CARRY-OVER TAB */}
      {activeTab === 'carryover' && (
        <div className="carryover-panel">
          <p className="carryover-desc">Halda töötajate üle kantud puhkusepäevi eelmisest aastast.</p>
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
        </div>
      )}

      {/* BLACKOUTS TAB */}
      {activeTab === 'blackouts' && (
        <div className="blackouts-panel">
          <div className="blackouts-header">
            <p className="carryover-desc">Määra perioodid, mil puhkuse võtmine pole lubatud (nt aastalõpp, auditid).</p>
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
                <input type="text" placeholder="Kirjeldus (valikuline)" value={newBlackout.description}
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
                  <div className="blackout-dates">
                    {formatDate(b.startDate)} – {formatDate(b.endDate)}
                  </div>
                  <button className="btn-delete-admin" onClick={() => handleBlackoutDelete(b.id)}>Kustuta</button>
                </div>
              ))
            )}
          </div>
        </div>
      )}

      {/* NOTIFICATIONS TAB */}
      {activeTab === 'notifications' && (
        <div className="notifications-panel">
          <p className="carryover-desc">Viimased saadetud teavitused (arendusrežiimis logitud, mitte päriselt saadetud).</p>
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
