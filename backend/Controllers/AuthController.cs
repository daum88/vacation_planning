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
    public class AuthController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            VacationRequestContext context,
            IAuthService authService,
            IAuditLogService auditLogService,
            IUserService userService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _authService = authService;
            _auditLogService = auditLogService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// POST: api/Auth/login - Login with email and password
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Email ja parool on kohustuslikud." });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _authService.AuthenticateAsync(request.Email, request.Password);
                
                if (result == null)
                {
                    await _auditLogService.LogAsync(
                        AuditEventType.LoginFailed,
                        userEmail: request.Email,
                        details: "Invalid credentials",
                        success: false,
                        ipAddress: ipAddress
                    );

                    return Unauthorized(new { message = "Vale email või parool." });
                }

                var (token, user) = result.Value;

                await _auditLogService.LogAsync(
                    AuditEventType.Login,
                    userId: user.Id,
                    userEmail: user.Email,
                    details: $"Successful login",
                    ipAddress: ipAddress
                );

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsAdmin = user.IsAdmin,
                    Department = user.Department,
                    IsTemporaryPassword = user.IsTemporaryPassword,
                    IsProfileComplete = user.IsProfileComplete
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new { message = "Serveri viga sisselogimisel." });
            }
        }

        /// <summary>
        /// POST: api/Auth/change-password - Change password for authenticated user
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                // Verify current password
                if (!_authService.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                {
                    await _auditLogService.LogAsync(
                        AuditEventType.PasswordChanged,
                        userId: userId,
                        userEmail: user.Email,
                        details: "Failed password change - incorrect current password",
                        success: false,
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    return BadRequest(new { message = "Praegune parool on vale." });
                }

                // Update password
                user.PasswordHash = _authService.HashPassword(dto.NewPassword);
                user.IsTemporaryPassword = false;
                user.LastPasswordChangeAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.PasswordChanged,
                    userId: userId,
                    userEmail: user.Email,
                    details: "Password changed successfully",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("User {UserId} changed their password", userId);

                return Ok(new { message = "Parool edukalt muudetud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user");
                return StatusCode(500, new { message = "Viga parooli muutmisel." });
            }
        }

        /// <summary>
        /// POST: api/Auth/logout - Logout (for audit logging)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var userEmail = _userService.GetCurrentUserEmail();

                await _auditLogService.LogAsync(
                    AuditEventType.Logout,
                    userId: userId,
                    userEmail: userEmail,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("User {UserId} logged out", userId);

                return Ok(new { message = "Välja logitud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Viga väljalogimisel." });
            }
        }
    }
}
