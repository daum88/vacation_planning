using Mapster;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;

namespace VacationRequestApi.Mapping
{
    /// <summary>
    /// Mapster configuration for DTO ↔ Model mapping
    /// </summary>
    public static class MappingConfig
    {
        public static void Configure()
        {
            // ──────────────────────────────────────────
            // USER MAPPINGS
            // ──────────────────────────────────────────
            TypeAdapterConfig<User, UserDto>.NewConfig()
                .Map(d => d.FullName, s => $"{s.FirstName} {s.LastName}")
                .Map(d => d.ManagerName, s =>
                    s.Manager != null ? $"{s.Manager.FirstName} {s.Manager.LastName}" : null);

            // ──────────────────────────────────────────
            // LEAVE TYPE MAPPINGS
            // ──────────────────────────────────────────
            TypeAdapterConfig<LeaveType, LeaveTypeDto>.NewConfig(); // 1:1 mapping

            // ──────────────────────────────────────────
            // VACATION REQUEST MAPPINGS
            // ──────────────────────────────────────────
            TypeAdapterConfig<VacationRequest, VacationRequestResponseDto>.NewConfig()
                .Map(d => d.UserName, s =>
                    s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : null)
                .Map(d => d.UserEmail, s =>
                    s.User != null ? s.User.Email : null)
                .Map(d => d.Department, s =>
                    s.User != null ? s.User.Department : null)
                .Map(d => d.LeaveTypeName, s =>
                    s.LeaveType != null ? s.LeaveType.Name : null)
                .Map(d => d.LeaveTypeColor, s =>
                    s.LeaveType != null ? s.LeaveType.Color : null)
                .Map(d => d.Status, s => s.Status.ToString())
                .Map(d => d.ApprovedByName, s =>
                    s.ApprovedBy != null ? $"{s.ApprovedBy.FirstName} {s.ApprovedBy.LastName}" : null)
                .Map(d => d.CalendarDaysCount, s =>
                    (s.EndDate.Date - s.StartDate.Date).Days + 1)
                .Ignore(d => d.DaysCount)       // computed by controller
                .Ignore(d => d.CanEdit)
                .Ignore(d => d.CanDelete)
                .Ignore(d => d.CanWithdraw);

            // ──────────────────────────────────────────
            // ATTACHMENT MAPPINGS
            // ──────────────────────────────────────────
            TypeAdapterConfig<VacationRequestAttachment, AttachmentDto>.NewConfig()
                .Ignore(d => d.UploadedByName); // no nav prop for uploader

            // ──────────────────────────────────────────
            // AUDIT LOG MAPPINGS
            // ──────────────────────────────────────────
            TypeAdapterConfig<AuditLog, AuditLogDto>.NewConfig()
                .Map(d => d.UserEmail, s =>
                    s.User != null ? s.User.Email : null)
                .Map(d => d.EventType, s => s.EventType.ToString());

            // ──────────────────────────────────────────
            // ORGANIZATION MAPPINGS
            // ──────────────────────────────────────────
            TypeAdapterConfig<Organization, OrganizationDto>.NewConfig()
                .Map(d => d.MemberCount, s =>
                    s.Users != null ? s.Users.Count(u => u.IsActive) : 0);

            TypeAdapterConfig<OrganizationCreateDto, Organization>.NewConfig()
                .Ignore(d => d.Id)
                .Ignore(d => d.Users)
                .Ignore(d => d.JoinRequests)
                .Map(d => d.IsActive, _ => true)
                .Map(d => d.CreatedAt, _ => DateTime.UtcNow);

            // ──────────────────────────────────────────
            // JOIN REQUEST MAPPINGS
            // ──────────────────────────────────────────
            TypeAdapterConfig<JoinRequest, JoinRequestDto>.NewConfig()
                .Map(d => d.UserFullName, s =>
                    s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : string.Empty)
                .Map(d => d.UserEmail, s =>
                    s.User != null ? s.User.Email : string.Empty)
                .Map(d => d.OrganizationName, s =>
                    s.Organization != null ? s.Organization.Name : string.Empty)
                .Map(d => d.Status, s => s.Status.ToString())
                .Map(d => d.ReviewedByName, s =>
                    s.ReviewedBy != null ? $"{s.ReviewedBy.FirstName} {s.ReviewedBy.LastName}" : null);
        }
    }
}
