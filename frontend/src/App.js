import React, { useState, useEffect, useRef, useMemo } from 'react';
import VacationRequestForm from './components/VacationRequestForm/VacationRequestForm';
import VacationRequestList from './components/VacationRequestList/VacationRequestList';
import Statistics from './components/Statistics/Statistics';
import AdminDashboard from './components/AdminDashboard/AdminDashboard';
import TeamCalendar from './components/TeamCalendar/TeamCalendar';
import YearTimeline from './components/YearTimeline/YearTimeline';
import { ToastProvider, useToast } from './components/Toast/Toast';
import { vacationRequestsApi, usersApi, setUserId, getCurrentUserId } from './api/api';
import './App.css';

// ── User picker ────────────────────────────────────────────────────────
const initials = (u) =>
  u ? `${u.firstName?.[0] ?? ''}${u.lastName?.[0] ?? ''}`.toUpperCase() : '?';

const UserPicker = ({ users, currentUser, onSelect }) => {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  useEffect(() => {
    if (!open) return;
    const fn = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false); };
    document.addEventListener('mousedown', fn);
    return () => document.removeEventListener('mousedown', fn);
  }, [open]);

  if (!currentUser) return null;

  return (
    <div ref={ref} className={`up-wrapper ${open ? 'up-open' : ''}`}>
      <button
        type="button"
        className="up-trigger"
        onClick={() => setOpen(o => !o)}
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        <div className="up-avatar" aria-hidden="true">{initials(currentUser)}</div>
        <div className="up-info">
          <span className="up-name">{currentUser.fullName}</span>
          <span className="up-meta">
            {currentUser.department}
            {currentUser.isAdmin && <span className="up-admin-badge">Admin</span>}
          </span>
        </div>
        <svg className="up-chevron" width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
          <path d="M6 9l6 6 6-6" />
        </svg>
      </button>

      {open && (
        <div className="up-dropdown" role="listbox">
          <div className="up-dropdown-label">Lülitu kasutajale</div>
          {users.map(u => (
            <button
              key={u.id}
              type="button"
              role="option"
              aria-selected={u.id === currentUser.id}
              className={`up-option ${u.id === currentUser.id ? 'up-option-active' : ''}`}
              onClick={() => { onSelect(u.id); setOpen(false); }}
            >
              <div className="up-option-avatar">{initials(u)}</div>
              <div className="up-option-info">
                <div className="up-option-name">
                  {u.fullName}
                  {u.isAdmin && <span className="up-admin-badge">Admin</span>}
                </div>
                <div className="up-option-meta">
                  {u.department}{u.position ? ` · ${u.position}` : ''}
                </div>
              </div>
              {u.id === currentUser.id && (
                <svg className="up-check" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                  <polyline points="20 6 9 17 4 12" />
                </svg>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  );
};

// ── Main app ───────────────────────────────────────────────────────────
function AppContent() {
  const [requests, setRequests]       = useState([]);
  const [users, setUsers]             = useState([]);
  const [currentUser, setCurrentUser] = useState(null);
  const [loading, setLoading]         = useState(true);
  const [editingRequest, setEditingRequest] = useState(null);
  const [currentView, setCurrentView] = useState('requests');
  // For admin users: 'admin' or 'my-requests'
  const [adminSubView, setAdminSubView] = useState('admin');
  const [selectedUserId, setSelectedUserId] = useState(parseInt(getCurrentUserId()));
  const toast   = useToast();
  const formRef = useRef(null);

  const isAdmin = currentUser?.isAdmin ?? false;

  const overview = useMemo(() => {
    const pending  = requests.filter(r => r.status === 'Pending').length;
    const approved = requests.filter(r => r.status === 'Approved').length;
    const upcoming = requests
      .filter(r => r.status === 'Approved' && new Date(r.startDate) >= new Date())
      .sort((a, b) => new Date(a.startDate) - new Date(b.startDate))[0];
    return { pending, approved, upcoming };
  }, [requests]);

  useEffect(() => { fetchUsers(); }, []);

  useEffect(() => {
    fetchCurrentUser();
    fetchRequests();
  }, [selectedUserId]); // eslint-disable-line

  const fetchUsers = async () => {
    try {
      const r = await usersApi.getAll();
      setUsers(r.data);
    } catch (e) { console.error(e); }
  };

  const fetchCurrentUser = async () => {
    try {
      const r = await usersApi.getCurrent();
      setCurrentUser(r.data);
    } catch (e) { console.error(e); }
  };

  const fetchRequests = async () => {
    try {
      setLoading(true);
      const r = await vacationRequestsApi.getAll();
      setRequests(r.data);
    } catch (e) {
      toast.error(e.message || 'Viga andmete laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const handleSuccess = () => {
    setEditingRequest(null);
    fetchRequests();
    fetchCurrentUser();
  };

  const handleEdit = (request) => {
    setEditingRequest(request);
    // If admin is in admin view, switch to my-requests to show the form
    if (isAdmin) setAdminSubView('my-requests');
    setCurrentView('requests');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleDelete = async (id) => {
    try {
      await vacationRequestsApi.delete(id);
      toast.success('Taotlus kustutatud');
      fetchRequests();
      fetchCurrentUser();
    } catch (e) {
      toast.error(e.response?.data?.message || 'Viga taotluse kustutamisel');
    }
  };

  const handleWithdraw = async (id) => {
    try {
      await vacationRequestsApi.withdraw(id);
      toast.success('Taotlus tagasi võetud');
      fetchRequests();
      fetchCurrentUser();
    } catch (e) {
      toast.error(e.response?.data?.message || 'Viga taotluse tagasivõtmisel');
    }
  };

  const handleUserSwitch = (userId) => {
    setUserId(userId);
    setSelectedUserId(userId);
    setEditingRequest(null);
    setCurrentView('requests');
    setAdminSubView('admin');
  };

  const renderEmployeeDashboard = () => {
    switch (currentView) {
      case 'statistics': return <Statistics key={selectedUserId} />;
      case 'calendar':   return <TeamCalendar />;
      case 'timeline':   return <YearTimeline />;
      default:
        return (
          <div className="dashboard-flow">
            <section ref={formRef} className="primary-action-section">
              <div className="section-kicker">UUS TAOTLUS</div>
              <VacationRequestForm
                onSuccess={handleSuccess}
                editRequest={editingRequest}
                onCancel={() => setEditingRequest(null)}
              />
            </section>
            <section className="history-section">
              <div className="section-kicker">MINU AJALUGU</div>
              <VacationRequestList
                requests={requests}
                onEdit={handleEdit}
                onDelete={handleDelete}
                onWithdraw={handleWithdraw}
                loading={loading}
              />
            </section>
          </div>
        );
    }
  };

  // ── Balance strip (employee + admin "minu puhkus") ─────────────────
  const renderSummaryBar = () => {
    if (!currentUser) return null;
    return (
      <section className="top-summary">
        <div className="top-summary-head">
          <div>
            <h2>{currentUser.fullName}</h2>
            <p>{currentUser.department}{currentUser.position ? ` · ${currentUser.position}` : ''}</p>
          </div>
          <button
            className="quick-cta"
            onClick={() => {
              if (isAdmin) setAdminSubView('my-requests');
              setCurrentView('requests');
              formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }}
          >
            Uus taotlus
          </button>
        </div>
        <div className="balance-strip">
          <div className="balance-strip-item"><span>Alles</span><strong>{currentUser.remainingLeaveDays}</strong></div>
          <div className="balance-strip-item"><span>Kasutatud</span><strong>{currentUser.usedLeaveDays}</strong></div>
          <div className="balance-strip-item"><span>Aastas</span><strong>{currentUser.annualLeaveDays}</strong></div>
          <div className="balance-strip-item"><span>Ootel</span><strong>{overview.pending}</strong></div>
        </div>
        <div className="top-summary-note">
          {overview.upcoming
            ? `Järgmine puhkus: ${new Date(overview.upcoming.startDate).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })} – ${new Date(overview.upcoming.endDate).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}`
            : 'Planeeritud puhkusi pole. Vali kuupäevad ja esita taotlus.'}
        </div>
      </section>
    );
  };

  return (
    <div className="App">
      {/* ── Header ────────────────────────────────────────────────── */}
      <header className="app-header">
        <div className="header-content">
          <div className="header-brand">
            <span className="header-logo">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <rect x="3" y="4" width="18" height="18" rx="2"/>
                <line x1="16" y1="2" x2="16" y2="6"/>
                <line x1="8" y1="2" x2="8" y2="6"/>
                <line x1="3" y1="10" x2="21" y2="10"/>
              </svg>
            </span>
            <h1 className="header-title">Puhkusetaotlused</h1>
          </div>
          <UserPicker
            users={users}
            currentUser={currentUser}
            onSelect={handleUserSwitch}
          />
        </div>
      </header>

      {/* ── Main ──────────────────────────────────────────────────── */}
      <main className="app-main">
        <div className="container">

          {isAdmin ? (
            /* ── Admin ── */
            <div className="admin-wrapper">
              {/* Admin/My-requests toggle */}
              <div className="admin-mode-bar">
                <button
                  className={`admin-mode-btn ${adminSubView === 'admin' ? 'active' : ''}`}
                  onClick={() => setAdminSubView('admin')}
                >
                  Administreerimine
                </button>
                <button
                  className={`admin-mode-btn ${adminSubView === 'my-requests' ? 'active' : ''}`}
                  onClick={() => setAdminSubView('my-requests')}
                >
                  Minu puhkus
                </button>
              </div>

              {adminSubView === 'admin' ? (
                <AdminDashboard currentAdminUserId={selectedUserId} />
              ) : (
                <div>
                  {renderSummaryBar()}
                  <div className="view-toggle">
                    {[
                      { id: 'requests',   label: 'Taotlused',  cls: 'tab-requests'   },
                      { id: 'statistics', label: 'Statistika', cls: 'tab-statistics' },
                      { id: 'calendar',   label: 'Kalender',   cls: 'tab-calendar'   },
                      { id: 'timeline',   label: 'Aasta',      cls: 'tab-timeline'   },
                    ].map(t => (
                      <button
                        key={t.id}
                        className={`toggle-btn ${t.cls} ${currentView === t.id ? 'active' : ''}`}
                        onClick={() => setCurrentView(t.id)}
                      >
                        {t.label}
                      </button>
                    ))}
                  </div>
                  {renderEmployeeDashboard()}
                </div>
              )}
            </div>
          ) : (
            /* ── Employee ── */
            <div>
              {renderSummaryBar()}
              <div className="view-toggle">
                {[
                  { id: 'requests',   label: 'Taotlused',  cls: 'tab-requests'   },
                  { id: 'statistics', label: 'Statistika', cls: 'tab-statistics' },
                  { id: 'calendar',   label: 'Kalender',   cls: 'tab-calendar'   },
                  { id: 'timeline',   label: 'Aasta',      cls: 'tab-timeline'   },
                ].map(t => (
                  <button
                    key={t.id}
                    className={`toggle-btn ${t.cls} ${currentView === t.id ? 'active' : ''}`}
                    onClick={() => setCurrentView(t.id)}
                  >
                    {t.label}
                  </button>
                ))}
              </div>
              {renderEmployeeDashboard()}
            </div>
          )}

        </div>
      </main>

      <footer className="app-footer">
        <p>
          © 2026 Puhkusetaotluste süsteem
          {currentUser && ` · ${currentUser.fullName}`}
          {isAdmin && <span className="footer-admin-badge">Admin</span>}
        </p>
      </footer>
    </div>
  );
}

function App() {
  return (
    <ToastProvider>
      <AppContent />
    </ToastProvider>
  );
}

export default App;
