import React, { useState, useEffect, useMemo } from 'react';
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

  const formatShortDate = (dateString) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString('et-EE', {
      day: '2-digit',
      month: 'long',
      year: 'numeric'
    });
  };

  const maxMonthlyDays = useMemo(() => {
    if (!statistics?.monthlyBreakdown?.length) return 1;
    return Math.max(...statistics.monthlyBreakdown.map(m => m.daysCount || 0), 1);
  }, [statistics]);

  if (loading) {
    return (
      <div className="statistics-container">
        <div className="loading">Laadimine...</div>
      </div>
    );
  }

  if (error || !statistics) {
    return (
      <div className="statistics-container">
        <div className="error-message">{error || 'Statistika puudub'}</div>
      </div>
    );
  }

  const { userBalance, leaveTypeUsage } = statistics;

  return (
    <div className="statistics-container">
      <header className="statistics-hero">
        <div>
          <h2>Statistika</h2>
          <p>Ülevaade sinu puhkuse kasutusest, trendidest ja ekspordist.</p>
        </div>
        <div className="export-buttons">
          <button onClick={handleExportCSV} className="btn-export">CSV eksport</button>
          <button onClick={handleExportICal} className="btn-export">iCal eksport</button>
        </div>
      </header>

      <section className="statistics-grid-top">
        {userBalance && (
          <article className="balance-hero-card">
            <div className="balance-headline">Puhkuse saldo</div>
            <div className="balance-main-row">
              <div className="balance-main-value">{userBalance.remainingLeaveDays}</div>
              <div className="balance-main-label">päeva alles</div>
            </div>
            <div className="balance-progress-track">
              <div
                className="balance-progress-value"
                style={{ width: `${Math.min(100, (userBalance.usedLeaveDays / Math.max(1, userBalance.annualLeaveDays)) * 100)}%` }}
              />
            </div>
            <div className="balance-meta-grid">
              <div><span>Aastane</span><strong>{userBalance.annualLeaveDays}</strong></div>
              <div><span>Kasutatud</span><strong>{userBalance.usedLeaveDays}</strong></div>
              <div><span>Ülekanne</span><strong>{userBalance.carryOverDays}</strong></div>
              <div><span>Ootel</span><strong>{userBalance.pendingDays}</strong></div>
              <div><span>Kinnitatud</span><strong>{userBalance.approvedDays}</strong></div>
            </div>
          </article>
        )}

        <article className="kpi-grid-card">
          <div className="kpi-item">
            <span>Taotlusi kokku</span>
            <strong>{statistics.totalRequests}</strong>
          </div>
          <div className="kpi-item">
            <span>Päevi kokku</span>
            <strong>{statistics.totalDays}</strong>
          </div>
          <div className="kpi-item">
            <span>Sel aastal</span>
            <strong>{statistics.currentYearDays}</strong>
          </div>
          <div className="kpi-item">
            <span>Tulevasi puhkuseid</span>
            <strong>{statistics.upcomingVacationsCount}</strong>
          </div>
        </article>

        {statistics.nextVacationDate && (
          <article className="next-vacation-panel">
            <span>Järgmine puhkus</span>
            <strong>{formatShortDate(statistics.nextVacationDate)}</strong>
          </article>
        )}
      </section>

      <section className="statistics-grid-bottom">
        {leaveTypeUsage && leaveTypeUsage.length > 0 && (
          <article className="leave-types-panel">
            <h3>Puhkuse liigid</h3>
            <div className="leave-types-list">
              {leaveTypeUsage.map((usage) => (
                <div key={usage.leaveTypeId} className="leave-type-row">
                  <div className="leave-type-left">
                    <span className="leave-type-dot" style={{ backgroundColor: usage.color }} />
                    <span>{usage.leaveTypeName}</span>
                  </div>
                  <div className="leave-type-right">
                    <strong>{usage.daysUsed}</strong>
                    <small>päeva • {usage.requestsCount} taotlust</small>
                  </div>
                </div>
              ))}
            </div>
          </article>
        )}

        {statistics.monthlyBreakdown && statistics.monthlyBreakdown.length > 0 && (
          <article className="monthly-panel">
            <h3>Kuude lõikes</h3>
            <div className="monthly-list">
              {statistics.monthlyBreakdown.map((month, index) => (
                <div key={index} className="monthly-row">
                  <div className="monthly-name">{month.monthName}</div>
                  <div className="monthly-bar">
                    <div
                      className="monthly-bar-fill"
                      style={{ width: `${Math.round((month.daysCount / maxMonthlyDays) * 100)}%` }}
                    />
                  </div>
                  <div className="monthly-meta">
                    <strong>{month.daysCount}</strong>
                    <span>{month.requestsCount} taotlust</span>
                  </div>
                </div>
              ))}
            </div>
          </article>
        )}
      </section>
    </div>
  );
};

export default Statistics;
