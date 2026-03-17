import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { getEstonianPublicHolidays, isWeekend } from '../../utils/dateUtils';
import './YearTimeline.css';

const MONTHS = ['Jaan','Veebr','Märts','Apr','Mai','Juuni','Juuli','Aug','Sept','Okt','Nov','Dets'];
const MONTHS_FULL = ['Jaanuar','Veebruar','Märts','Aprill','Mai','Juuni','Juuli','August','September','Oktoober','November','Detsember'];

function getDaysInMonth(year, month) {
  return new Date(year, month + 1, 0).getDate();
}

function isoDate(y, m, d) {
  return `${y}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
}

const YearTimeline = () => {
  const [year, setYear]         = useState(new Date().getFullYear());
  const [requests, setRequests] = useState([]);
  const [loading, setLoading]   = useState(true);
  const [tooltip, setTooltip]   = useState(null); // { html, x, y }

  useEffect(() => { fetchRequests(); }, [year]); // eslint-disable-line

  const fetchRequests = async () => {
    setLoading(true);
    try {
      const r = await vacationRequestsApi.getAll({
        startDateFrom: `${year}-01-01`,
        startDateTo:   `${year}-12-31`,
      });
      setRequests(r.data);
    } catch (e) { console.error(e); }
    finally     { setLoading(false); }
  };

  const { holidaySet, holidayNames } = useMemo(() => {
    const all = [...getEstonianPublicHolidays(year - 1), ...getEstonianPublicHolidays(year), ...getEstonianPublicHolidays(year + 1)];
    const set   = new Set(all.map(h => h.date.toISOString().split('T')[0]));
    const names = {};
    all.forEach(h => { names[h.date.toISOString().split('T')[0]] = h.name; });
    return { holidaySet: set, holidayNames: names };
  }, [year]);

  const vacationDays = useMemo(() => {
    const map = {};
    requests.forEach(r => {
      if (r.status === 'Withdrawn' || r.status === 'Rejected') return;
      const cur = new Date(r.startDate);
      const end = new Date(r.endDate);
      while (cur <= end) {
        const key = cur.toISOString().split('T')[0];
        map[key] = { status: r.status, leaveTypeName: r.leaveTypeName, color: r.leaveTypeColor || '#0071E3' };
        cur.setDate(cur.getDate() + 1);
      }
    });
    return map;
  }, [requests]);

  const today = new Date().toISOString().split('T')[0];

  const statusLabel = (s) => s === 'Approved' ? 'Kinnitatud' : s === 'Pending' ? 'Ootel' : s;

  const totalWorkingDaysOnLeave = useMemo(() =>
    Object.entries(vacationDays).filter(([date, info]) => {
      if (info.status !== 'Approved') return false;
      if (holidaySet.has(date)) return false;
      return !isWeekend(new Date(date));
    }).length
  , [vacationDays, holidaySet]);

  const approved = requests.filter(r => r.status === 'Approved');
  const pending  = requests.filter(r => r.status === 'Pending');

  const buildTooltipLines = useCallback((dateStr, vacation) => {
    const d = new Date(dateStr + 'T00:00:00');
    const [y, m, day] = dateStr.split('-');
    const weekday = d.toLocaleDateString('et-EE', { weekday: 'long' });
    const fullDate = `${weekday.charAt(0).toUpperCase() + weekday.slice(1)}, ${parseInt(day)}. ${MONTHS_FULL[parseInt(m) - 1]} ${y}`;
    const lines = [fullDate];
    if (holidayNames[dateStr]) lines.push(`Riigipüha: ${holidayNames[dateStr]}`);
    if (vacation) lines.push(`${vacation.leaveTypeName} — ${statusLabel(vacation.status)}`);
    if (isWeekend(d)) lines.push('Nädalavahetus');
    return lines;
  }, [holidayNames]); // eslint-disable-line

  const handleCellEnter = useCallback((e, dateStr) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const vac = vacationDays[dateStr];
    const lines = buildTooltipLines(dateStr, vac);
    setTooltip({
      lines,
      x: rect.left + rect.width / 2,
      y: rect.top,
      color: vac?.status === 'Approved' ? vac.color : null,
    });
  }, [vacationDays, buildTooltipLines]);

  const handleCellLeave = useCallback(() => setTooltip(null), []);

  return (
    <div className="year-timeline">
      {/* Custom tooltip — rendered at fixed position */}
      {tooltip && (
        <div
          className="yt-tooltip"
          style={{ left: tooltip.x, top: tooltip.y }}
        >
          {tooltip.color && <span className="yt-tooltip-swatch" style={{ background: tooltip.color }} />}
          {tooltip.lines.map((l, i) => (
            <div key={i} className={i === 0 ? 'yt-tooltip-date' : 'yt-tooltip-line'}>{l}</div>
          ))}
        </div>
      )}

      <div className="year-timeline-header">
        <div>
          <h2>Aasta ülevaade</h2>
          <p>Kõik puhkusepäevad {year}. aasta lõikes.</p>
        </div>
        <div className="year-nav">
          <button className="year-nav-btn" onClick={() => setYear(y => y - 1)}>‹</button>
          <span className="year-label">{year}</span>
          <button className="year-nav-btn" onClick={() => setYear(y => y + 1)}>›</button>
        </div>
      </div>

      <div className="year-timeline-stats">
        <div className="yt-stat"><span>Kasutatud tööpäevi</span><strong>{totalWorkingDaysOnLeave}</strong></div>
        <div className="yt-stat"><span>Kinnitatud taotlusi</span><strong>{approved.length}</strong></div>
        <div className="yt-stat"><span>Ootel taotlusi</span><strong>{pending.length}</strong></div>
        <div className="yt-stat"><span>Riigipühi aastas</span><strong>{holidaySet.size}</strong></div>
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
                    const isHoliday = holidaySet.has(dateStr);
                    const isWknd    = isWeekend(new Date(dateStr));
                    const isToday   = dateStr === today;
                    const vacation  = vacationDays[dateStr];

                    let cls = 'yt-day';
                    if (isToday)   cls += ' yt-today';
                    else if (isHoliday) cls += ' yt-holiday';
                    else if (isWknd)    cls += ' yt-weekend';

                    return (
                      <div
                        key={d}
                        className={cls}
                        onMouseEnter={(e) => handleCellEnter(e, dateStr)}
                        onMouseLeave={handleCellLeave}
                        style={vacation ? {
                          background: vacation.status === 'Approved'
                            ? vacation.color
                            : `repeating-linear-gradient(45deg, ${vacation.color}44, ${vacation.color}44 2px, transparent 2px, transparent 5px)`,
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
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: 'transparent', border: '1px dashed #999' }} />Ootel taotlus</div>
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: '#fffdf0', border: '1px solid #ffdca1' }} />Riigipüha</div>
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: '#f5f5f7' }} />Nädalavahetus</div>
        <div className="yt-legend-item"><div className="yt-legend-dot" style={{ background: '#1D1D1F' }} />Täna</div>
      </div>
    </div>
  );
};

export default YearTimeline;
