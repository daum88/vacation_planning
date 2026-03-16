using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class DepartmentCapacity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        /// <summary>Max number of people from this department allowed on leave simultaneously.</summary>
        public int MaxConcurrent { get; set; } = 2;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
