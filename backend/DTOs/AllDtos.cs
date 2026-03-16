namespace VacationRequestApi.DTOs
{
    // User DTOs
    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? Position { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public int AnnualLeaveDays { get; set; }
        public int UsedLeaveDays { get; set; }
        public int CarryOverDays { get; set; }
        public int RemainingLeaveDays { get; set; }
        public DateTime HireDate { get; set; }
    }

    // Leave Type DTOs
    public class LeaveTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Color { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
        public bool RequiresAttachment { get; set; }
        public int MaxDaysPerYear { get; set; }
        public bool IsPaid { get; set; }
        public bool IsActive { get; set; }
    }

    // Enhanced Vacation Request DTOs
    public class VacationRequestCreateDto
    {
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Comment { get; set; }
    }

    public class VacationRequestUpdateDto
    {
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Comment { get; set; }
    }

    public class VacationRequestResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? Department { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
        public string? LeaveTypeColor { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Comment { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? AdminComment { get; set; }
        public int DaysCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanWithdraw { get; set; }
    }

    public class ApprovalDto
    {
        public bool Approved { get; set; }
        public string? AdminComment { get; set; }
    }

    // Attachment DTOs
    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int UploadedByUserId { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    // Audit Log DTOs
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
    }

    // Filter DTOs
    public class VacationRequestFilterDto
    {
        public int? UserId { get; set; }
        public int? LeaveTypeId { get; set; }
        public string? Status { get; set; } // Pending, Approved, Rejected, Withdrawn
        public string? Department { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } // StartDate, EndDate, CreatedAt, Status
        public bool SortDescending { get; set; } = true;
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }

    // Calendar DTOs
    public class CalendarEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
    }

    public class TeamCalendarDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<CalendarEventDto> Events { get; set; } = new List<CalendarEventDto>();
        public Dictionary<DateTime, int> DailyAbsenceCount { get; set; } = new Dictionary<DateTime, int>();
    }

    // Statistics DTOs (Enhanced)
    public class VacationStatisticsDto
    {
        public int TotalRequests { get; set; }
        public int TotalDays { get; set; }
        public int CurrentYearDays { get; set; }
        public int UpcomingVacationsCount { get; set; }
        public DateTime? NextVacationDate { get; set; }
        public List<MonthlyBreakdown> MonthlyBreakdown { get; set; } = new List<MonthlyBreakdown>();
        public UserBalanceDto? UserBalance { get; set; }
        public List<LeaveTypeUsageDto> LeaveTypeUsage { get; set; } = new List<LeaveTypeUsageDto>();
    }

    public class MonthlyBreakdown
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int DaysCount { get; set; }
        public int RequestsCount { get; set; }
    }

    public class UserBalanceDto
    {
        public int AnnualLeaveDays { get; set; }
        public int UsedLeaveDays { get; set; }
        public int CarryOverDays { get; set; }
        public int RemainingLeaveDays { get; set; }
        public int PendingDays { get; set; }
        public int ApprovedDays { get; set; }
    }

    public class LeaveTypeUsageDto
    {
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int DaysUsed { get; set; }
        public int RequestsCount { get; set; }
    }
}
