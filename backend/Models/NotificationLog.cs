using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class NotificationLog
    {
        public int Id { get; set; }
        public int? RequestId { get; set; }

        [Required, MaxLength(200)]
        public string ToEmail { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public bool IsMock { get; set; } = true;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
