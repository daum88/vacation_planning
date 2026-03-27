using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VacationRequestApi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public int? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        [Required]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Position { get; set; }

        public int? ManagerId { get; set; }
        public User? Manager { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsAdmin { get; set; } = false;

        // Password & Profile management
        public bool IsTemporaryPassword { get; set; } = false;
        public bool IsProfileComplete { get; set; } = false;
        public DateTime? LastPasswordChangeAt { get; set; }

        // Vacation balance
        public int AnnualLeaveDays { get; set; } = 25; // Default 25 days per year
        public int UsedLeaveDays { get; set; } = 0;
        public int CarryOverDays { get; set; } = 0; // Days carried from previous year

        public DateTime HireDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
        public ICollection<User> DirectReports { get; set; } = new List<User>();
        public ICollection<JoinRequest> JoinRequests { get; set; } = new List<JoinRequest>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        // Computed property
        public int RemainingLeaveDays => AnnualLeaveDays + CarryOverDays - UsedLeaveDays;

        public string FullName => $"{FirstName} {LastName}";
    }
}
