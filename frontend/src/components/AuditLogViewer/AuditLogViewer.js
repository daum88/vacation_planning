import React, { useState, useEffect, useCallback } from 'react';
import { auditLogsApi } from '../../api/api';
import { AUDIT_EVENT_LABELS, PAGINATION } from '../../constants/appConstants';
import { fmtDateTime } from '../../utils/dateUtils';
import './AuditLogViewer.css';

const EVENT_LABELS = AUDIT_EVENT_LABELS;
const PAGE_SIZE = PAGINATION.AUDIT_PAGE_SIZE;

export default function AuditLogViewer() {
  const [logs, setLogs] = useState([]);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({ search: '', eventType: '', success: '', page: 1 });
  const [totalCount, setTotalCount] = useState(0);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    try {
      const params = {
        limit: PAGE_SIZE,
        offset: (filters.page - 1) * PAGE_SIZE,
      };
      if (filters.eventType) params.eventType = filters.eventType;
      if (filters.success !== '') params.success = filters.success === 'true';

      const logsRes = await auditLogsApi.getLogs(params);

      const data = logsRes.data;
      if (Array.isArray(data)) {
        setLogs(data);
        setTotalCount(data.length);
      } else {
        setLogs(data.items || data);
        setTotalCount(data.totalCount || (data.items || data).length);
      }

      if (!summary) {
        const summaryRes = await auditLogsApi.getSummary();
        setSummary(summaryRes.data);
      }
    } catch {
      // silently fail
    } finally {
      setLoading(false);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters.page, filters.eventType, filters.success]);

  useEffect(() => { fetchLogs(); }, [fetchLogs]);

  const filteredLogs = filters.search
    ? logs.filter(l =>
        (l.userEmail || '').toLowerCase().includes(filters.search.toLowerCase()) ||
        (l.eventType || '').toLowerCase().includes(filters.search.toLowerCase()) ||
        (l.details || '').toLowerCase().includes(filters.search.toLowerCase())
      )
    : logs;

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  return (
    <div className="alv-container">
      <div className="alv-header">
        <h2 className="alv-title">Auditilogi</h2>
        <button className="alv-refresh" onClick={() => { setSummary(null); fetchLogs(); }}>
          Uuenda
        </button>
      </div>

      {/* Summary cards */}
      {summary && (
        <div className="alv-summary">
          <div className="alv-stat">
            <span className="alv-stat-value">{summary.totalLogs ?? '—'}</span>
            <span className="alv-stat-label">Kokku sündmusi</span>
          </div>
          <div className="alv-stat">
            <span className="alv-stat-value alv-stat-value--danger">{summary.failedLogins ?? '—'}</span>
            <span className="alv-stat-label">Ebaõnnestunud sisselogimised</span>
          </div>
          <div className="alv-stat">
            <span className="alv-stat-value">{summary.activeUsersToday ?? '—'}</span>
            <span className="alv-stat-label">Aktiivsed kasutajad täna</span>
          </div>
          <div className="alv-stat">
            <span className="alv-stat-value">{summary.logsLast24h ?? '—'}</span>
            <span className="alv-stat-label">Viimase 24h sündmused</span>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="alv-filters">
        <input
          className="alv-search"
          type="text"
          placeholder="Otsi emaili, sündmuse tüübi järgi..."
          value={filters.search}
          onChange={e => setFilters(p => ({ ...p, search: e.target.value }))}
        />
        <select
          className="alv-select"
          value={filters.eventType}
          onChange={e => setFilters(p => ({ ...p, eventType: e.target.value, page: 1 }))}
        >
          <option value="">Kõik sündmused</option>
          {Object.entries(EVENT_LABELS).map(([key, label]) => (
            <option key={key} value={key}>{label}</option>
          ))}
        </select>
        <select
          className="alv-select"
          value={filters.success}
          onChange={e => setFilters(p => ({ ...p, success: e.target.value, page: 1 }))}
        >
          <option value="">Kõik olukorrad</option>
          <option value="true">Edukas</option>
          <option value="false">Ebaõnnestunud</option>
        </select>
      </div>

      {/* Log table */}
      {loading ? (
        <div className="alv-loading">Laadin logi...</div>
      ) : (
        <>
          <div className="alv-table-wrap">
            <table className="alv-table">
              <thead>
                <tr>
                  <th>Aeg</th>
                  <th>Kasutaja</th>
                  <th>Sündmus</th>
                  <th>Üksus</th>
                  <th>IP</th>
                  <th>Olek</th>
                  <th>Detailid</th>
                </tr>
              </thead>
              <tbody>
                {filteredLogs.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="alv-empty">Logisid ei leitud</td>
                  </tr>
                ) : filteredLogs.map(log => (
                  <tr key={log.id}>
                    <td>
                      <span className="alv-time">{fmtDateTime(log.createdAt)}</span>
                    </td>
                    <td>
                      <span className="alv-email">{log.userEmail || '—'}</span>
                    </td>
                    <td>
                      <span className={`alv-event-type alv-event-type--${log.success ? 'success' : 'fail'}`}>
                        {EVENT_LABELS[log.eventType] || log.eventType}
                      </span>
                    </td>
                    <td>
                      {log.entityType ? (
                        <span className="alv-entity">
                          {log.entityType}{log.entityId ? ` #${log.entityId}` : ''}
                        </span>
                      ) : '—'}
                    </td>
                    <td>
                      <span className="alv-ip">{log.ipAddress || '—'}</span>
                    </td>
                    <td>
                      <span className={`alv-status alv-status--${log.success ? 'ok' : 'fail'}`}>
                        {log.success ? 'OK' : 'FAIL'}
                      </span>
                    </td>
                    <td>
                      <span className="alv-details" title={log.details || ''}>
                        {log.details ? log.details.slice(0, 40) + (log.details.length > 40 ? '…' : '') : '—'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="alv-pagination">
              <button
                className="alv-page-btn"
                disabled={filters.page <= 1}
                onClick={() => setFilters(p => ({ ...p, page: p.page - 1 }))}
              >
                Eelmine
              </button>
              <span className="alv-page-info">
                Lehekülg {filters.page} / {totalPages}
              </span>
              <button
                className="alv-page-btn"
                disabled={filters.page >= totalPages}
                onClick={() => setFilters(p => ({ ...p, page: p.page + 1 }))}
              >
                Järgmine
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
