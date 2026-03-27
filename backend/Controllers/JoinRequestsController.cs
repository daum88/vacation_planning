using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Extensions;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JoinRequestsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserService _userService;
        private readonly ILogger<JoinRequestsController> _logger;

        public JoinRequestsController(
            VacationRequestContext context,
            IAuditLogService auditLogService,
            IUserService userService,
            ILogger<JoinRequestsController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/JoinRequests - Get all join requests (admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JoinRequestDto>>> GetJoinRequests(
            [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.JoinRequests
                    .Include(jr => jr.User)
                    .Include(jr => jr.Organization)
                    .Include(jr => jr.ReviewedBy)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<JoinRequestStatus>(status, out var statusEnum))
                {
                    query = query.Where(jr => jr.Status == statusEnum);
                }

                var requests = await query
                    .OrderByDescending(jr => jr.CreatedAt)
                    .Include(jr => jr.User)
                    .Include(jr => jr.Organization)
                    .Include(jr => jr.ReviewedBy)
                    .ToListAsync();

                var dtos = requests.ToDtos().ToList();

                _logger.LogInformation("Retrieved {Count} join requests", dtos.Count);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving join requests");
                return StatusCode(500, new { message = "Viga liitumise taotluste laadimisel." });
            }
        }

        /// <summary>
        /// GET: api/JoinRequests/my - Get current user's join requests
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<JoinRequestDto>>> GetMyJoinRequests()
        {
            try
            {
                var userId = _userService.GetCurrentUserId();

                var requests = await _context.JoinRequests
                    .Include(jr => jr.Organization)
                    .Include(jr => jr.ReviewedBy)
                    .Include(jr => jr.User)
                    .Where(jr => jr.UserId == userId)
                    .OrderByDescending(jr => jr.CreatedAt)
                    .ToListAsync();

                return Ok(requests.ToDtos());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user's join requests");
                return StatusCode(500, new { message = "Viga liitumise taotluste laadimisel." });
            }
        }

        /// <summary>
        /// GET: api/JoinRequests/5 - Get specific join request
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<JoinRequestDto>> GetJoinRequest(int id)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var request = await _context.JoinRequests
                    .Include(jr => jr.User)
                    .Include(jr => jr.Organization)
                    .Include(jr => jr.ReviewedBy)
                    .Where(jr => jr.Id == id)
                    .FirstOrDefaultAsync();

                if (request == null)
                {
                    return NotFound(new { message = "Liitumise taotlust ei leitud." });
                }

                // Only admin or request owner can view
                if (!isAdmin && request.UserId != userId)
                {
                    return Forbid();
                }

                var dto = new JoinRequestDto
                {
                    Id = request.Id,
                    UserId = request.UserId,
                    UserFullName = request.User.FullName,
                    UserEmail = request.User.Email,
                    OrganizationId = request.OrganizationId,
                    OrganizationName = request.Organization.Name,
                    Message = request.Message,
                    Status = request.Status.ToString(),
                    ReviewedByUserId = request.ReviewedByUserId,
                    ReviewedByName = request.ReviewedBy?.FullName,
                    ReviewedAt = request.ReviewedAt,
                    ReviewNote = request.ReviewNote,
                    CreatedAt = request.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving join request {Id}", id);
                return StatusCode(500, new { message = "Viga liitumise taotluse laadimisel." });
            }
        }

        /// <summary>
        /// POST: api/JoinRequests/5/review - Approve or reject join request (admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/review")]
        public async Task<IActionResult> ReviewJoinRequest(int id, [FromBody] JoinRequestReviewDto dto)
        {
            try
            {
                var reviewerId = _userService.GetCurrentUserId();

                var joinRequest = await _context.JoinRequests
                    .Include(jr => jr.User)
                    .Include(jr => jr.Organization)
                    .FirstOrDefaultAsync(jr => jr.Id == id);

                if (joinRequest == null)
                {
                    return NotFound(new { message = "Liitumise taotlust ei leitud." });
                }

                if (joinRequest.Status != JoinRequestStatus.Pending)
                {
                    return BadRequest(new { message = "Liitumise taotlus on juba vaadatud." });
                }

                // Update join request
                joinRequest.Status = dto.Approve ? JoinRequestStatus.Approved : JoinRequestStatus.Rejected;
                joinRequest.ReviewedByUserId = reviewerId;
                joinRequest.ReviewedAt = DateTime.UtcNow;
                joinRequest.ReviewNote = dto.Note;

                // If approved, activate user and set details
                if (dto.Approve)
                {
                    var user = joinRequest.User;
                    user.IsActive = true;
                    user.IsProfileComplete = true;
                    user.Department = dto.Department ?? "Määramata";
                    user.Position = dto.Position;
                    user.ManagerId = dto.ManagerId;
                    user.AnnualLeaveDays = dto.AnnualLeaveDays;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _auditLogService.LogAsync(
                        AuditEventType.JoinRequestApproved,
                        userId: reviewerId,
                        entityType: "JoinRequest",
                        entityId: joinRequest.Id,
                        details: $"Approved join request for {user.Email} to join {joinRequest.Organization.Name}",
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    _logger.LogInformation("Join request {Id} approved by user {ReviewerId} for user {UserId}",
                        id, reviewerId, user.Id);
                }
                else
                {
                    await _auditLogService.LogAsync(
                        AuditEventType.JoinRequestRejected,
                        userId: reviewerId,
                        entityType: "JoinRequest",
                        entityId: joinRequest.Id,
                        details: $"Rejected join request for {joinRequest.User.Email}",
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                    );

                    _logger.LogInformation("Join request {Id} rejected by user {ReviewerId}",
                        id, reviewerId);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = dto.Approve ? "Liitumise taotlus kinnitatud." : "Liitumise taotlus tagasi lükatud.",
                    status = joinRequest.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing join request {Id}", id);
                return StatusCode(500, new { message = "Viga liitumise taotluse vaatamisel." });
            }
        }

        /// <summary>
        /// DELETE: api/JoinRequests/5 - Cancel own pending join request
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelJoinRequest(int id)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();

                var joinRequest = await _context.JoinRequests
                    .FirstOrDefaultAsync(jr => jr.Id == id && jr.UserId == userId);

                if (joinRequest == null)
                {
                    return NotFound(new { message = "Liitumise taotlust ei leitud." });
                }

                if (joinRequest.Status != JoinRequestStatus.Pending)
                {
                    return BadRequest(new { message = "Ainult ootel taotlusi saab tühistada." });
                }

                _context.JoinRequests.Remove(joinRequest);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.JoinRequestRejected,
                    userId: userId,
                    entityType: "JoinRequest",
                    entityId: id,
                    details: "User cancelled their own join request",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("Join request {Id} cancelled by user {UserId}", id, userId);

                return Ok(new { message = "Liitumise taotlus tühistatud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling join request {Id}", id);
                return StatusCode(500, new { message = "Viga liitumise taotluse tühistamisel." });
            }
        }
    }
}
