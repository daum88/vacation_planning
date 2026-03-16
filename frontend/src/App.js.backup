import React, { useState, useEffect } from 'react';
import VacationRequestForm from './components/VacationRequestForm/VacationRequestForm';
import VacationRequestList from './components/VacationRequestList/VacationRequestList';
import Statistics from './components/Statistics/Statistics';
import AdminDashboard from './components/AdminDashboard/AdminDashboard';
import { vacationRequestsApi, setUserRole, getCurrentRole } from './api/api';
import './App.css';

function App() {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingRequest, setEditingRequest] = useState(null);
  const [error, setError] = useState(null);
  const [currentView, setCurrentView] = useState('requests'); // 'requests', 'statistics', 'admin'
  const [userRole, setUserRoleState] = useState(getCurrentRole());

  const fetchRequests = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await vacationRequestsApi.getAll();
      setRequests(response.data);
    } catch (error) {
      console.error('Viga puhkusetaotluste laadimisel:', error);
      setError(error.message || 'Viga andmete laadimisel. Palun kontrollige, kas backend server töötab.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (userRole === 'employee') {
      fetchRequests();
    } else {
      setLoading(false);
    }
  }, [userRole]);

  const handleSuccess = () => {
    setEditingRequest(null);
    fetchRequests();
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
        fetchRequests();
      } catch (error) {
        console.error('Viga kustutamisel:', error);
        alert(error.response?.data?.message || 'Viga taotluse kustutamisel.');
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
    setError(null);
    if (role === 'employee') {
      fetchRequests();
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
      </header>

      <main className="app-main">
        <div className="container">
          {error && (
            <div className="global-error">
              <div>
                <strong>⚠️ Viga:</strong>
                <p>{error}</p>
              </div>
              <button onClick={fetchRequests} className="retry-button">
                Proovi uuesti
              </button>
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
              </div>

              {currentView === 'requests' ? (
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
                    loading={loading}
                  />
                </>
              ) : (
                <Statistics key={requests.length} />
              )}
            </>
          )}
        </div>
      </main>

      <footer className="app-footer">
        <p>© 2026 Puhkusetaotluste süsteem • Praegune roll: {userRole === 'admin' ? 'Administraator' : 'Töötaja'}</p>
      </footer>
    </div>
  );
}

export default App;
