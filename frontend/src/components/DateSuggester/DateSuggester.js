import React, { useState, useEffect, useCallback } from 'react';
import { getEstonianPublicHolidays, isWeekend, countWorkingDays } from '../../utils/dateUtils';
import { vacationRequestsApi } from '../../api/api';
import './DateSuggester.css';

/**
 * Scans the next 26 weeks and finds vacation windows that maximise "free days per working day used".
 * A window that lands over a public holiday gives you extra calendar days for the same leave balance cost.
 */
const DateSuggester = ({ remainingDays, blackouts = [], onSelect }) => {
  const [open, setOpen] = useState(false);
  const [suggestions, setSuggestions] = useState([]);
  const [computing, setComputing] = useState(false);
  const [teamRequests, setTeamRequests] = useState([]);

  const loadTeamData = useCallback(async () => {
    try {
      const today = new Date();
      const sixMonths = new Date(today);
      sixMonths.setMonth(today.getMonth() + 6);
      const res = await vacationRequestsApi.getAllAdmin({
        startDateFrom: today.toISOString().split('T')[0],
        startDateTo: sixMonths.toISOString().split('T')[0],
        status: 'Approved',
      });
      setTeamRequests(res.data || []);
    } catch {
      setTeamRequests([]);
    }
  }, []);

  useEffect(() => {
    if (open && teamRequests.length === 0) loadTeamData();
  }, [open, teamRequests.length, loadTeamData]);

  useEffect(() => {
    if (open && remainingDays > 0) compute();
  }, [open, remainingDays, teamRequests, blackouts]); // eslint-disable-line

  const nextMonday = () => {
    const d = new Date();
    d.setHours(0, 0, 0, 0);
    const day = d.getDay();
    const daysUntilMonday = day === 0 ? 1 : day === 1 ? 7 : 8 - day;
    d.setDate(d.getDate() + daysUntilMonday);
    return d;
  };

  const addWorkingDays = (startDate, workingDaysNeeded) => {
    const year1 = startDate.getFullYear();
    const year2 = year1 + 1;
    const hols = new Set([
      ...getEstonianPublicHolidays(year1).map(h => h.date.toISOString().split('T')[0]),
      ...getEstonianPublicHolidays(year2).map(h => h.date.toISOString().split('T')[0]),
    ]);

    let count = 0;
    const cur = new Date(startDate);
    while (count < workingDaysNeeded) {
      const key = cur.toISOString().split('T')[0];
      if (!isWeekend(cur) && !hols.has(key)) count++;
      if (count < workingDaysNeeded) cur.setDate(cur.getDate() + 1);
    }
    return new Date(cur);
  };

  const calendarDaysBetween = (start, end) =>
    Math.round((end - start) / (1000 * 60 * 60 * 24)) + 1;

  const overlapsBlackout = (start, end) =>
    blackouts.some(b => {
      const bs = new Date(b.startDate);
      const be = new Date(b.endDate);
      return bs <= end && be >= start;
    });

  const teamConflictsInWindow = (start, end) =>
    teamRequests.filter(r => {
      const rs = new Date(r.startDate);
      const re = new Date(r.endDate);
      return rs <= end && re >= start;
    }).length;

  const compute = () => {
    setComputing(true);
    const results = [];
    const hols1 = getEstonianPublicHolidays(new Date().getFullYear());
    const hols2 = getEstonianPublicHolidays(new Date().getFullYear() + 1);
    const allHols = [...hols1, ...hols2];
    const holSet = new Set(allHols.map(h => h.date.toISOString().split('T')[0]));

    const targetLengths = [5, 7, 10, 14].filter(l => l <= remainingDays);
    if (targetLengths.length === 0 && remainingDays > 0) targetLengths.push(remainingDays);

    const start = nextMonday();

    for (let weekOffset = 0; weekOffset < 26; weekOffset++) {
      const windowStart = new Date(start);
      windowStart.setDate(start.getDate() + weekOffset * 7);

      // Also try mid-week starts near holidays
      const startCandidates = [new Date(windowStart)];
      // Add Thursday of that week (to capture long weekends)
      const thu = new Date(windowStart);
      thu.setDate(windowStart.getDate() + 3);
      startCandidates.push(thu);

      for (const ws of startCandidates) {
        for (const workDays of targetLengths) {
          if (workDays > remainingDays) continue;

          const we = addWorkingDays(ws, workDays);

          if (overlapsBlackout(ws, we)) continue;

          const calDays = calendarDaysBetween(ws, we);
          const holidaysInWindow = [];
          const cur = new Date(ws);
          while (cur <= we) {
            const key = cur.toISOString().split('T')[0];
            if (holSet.has(key)) {
              const h = allHols.find(h => h.date.toISOString().split('T')[0] === key);
              if (h) holidaysInWindow.push(h.name);
            }
            cur.setDate(cur.getDate() + 1);
          }

          const conflicts = teamConflictsInWindow(ws, we);
          // Score: extra days (calendar - working) as fraction, penalise for conflicts
          const extraDays = calDays - workDays;
          const score = (extraDays / workDays) * 100 - conflicts * 5;

          results.push({
            startDate: ws.toISOString().split('T')[0],
            endDate: we.toISOString().split('T')[0],
            workingDays: workDays,
            calendarDays: calDays,
            extraDays,
            holidays: holidaysInWindow,
            conflicts,
            score,
          });
        }
      }
    }

    // Sort by score desc, then deduplicate overlapping windows
    results.sort((a, b) => b.score - a.score);
    const deduped = [];
    for (const r of results) {
      const rs = new Date(r.startDate);
      const re = new Date(r.endDate);
      const overlapsExisting = deduped.some(d => {
        const ds = new Date(d.startDate);
        const de = new Date(d.endDate);
        return ds <= re && de >= rs;
      });
      if (!overlapsExisting) deduped.push(r);
      if (deduped.length >= 6) break;
    }

    setSuggestions(deduped);
    setComputing(false);
  };

  const formatDateEt = (isoStr) => {
    const d = new Date(isoStr);
    return d.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' });
  };

  const formatWeekday = (isoStr) => {
    const d = new Date(isoStr);
    return d.toLocaleDateString('et-EE', { weekday: 'short' });
  };

  return (
    <div className="date-suggester">
      <button
        type="button"
        className={`suggester-toggle ${open ? 'open' : ''}`}
        onClick={() => setOpen(o => !o)}
      >
        <span className="suggester-icon">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="12" cy="12" r="10"/>
            <path d="M12 6v6l4 2"/>
          </svg>
        </span>
        {open ? 'Peida soovitused' : 'Näita parimaid puhkuseaegu'}
        <svg className="chevron" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
          <path d="M6 9l6 6 6-6"/>
        </svg>
      </button>

      {open && (
        <div className="suggester-panel">
          <div className="suggester-desc">
            Parimad perioodid järgmise 6 kuu jooksul — arvutatud riigipühade ja meeskonna koormuse põhjal.
            Rohkem kalendripäevi samaväärse puhkusepäeva kuluga = kõrgem skoor.
          </div>

          {computing && <div className="suggester-loading">Arvutan…</div>}

          {!computing && suggestions.length === 0 && (
            <div className="suggester-empty">Piisavalt vabu perioode ei leitud.</div>
          )}

          {!computing && suggestions.map((s, i) => (
            <div key={i} className="suggestion-row">
              <div className="suggestion-dates">
                <span className="suggestion-start">
                  <span className="suggestion-weekday">{formatWeekday(s.startDate)}</span>
                  {formatDateEt(s.startDate)}
                </span>
                <span className="suggestion-sep">–</span>
                <span className="suggestion-end">
                  <span className="suggestion-weekday">{formatWeekday(s.endDate)}</span>
                  {formatDateEt(s.endDate)}
                </span>
              </div>
              <div className="suggestion-meta">
                <span className="suggestion-badge work">{s.workingDays} tp</span>
                <span className="suggestion-badge cal">{s.calendarDays} kp</span>
                {s.extraDays > 0 && (
                  <span className="suggestion-badge bonus">+{s.extraDays} tasuta</span>
                )}
                {s.holidays.length > 0 && (
                  <span className="suggestion-badge holiday" title={s.holidays.join(', ')}>
                    {s.holidays.length} riigipüha
                  </span>
                )}
                {s.conflicts > 0 && (
                  <span className="suggestion-badge conflict">{s.conflicts} kolleegi</span>
                )}
              </div>
              <button
                type="button"
                className="suggestion-select"
                onClick={() => { onSelect(s.startDate, s.endDate); setOpen(false); }}
              >
                Vali
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default DateSuggester;
