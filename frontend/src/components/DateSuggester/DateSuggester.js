import React, { useState, useEffect, useCallback } from 'react';
import {
  getEstonianPublicHolidays,
  toISOStr, addWorkingDays, nextMonday,
} from '../../utils/dateUtils';
import { calendarApi } from '../../api/api';
import './DateSuggester.css';

const DateSuggester = ({ remainingDays, blackouts = [], department, onSelect }) => {
  const [open, setOpen]               = useState(false);
  const [suggestions, setSuggestions] = useState([]);
  const [computing, setComputing]     = useState(false);
  const [teamAbsences, setTeamAbsences] = useState([]);

  const loadTeamData = useCallback(async () => {
    try {
      const today = new Date();
      const end   = new Date(today);
      end.setMonth(end.getMonth() + 7);
      const res = await calendarApi.getTeamCalendar(
        toISOStr(today),
        toISOStr(end),
        department || undefined
      );
      const approved = (res.data?.events || []).filter(e => e.status === 'Approved');
      setTeamAbsences(approved);
    } catch {
      setTeamAbsences([]);
    }
  }, [department]);

  useEffect(() => {
    if (open && teamAbsences.length === 0) loadTeamData();
  }, [open, teamAbsences.length, loadTeamData]);

  useEffect(() => {
    if (open && remainingDays > 0) compute();
  }, [open, remainingDays, teamAbsences, blackouts]); // eslint-disable-line

  const calDaysBetween = (s, e) => Math.round((e - s) / 86400000) + 1;

  const overlapsBlackout = (s, e) =>
    blackouts.some(b => new Date(b.startDate) <= e && new Date(b.endDate) >= s);

  // Count how many team members are on leave in this window
  const teamConflictsInWindow = (ws, we) => {
    const set = new Set();
    teamAbsences.forEach(ev => {
      const evS = new Date(ev.start);
      const evE = new Date(ev.end);
      if (evS <= we && evE >= ws) set.add(ev.userName);
    });
    return set.size;
  };

  // Approximate total team size from absences data
  const estimatedTeamSize = Math.max(5,
    new Set(teamAbsences.map(e => e.userName)).size + 3
  );

  const compute = () => {
    setComputing(true);

    const hols1 = getEstonianPublicHolidays(new Date().getFullYear());
    const hols2 = getEstonianPublicHolidays(new Date().getFullYear() + 1);
    const allHols = [...hols1, ...hols2];
    const holSet  = new Set(allHols.map(h => toISOStr(h.date)));
    const holNameMap = {};
    allHols.forEach(h => { holNameMap[toISOStr(h.date)] = h.name; });

    const targetLengths = [3, 5, 7, 10, 14].filter(l => l <= remainingDays);
    if (!targetLengths.length && remainingDays > 0) targetLengths.push(remainingDays);

    const monday = nextMonday();
    const results = [];

    for (let week = 0; week < 26; week++) {
      // Try Monday and Thursday starts each week
      const candidates = [0, 3].map(off => {
        const d = new Date(monday);
        d.setDate(monday.getDate() + week * 7 + off);
        return d;
      });

      for (const ws of candidates) {
        for (const wDays of targetLengths) {
          if (wDays > remainingDays) continue;
          const we = addWorkingDays(ws, wDays);
          if (overlapsBlackout(ws, we)) continue;

          const calDays = calDaysBetween(ws, we);
          const extraDays = calDays - wDays;

          // Collect holidays in window
          const hols = [];
          const cur = new Date(ws);
          while (cur <= we) {
            const k = toISOStr(cur);
            if (holSet.has(k)) hols.push(holNameMap[k]);
            cur.setDate(cur.getDate() + 1);
          }

          const conflicts = teamConflictsInWindow(ws, we);
          const conflictPct = conflicts / estimatedTeamSize; // 0–1

          // Score: extra free days per working day used (holiday/weekend leverage)
          // minus a penalty proportional to how much of the team is already off
          const score = (extraDays / wDays) * 100 - conflictPct * 40;

          results.push({
            startDate: toISOStr(ws),
            endDate:   toISOStr(we),
            workingDays: wDays,
            calendarDays: calDays,
            extraDays,
            holidays: hols,
            conflicts,
            conflictPct,
            score,
          });
        }
      }
    }

    // Sort by score, deduplicate overlapping windows, take top 6
    results.sort((a, b) => b.score - a.score);
    const deduped = [];
    for (const r of results) {
      const rs = new Date(r.startDate), re = new Date(r.endDate);
      const overlaps = deduped.some(d => {
        const ds = new Date(d.startDate), de = new Date(d.endDate);
        return ds <= re && de >= rs;
      });
      if (!overlaps) deduped.push(r);
      if (deduped.length >= 6) break;
    }

    setSuggestions(deduped);
    setComputing(false);
  };

  const fmtDate = (s) => new Date(s).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' });
  const fmtWD   = (s) => new Date(s).toLocaleDateString('et-EE', { weekday: 'short' });

  const conflictLabel = (n, pct) => {
    if (n === 0) return null;
    if (pct >= 0.5) return { text: `${n} kolleegi — tihe`, cls: 'conflict conflict-high' };
    if (pct >= 0.25) return { text: `${n} kolleegi`, cls: 'conflict' };
    return { text: `${n} kolleegi`, cls: 'conflict conflict-low' };
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
            Parimad perioodid järgmise 6 kuu jooksul — optimeeritud riigipühade ja meeskonna kättesaadavuse põhjal.
            {teamAbsences.length > 0 && ` Arvestab ${estimatedTeamSize} meeskonnaliikmega.`}
          </div>

          {computing && <div className="suggester-loading">Arvutan…</div>}

          {!computing && suggestions.length === 0 && (
            <div className="suggester-empty">Piisavalt vabu perioode ei leitud.</div>
          )}

          {!computing && suggestions.map((s, i) => {
            const cLabel = conflictLabel(s.conflicts, s.conflictPct);
            return (
              <div key={i} className="suggestion-row">
                <div className="suggestion-dates">
                  <span className="suggestion-start">
                    <span className="suggestion-weekday">{fmtWD(s.startDate)}</span>
                    {fmtDate(s.startDate)}
                  </span>
                  <span className="suggestion-sep">–</span>
                  <span className="suggestion-end">
                    <span className="suggestion-weekday">{fmtWD(s.endDate)}</span>
                    {fmtDate(s.endDate)}
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
                  {cLabel && (
                    <span className={`suggestion-badge ${cLabel.cls}`}>{cLabel.text}</span>
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
            );
          })}
        </div>
      )}
    </div>
  );
};

export default DateSuggester;
