import React, { useState, useEffect, useRef, useMemo } from 'react';
import VacationRequestForm from './components/VacationRequestForm/VacationRequestForm';
import VacationRequestList from './components/VacationRequestList/VacationRequestList';
import Statistics from './components/Statistics/Statistics';
import AdminDashboard from './components/AdminDashboard/AdminDashboard';
import TeamCalendar from './components/TeamCalendar/TeamCalendar';
import YearTimeline from './components/YearTimeline/YearTimeline';
import { ToastProvider, useToast } from './components/Toast/Toast';
import { vacationRequestsApi, usersApi, setUserRole, setUserId, getCurrentRole, getCurrentUserId } from './api/api';
import './App.css';

function AppContent() {
  const [requests, setRequests] = useState([]);
  const [users, setUsers] = useState([]);
  const [currentUser, setCurrentUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editingRequest, setEditingRequest] = useState(null);
  const [currentView, setCurrentView] = useState('requests'); // 'requests', 'statistics', 'calendar', 'timeline', 'admin'
  const [userRole, setUserRoleState] = useState(getCurrentRole());
  const [selectedUserId, setSelectedUserId] = useState(parseInt(getCurrentUserId()));
  const toast = useToast();
  const formSectionRef = useRef(null);

  const overview = useMemo(() => {
    const pending = requests.filter(r => r.status === 'Pending').length;
    const approved = requests.filter(r => r.status === 'Approved').length;
    const upcoming = requests
      .filter(r => r.status === 'Approved' && new Date(r.startDate) >= new Date())
      .sort((a, b) => new Date(a.startDate) - new Date(b.startDate))[0];

    return { pending, approved, upcoming };
  }, [requests]);

  useEffect(() => {
    fetchUsers();
  }, []);

  useEffect(() => {
    if (userRole === 'employee') {
      fetchRequests();
    } else {
      setLoading(false);
    }
    fetchCurrentUser();
  }, [userRole, selectedUserId]);

  const fetchUsers = async () => {
    try {
      const response = await usersApi.getAll();
      setUsers(response.data);
    } catch (error) {
      console.error('Error fetching users:', error);
    }
  };

  const fetchCurrentUser = async () => {
    try {
      const response = await usersApi.getCurrent();
      setCurrentUser(response.data);
    } catch (error) {
      console.error('Error fetching current user:', error);
    }
  };

  const fetchRequests = async () => {
    try {
      setLoading(true);
      const response = await vacationRequestsApi.getAll();
      setRequests(response.data);
    } catch (error) {
      console.error('Error fetching requests:', error);
      toast.error(error.message || 'Viga andmete laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const handleSuccess = () => {
    setEditingRequest(null);
    fetchRequests();
    fetchCurrentUser(); // Refresh balance
  };

  const handleEdit = (request) => {
    setEditingRequest(request);
    setCurrentView('requests');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleDelete = async (id) => {
    try {
      await vacationRequestsApi.delete(id);
      toast.success('Taotlus kustutatud');
      fetchRequests();
      fetchCurrentUser();
    } catch (error) {
      console.error('Error deleting request:', error);
      toast.error(error.response?.data?.message || 'Viga taotluse kustutamisel');
    }
  };

  const handleWithdraw = async (id) => {
    try {
      await vacationRequestsApi.withdraw(id);
      toast.success('Taotlus tagasi võetud');
      fetchRequests();
      fetchCurrentUser();
    } catch (error) {
      console.error('Error withdrawing request:', error);
      toast.error(error.response?.data?.message || 'Viga taotluse tagasivõtmisel');
    }
  };

  const handleCancelEdit = () => {
    setEditingRequest(null);
  };

  const handleRoleSwitch = (role) => {
    setUserRole(role);
    setUserRoleState(role);
    setCurrentView(role === 'admin' ? 'admin' : 'requests');
    setEditingRequest(null);
    if (role === 'employee') {
      fetchRequests();
    }
  };

  const handleUserSwitch = (userId) => {
    setUserId(userId);
    setSelectedUserId(userId);
    setEditingRequest(null);
    if (userRole === 'employee') {
      fetchRequests();
    }
    fetchCurrentUser();
  };

  const scrollToForm = () => {
    formSectionRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  };

  const renderEmployeeView = () => {
    switch (currentView) {
      case 'statistics':
        return <Statistics key={requests.length} />;
      case 'calendar':
        return <TeamCalendar />;
      case 'timeline':
        return <YearTimeline />;
      default:
        return (
          <div className="dashboard-flow">
            <section ref={formSectionRef} className="primary-action-section">
              <div className="section-kicker">UUS TAOTLUS</div>
              <VacationRequestForm
                onSuccess={handleSuccess}
                editRequest={editingRequest}
                onCancel={handleCancelEdit}
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

  return (
    <div className="App">
      <header className="app-header">
        <div className="header-content">
          <div className="header-left">
            <h1>Puhkusetaotlused</h1>
            <p>Halda puhkusesoove lihtsalt ja selgelt</p>
          </div>
          
          <div className="header-controls">
            {/* User Selector */}
            {users.length > 0 && (
              <div className="user-selector">
                <label htmlFor="userSelect" className="sr-only">Kasutaja</label>
                <select
                  id="userSelect"
                  value={selectedUserId}
                  onChange={(e) => handleUserSwitch(parseInt(e.target.value))}
                  className="user-select"
                >
                  {users.map(user => (
                    <option key={user.id} value={user.id}>
                      {user.fullName} {user.isAdmin && '(Admin)'}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {/* Role Switcher */}
            <div className="role-switcher">
              <button
                className={`role-btn ${userRole === 'employee' ? 'active' : ''}`}
                onClick={() => handleRoleSwitch('employee')}
              >
                Töötaja
              </button>
              <button
                className={`role-btn ${userRole === 'admin' ? 'active' : ''}`}
                onClick={() => handleRoleSwitch('admin')}
              >
                Admin
              </button>
            </div>
          </div>
        </div>
      </header>

      <main className="app-main">
        <div className="container">
          {userRole !== 'admin' && currentUser && (
            <section className="top-summary">
              <div className="top-summary-head">
                <div>
                  <h2>{currentUser.fullName}</h2>
                  <p>{currentUser.department} • {currentUser.position}</p>
                </div>
                <button className="quick-cta" onClick={scrollToForm}>Uus taotlus</button>
              </div>

              <div className="balance-strip">
                <div className="balance-strip-item"><span>Alles</span><strong>{currentUser.remainingLeaveDays}</strong></div>
                <div className="balance-strip-item"><span>Kasutatud</span><strong>{currentUser.usedLeaveDays}</strong></div>
                <div className="balance-strip-item"><span>Aastas</span><strong>{currentUser.annualLeaveDays}</strong></div>
                <div className="balance-strip-item"><span>Ootel</span><strong>{overview.pending}</strong></div>
              </div>

              <div className="top-summary-note">
                {overview.upcoming
                  ? `Järgmine planeeritud puhkus: ${new Date(overview.upcoming.startDate).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })} – ${new Date(overview.upcoming.endDate).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' })}`
                  : 'Sul ei ole veel planeeritud puhkust. Vali kuupäevad ja saada esimene taotlus.'}
              </div>
            </section>
          )}

          {userRole === 'admin' ? (
            <AdminDashboard />
          ) : (
            <>
              <div className="view-toggle">
                <button
                  className={`toggle-btn tab-requests ${currentView === 'requests' ? 'active' : ''}`}
                  onClick={() => setCurrentView('requests')}
                >
                  Taotlused
                </button>
                <button
                  className={`toggle-btn tab-statistics ${currentView === 'statistics' ? 'active' : ''}`}
                  onClick={() => setCurrentView('statistics')}
                >
                  Statistika
                </button>
                <button
                  className={`toggle-btn tab-calendar ${currentView === 'calendar' ? 'active' : ''}`}
                  onClick={() => setCurrentView('calendar')}
                >
                  Kalender
                </button>
                <button
                  className={`toggle-btn tab-timeline ${currentView === 'timeline' ? 'active' : ''}`}
                  onClick={() => setCurrentView('timeline')}
                >
                  Aasta
                </button>
              </div>

              {renderEmployeeView()}
            </>
          )}
        </div>
      </main>

      <footer className="app-footer">
        <p>© 2026 Puhkusetaotluste süsteem • {currentUser?.fullName} ({userRole === 'admin' ? 'Administraator' : 'Töötaja'})</p>
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
