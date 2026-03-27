using VacationRequestApi.Models;

namespace VacationRequestApi.Services
{
    // Legacy audit service interface - kept for backwards compatibility
    // New code should use IAuditLogService instead
    public interface IAuditService
    {
        Task LogActionAsync(int vacationRequestId, int userId, string action, string? details = null, 
            DateTime? timestamp = null);
        Task<List<object>> GetAuditLogsForRequestAsync(int vacationRequestId);
    }

    // Empty implementation - audit logging handled by AuditLogService
    public class AuditService : IAuditService
    {
        public Task LogActionAsync(int vacationRequestId, int userId, string action, string? details = null, 
            DateTime? timestamp = null)
        {
            // No-op: AuditLogService handles all audit logging now
            return Task.CompletedTask;
        }

        public Task<List<object>> GetAuditLogsForRequestAsync(int vacationRequestId)
        {
            // Return empty list
            return Task.FromResult(new List<object>());
        }
    }
}
