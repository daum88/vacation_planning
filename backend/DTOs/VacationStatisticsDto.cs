namespace VacationRequestApi.DTOs
{
    public class VacationStatisticsDto
    {
        public int TotalRequests { get; set; }
        public int TotalDays { get; set; }
        public int UpcomingRequests { get; set; }
        public int PastRequests { get; set; }
        public int CurrentYearDays { get; set; }
        public DateTime? NextVacationStart { get; set; }
        public List<MonthlyStatistic> MonthlyBreakdown { get; set; } = new();
    }

    public class MonthlyStatistic
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int DaysCount { get; set; }
        public int RequestsCount { get; set; }
    }
}
