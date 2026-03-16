using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class RequestComment
    {
        public int Id { get; set; }

        [Required]
        public int VacationRequestId { get; set; }
        public VacationRequest VacationRequest { get; set; } = null!;

        [Required]
        public int AuthorUserId { get; set; }
        public User Author { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Text { get; set; } = string.Empty;

        /// <summary>True when the comment was posted by an admin/manager.</summary>
        public bool IsAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
