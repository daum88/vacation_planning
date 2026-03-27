using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.BackgroundJobs
{
    /// <summary>
    /// Background service for scheduled tasks: reminders, cleanup, maintenance
    /// </summary>
    public class ScheduledJobsService : BackgroundService
    {
        private readonly ILogger<ScheduledJobsService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Job intervals
        private static readonly TimeSpan ReminderInterval = TimeSpan.FromHours(24);
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(7);

        public ScheduledJobsService(
            ILogger<ScheduledJobsService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled jobs service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDailyJobsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in daily scheduled jobs");
                }

                // Wait until next day
                await Task.Delay(ReminderInterval, stoppingToken);
            }
        }

        private async Task RunDailyJobsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running daily scheduled jobs at {Time}", DateTime.UtcNow);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<VacationRequestContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var publicHolidayService = scope.ServiceProvider.GetRequiredService<IPublicHolidayService>();

            await SendUpcomingVacationRemindersAsync(context, emailService, cancellationToken);
            await CleanupOldAuditLogsAsync(context, cancellationToken);
        }

        /// <summary>
        /// Send reminders for vacations starting in 3 days
        /// </summary>
        private async Task SendUpcomingVacationRemindersAsync(
            VacationRequestContext context,
            IEmailService emailService,
            CancellationToken cancellationToken)
        {
            var reminderDate = DateTime.UtcNow.Date.AddDays(3);

            var upcomingVacations = await context.VacationRequests
                .Include(vr => vr.User)
                .Where(vr =>
                    vr.Status == VacationRequestStatus.Approved &&
                    vr.StartDate.Date == reminderDate &&
                    vr.User != null &&
                    vr.User.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var vacation in upcomingVacations)
            {
                try
                {
                    if (vacation.User != null)
                    {
                        await emailService.SendVacationRequestApprovedEmailAsync(
                            vacation.User.Email,
                            vacation.User.FullName,
                            vacation.StartDate,
                            vacation.EndDate,
                            "Meeldetuletus: sinu puhkus algab 3 päeva pärast!");

                        _logger.LogInformation(
                            "Sent vacation reminder to {Email} for {StartDate}",
                            vacation.User.Email,
                            vacation.StartDate.ToShortDateString());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send vacation reminder for request {Id}", vacation.Id);
                }
            }

            _logger.LogInformation("Sent {Count} vacation reminders", upcomingVacations.Count);
        }

        /// <summary>
        /// Remove audit logs older than retention period
        /// </summary>
        private async Task CleanupOldAuditLogsAsync(
            VacationRequestContext context,
            CancellationToken cancellationToken)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-Constants.AppConstants.AuditLogRetentionDays);

            var oldLogs = await context.AuditLogs
                .Where(al => al.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Count > 0)
            {
                context.AuditLogs.RemoveRange(oldLogs);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Cleaned up {Count} old audit log entries older than {Date}",
                    oldLogs.Count,
                    cutoffDate.ToShortDateString());
            }
        }
    }
}
