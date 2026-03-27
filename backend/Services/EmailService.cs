using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using VacationRequestApi.Configuration;
using Microsoft.Extensions.Options;

namespace VacationRequestApi.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string fullName, string temporaryPassword);
        Task SendPasswordResetEmailAsync(string toEmail, string fullName, string temporaryPassword);
        Task SendJoinRequestSubmittedEmailAsync(string toEmail, string fullName, string organizationName);
        Task SendJoinRequestApprovedEmailAsync(string toEmail, string fullName, string organizationName);
        Task SendJoinRequestRejectedEmailAsync(string toEmail, string fullName, string organizationName, string? reason);
        Task SendVacationRequestSubmittedEmailAsync(string toEmail, string fullName, DateTime startDate, DateTime endDate);
        Task SendVacationRequestApprovedEmailAsync(string toEmail, string fullName, DateTime startDate, DateTime endDate, string? comment);
        Task SendVacationRequestRejectedEmailAsync(string toEmail, string fullName, DateTime startDate, DateTime endDate, string? comment);
        Task SendAdminNotificationEmailAsync(string adminEmail, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly AppSettings _appSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            IOptions<AppSettings> appSettings,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _appSettings = appSettings.Value;
            _logger = logger;
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string fullName, string temporaryPassword)
        {
            var subject = $"Tere tulemast - {_appSettings.ApplicationName}";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Oled kutsutud liituma {_appSettings.CompanyName} puhkusetaotluste süsteemiga.</p>
                <p><strong>Sinu ajutine parool:</strong> <code>{temporaryPassword}</code></p>
                <p>Palun logi sisse ja muuda oma parool:</p>
                <p><a href='{_appSettings.FrontendUrl}/login'>{_appSettings.FrontendUrl}/login</a></p>
                <p>Kui sul on küsimusi, võta meiega ühendust: {_appSettings.SupportEmail}</p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string temporaryPassword)
        {
            var subject = "Parooli lähtestamine";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Sinu parool on lähtestatud administraatori poolt.</p>
                <p><strong>Uus ajutine parool:</strong> <code>{temporaryPassword}</code></p>
                <p>Palun logi sisse ja muuda oma parool:</p>
                <p><a href='{_appSettings.FrontendUrl}/login'>{_appSettings.FrontendUrl}/login</a></p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendJoinRequestSubmittedEmailAsync(string toEmail, string fullName, string organizationName)
        {
            var subject = "Liitumise taotlus esitatud";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Sinu liitumise taotlus organisatsiooniga <strong>{organizationName}</strong> on edukalt esitatud.</p>
                <p>Administraator vaatab sinu taotluse üle ja saadab sulle kinnituse peatselt.</p>
                <p>Kui sul on küsimusi, võta meiega ühendust: {_appSettings.SupportEmail}</p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendJoinRequestApprovedEmailAsync(string toEmail, string fullName, string organizationName)
        {
            var subject = "Liitumise taotlus kinnitatud";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Sinu liitumise taotlus organisatsiooniga <strong>{organizationName}</strong> on kinnitatud! 🎉</p>
                <p>Saad nüüd sisse logida ja alustada puhkusetaotluste esitamist:</p>
                <p><a href='{_appSettings.FrontendUrl}/login'>{_appSettings.FrontendUrl}/login</a></p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendJoinRequestRejectedEmailAsync(string toEmail, string fullName, string organizationName, string? reason)
        {
            var subject = "Liitumise taotlus tagasi lükatud";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Kahjuks on sinu liitumise taotlus organisatsiooniga <strong>{organizationName}</strong> tagasi lükatud.</p>
                {(!string.IsNullOrEmpty(reason) ? $"<p><strong>Põhjus:</strong> {reason}</p>" : "")}
                <p>Kui sul on küsimusi, võta meiega ühendust: {_appSettings.SupportEmail}</p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendVacationRequestSubmittedEmailAsync(string toEmail, string fullName, DateTime startDate, DateTime endDate)
        {
            var subject = "Puhkusetaotlus esitatud";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Sinu puhkusetaotlus on edukalt esitatud.</p>
                <p><strong>Periood:</strong> {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}</p>
                <p>Administraator vaatab sinu taotluse üle peatselt.</p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendVacationRequestApprovedEmailAsync(string toEmail, string fullName, DateTime startDate, DateTime endDate, string? comment)
        {
            var subject = "Puhkusetaotlus kinnitatud";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Sinu puhkusetaotlus on kinnitatud! ✅</p>
                <p><strong>Periood:</strong> {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}</p>
                {(!string.IsNullOrEmpty(comment) ? $"<p><strong>Kommentaar:</strong> {comment}</p>" : "")}
                <p>Head puhkust!</p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendVacationRequestRejectedEmailAsync(string toEmail, string fullName, DateTime startDate, DateTime endDate, string? comment)
        {
            var subject = "Puhkusetaotlus tagasi lükatud";
            var body = $@"
                <h2>Tere {fullName}!</h2>
                <p>Kahjuks on sinu puhkusetaotlus tagasi lükatud.</p>
                <p><strong>Periood:</strong> {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}</p>
                {(!string.IsNullOrEmpty(comment) ? $"<p><strong>Põhjus:</strong> {comment}</p>" : "")}
                <p>Kui sul on küsimusi, võta meiega ühendust.</p>
            ";

            await SendEmailAsync(toEmail, fullName, subject, body);
        }

        public async Task SendAdminNotificationEmailAsync(string adminEmail, string subject, string message)
        {
            var body = $@"
                <h2>Admin teade</h2>
                <p>{message}</p>
            ";

            await SendEmailAsync(adminEmail, "Admin", subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (!_emailSettings.Enabled)
            {
                _logger.LogInformation("Email sending is disabled. Would send: {Subject} to {Email}", subject, toEmail);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, 
                    _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                
                if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername))
                {
                    await smtp.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                }

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully: {Subject} to {Email}", subject, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email: {Subject} to {Email}", subject, toEmail);
                // Don't throw - email failure shouldn't break the application
            }
        }
    }
}
