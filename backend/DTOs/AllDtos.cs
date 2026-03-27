using System.ComponentModel.DataAnnotations;

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
        public int AdvanceNoticeDays { get; set; }
        public bool IsPaid { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class LeaveTypeCreateUpdateDto
    {
        [Required(ErrorMessage = "Nimi on kohustuslik")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(7)]
        public string Color { get; set; } = "#007AFF";

        public bool RequiresApproval { get; set; } = true;
        public bool RequiresAttachment { get; set; } = false;

        [Range(1, 365)]
        public int MaxDaysPerYear { get; set; } = 25;

        [Range(0, 90)]
        public int AdvanceNoticeDays { get; set; } = 0;

        public bool IsPaid { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    // Enhanced Vacation Request DTOs
    public class VacationRequestCreateDto
    {
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Comment { get; set; }
        public string? SubstituteName { get; set; }
    }

    public class VacationRequestUpdateDto
    {
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Comment { get; set; }
        public string? SubstituteName { get; set; }
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
        public string? SubstituteName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? AdminComment { get; set; }
        public int DaysCount { get; set; }        // working days
        public int CalendarDaysCount { get; set; } // total calendar days
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
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public bool Success { get; set; }
        public DateTime CreatedAt { get; set; }
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

    // Blackout Period DTOs
    public class BlackoutPeriodDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BlackoutPeriodCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    // Public Holiday DTOs (simple, used by old iCal + dateUtils)
    public class PublicHolidaySimpleDto
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Notification Log DTOs
    public class NotificationLogDto
    {
        public int Id { get; set; }
        public int? RequestId { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsMock { get; set; }
        public DateTime SentAt { get; set; }
    }

    // Carry-over update DTO
    public class CarryOverUpdateDto
    {
        public int CarryOverDays { get; set; }
    }

    // Department Capacity DTOs
    public class DepartmentCapacityDto
    {
        public int Id { get; set; }
        public string Department { get; set; } = string.Empty;
        public int MaxConcurrent { get; set; }
        public bool IsActive { get; set; }
    }

    public class DepartmentCapacityCreateDto
    {
        public string Department { get; set; } = string.Empty;
        public int MaxConcurrent { get; set; } = 2;
    }

    public class DepartmentCapacityCheckDto
    {
        public bool HasLimit { get; set; }
        public int MaxConcurrent { get; set; }
        public int CurrentCount { get; set; }
        public bool WouldExceed { get; set; }
        public string Department { get; set; } = string.Empty;
    }

    // Request Comment DTOs
    public class RequestCommentDto
    {
        public int Id { get; set; }
        public int VacationRequestId { get; set; }
        public int AuthorUserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCommentDto
    {
        public string Text { get; set; } = string.Empty;
    }

    // Bulk Approve DTOs
    public class BulkApproveItemDto
    {
        public int Id { get; set; }
        public bool Approved { get; set; }
        public string? AdminComment { get; set; }
    }

    public class BulkApproveResultDto
    {
        public int Processed { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    // Annual Reset DTOs
    public class AnnualResetResultDto
    {
        public int UsersReset { get; set; }
        public int Year { get; set; }
        public int MaxCarryOverDays { get; set; }
        public List<AnnualResetUserDto> Details { get; set; } = new List<AnnualResetUserDto>();
    }

    public class AnnualResetUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int PreviousUsedDays { get; set; }
        public int PreviousCarryOver { get; set; }
        public int NewCarryOver { get; set; }
    }

    // Authentication DTOs
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string? Department { get; set; }
        public bool IsTemporaryPassword { get; set; }
        public bool IsProfileComplete { get; set; }
    }

    // ── User-facing notification bell ──────────────────────────────────────

    public class UserNotificationItemDto
    {
        public int Id { get; set; }
        /// <summary>comment | approved | rejected | pending</summary>
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int RequestId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserNotificationsDto
    {
        public List<UserNotificationItemDto> Items { get; set; } = new();
        public int UnreadCount { get; set; }
        public DateTime FetchedAt { get; set; }
    }

    // ── Public Holidays ─────────────────────────────────────────────────────

    public class PublicHolidayDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public int? Year { get; set; }
    }

    public class PublicHolidayCreateUpdateDto
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsRecurring { get; set; } = false;
        public int? Year { get; set; }
    }

    // ── Manager Delegation ───────────────────────────────────────────────────

    public class ManagerDelegationDto
    {
        public int Id { get; set; }
        public int ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public int DelegateId { get; set; }
        public string DelegateName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrentlyActive { get; set; }
    }

    public class ManagerDelegationCreateDto
    {
        [Required]
        public int DelegateId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }
    }

    // ── Request History ──────────────────────────────────────────────────────

    public class RequestHistoryItemDto
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ActorName { get; set; }
        public bool ActorIsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
