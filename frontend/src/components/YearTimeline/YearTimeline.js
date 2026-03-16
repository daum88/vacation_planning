import React, { useState, useEffect, useMemo } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { getEstonianPublicHolidays, isWeekend } from '../../utils/dateUtils';
import './YearTimeline.css';

const MONTHS = ['Jaan', 'Veebr', 'Märts', 'Apr', 'Mai', 'Juuni', 'Juuli', 'Aug', 'Sept', 'Okt', 'Nov', 'Dets'];

function getDaysInMonth(year, month) {
  return new Date(year, month + 1, 0).getDate();
}

function isoDate(y, m, d) {
  return `${y}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
}

const YearTimeline = () => {
  const [year, setYear] = useState(new Date().getFullYear());
  const [requests, setRequests] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchRequests();
  }, [year]);

  const fetchRequests = async () => {
    setLoading(true);
    try {
      const response = await vacationRequestsApi.getAll({
        startDateFrom: `${year}-01-01`,
        startDateTo: `${year}-12-31`,
      });
      setRequests(response.data);
    } catch (err) {
      console.error('Error fetching requests for timeline:', err);
    } finally {
      setLoading(false);
    }
  };

  const holidays = useMemo(() => {
    const hols = getEstonianPublicHolidays(year);
    return new Set(hols.map(h => h.date.toISOString().split('T')[0]));
  }, [year]);

  const holidayNames = useMemo(() => {
    const hols = getEstonianPublicHolidays(year);
    const map = {};
    hols.forEach(h => { map[h.date.toISOString().split('T')[0]] = h.name; });
    return map;
  }, [year]);

  const vacationDays = useMemo(() => {
    const map = {}; // dateStr -> { status, leaveTypeName, color }
    requests.forEach(r => {
      if (r.status === 'Withdrawn' || r.status === 'Rejected') return;
      const start = new Date(r.startDate);
      const end = new Date(r.endDate);
      const cur = new Date(start);
      while (cur <= end) {
        const key = cur.toISOString().split('T')[0];
        map[key] = { status: r.status, leaveTypeName: r.leaveTypeName, color: r.leaveTypeColor || '#0071E3' };
        cur.setDate(cur.getDate() + 1);
      }
    });
    return map;
  }, [requests]);

  const today = new Date().toISOString().split('T')[0];

  const statusLabel = (status) => {
    if (status === 'Approved') return 'Kinnitatud';
    if (status === 'Pending') return 'Ootel';
    return status;
  };

  const totalWorkingDaysOnLeave = useMemo(() => {
    return Object.entries(vacationDays).filter(([date, info]) => {
      if (info.status !== 'Approved') return false;
      if (holidays.has(date)) return false;
      const d = new Date(date);
      return !isWeekend(d);
    }).length;
  }, [vacationDays, holidays]);

  const approvedRequests = requests.filter(r => r.status === 'Approved');
  const pendingRequests = requests.filter(r => r.status === 'Pending');

  return (
    <div className="year-timeline">
      <div className="year-timeline-header">
        <div>
          <h2>Aasta ülevaade</h2>
          <p>Kõik puhkusepäevad aasta lõikes.</p>
        </div>
        <div className="year-nav">
          <button className="year-nav-btn" onClick={() => setYear(y => y - 1)}>‹</button>
          <span className="year-label">{year}</span>
          <button className="year-nav-btn" onClick={() => setYear(y => y + 1)}>›</button>
        </div>
      </div>

      <div className="year-timeline-stats">
        <div className="yt-stat">
          <span>Kasutatud tööpäevi</span>
          <strong>{totalWorkingDaysOnLeave}</strong>
        </div>
        <div className="yt-stat">
          <span>Kinnitatud taotlusi</span>
          <strong>{approvedRequests.length}</strong>
        </div>
        <div className="yt-stat">
          <span>Ootel taotlusi</span>
          <strong>{pendingRequests.length}</strong>
        </div>
        <div className="yt-stat">
          <span>Riigipühi aastas</span>
          <strong>{holidays.size}</strong>
        </div>
      </div>

      {loading ? (
        <div className="yt-loading">Laadimine...</div>
      ) : (
        <div className="yt-grid">
          {Array.from({ length: 12 }, (_, monthIdx) => {
            const daysInMonth = getDaysInMonth(year, monthIdx);
            return (
              <div key={monthIdx} className="yt-month">
                <div className="yt-month-name">{MONTHS[monthIdx]}</div>
                <div className="yt-days">
                  {Array.from({ length: daysInMonth }, (_, dayIdx) => {
                    const d = dayIdx + 1;
                    const dateStr = isoDate(year, monthIdx, d);
                    const isHoliday = holidays.has(dateStr);
                    const isWknd = isWeekend(new Date(dateStr));
                    const isToday = dateStr === today;
                    const vacation = vacationDays[dateStr];

                    let cellClass = 'yt-day';
                    if (isToday) cellClass += ' yt-today';
                    else if (isHoliday) cellClass += ' yt-holiday';
                    else if (isWknd) cellClass += ' yt-weekend';

                    const title = [
                      `${d}. ${MONTHS[monthIdx]}`,
                      isHoliday ? holidayNames[dateStr] : null,
                      vacation ? `${vacation.leaveTypeName} — ${statusLabel(vacation.status)}` : null,
                    ].filter(Boolean).join('\n');

                    return (
                      <div
                        key={d}
                        className={cellClass}
                        title={title}
                        style={vacation ? {
                          background: vacation.status === 'Approved'
                            ? vacation.color
                            : `repeating-linear-gradient(45deg, ${vacation.color}33, ${vacation.color}33 2px, transparent 2px, transparent 5px)`,
                          borderColor: vacation.color,
                        } : {}}
                      />
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      )}

      <div className="yt-legend">
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: '#0071E3' }} />Kinnitatud puhkus</div>
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: '#ddd', border: '1px dashed #999' }} />Ootel taotlus</div>
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: '#fff4df', border: '1px solid #ffdca1' }} />Riigipüha</div>
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: '#f5f5f7' }} />Nädalavahetus</div>
      </div>
    </div>
  );
};

export default YearTimeline;
