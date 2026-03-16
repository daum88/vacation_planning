using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public enum VacationRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Withdrawn = 3
    }

    public class VacationRequest
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public int LeaveTypeId { get; set; } = 1; // Default to first leave type
        public LeaveType? LeaveType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        [Required]
        public VacationRequestStatus Status { get; set; } = VacationRequestStatus.Pending;

        public int? ApprovedByUserId { get; set; }
        public User? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? AdminComment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? SubstituteName { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation properties
        public ICollection<VacationRequestAttachment> Attachments { get; set; } = new List<VacationRequestAttachment>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
