using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VacationRequestApi.DTOs;
using VacationRequestApi.Extensions;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(
            IAuditLogService auditLogService,
            ILogger<AuditLogsController> logger)
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/AuditLogs - Get audit logs (admin only)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
            [FromQuery] int? userId = null,
            [FromQuery] string? eventType = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                AuditEventType? eventTypeEnum = null;
                if (!string.IsNullOrEmpty(eventType) && Enum.TryParse<AuditEventType>(eventType, out var parsed))
                {
                    eventTypeEnum = parsed;
                }

                var logs = await _auditLogService.GetLogsAsync(
                    userId: userId,
                    eventType: eventTypeEnum,
                    from: from,
                    to: to,
                    limit: Math.Min(limit, 1000) // Max 1000 records
                );

                var dtos = logs.ToDtos().ToList();

                _logger.LogInformation("Retrieved {Count} audit logs", dtos.Count);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new { message = "Viga audit logide laadimisel." });
            }
        }

        /// <summary>
        /// GET: api/AuditLogs/summary - Get audit summary statistics
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult> GetAuditSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            try
            {
                var logs = await _auditLogService.GetLogsAsync(
                    from: from ?? DateTime.UtcNow.AddDays(-30),
                    to: to ?? DateTime.UtcNow,
                    limit: 10000
                );

                var summary = new
                {
                    totalEvents = logs.Count,
                    successfulEvents = logs.Count(l => l.Success),
                    failedEvents = logs.Count(l => !l.Success),
                    eventsByType = logs.GroupBy(l => l.EventType)
                        .Select(g => new { eventType = g.Key.ToString(), count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .ToList(),
                    failedLogins = logs.Count(l => l.EventType == AuditEventType.LoginFailed),
                    unauthorizedAttempts = logs.Count(l => l.EventType == AuditEventType.Unauthorized),
                    recentFailedLogins = logs
                        .Where(l => l.EventType == AuditEventType.LoginFailed)
                        .OrderByDescending(l => l.CreatedAt)
                        .Take(10)
                        .Select(l => new
                        {
                            email = l.UserEmail,
                            ipAddress = l.IpAddress,
                            timestamp = l.CreatedAt
                        })
                        .ToList()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit summary");
                return StatusCode(500, new { message = "Viga audit kokkuvõtte laadimisel." });
            }
        }
    }
}
