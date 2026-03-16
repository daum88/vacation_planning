using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IUserService _userService;

        public NotificationsController(VacationRequestContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/Notifications?limit=50
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
    }
}
