import React, { useState, useEffect } from 'react';
import axios from 'axios';
import './Statistics.css';

const Statistics = () => {
  const [statistics, setStatistics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchStatistics();
  }, []);

  const fetchStatistics = async () => {
    try {
      setLoading(true);
      setError(null);
      const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';
      const response = await axios.get(`${API_URL}/VacationRequests/statistics`, {
        timeout: 10000
      });
      setStatistics(response.data);
    } catch (err) {
      console.error('Error fetching statistics:', err);
      setError('Viga statistika laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const handleExportCSV = async () => {
    try {
      const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';
      const response = await axios.get(`${API_URL}/VacationRequests/export/csv`, {
        responseType: 'blob',
        timeout: 10000
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `puhkusetaotlused_${new Date().toISOString().split('T')[0]}.csv`);
      document.body.appendChild(link);
      link.click();
      link.parentNode.removeChild(link);
    } catch (err) {
      console.error('Error exporting CSV:', err);
      alert('Viga CSV eksportimisel');
    }
  };

  const handleExportICal = async () => {
    try {
      const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';
      const response = await axios.get(`${API_URL}/VacationRequests/export/ical`, {
        responseType: 'blob',
        timeout: 10000
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `puhkused_${new Date().toISOString().split('T')[0]}.ics`);
      document.body.appendChild(link);
      link.click();
      link.parentNode.removeChild(link);
    } catch (err) {
      console.error('Error exporting iCal:', err);
      alert('Viga kalendri eksportimisel');
    }
  };

  if (loading) {
    return (
      <div className="statistics-container">
        <h2>📊 Statistika</h2>
        <div className="loading">Laadimine...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="statistics-container">
        <h2>📊 Statistika</h2>
        <div className="error-message">{error}</div>
      </div>
    );
  }

  if (!statistics) {
    return null;
  }

  const formatDate = (dateString) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('et-EE', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  return (
    <div className="statistics-container">
      <div className="statistics-header">
        <h2>📊 Statistika</h2>
        <div className="export-buttons">
          <button onClick={handleExportCSV} className="export-btn csv-btn" title="Ekspordi CSV">
            📄 CSV
          </button>
          <button onClick={handleExportICal} className="export-btn ical-btn" title="Ekspordi kalendrisse">
            📅 iCal
          </button>
        </div>
      </div>

      <div className="stats-grid">
        <div className="stat-card primary">
          <div className="stat-icon">📅</div>
          <div className="stat-content">
            <div className="stat-value">{statistics.totalDays}</div>
            <div className="stat-label">Kokku päevi</div>
          </div>
        </div>

        <div className="stat-card secondary">
          <div className="stat-icon">📝</div>
          <div className="stat-content">
            <div className="stat-value">{statistics.totalRequests}</div>
            <div className="stat-label">Taotlusi kokku</div>
          </div>
        </div>

        <div className="stat-card success">
          <div className="stat-icon">🏖️</div>
          <div className="stat-content">
            <div className="stat-value">{statistics.currentYearDays}</div>
            <div className="stat-label">Päevi sel aastal</div>
          </div>
        </div>

        <div className="stat-card info">
          <div className="stat-icon">⏭️</div>
          <div className="stat-content">
            <div className="stat-value">{statistics.upcomingRequests}</div>
            <div className="stat-label">Tulevasi puhkusi</div>
          </div>
        </div>
      </div>

      {statistics.nextVacationStart && (
        <div className="next-vacation">
          <strong>🎯 Järgmine puhkus:</strong> {formatDate(statistics.nextVacationStart)}
        </div>
      )}

      {statistics.monthlyBreakdown && statistics.monthlyBreakdown.length > 0 && (
        <div className="monthly-breakdown">
          <h3>Kuude kaupa</h3>
          <div className="month-list">
            {statistics.monthlyBreakdown.map((month, index) => (
              <div key={`${month.year}-${month.month}`} className="month-item">
                <div className="month-name">{month.monthName}</div>
                <div className="month-stats">
                  <span className="month-days">{month.daysCount} päeva</span>
                  <span className="month-requests">{month.requestsCount} taotlust</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default Statistics;
