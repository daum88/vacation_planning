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
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            VacationRequestContext context,
            IUserService userService,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/Notifications — admin-only: email notification log
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationLogDto>>> GetNotifications([FromQuery] int limit = 50)
        {
            if (!_userService.IsAdmin()) return Forbid();

            var logs = await _context.NotificationLogs
                .OrderByDescending(n => n.SentAt)
                .Take(limit)
                .ToListAsync();

            return Ok(logs.Select(n => new NotificationLogDto
            {
                Id = n.Id,
                RequestId = n.RequestId,
                ToEmail = n.ToEmail,
                Subject = n.Subject,
                Type = n.Type,
                IsMock = n.IsMock,
                SentAt = n.SentAt
            }));
        }

        /// <summary>
        /// GET: api/Notifications/my?since=ISO8601
        /// Returns new admin comments on the current user's vacation requests.
        /// Used by the notification bell in the header.
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<UserNotificationsDto>> GetMyNotifications(
            [FromQuery] string? since = null)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();

                DateTime? sinceDate = null;
                if (!string.IsNullOrWhiteSpace(since) && DateTime.TryParse(since, out var parsed))
                    sinceDate = parsed.ToUniversalTime();

                // Fetch new admin comments on this user's requests
                var commentsQuery = _context.RequestComments
                    .Include(c => c.Author)
                    .Include(c => c.VacationRequest)
                    .Where(c => c.VacationRequest.UserId == userId && c.IsAdmin && c.AuthorUserId != userId);

                if (sinceDate.HasValue)
                    commentsQuery = commentsQuery.Where(c => c.CreatedAt > sinceDate.Value);

                var comments = await commentsQuery
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(20)
                    .Select(c => new UserNotificationItemDto
                    {
                        Id         = c.Id,
                        Type       = "comment",
                        Message    = $"{c.Author.FullName}: {(c.Text.Length > 80 ? c.Text.Substring(0, 80) + "…" : c.Text)}",
                        RequestId  = c.VacationRequestId,
                        CreatedAt  = c.CreatedAt,
                    })
                    .ToListAsync();

                // Fetch recent status changes (Approved/Rejected) on this user's requests
                var statusChanges = await _context.VacationRequests
                    .Include(vr => vr.ApprovedBy)
                    .Where(vr =>
                        vr.UserId == userId &&
                        (vr.Status == VacationRequestStatus.Approved || vr.Status == VacationRequestStatus.Rejected) &&
                        vr.ApprovedAt.HasValue &&
                        (!sinceDate.HasValue || vr.ApprovedAt > sinceDate))
                    .OrderByDescending(vr => vr.ApprovedAt)
                    .Take(10)
                    .Select(vr => new UserNotificationItemDto
                    {
                        Id        = vr.Id * 10000, // synthetic unique id
                        Type      = vr.Status == VacationRequestStatus.Approved ? "approved" : "rejected",
                        Message   = vr.Status == VacationRequestStatus.Approved
                            ? $"Taotlus {vr.StartDate:dd.MM}–{vr.EndDate:dd.MM} kinnitati"
                            : $"Taotlus {vr.StartDate:dd.MM}–{vr.EndDate:dd.MM} lükati tagasi",
                        RequestId = vr.Id,
                        CreatedAt = vr.ApprovedAt!.Value,
                    })
                    .ToListAsync();

                var all = comments
                    .Concat(statusChanges)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();

                // ── Manager view: new requests from subordinates ──────────
                var subordinateRequests = await _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .Where(vr =>
                        vr.User != null &&
                        vr.User.ManagerId == userId &&
                        vr.Status == VacationRequestStatus.Pending &&
                        (!sinceDate.HasValue || vr.CreatedAt > sinceDate))
                    .OrderByDescending(vr => vr.CreatedAt)
                    .Take(10)
                    .Select(vr => new UserNotificationItemDto
                    {
                        Id        = vr.Id * 100000 + 1, // synthetic unique id
                        Type      = "pending",
                        Message   = $"{vr.User!.FullName} taotles puhkust {vr.StartDate:dd.MM}–{vr.EndDate:dd.MM}",
                        RequestId = vr.Id,
                        CreatedAt = vr.CreatedAt,
                    })
                    .ToListAsync();

                all = all.Concat(subordinateRequests).OrderByDescending(n => n.CreatedAt).ToList();

                return Ok(new UserNotificationsDto
                {
                    Items       = all,
                    UnreadCount = all.Count,
                    FetchedAt   = DateTime.UtcNow,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user notifications");
                return StatusCode(500, new { message = "Viga teavituste laadimisel." });
            }
        }
    }
}
