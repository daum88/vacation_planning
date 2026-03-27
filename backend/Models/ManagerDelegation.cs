using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    /// <summary>
    /// Tracks who is delegated to act as a manager while the original manager is absent.
    /// </summary>
    public class ManagerDelegation
    {
        public int Id { get; set; }

        /// <summary>The manager who is delegating.</summary>
        public int ManagerId { get; set; }
        public User? Manager { get; set; }

        /// <summary>The user who will act as manager during the period.</summary>
        public int DelegateId { get; set; }
        public User? Delegate { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
