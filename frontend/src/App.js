import React, { useState, useEffect } from 'react';
import VacationRequestForm from './components/VacationRequestForm/VacationRequestForm';
import VacationRequestList from './components/VacationRequestList/VacationRequestList';
import Statistics from './components/Statistics/Statistics';
import { vacationRequestsApi } from './api/api';
import './App.css';

function App() {
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [editingRequest, setEditingRequest] = useState(null);
  const [error, setError] = useState(null);
  const [showStatistics, setShowStatistics] = useState(false);

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
    fetchRequests();
  }, []);

  const handleSuccess = () => {
    setEditingRequest(null);
    fetchRequests();
  };

  const handleEdit = (request) => {
    setEditingRequest(request);
    setShowStatistics(false);
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

  const toggleStatistics = () => {
    setShowStatistics(!showStatistics);
  };

  return (
    <div className="App">
      <header className="app-header">
        <div className="header-content">
          <h1>🏖️ Puhkusetaotlused</h1>
          <p>Halda oma puhkusesoove lihtsalt ja mugavalt</p>
        </div>
      </header>

      <main className="app-main">
        <div className="container">
          {error && (
            <div className="global-error">
              <strong>⚠️ Viga:</strong> {error}
              <button onClick={fetchRequests} className="retry-button">
                Proovi uuesti
              </button>
            </div>
          )}

          <div className="view-toggle">
            <button 
              className={`toggle-btn ${!showStatistics ? 'active' : ''}`}
              onClick={() => setShowStatistics(false)}
            >
              📝 Taotlused
            </button>
            <button 
              className={`toggle-btn ${showStatistics ? 'active' : ''}`}
              onClick={toggleStatistics}
            >
              📊 Statistika
            </button>
          </div>

          {!showStatistics ? (
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
        </div>
      </main>

      <footer className="app-footer">
        <p>© 2026 Puhkusetaotluste süsteem</p>
      </footer>
    </div>
  );
}

export default App;
