export const formatDate = (dateString) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('et-EE', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  });
};

export const toISOStr = (d) =>
  d ? (d instanceof Date ? d : new Date(d)).toISOString().split('T')[0] : '';

// ── Estonian public holidays ───────────────────────────────────────

function calculateEaster(year) {
  const a = year % 19, b = Math.floor(year / 100), c = year % 100;
  const d = Math.floor(b / 4), e = b % 4, f = Math.floor((b + 8) / 25);
  const g = Math.floor((b - f + 1) / 3);
  const h = (19 * a + b - d - g + 15) % 30;
  const i = Math.floor(c / 4), k = c % 4;
  const l = (32 + 2 * e + 2 * i - h - k) % 7;
  const m = Math.floor((a + 11 * h + 22 * l) / 451);
  const month = Math.floor((h + l - 7 * m + 114) / 31) - 1;
  const day   = ((h + l - 7 * m + 114) % 31) + 1;
  return new Date(year, month, day);
}

function shift(date, days) {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

export function getEstonianPublicHolidays(year) {
  const easter = calculateEaster(year);
  return [
    { date: new Date(year, 0, 1),   name: 'Uusaasta' },
    { date: new Date(year, 1, 24),  name: 'Eesti Vabariigi aastapäev' },
    { date: shift(easter, -2),      name: 'Suur reede' },
    { date: easter,                 name: 'Ülestõusmispühade 1. püha' },
    { date: new Date(year, 4, 1),   name: 'Kevadpüha' },
    { date: shift(easter, 49),      name: 'Nelipühade 1. püha' },
    { date: new Date(year, 5, 23),  name: 'Võidupüha' },
    { date: new Date(year, 5, 24),  name: 'Jaanipäev' },
    { date: new Date(year, 7, 20),  name: 'Taasiseseisvumispäev' },
    { date: new Date(year, 11, 24), name: 'Jõululaupäev' },
    { date: new Date(year, 11, 25), name: 'Esimene jõulupüha' },
    { date: new Date(year, 11, 26), name: 'Teine jõulupüha' },
  ];
}

const _holidayCache = {};
function getCachedHolidays(year) {
  if (!_holidayCache[year]) {
    _holidayCache[year] = new Set(
      getEstonianPublicHolidays(year).map(h => toISOStr(h.date))
    );
  }
  return _holidayCache[year];
}

export function isPublicHoliday(date) {
  const d = typeof date === 'string' ? new Date(date) : date;
  return getCachedHolidays(d.getFullYear()).has(toISOStr(d));
}

export function isWeekend(date) {
  const dow = (typeof date === 'string' ? new Date(date) : date).getDay();
  return dow === 0 || dow === 6;
}

export function isNonWorkingDay(date) {
  return isWeekend(date) || isPublicHoliday(date);
}

export function countWorkingDays(startDate, endDate) {
  if (!startDate || !endDate) return 0;
  const start = new Date(startDate), end = new Date(endDate);
  if (end < start) return 0;
  let count = 0;
  const cur = new Date(start);
  while (cur <= end) {
    if (!isNonWorkingDay(cur)) count++;
    cur.setDate(cur.getDate() + 1);
  }
  return count;
}

export function countCalendarDays(startDate, endDate) {
  if (!startDate || !endDate) return 0;
  const diff = new Date(endDate) - new Date(startDate);
  return Math.max(0, Math.floor(diff / 86400000) + 1);
}

export function getHolidaysInRange(startDate, endDate) {
  const start = new Date(startDate), end = new Date(endDate);
  const result = [];
  for (let y = start.getFullYear(); y <= end.getFullYear(); y++) {
    getEstonianPublicHolidays(y).forEach(h => {
      if (h.date >= start && h.date <= end) result.push(h);
    });
  }
  return result.sort((a, b) => a.date - b.date);
}

// Returns the next Monday from today (skips to the Monday after next
// if today is already Monday, to avoid same-day suggestions).
export function nextMonday() {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  const dow  = d.getDay();
  const skip = dow === 0 ? 1 : dow === 1 ? 7 : 8 - dow;
  d.setDate(d.getDate() + skip);
  return d;
}

// Advance `start` by exactly `needed` working days (skipping weekends
// and Estonian public holidays). Returns the last working day.
export function addWorkingDays(start, needed) {
  const holSet = new Set([
    ...getEstonianPublicHolidays(start.getFullYear()),
    ...getEstonianPublicHolidays(start.getFullYear() + 1),
  ].map(h => toISOStr(h.date)));

  let count = 0;
  const cur = new Date(start);
  while (count < needed) {
    const key = toISOStr(cur);
    if (!isWeekend(cur) && !holSet.has(key)) count++;
    if (count < needed) cur.setDate(cur.getDate() + 1);
  }
  return new Date(cur);
}
