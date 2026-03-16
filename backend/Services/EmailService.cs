using VacationRequestApi.Data;
using VacationRequestApi.Models;

namespace VacationRequestApi.Services
{
    public interface IEmailService
    {
        Task SendRequestSubmittedEmailAsync(int requestId, string employeeName, string employeeEmail);
        Task SendRequestApprovedEmailAsync(int requestId, string employeeName, string employeeEmail, string? adminComment);
        Task SendRequestRejectedEmailAsync(int requestId, string employeeName, string employeeEmail, string? adminComment);
        Task SendNewRequestNotificationToAdminsAsync(int requestId, string employeeName, DateTime startDate, DateTime endDate);
        Task SendUpcomingVacationReminderAsync(string employeeName, string employeeEmail, DateTime startDate, int daysUntil);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly VacationRequestContext _context;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, VacationRequestContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task SendRequestSubmittedEmailAsync(int requestId, string employeeName, string employeeEmail)
        {
            var subject = "Puhkusetaotlus esitatud";
            var body = $"Tere, {employeeName}! Sinu puhkusetaotlus #{requestId} on edukalt esitatud ja ootab kinnitust.";
            await SendEmailAsync(employeeEmail, subject, body, "Submitted", requestId);
        }

        public async Task SendRequestApprovedEmailAsync(int requestId, string employeeName, string employeeEmail, string? adminComment)
        {
            var subject = "Puhkusetaotlus kinnitatud";
            var body = $"Tere, {employeeName}! Sinu puhkusetaotlus #{requestId} on kinnitatud.{(adminComment != null ? $" Kommentaar: {adminComment}" : "")}";
            await SendEmailAsync(employeeEmail, subject, body, "Approved", requestId);
        }

        public async Task SendRequestRejectedEmailAsync(int requestId, string employeeName, string employeeEmail, string? adminComment)
        {
            var subject = "Puhkusetaotlus tagasi lükatud";
            var body = $"Tere, {employeeName}! Sinu puhkusetaotlus #{requestId} lükati tagasi.{(adminComment != null ? $" Põhjus: {adminComment}" : "")}";
            await SendEmailAsync(employeeEmail, subject, body, "Rejected", requestId);
        }

        public async Task SendNewRequestNotificationToAdminsAsync(int requestId, string employeeName, DateTime startDate, DateTime endDate)
        {
            var subject = "Uus puhkusetaotlus ootab kinnitust";
            var body = $"{employeeName} esitas taotluse #{requestId}: {startDate:dd.MM.yyyy} – {endDate:dd.MM.yyyy}.";
            var adminEmails = _configuration.GetSection("Email:AdminEmails").Get<List<string>>() ?? new List<string>();
            if (!adminEmails.Any()) adminEmails.Add("admin@example.com");
            foreach (var adminEmail in adminEmails)
                await SendEmailAsync(adminEmail, subject, body, "AdminNotification", requestId);
        }

        public async Task SendUpcomingVacationReminderAsync(string employeeName, string employeeEmail, DateTime startDate, int daysUntil)
        {
            var subject = $"Meeldetuletus: Puhkus algab {daysUntil} päeva pärast";
            var body = $"Tere, {employeeName}! Sinu puhkus algab {startDate:dd.MM.yyyy} ({daysUntil} päeva pärast).";
            await SendEmailAsync(employeeEmail, subject, body, "Reminder", null);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body, string type, int? requestId)
        {
            var isMock = _configuration.GetValue<bool>("Email:UseMockEmail", true);
            _logger.LogInformation("[EMAIL:{Type}] To={To} Subject={Subject}", type, toEmail, subject);

            try
            {
                _context.NotificationLogs.Add(new NotificationLog
                {
                    RequestId = requestId,
                    ToEmail = toEmail,
                    Subject = subject,
                    Type = type,
                    IsMock = isMock,
                    SentAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save notification log for {Type}", type);
            }

            await Task.CompletedTask;
        }
    }
}
