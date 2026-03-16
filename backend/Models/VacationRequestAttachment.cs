using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class VacationRequestAttachment
    {
        public int Id { get; set; }

        public int VacationRequestId { get; set; }
        public VacationRequest VacationRequest { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; } // in bytes

        [Required]
        public string FilePath { get; set; } = string.Empty; // Physical path on server

        public int UploadedByUserId { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
