using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.Models;

namespace VacationRequestApi.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(int vacationRequestId, int userId, AuditAction action, string? details = null, 
            object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null);
        Task<List<AuditLog>> GetAuditLogsForRequestAsync(int vacationRequestId);
    }

    public class AuditService : IAuditService
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(VacationRequestContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(int vacationRequestId, int userId, AuditAction action, 
            string? details = null, object? oldValues = null, object? newValues = null, 
            string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    VacationRequestId = vacationRequestId,
                    UserId = userId,
                    Action = action,
                    Details = details,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Audit log created: User {UserId} performed {Action} on Request {RequestId}",
                    userId, action, vacationRequestId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log for request {RequestId}", vacationRequestId);
                // Don't throw - audit failure shouldn't break the main operation
            }
        }

        public async Task<List<AuditLog>> GetAuditLogsForRequestAsync(int vacationRequestId)
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.VacationRequestId == vacationRequestId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }
    }
}
