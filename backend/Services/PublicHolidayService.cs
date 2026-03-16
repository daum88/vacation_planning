namespace VacationRequestApi.Services
{
    public interface IPublicHolidayService
    {
        List<DateTime> GetEstonianPublicHolidays(int year);
        List<(DateTime Date, string Name)> GetEstonianPublicHolidaysNamed(int year);
        bool IsPublicHoliday(DateTime date);
        bool IsWeekend(DateTime date);
        int CountWorkingDays(DateTime startDate, DateTime endDate);
    }

    public class PublicHolidayService : IPublicHolidayService
    {
        private readonly Dictionary<int, List<(DateTime Date, string Name)>> _cache = new();

        public List<(DateTime Date, string Name)> GetEstonianPublicHolidaysNamed(int year)
        {
            if (_cache.TryGetValue(year, out var cached)) return cached;

            var easter = CalculateEaster(year);
            var holidays = new List<(DateTime, string)>
            {
                (new DateTime(year, 1, 1),   "Uusaasta"),
                (new DateTime(year, 2, 24),  "Eesti Vabariigi aastapäev"),
                (easter.AddDays(-2),          "Suur reede"),
                (easter,                      "Ülestõusmispühade 1. püha"),
                (new DateTime(year, 5, 1),   "Kevadpüha"),
                (easter.AddDays(49),          "Nelipühade 1. püha"),
                (new DateTime(year, 6, 23),  "Võidupüha"),
                (new DateTime(year, 6, 24),  "Jaanipäev"),
                (new DateTime(year, 8, 20),  "Taasiseseisvumispäev"),
                (new DateTime(year, 12, 24), "Jõululaupäev"),
                (new DateTime(year, 12, 25), "Esimene jõulupüha"),
                (new DateTime(year, 12, 26), "Teine jõulupüha"),
            };

            _cache[year] = holidays;
            return holidays;
        }

        public List<DateTime> GetEstonianPublicHolidays(int year)
            => GetEstonianPublicHolidaysNamed(year).Select(h => h.Date).ToList();

        public bool IsPublicHoliday(DateTime date)
            => GetEstonianPublicHolidays(date.Year).Any(h => h.Date == date.Date);

        public bool IsWeekend(DateTime date)
            => date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        public int CountWorkingDays(DateTime startDate, DateTime endDate)
        {
            int count = 0;
            var current = startDate.Date;
            while (current <= endDate.Date)
            {
                if (!IsWeekend(current) && !IsPublicHoliday(current))
                    count++;
                current = current.AddDays(1);
            }
            return count;
        }

        private static DateTime CalculateEaster(int year)
        {
            int a = year % 19, b = year / 100, c = year % 100;
            int d = b / 4, e = b % 4, f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4, k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;
            return new DateTime(year, month, day);
        }
    }
}
