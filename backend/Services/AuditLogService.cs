using VacationRequestApi.Data;
using VacationRequestApi.Models;
using Microsoft.EntityFrameworkCore;

namespace VacationRequestApi.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            AuditEventType eventType,
            int? userId = null,
            string? userEmail = null,
            string? entityType = null,
            int? entityId = null,
            string? details = null,
            bool success = true,
            string? ipAddress = null,
            string? userAgent = null
        );

        Task<List<AuditLog>> GetLogsAsync(
            int? userId = null,
            AuditEventType? eventType = null,
            DateTime? from = null,
            DateTime? to = null,
            int limit = 100
        );
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            VacationRequestContext context,
            ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(
            AuditEventType eventType,
            int? userId = null,
            string? userEmail = null,
            string? entityType = null,
            int? entityId = null,
            string? details = null,
            bool success = true,
            string? ipAddress = null,
            string? userAgent = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    EventType = eventType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details,
                    Success = success,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Also log to file for critical events
                if (!success || eventType == AuditEventType.LoginFailed || eventType == AuditEventType.Unauthorized)
                {
                    _logger.LogWarning(
                        "AUDIT: {EventType} | User: {UserId}/{Email} | Success: {Success} | IP: {IP} | Details: {Details}",
                        eventType, userId, userEmail, success, ipAddress, details
                    );
                }
            }
            catch (Exception ex)
            {
                // Don't let audit logging break the application
                _logger.LogError(ex, "Failed to write audit log for event {EventType}", eventType);
            }
        }

        public async Task<List<AuditLog>> GetLogsAsync(
            int? userId = null,
            AuditEventType? eventType = null,
            DateTime? from = null,
            DateTime? to = null,
            int limit = 100)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            if (eventType.HasValue)
                query = query.Where(a => a.EventType == eventType.Value);

            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(a => a.CreatedAt <= to.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
