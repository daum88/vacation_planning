using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(
            VacationRequestContext context,
            IAuthService authService,
            IAuditLogService auditLogService,
            ILogger<RegistrationController> logger)
        {
            _context = context;
            _authService = authService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Self-registration: user creates account and requests to join organization
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    return BadRequest(new { message = "Email on juba registreeritud." });
                }

                // Check if organization exists
                var organization = await _context.Organizations.FindAsync(dto.OrganizationId);
                if (organization == null)
                {
                    return NotFound(new { message = "Organisatsiooni ei leitud." });
                }

                // Create user (inactive until join request approved)
                var user = new User
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PasswordHash = _authService.HashPassword(dto.Password),
                    OrganizationId = dto.OrganizationId,
                    Department = "Määramata",
                    IsActive = false, // Inactive until approved
                    IsProfileComplete = false,
                    IsTemporaryPassword = false,
                    HireDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create join request
                var joinRequest = new JoinRequest
                {
                    UserId = user.Id,
                    OrganizationId = dto.OrganizationId,
                    Message = dto.JoinMessage ?? "Soovin liituda teie organisatsiooniga.",
                    Status = JoinRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.JoinRequests.Add(joinRequest);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.Register,
                    userId: user.Id,
                    userEmail: user.Email,
                    entityType: "JoinRequest",
                    entityId: joinRequest.Id,
                    details: $"Registered and requested to join {organization.Name}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("User {Email} registered and requested to join organization {OrgId}",
                    user.Email, dto.OrganizationId);

                return Ok(new
                {
                    message = "Registreerimine õnnestus! Ootame administraatori kinnitust.",
                    userId = user.Id,
                    joinRequestId = joinRequest.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", dto.Email);
                return StatusCode(500, new { message = "Viga registreerimisel." });
            }
        }

        /// <summary>
        /// Complete profile after first login (for invited users with temporary password)
        /// </summary>
        [Authorize]
        [HttpPost("complete-profile")]
        public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return NotFound(new { message = "Kasutajat ei leitud." });

                if (!user.IsTemporaryPassword)
                    return BadRequest(new { message = "Profiil on juba täidetud." });

                // Update profile
                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.Department = dto.Department;
                user.Position = dto.Position;
                user.HireDate = dto.HireDate;
                user.PasswordHash = _authService.HashPassword(dto.NewPassword);
                user.IsTemporaryPassword = false;
                user.IsProfileComplete = true;
                user.LastPasswordChangeAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.ProfileCompleted,
                    userId: user.Id,
                    userEmail: user.Email,
                    details: "Profile completed and password changed"
                );

                _logger.LogInformation("User {UserId} completed their profile", userId);

                // Generate new token with updated claims
                var newToken = _authService.GenerateJwtToken(user);

                return Ok(new
                {
                    message = "Profiil edukalt täidetud!",
                    token = newToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing profile");
                return StatusCode(500, new { message = "Viga profiili täitmisel." });
            }
        }
    }
}
