import React, { useState, useEffect } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { useToast } from '../Toast/Toast';
import './Statistics.css';

const Statistics = () => {
  const [statistics, setStatistics] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const toast = useToast();

  useEffect(() => {
    fetchStatistics();
  }, []);

  const fetchStatistics = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await vacationRequestsApi.getStatistics();
      setStatistics(response.data);
    } catch (err) {
      console.error('Error fetching statistics:', err);
      setError('Viga statistika laadimisel');
      toast.error('Viga statistika laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const handleExportCSV = async () => {
    try {
      const response = await vacationRequestsApi.exportCsv();
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `puhkusetaotlused_${new Date().toISOString().split('T')[0]}.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      toast.success('CSV fail alla laaditud ✓');
    } catch (err) {
      console.error('Error exporting CSV:', err);
      toast.error('Viga CSV eksportimisel');
    }
  };

  const handleExportICal = async () => {
    try {
      const response = await vacationRequestsApi.exportIcal();
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `puhkused_${new Date().toISOString().split('T')[0]}.ics`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      toast.success('Kalendrifail alla laaditud ✓');
    } catch (err) {
      console.error('Error exporting iCal:', err);
      toast.error('Viga kalendri eksportimisel');
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('et-EE', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  if (loading) {
    return (
      <div className="statistics-container">
        <h2>📊 Statistika</h2>
        <div className="loading">Laadimine...</div>
      </div>
    );
  }

  if (error || !statistics) {
    return (
      <div className="statistics-container">
        <h2>📊 Statistika</h2>
        <div className="error-message">{error || 'Statistika puudub'}</div>
      </div>
    );
  }

  const { userBalance, leaveTypeUsage } = statistics;

  return (
    <div className="statistics-container">
      <div className="statistics-header">
        <h2>📊 Statistika</h2>
        <div className="export-buttons">
          <button onClick={handleExportCSV} className="btn-export">
            📄 CSV
          </button>
          <button onClick={handleExportICal} className="btn-export">
            📅 iCal
          </button>
        </div>
      </div>

      {/* Balance Card */}
      {userBalance && (
        <div className="balance-card">
          <h3>💼 Puhkuste saldo</h3>
          <div className="balance-grid">
            <div className="balance-item large">
              <span className="balance-value">{userBalance.remainingLeaveDays}</span>
              <span className="balance-label">Jääk</span>
            </div>
            <div className="balance-item">
              <span className="balance-value">{userBalance.annualLeaveDays}</span>
              <span className="balance-label">Aastane</span>
            </div>
            <div className="balance-item">
              <span className="balance-value">{userBalance.usedLeaveDays}</span>
              <span className="balance-label">Kasutatud</span>
            </div>
            <div className="balance-item">
              <span className="balance-value">{userBalance.carryOverDays}</span>
              <span className="balance-label">Ülekanne</span>
            </div>
            <div className="balance-item">
              <span className="balance-value">{userBalance.pendingDays}</span>
              <span className="balance-label">Ootel</span>
            </div>
            <div className="balance-item">
              <span className="balance-value">{userBalance.approvedDays}</span>
              <span className="balance-label">Kinnitatud</span>
            </div>
          </div>
        </div>
      )}

      {/* Main Statistics Grid */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon">📝</div>
          <div className="stat-value">{statistics.totalRequests}</div>
          <div className="stat-label">Taotlust kokku</div>
        </div>

        <div className="stat-card">
          <div className="stat-icon">📅</div>
          <div className="stat-value">{statistics.totalDays}</div>
          <div className="stat-label">Päeva kokku</div>
        </div>

        <div className="stat-card">
          <div className="stat-icon">🗓️</div>
          <div className="stat-value">{statistics.currentYearDays}</div>
          <div className="stat-label">Päeva sel aastal</div>
        </div>

        <div className="stat-card">
          <div className="stat-icon">🏖️</div>
          <div className="stat-value">{statistics.upcomingVacationsCount}</div>
          <div className="stat-label">Tulevasi puhkuseid</div>
        </div>
      </div>

      {/* Next Vacation */}
      {statistics.nextVacationDate && (
        <div className="next-vacation-card">
          <div className="next-vacation-icon">🎉</div>
          <div className="next-vacation-content">
            <h4>Järgmine puhkus</h4>
            <p className="next-vacation-date">{formatDate(statistics.nextVacationDate)}</p>
          </div>
        </div>
      )}

      {/* Leave Type Usage */}
      {leaveTypeUsage && leaveTypeUsage.length > 0 && (
        <div className="leave-types-section">
          <h3>📊 Kasutus puhkuse tüübi järgi</h3>
          <div className="leave-types-grid">
            {leaveTypeUsage.map((usage) => (
              <div key={usage.leaveTypeId} className="leave-type-card">
                <div className="leave-type-header">
                  <div
                    className="leave-type-color"
                    style={{ backgroundColor: usage.color }}
                  />
                  <strong>{usage.leaveTypeName}</strong>
                </div>
                <div className="leave-type-stats">
                  <div className="leave-type-stat">
                    <span className="stat-number">{usage.daysUsed}</span>
                    <span className="stat-text">päeva</span>
                  </div>
                  <div className="leave-type-stat">
                    <span className="stat-number">{usage.requestsCount}</span>
                    <span className="stat-text">taotlust</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Monthly Breakdown */}
      {statistics.monthlyBreakdown && statistics.monthlyBreakdown.length > 0 && (
        <div className="monthly-section">
          <h3>📅 Kuude kaupa</h3>
          <div className="monthly-grid">
            {statistics.monthlyBreakdown.map((month, index) => (
              <div key={index} className="month-card">
                <div className="month-name">{month.monthName}</div>
                <div className="month-stats">
                  <div className="month-stat">
                    <span className="month-value">{month.daysCount}</span>
                    <span className="month-label">päeva</span>
                  </div>
                  <div className="month-stat">
                    <span className="month-value">{month.requestsCount}</span>
                    <span className="month-label">taotlust</span>
                  </div>
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
