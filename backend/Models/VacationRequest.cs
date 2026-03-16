using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class VacationRequest
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        [Required]
        public VacationRequestStatus Status { get; set; } = VacationRequestStatus.Pending;

        public int? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(500)]
        public string? AdminComment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }

    public enum VacationRequestStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
