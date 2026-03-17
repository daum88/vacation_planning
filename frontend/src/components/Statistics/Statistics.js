import React, { useState, useEffect, useMemo } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { useToast } from '../Toast/Toast';
import { downloadBlob } from '../../utils/fileUtils';
import { MONTHS_SHORT } from '../../utils/locale';
import './Statistics.css';

const Statistics = () => {
  const [statistics, setStatistics] = useState(null);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState(null);
  const toast = useToast();

  useEffect(() => { fetchStatistics(); }, []);

  const fetchStatistics = async () => {
    try {
      setLoading(true);
      setError(null);
      const r = await vacationRequestsApi.getStatistics();
      setStatistics(r.data);
    } catch (err) {
      setError('Viga statistika laadimisel');
      toast.error('Viga statistika laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const handleExportCSV = async () => {
    try {
      const r = await vacationRequestsApi.exportCsv();
      downloadBlob(r.data, `puhkusetaotlused_${new Date().toISOString().split('T')[0]}.csv`);
      toast.success('CSV alla laaditud');
    } catch { toast.error('Viga CSV eksportimisel'); }
  };

  const handleExportICal = async () => {
    try {
      const r = await vacationRequestsApi.exportIcal();
      downloadBlob(r.data, `puhkused_${new Date().toISOString().split('T')[0]}.ics`);
      toast.success('Kalendrifail alla laaditud');
    } catch { toast.error('Viga kalendri eksportimisel'); }
  };

  const maxMonthlyDays = useMemo(() => {
    if (!statistics?.monthlyBreakdown?.length) return 1;
    return Math.max(...statistics.monthlyBreakdown.map(m => m.daysCount || 0), 1);
  }, [statistics]);

  const usedPct = useMemo(() => {
    if (!statistics?.userBalance) return 0;
    const { usedLeaveDays, annualLeaveDays } = statistics.userBalance;
    return Math.min(100, Math.round((usedLeaveDays / Math.max(1, annualLeaveDays + (statistics.userBalance.carryOverDays || 0))) * 100));
  }, [statistics]);

  if (loading) return (
    <div className="st-shell">
      <div className="st-loading">Laadimine…</div>
    </div>
  );

  if (error || !statistics) return (
    <div className="st-shell">
      <div className="st-error">{error || 'Statistika puudub'}</div>
    </div>
  );

  const bal = statistics.userBalance;
  const { leaveTypeUsage, monthlyBreakdown } = statistics;

  const nextDate = statistics.nextVacationDate
    ? new Date(statistics.nextVacationDate).toLocaleDateString('et-EE', { day: '2-digit', month: 'long', year: 'numeric' })
    : null;

  return (
    <div className="st-shell">

      {/* ── Header ─────────────────────────────────────────────────── */}
      <div className="st-page-header">
        <div>
          <h2 className="st-title">Statistika</h2>
          <p className="st-subtitle">Sinu puhkusepäevade kasutus ja taotluste ajalugu.</p>
        </div>
        <div className="st-export-row">
          <button className="st-export-btn" onClick={handleExportCSV}>CSV eksport</button>
          <button className="st-export-btn" onClick={handleExportICal}>iCal eksport</button>
        </div>
      </div>

      {/* ── Balance strip ───────────────────────────────────────────── */}
      {bal && (
        <section className="st-section">
          <div className="st-section-title">Puhkuse saldo</div>
          <div className="st-balance-strip">
            <div className="st-bal-item st-bal-primary">
              <span>Alles</span>
              <strong>{bal.remainingLeaveDays}</strong>
            </div>
            <div className="st-bal-item">
              <span>Kasutatud</span>
              <strong>{bal.usedLeaveDays}</strong>
            </div>
            <div className="st-bal-item">
              <span>Aastane norm</span>
              <strong>{bal.annualLeaveDays}</strong>
            </div>
            <div className="st-bal-item">
              <span>Ülekanne</span>
              <strong>{bal.carryOverDays}</strong>
            </div>
            <div className="st-bal-item">
              <span>Ootel</span>
              <strong>{bal.pendingDays}</strong>
            </div>
            <div className="st-bal-item">
              <span>Kinnitatud</span>
              <strong>{bal.approvedDays}</strong>
            </div>
          </div>

          {/* Progress bar */}
          <div className="st-progress-row">
            <div className="st-progress-track">
              <div
                className="st-progress-fill"
                style={{ width: `${usedPct}%` }}
              />
            </div>
            <span className="st-progress-label">
              {usedPct}% aasta normist kasutatud
            </span>
          </div>

          {nextDate && (
            <div className="st-next-vacation">
              Järgmine planeeritud puhkus: <strong>{nextDate}</strong>
            </div>
          )}
        </section>
      )}

      {/* ── KPI row ─────────────────────────────────────────────────── */}
      <section className="st-section">
        <div className="st-section-title">Kokkuvõte</div>
        <div className="st-kpi-row">
          <div className="st-kpi">
            <span>Taotlusi kokku</span>
            <strong>{statistics.totalRequests}</strong>
          </div>
          <div className="st-kpi">
            <span>Päevi kokku</span>
            <strong>{statistics.totalDays}</strong>
          </div>
          <div className="st-kpi">
            <span>Sel aastal kasutatud</span>
            <strong>{statistics.currentYearDays}</strong>
          </div>
          <div className="st-kpi">
            <span>Tulevasi puhkuseid</span>
            <strong>{statistics.upcomingVacationsCount}</strong>
          </div>
        </div>
      </section>

      {/* ── Bottom two columns ──────────────────────────────────────── */}
      <div className="st-two-col">

        {/* Monthly breakdown */}
        {monthlyBreakdown && monthlyBreakdown.length > 0 && (
          <section className="st-section">
            <div className="st-section-title">Kuude lõikes</div>
            <div className="st-chart">
              {monthlyBreakdown.map((m, i) => {
                const pct = Math.round((m.daysCount / maxMonthlyDays) * 100);
                return (
                  <div key={i} className="st-chart-row">
                    <div className="st-chart-label">
                      {MONTHS_SHORT[i] ?? m.monthName}
                    </div>
                    <div className="st-chart-track">
                      {pct > 0 && (
                        <div
                          className="st-chart-fill"
                          style={{ width: `${pct}%` }}
                        />
                      )}
                    </div>
                    <div className="st-chart-value">
                      {m.daysCount > 0 ? (
                        <>
                          <strong>{m.daysCount}</strong>
                          <span>{m.requestsCount}</span>
                        </>
                      ) : (
                        <span className="st-chart-zero">—</span>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
            <div className="st-chart-legend">
              <span>Tulp = tööpäevad · teine arv = taotluste arv</span>
            </div>
          </section>
        )}

        {/* Leave type breakdown */}
        {leaveTypeUsage && leaveTypeUsage.length > 0 && (
          <section className="st-section">
            <div className="st-section-title">Puhkuse liigid</div>
            <table className="st-table">
              <thead>
                <tr>
                  <th>Liik</th>
                  <th>Taotlusi</th>
                  <th>Päevi</th>
                </tr>
              </thead>
              <tbody>
                {leaveTypeUsage.map(u => (
                  <tr key={u.leaveTypeId}>
                    <td>
                      <div className="st-ltype-cell">
                        <span
                          className="st-ltype-dot"
                          style={{ background: u.color }}
                        />
                        {u.leaveTypeName}
                      </div>
                    </td>
                    <td>{u.requestsCount}</td>
                    <td><strong>{u.daysUsed}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        )}
      </div>

    </div>
  );
};

export default Statistics;
