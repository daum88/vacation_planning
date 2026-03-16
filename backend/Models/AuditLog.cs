using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public enum AuditAction
    {
        Created,
        Updated,
        Deleted,
        Approved,
        Rejected,
        Withdrawn,
        AttachmentAdded,
        AttachmentDeleted,
        CommentAdded
    }

    public class AuditLog
    {
        public int Id { get; set; }

        public int VacationRequestId { get; set; }
        public VacationRequest? VacationRequest { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public AuditAction Action { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        [MaxLength(2000)]
        public string? OldValues { get; set; } // JSON string

        [MaxLength(2000)]
        public string? NewValues { get; set; } // JSON string

        public DateTime Timestamp { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }
}
