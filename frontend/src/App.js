import React, { useState, useEffect } from 'react';
import VacationRequestForm from './components/VacationRequestForm/VacationRequestForm';
import VacationRequestList from './components/VacationRequestList/VacationRequestList';
import Statistics from './components/Statistics/Statistics';
import AdminDashboard from './components/AdminDashboard/AdminDashboard';
import TeamCalendar from './components/TeamCalendar/TeamCalendar';
import { ToastProvider, useToast } from './components/Toast/Toast';
import { vacationRequestsApi, usersApi, setUserRole, setUserId, getCurrentRole, getCurrentUserId } from './api/api';
import './App.css';

function AppContent() {
  const [requests, setRequests] = useState([]);
  const [users, setUsers] = useState([]);
  const [currentUser, setCurrentUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [editingRequest, setEditingRequest] = useState(null);
  const [currentView, setCurrentView] = useState('requests'); // 'requests', 'statistics', 'calendar', 'admin'
  const [userRole, setUserRoleState] = useState(getCurrentRole());
  const [selectedUserId, setSelectedUserId] = useState(parseInt(getCurrentUserId()));
  const toast = useToast();

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
    if (window.confirm('Kas oled kindel, et soovid selle taotluse kustutada?')) {
      try {
        await vacationRequestsApi.delete(id);
        toast.success('Taotlus kustutatud ✓');
        fetchRequests();
        fetchCurrentUser(); // Refresh balance
      } catch (error) {
        console.error('Error deleting request:', error);
        toast.error(error.response?.data?.message || 'Viga taotluse kustutamisel');
      }
    }
  };

  const handleWithdraw = async (id) => {
    if (window.confirm('Kas oled kindel, et soovid selle taotluse tagasi võtta?')) {
      try {
        await vacationRequestsApi.withdraw(id);
        toast.success('Taotlus tagasi võetud ✓');
        fetchRequests();
        fetchCurrentUser(); // Refresh balance
      } catch (error) {
        console.error('Error withdrawing request:', error);
        toast.error(error.response?.data?.message || 'Viga taotluse tagasivõtmisel');
      }
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

  const renderEmployeeView = () => {
    switch (currentView) {
      case 'statistics':
        return <Statistics key={requests.length} />;
      case 'calendar':
        return <TeamCalendar />;
      default:
        return (
          <>
            <VacationRequestForm
              onSuccess={handleSuccess}
              editRequest={editingRequest}
              onCancel={handleCancelEdit}
            />
            <VacationRequestList
              requests={requests}
              onEdit={handleEdit}
              onDelete={handleDelete}
              onWithdraw={handleWithdraw}
              loading={loading}
            />
          </>
        );
    }
  };

  return (
    <div className="App">
      <header className="app-header">
        <div className="header-content">
          <div className="header-left">
            <h1>🏖️ Puhkusetaotlused</h1>
            <p>Halda oma puhkusesoove lihtsalt ja mugavalt</p>
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
                      {user.fullName} {user.isAdmin && '👔'}
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
                👤 Töötaja
              </button>
              <button
                className={`role-btn ${userRole === 'admin' ? 'active' : ''}`}
                onClick={() => handleRoleSwitch('admin')}
              >
                👔 Admin
              </button>
            </div>
          </div>
        </div>
      </header>

      <main className="app-main">
        <div className="container">
          {/* User Info Card */}
          {currentUser && (
            <div className="user-info-card">
              <div className="user-details">
                <strong>{currentUser.fullName}</strong>
                <span>{currentUser.department} • {currentUser.position}</span>
              </div>
              <div className="user-balance">
                <div className="balance-stat">
                  <span className="stat-value">{currentUser.remainingLeaveDays}</span>
                  <span className="stat-label">jääk</span>
                </div>
                <div className="balance-stat">
                  <span className="stat-value">{currentUser.usedLeaveDays}</span>
                  <span className="stat-label">kasutatud</span>
                </div>
                <div className="balance-stat">
                  <span className="stat-value">{currentUser.annualLeaveDays}</span>
                  <span className="stat-label">aastane</span>
                </div>
              </div>
            </div>
          )}

          {userRole === 'admin' ? (
            <AdminDashboard />
          ) : (
            <>
              <div className="view-toggle">
                <button
                  className={`toggle-btn ${currentView === 'requests' ? 'active' : ''}`}
                  onClick={() => setCurrentView('requests')}
                >
                  📝 Taotlused
                </button>
                <button
                  className={`toggle-btn ${currentView === 'statistics' ? 'active' : ''}`}
                  onClick={() => setCurrentView('statistics')}
                >
                  📊 Statistika
                </button>
                <button
                  className={`toggle-btn ${currentView === 'calendar' ? 'active' : ''}`}
                  onClick={() => setCurrentView('calendar')}
                >
                  📅 Kalender
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
