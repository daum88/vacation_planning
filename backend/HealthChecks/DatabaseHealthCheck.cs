using Microsoft.Extensions.Diagnostics.HealthChecks;
using VacationRequestApi.Data;

namespace VacationRequestApi.HealthChecks
{
    /// <summary>
    /// Database connectivity health check
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(VacationRequestContext context, ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to query the database
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                if (canConnect)
                {
                    // Also count users to verify schema is working
                    var userCount = _context.Users.Count();
                    return HealthCheckResult.Healthy($"Database is responsive. Users: {userCount}");
                }

                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database error", ex);
            }
        }
    }
}
