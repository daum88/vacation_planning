import React, { useState, useRef, useEffect, useMemo } from 'react';
import { getEstonianPublicHolidays, isWeekend, toISOStr } from '../../utils/dateUtils';
import { MONTHS_FULL, WEEKDAYS_SHORT } from '../../utils/locale';
import './DatePicker.css';

const parse = (str) => (str ? new Date(str + 'T00:00:00') : null);

const DatePicker = ({
  value = '',
  onChange,
  minDate = '',
  maxDate = '',
  placeholder = 'Vali kuupäev',
  disabled = false,
  error = false,
  blackouts = [],
  rangeStart = '',
  rangeEnd = '',
  id,
}) => {
  const today = useMemo(() => { const d = new Date(); d.setHours(0,0,0,0); return d; }, []);
  const todayStr = toISOStr(today);

  const [open, setOpen] = useState(false);
  const selectedDate = parse(value);
  const [viewYear, setViewYear]  = useState(selectedDate?.getFullYear() ?? today.getFullYear());
  const [viewMonth, setViewMonth] = useState(selectedDate?.getMonth()    ?? today.getMonth());

  const ref = useRef(null);

  // Sync view when value changes externally
  useEffect(() => {
    if (value) {
      const d = parse(value);
      if (d) { setViewYear(d.getFullYear()); setViewMonth(d.getMonth()); }
    }
  }, [value]);

  // Close on outside click
  useEffect(() => {
    if (!open) return;
    const fn = (e) => {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    };
    document.addEventListener('mousedown', fn);
    return () => document.removeEventListener('mousedown', fn);
  }, [open]);

  // Holidays for view year ± 1
  const { holidaySet, holidayNames } = useMemo(() => {
    const all = [
      ...getEstonianPublicHolidays(viewYear - 1),
      ...getEstonianPublicHolidays(viewYear),
      ...getEstonianPublicHolidays(viewYear + 1),
    ];
    const set  = new Set(all.map(h => toISOStr(h.date)));
    const names = {};
    all.forEach(h => { names[toISOStr(h.date)] = h.name; });
    return { holidaySet: set, holidayNames: names };
  }, [viewYear]);

  const isBlackout = (str) =>
    blackouts.some(b => str >= b.startDate.split('T')[0] && str <= b.endDate.split('T')[0]);

  // Build 42-cell grid (6 rows × 7 cols), week starts Monday
  const gridDays = useMemo(() => {
    const first = new Date(viewYear, viewMonth, 1);
    const last  = new Date(viewYear, viewMonth + 1, 0);
    // dow: 0=Sun → convert to Mon-first: (dow+6)%7
    const startPad = (first.getDay() + 6) % 7;
    const cells = [];
    for (let i = startPad - 1; i >= 0; i--) {
      const d = new Date(viewYear, viewMonth, -i);
      cells.push({ date: d, cur: false });
    }
    for (let d = 1; d <= last.getDate(); d++) {
      cells.push({ date: new Date(viewYear, viewMonth, d), cur: true });
    }
    while (cells.length < 42) {
      const d = new Date(viewYear, viewMonth + 1, cells.length - startPad - last.getDate() + 1);
      cells.push({ date: d, cur: false });
    }
    return cells;
  }, [viewYear, viewMonth]);

  const prevMonth = () => {
    if (viewMonth === 0) { setViewYear(y => y - 1); setViewMonth(11); }
    else setViewMonth(m => m - 1);
  };
  const nextMonth = () => {
    if (viewMonth === 11) { setViewYear(y => y + 1); setViewMonth(0); }
    else setViewMonth(m => m + 1);
  };
  const goToToday = () => { setViewYear(today.getFullYear()); setViewMonth(today.getMonth()); };

  const handleSelect = (str, disabled) => {
    if (disabled) return;
    onChange(str);
    setOpen(false);
  };

  const formatDisplay = (str) => {
    if (!str) return '';
    const d = parse(str);
    return d.toLocaleDateString('et-EE', { day: '2-digit', month: '2-digit', year: 'numeric' });
  };

  // Build per-cell class list
  const cellInfo = (date, cur) => {
    const str = toISOStr(date);
    const isSelected  = str === value;
    const isToday     = str === todayStr;
    const isHol       = holidaySet.has(str);
    const isWknd      = isWeekend(date);
    const isBlk       = isBlackout(str);
    const isPast      = minDate && str < minDate;
    const isFuture    = maxDate && str > maxDate;
    const cellDisabled = isPast || isFuture || isBlk;

    // Range highlight
    const rs = rangeStart || value;
    const re = rangeEnd   || value;
    const inRange   = rs && re && rs < re && str > rs && str < re;
    const atStart   = rs && str === rs && re && rs < re;
    const atEnd     = re && str === re && rs && rs < re;

    const classes = [
      'dp-day',
      cur ? '' : 'dp-other',
      isSelected ? 'dp-selected' : '',
      isToday    ? 'dp-today'    : '',
      isHol  && !isSelected ? 'dp-holiday' : '',
      isWknd && !isSelected ? 'dp-weekend' : '',
      isBlk      ? 'dp-blackout' : '',
      inRange    ? 'dp-in-range' : '',
      atStart    ? 'dp-range-start' : '',
      atEnd      ? 'dp-range-end'   : '',
      cellDisabled ? 'dp-disabled' : '',
    ].filter(Boolean).join(' ');

    // Tooltip
    const titleParts = [
      date.toLocaleDateString('et-EE', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }),
    ];
    if (isHol) titleParts.push(holidayNames[str]);
    if (isBlk) titleParts.push('Blokeeritud periood');

    return { str, classes, disabled: cellDisabled, title: titleParts.join(' — ') };
  };

  return (
    <div
      ref={ref}
      className={[
        'dp-wrapper',
        open ? 'dp-open' : '',
        disabled ? 'dp-disabled-wrapper' : '',
        error ? 'dp-error' : '',
      ].filter(Boolean).join(' ')}
    >
      <div
        id={id}
        className="dp-trigger"
        onClick={() => !disabled && setOpen(o => !o)}
        tabIndex={disabled ? -1 : 0}
        onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setOpen(o => !o); } if (e.key === 'Escape') setOpen(false); }}
      >
        <svg className="dp-cal-icon" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
          <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
          <line x1="3" y1="10" x2="21" y2="10"/>
        </svg>
        <span className={value ? 'dp-display-value' : 'dp-display-placeholder'}>
          {value ? formatDisplay(value) : placeholder}
        </span>
        <svg className="dp-chevron" width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
          <path d="M6 9l6 6 6-6"/>
        </svg>
      </div>

      {open && (
        <div className="dp-popup">
          {/* Header */}
          <div className="dp-header">
            <button type="button" className="dp-nav-btn" onClick={prevMonth} title="Eelmine kuu">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="M15 18l-6-6 6-6"/></svg>
            </button>
            <button type="button" className="dp-month-year" onClick={goToToday}>
              {MONTHS_FULL[viewMonth]} {viewYear}
            </button>
            <button type="button" className="dp-nav-btn" onClick={nextMonth} title="Järgmine kuu">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="M9 18l6-6-6-6"/></svg>
            </button>
          </div>

          {/* Day-of-week labels */}
          <div className="dp-dow-row">
            {WEEKDAYS_SHORT.map(d => <div key={d} className="dp-dow">{d}</div>)}
          </div>

          {/* Day grid */}
          <div className="dp-grid">
            {gridDays.map(({ date, cur }, i) => {
              const { str, classes, disabled: cd, title } = cellInfo(date, cur);
              return (
                <button
                  key={i}
                  type="button"
                  className={classes}
                  title={title}
                  disabled={cd}
                  onClick={() => handleSelect(str, cd)}
                  tabIndex={-1}
                >
                  {date.getDate()}
                  {holidaySet.has(str) && <span className="dp-hol-dot" />}
                </button>
              );
            })}
          </div>

          {/* Footer */}
          <div className="dp-footer">
            <button type="button" className="dp-footer-btn" onClick={() => { if (!(minDate && todayStr < minDate)) { onChange(todayStr); setOpen(false); } }}>
              Täna
            </button>
            {value && (
              <button type="button" className="dp-footer-btn dp-clear" onClick={() => { onChange(''); }}>
                Tühjenda
              </button>
            )}
            <button type="button" className="dp-footer-btn dp-close-btn" onClick={() => setOpen(false)}>
              Sulge
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default DatePicker;
