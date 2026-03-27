using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminUserManagementController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserService _userService;
        private readonly ILogger<AdminUserManagementController> _logger;

        public AdminUserManagementController(
            VacationRequestContext context,
            IAuthService authService,
            IAuditLogService auditLogService,
            IUserService userService,
            ILogger<AdminUserManagementController> logger)
        {
            _context = context;
            _authService = authService;
            _auditLogService = auditLogService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/AdminUserManagement - List all users in the organisation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.Department,
                        u.Position,
                        u.IsAdmin,
                        u.IsActive,
                        u.IsProfileComplete,
                        u.IsTemporaryPassword,
                        u.RemainingLeaveDays,
                        u.AnnualLeaveDays,
                        u.CarryOverDays,
                        u.HireDate,
                        u.ManagerId,
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return StatusCode(500, new { message = "Viga kasutajate laadimisel." });
            }
        }

        /// <summary>
        /// POST: api/AdminUserManagement/invite - Invite user with temporary password
        /// </summary>
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
        {
            try
            {
                var adminId = _userService.GetCurrentUserId();

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

                // Generate temporary password
                var temporaryPassword = _authService.GenerateTemporaryPassword();

                // Create user
                var user = new User
                {
                    FirstName = "Määramata",
                    LastName = "Määramata",
                    Email = dto.Email,
                    PasswordHash = _authService.HashPassword(temporaryPassword),
                    OrganizationId = dto.OrganizationId,
                    Department = "Määramata",
                    IsActive = true,
                    IsAdmin = dto.IsAdmin,
                    IsTemporaryPassword = true,
                    IsProfileComplete = false,
                    AnnualLeaveDays = dto.AnnualLeaveDays,
                    UsedLeaveDays = 0,
                    CarryOverDays = 0,
                    HireDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.UserCreated,
                    userId: adminId,
                    entityType: "User",
                    entityId: user.Id,
                    details: $"Admin invited user {dto.Email} to organization {organization.Name}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("User {UserId} invited by admin {AdminId} to organization {OrgId}",
                    user.Id, adminId, dto.OrganizationId);

                return Ok(new
                {
                    message = "Kasutaja kutsutud. Saada talle ajutine parool.",
                    userId = user.Id,
                    email = user.Email,
                    temporaryPassword = temporaryPassword,
                    instructions = "Kasutaja peab esimesel sisselogimisel profiili täitma ja parooli muutma."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting user {Email}", dto.Email);
                return StatusCode(500, new { message = "Viga kasutaja kutsumisel." });
            }
        }

        /// <summary>
        /// POST: api/AdminUserManagement/5/reset-password - Reset user password to temporary
        /// </summary>
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(int id)
        {
            try
            {
                var adminId = _userService.GetCurrentUserId();
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                // Generate new temporary password
                var temporaryPassword = _authService.GenerateTemporaryPassword();

                user.PasswordHash = _authService.HashPassword(temporaryPassword);
                user.IsTemporaryPassword = true;
                user.LastPasswordChangeAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.PasswordChanged,
                    userId: adminId,
                    entityType: "User",
                    entityId: user.Id,
                    details: $"Admin reset password for user {user.Email}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("Password reset for user {UserId} by admin {AdminId}", id, adminId);

                return Ok(new
                {
                    message = "Parool lähtestatud. Saada kasutajale uus ajutine parool.",
                    temporaryPassword = temporaryPassword,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {Id}", id);
                return StatusCode(500, new { message = "Viga parooli lähtestamisel." });
            }
        }

        /// <summary>
        /// PUT: api/AdminUserManagement/5/activate - Activate/deactivate user
        /// </summary>
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ToggleUserActivation(int id, [FromBody] bool isActive)
        {
            try
            {
                var adminId = _userService.GetCurrentUserId();
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.UserUpdated,
                    userId: adminId,
                    entityType: "User",
                    entityId: user.Id,
                    details: $"User {user.Email} {(isActive ? "activated" : "deactivated")}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("User {UserId} {Status} by admin {AdminId}",
                    id, isActive ? "activated" : "deactivated", adminId);

                return Ok(new
                {
                    message = isActive ? "Kasutaja aktiveeritud." : "Kasutaja deaktiveeritud.",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user activation for {Id}", id);
                return StatusCode(500, new { message = "Viga kasutaja oleku muutmisel." });
            }
        }

        /// <summary>
        /// PUT: api/AdminUserManagement/5/admin - Toggle admin status
        /// </summary>
        [HttpPut("{id}/admin")]
        public async Task<IActionResult> ToggleAdminStatus(int id, [FromBody] bool isAdmin)
        {
            try
            {
                var adminId = _userService.GetCurrentUserId();
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                // Don't allow removing own admin status
                if (id == adminId && !isAdmin)
                {
                    return BadRequest(new { message = "Sa ei saa enda admini staatust eemaldada." });
                }

                user.IsAdmin = isAdmin;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.UserUpdated,
                    userId: adminId,
                    entityType: "User",
                    entityId: user.Id,
                    details: $"User {user.Email} admin status set to {isAdmin}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("User {UserId} admin status set to {IsAdmin} by admin {AdminId}",
                    id, isAdmin, adminId);

                return Ok(new
                {
                    message = isAdmin ? "Kasutaja määratud adminiks." : "Admini staatus eemaldatud.",
                    isAdmin = user.IsAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling admin status for user {Id}", id);
                return StatusCode(500, new { message = "Viga admini staatuse muutmisel." });
            }
        }

        /// <summary>
        /// DELETE: api/AdminUserManagement/5 - Permanently delete user
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var adminId = _userService.GetCurrentUserId();

                // Don't allow deleting yourself
                if (id == adminId)
                {
                    return BadRequest(new { message = "Sa ei saa ennast kustutada." });
                }

                var user = await _context.Users
                    .Include(u => u.VacationRequests)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                // Check if user has vacation requests
                if (user.VacationRequests.Any())
                {
                    return BadRequest(new
                    {
                        message = "Kasutajat ei saa kustutada, kuna tal on puhkuse taotlusi. Deaktiveeri kasutaja selle asemel."
                    });
                }

                var userEmail = user.Email;
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.UserDeleted,
                    userId: adminId,
                    entityType: "User",
                    entityId: id,
                    details: $"Permanently deleted user {userEmail}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogWarning("User {UserId} ({Email}) permanently deleted by admin {AdminId}",
                    id, userEmail, adminId);

                return Ok(new { message = "Kasutaja kustutatud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, new { message = "Viga kasutaja kustutamisel." });
            }
        }
    }
}
