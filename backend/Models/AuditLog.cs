namespace VacationRequestApi.Models
{
    public enum AuditEventType
    {
        Login,
        LoginFailed,
        Logout,
        Register,
        ProfileCompleted,
        PasswordChanged,
        UserCreated,
        UserUpdated,
        UserDeleted,
        VacationRequestCreated,
        VacationRequestUpdated,
        VacationRequestApproved,
        VacationRequestRejected,
        VacationRequestCancelled,
        JoinRequestCreated,
        JoinRequestApproved,
        JoinRequestRejected,
        OrganizationCreated,
        OrganizationUpdated,
        Unauthorized
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public AuditEventType EventType { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool Success { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User? User { get; set; }
    }
}
