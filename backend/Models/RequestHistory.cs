using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    /// <summary>
    /// One entry per meaningful change on a VacationRequest (status change, edit, comment).
    /// Visible to both the requester and admins.
    /// </summary>
    public class RequestHistory
    {
        public int Id { get; set; }

        public int VacationRequestId { get; set; }
        public VacationRequest? VacationRequest { get; set; }

        public int? ActorUserId { get; set; }
        public User? Actor { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty; // created | status_changed | edited | comment_added

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? OldValue { get; set; }

        [MaxLength(50)]
        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
