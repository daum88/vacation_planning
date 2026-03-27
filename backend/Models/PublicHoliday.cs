using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class PublicHoliday
    {
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>True = repeats on the same month/day every year (fixed holidays).</summary>
        public bool IsRecurring { get; set; } = false;

        /// <summary>Null for recurring holidays, explicit year for one-off entries.</summary>
        public int? Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
