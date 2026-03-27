using VacationRequestApi.DTOs;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Extensions
{
    /// <summary>
    /// Extension methods for mapping Models to DTOs.
    /// Centralises all inline "new XxxDto {}" projections scattered across controllers.
    /// </summary>
    public static class MappingExtensions
    {
        // ──────────────────────────────────────────────────────────────────────
        // User
        // ──────────────────────────────────────────────────────────────────────

        public static UserDto ToDto(this User user) => new()
        {
            Id               = user.Id,
            FirstName        = user.FirstName,
            LastName         = user.LastName,
            FullName         = user.FullName,
            Email            = user.Email,
            Department       = user.Department,
            Position         = user.Position,
            ManagerId        = user.ManagerId,
            ManagerName      = user.Manager?.FullName,
            IsActive         = user.IsActive,
            IsAdmin          = user.IsAdmin,
            AnnualLeaveDays  = user.AnnualLeaveDays,
            UsedLeaveDays    = user.UsedLeaveDays,
            CarryOverDays    = user.CarryOverDays,
            RemainingLeaveDays = user.RemainingLeaveDays,
            HireDate         = user.HireDate,
        };

        public static IEnumerable<UserDto> ToDtos(this IEnumerable<User> users)
            => users.Select(u => u.ToDto());

        // ──────────────────────────────────────────────────────────────────────
        // AuditLog
        // ──────────────────────────────────────────────────────────────────────

        public static AuditLogDto ToDto(this AuditLog log) => new()
        {
            Id          = log.Id,
            UserId      = log.UserId,
            UserEmail   = log.UserEmail,
            EventType   = log.EventType.ToString(),
            EntityType  = log.EntityType,
            EntityId    = log.EntityId,
            Details     = log.Details,
            IpAddress   = log.IpAddress,
            Success     = log.Success,
            CreatedAt   = log.CreatedAt,
        };

        public static IEnumerable<AuditLogDto> ToDtos(this IEnumerable<AuditLog> logs)
            => logs.Select(l => l.ToDto());

        // ──────────────────────────────────────────────────────────────────────
        // JoinRequest
        // ──────────────────────────────────────────────────────────────────────

        public static JoinRequestDto ToDto(this JoinRequest jr) => new()
        {
            Id               = jr.Id,
            UserId           = jr.UserId,
            UserFullName     = jr.User?.FullName ?? string.Empty,
            UserEmail        = jr.User?.Email ?? string.Empty,
            OrganizationId   = jr.OrganizationId,
            OrganizationName = jr.Organization?.Name ?? string.Empty,
            Message          = jr.Message,
            Status           = jr.Status.ToString(),
            ReviewedByUserId = jr.ReviewedByUserId,
            ReviewedByName   = jr.ReviewedBy?.FullName,
            ReviewedAt       = jr.ReviewedAt,
            ReviewNote       = jr.ReviewNote,
            CreatedAt        = jr.CreatedAt,
        };

        public static IEnumerable<JoinRequestDto> ToDtos(this IEnumerable<JoinRequest> requests)
            => requests.Select(r => r.ToDto());

        // ──────────────────────────────────────────────────────────────────────
        // Organization
        // ──────────────────────────────────────────────────────────────────────

        public static OrganizationDto ToDto(this Organization org) => new()
        {
            Id           = org.Id,
            Name         = org.Name,
            Description  = org.Description,
            Address      = org.Address,
            ContactEmail = org.ContactEmail,
            ContactPhone = org.ContactPhone,
            IsActive     = org.IsActive,
            MemberCount  = org.Users?.Count(u => u.IsActive) ?? 0,
            CreatedAt    = org.CreatedAt,
        };

        public static IEnumerable<OrganizationDto> ToDtos(this IEnumerable<Organization> orgs)
            => orgs.Select(o => o.ToDto());

        // ──────────────────────────────────────────────────────────────────────
        // VacationRequest → VacationRequestResponseDto
        // (DaysCount and CanXxx flags are caller-computed; pass them in)
        // ──────────────────────────────────────────────────────────────────────

        public static VacationRequestResponseDto ToDto(
            this VacationRequest req,
            int daysCount,
            bool canEdit,
            bool canDelete,
            bool canWithdraw) => new()
        {
            Id               = req.Id,
            UserId           = req.UserId,
            UserName         = req.User?.FullName,
            UserEmail        = req.User?.Email,
            Department       = req.User?.Department,
            LeaveTypeId      = req.LeaveTypeId,
            LeaveTypeName    = req.LeaveType?.Name,
            LeaveTypeColor   = req.LeaveType?.Color,
            StartDate        = req.StartDate,
            EndDate          = req.EndDate,
            Comment          = req.Comment,
            SubstituteName   = req.SubstituteName,
            Status           = req.Status.ToString(),
            ApprovedByUserId = req.ApprovedByUserId,
            ApprovedByName   = req.ApprovedBy?.FullName,
            ApprovedAt       = req.ApprovedAt,
            AdminComment     = req.AdminComment,
            DaysCount        = daysCount,
            CalendarDaysCount = (req.EndDate.Date - req.StartDate.Date).Days + 1,
            CreatedAt        = req.CreatedAt,
            UpdatedAt        = req.UpdatedAt,
            Attachments      = req.Attachments.Select(a => new AttachmentDto
            {
                Id                = a.Id,
                FileName          = a.FileName,
                ContentType       = a.ContentType,
                FileSize          = a.FileSize,
                UploadedByUserId  = a.UploadedByUserId,
                UploadedAt        = a.UploadedAt,
            }).ToList(),
            CanEdit     = canEdit,
            CanDelete   = canDelete,
            CanWithdraw = canWithdraw,
        };
    }
}
