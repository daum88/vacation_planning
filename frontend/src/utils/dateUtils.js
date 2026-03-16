export const formatDate = (dateString) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('et-EE', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  });
};

export const formatDateForInput = (dateString) => {
  const date = new Date(dateString);
  return date.toISOString().split('T')[0];
};

export const getTodayString = () => {
  return new Date().toISOString().split('T')[0];
};

export const isDateInPast = (dateString) => {
  const date = new Date(dateString);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return date < today;
};

// --- Estonian public holidays ---

function calculateEaster(year) {
  const a = year % 19, b = Math.floor(year / 100), c = year % 100;
  const d = Math.floor(b / 4), e = b % 4, f = Math.floor((b + 8) / 25);
  const g = Math.floor((b - f + 1) / 3);
  const h = (19 * a + b - d - g + 15) % 30;
  const i = Math.floor(c / 4), k = c % 4;
  const l = (32 + 2 * e + 2 * i - h - k) % 7;
  const m = Math.floor((a + 11 * h + 22 * l) / 451);
  const month = Math.floor((h + l - 7 * m + 114) / 31) - 1; // 0-indexed
  const day = ((h + l - 7 * m + 114) % 31) + 1;
  return new Date(year, month, day);
}

function addDays(date, days) {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

export function getEstonianPublicHolidays(year) {
  const easter = calculateEaster(year);
  return [
    { date: new Date(year, 0, 1),  name: 'Uusaasta' },
    { date: new Date(year, 1, 24), name: 'Eesti Vabariigi aastapäev' },
    { date: addDays(easter, -2),   name: 'Suur reede' },
    { date: easter,                name: 'Ülestõusmispühade 1. püha' },
    { date: new Date(year, 4, 1),  name: 'Kevadpüha' },
    { date: addDays(easter, 49),   name: 'Nelipühade 1. püha' },
    { date: new Date(year, 5, 23), name: 'Võidupüha' },
    { date: new Date(year, 5, 24), name: 'Jaanipäev' },
    { date: new Date(year, 7, 20), name: 'Taasiseseisvumispäev' },
    { date: new Date(year, 11, 24), name: 'Jõululaupäev' },
    { date: new Date(year, 11, 25), name: 'Esimene jõulupüha' },
    { date: new Date(year, 11, 26), name: 'Teine jõulupüha' },
  ];
}

function isoDate(d) {
  return d.toISOString().split('T')[0];
}

// Cache for performance
const _cache = {};
function getCachedHolidays(year) {
  if (!_cache[year]) {
    _cache[year] = new Set(getEstonianPublicHolidays(year).map(h => isoDate(h.date)));
  }
  return _cache[year];
}

export function isPublicHoliday(date) {
  const d = typeof date === 'string' ? new Date(date) : date;
  const year = d.getFullYear();
  return getCachedHolidays(year).has(isoDate(d));
}

export function isWeekend(date) {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.getDay() === 0 || d.getDay() === 6;
}

export function isNonWorkingDay(date) {
  return isWeekend(date) || isPublicHoliday(date);
}

export function countWorkingDays(startDate, endDate) {
  if (!startDate || !endDate) return 0;
  const start = typeof startDate === 'string' ? new Date(startDate) : new Date(startDate);
  const end = typeof endDate === 'string' ? new Date(endDate) : new Date(endDate);
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
  const start = new Date(startDate);
  const end = new Date(endDate);
  return Math.max(0, Math.floor((end - start) / (1000 * 60 * 60 * 24)) + 1);
}

// Legacy alias
export const calculateDays = countCalendarDays;

export function getHolidaysInRange(startDate, endDate) {
  const start = new Date(startDate);
  const end = new Date(endDate);
  const result = [];
  const years = new Set();
  for (let y = start.getFullYear(); y <= end.getFullYear(); y++) years.add(y);
  years.forEach(year => {
    getEstonianPublicHolidays(year).forEach(h => {
      if (h.date >= start && h.date <= end) result.push(h);
    });
  });
  return result.sort((a, b) => a.date - b.date);
}
