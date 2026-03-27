using System.ComponentModel.DataAnnotations;
using VacationRequestApi.Models;

namespace VacationRequestApi.DTOs
{
    // ══════════════════════════════════════════════════════════════════
    // AUTHENTICATION & REGISTRATION
    // ══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Self-registration - user creates their own account
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email on kohustuslik")]
        [EmailAddress(ErrorMessage = "Vigane email formaat")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parool on kohustuslik")]
        [MinLength(8, ErrorMessage = "Parool peab olema vähemalt 8 tähemärki")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Eesnimi on kohustuslik")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Perekonnanimi on kohustuslik")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Organisatsioon on kohustuslik")]
        public int OrganizationId { get; set; }

        [MaxLength(500)]
        public string? JoinMessage { get; set; }
    }

    /// <summary>
    /// Complete profile after first login (for invited users)
    /// </summary>
    public class CompleteProfileDto
    {
        [Required(ErrorMessage = "Eesnimi on kohustuslik")]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Perekonnanimi on kohustuslik")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Osakond on kohustuslik")]
        [MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Position { get; set; }

        [Required(ErrorMessage = "Tööle asumise kuupäev on kohustuslik")]
        public DateTime HireDate { get; set; }

        [Required(ErrorMessage = "Uus parool on kohustuslik")]
        [MinLength(8, ErrorMessage = "Parool peab olema vähemalt 8 tähemärki")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Admin invites user with temporary password
    /// </summary>
    public class InviteUserDto
    {
        [Required(ErrorMessage = "Email on kohustuslik")]
        [EmailAddress(ErrorMessage = "Vigane email formaat")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Organisatsioon on kohustuslik")]
        public int OrganizationId { get; set; }

        public bool IsAdmin { get; set; } = false;

        [Range(10, 50, ErrorMessage = "Aastane puhkusepäevade arv peab olema vahemikus 10-50")]
        public int AnnualLeaveDays { get; set; } = 25;
    }

    /// <summary>
    /// Change password
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Praegune parool on kohustuslik")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Uus parool on kohustuslik")]
        [MinLength(8, ErrorMessage = "Parool peab olema vähemalt 8 tähemärki")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════
    // ORGANIZATIONS
    // ══════════════════════════════════════════════════════════════════

    public class OrganizationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrganizationCreateDto
    {
        [Required(ErrorMessage = "Nimi on kohustuslik")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [EmailAddress]
        [MaxLength(200)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════
    // JOIN REQUESTS
    // ══════════════════════════════════════════════════════════════════

    public class JoinRequestDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class JoinRequestReviewDto
    {
        [Required]
        public bool Approve { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        // If approved, assign department/position
        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }

        public int? ManagerId { get; set; }

        [Range(10, 50)]
        public int AnnualLeaveDays { get; set; } = 25;
    }
}
