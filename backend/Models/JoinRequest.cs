namespace VacationRequestApi.Models
{
    public enum JoinRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class JoinRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrganizationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;
        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public Organization Organization { get; set; } = null!;
        public User? ReviewedBy { get; set; }
    }
}
