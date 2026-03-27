using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class LeaveType
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(7)] // Hex color code
        public string Color { get; set; } = "#007AFF";

        public bool RequiresApproval { get; set; } = true;

        public bool RequiresAttachment { get; set; } = false;

        public int MaxDaysPerYear { get; set; } = 25;

        /// <summary>Minimum working days advance notice required before start date. 0 = no restriction.</summary>
        public int AdvanceNoticeDays { get; set; } = 0;

        public bool IsPaid { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
    }
}
