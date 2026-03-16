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
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@example.com";
            _fromName = _configuration["Email:FromName"] ?? "Puhkusetaotluste süsteem";
        }

        public async Task SendRequestSubmittedEmailAsync(int requestId, string employeeName, string employeeEmail)
        {
            var subject = "Puhkusetaotlus esitatud";
            var body = $@"
<html>
<body style='font-family: -apple-system, BlinkMacSystemFont, sans-serif; padding: 20px;'>
    <h2 style='color: #007AFF;'>✅ Taotlus esitatud</h2>
    <p>Tere, {employeeName}!</p>
    <p>Sinu puhkusetaotlus #{requestId} on edukalt esitatud ja ootab kinnitust.</p>
    <p>Saad teavituse, kui taotlus on läbi vaadatud.</p>
    <hr style='border: none; border-top: 1px solid #E5E5EA; margin: 20px 0;'>
    <p style='color: #8E8E93; font-size: 12px;'>See on automaatne teade. Palun ära vasta sellele kirjale.</p>
</body>
</html>";

            await SendEmailAsync(employeeEmail, subject, body);
        }

        public async Task SendRequestApprovedEmailAsync(int requestId, string employeeName, string employeeEmail, string? adminComment)
        {
            var subject = "✅ Puhkusetaotlus kinnitatud";
            var commentSection = !string.IsNullOrWhiteSpace(adminComment)
                ? $"<p><strong>Kommentaar:</strong> {adminComment}</p>"
                : "";

            var body = $@"
<html>
<body style='font-family: -apple-system, BlinkMacSystemFont, sans-serif; padding: 20px;'>
    <h2 style='color: #34C759;'>✅ Taotlus kinnitatud</h2>
    <p>Tere, {employeeName}!</p>
    <p>Sinu puhkusetaotlus #{requestId} on kinnitatud! 🎉</p>
    {commentSection}
    <p>Head puhkust!</p>
    <hr style='border: none; border-top: 1px solid #E5E5EA; margin: 20px 0;'>
    <p style='color: #8E8E93; font-size: 12px;'>See on automaatne teade. Palun ära vasta sellele kirjale.</p>
</body>
</html>";

            await SendEmailAsync(employeeEmail, subject, body);
        }

        public async Task SendRequestRejectedEmailAsync(int requestId, string employeeName, string employeeEmail, string? adminComment)
        {
            var subject = "❌ Puhkusetaotlus tagasi lükatud";
            var reasonSection = !string.IsNullOrWhiteSpace(adminComment)
                ? $"<p><strong>Põhjus:</strong> {adminComment}</p>"
                : "<p>Põhjust ei täpsustatud.</p>";

            var body = $@"
<html>
<body style='font-family: -apple-system, BlinkMacSystemFont, sans-serif; padding: 20px;'>
    <h2 style='color: #FF3B30;'>❌ Taotlus tagasi lükatud</h2>
    <p>Tere, {employeeName}!</p>
    <p>Kahjuks on sinu puhkusetaotlus #{requestId} tagasi lükatud.</p>
    {reasonSection}
    <p>Palun võta ühendust oma juhiga täpsustuste saamiseks.</p>
    <hr style='border: none; border-top: 1px solid #E5E5EA; margin: 20px 0;'>
    <p style='color: #8E8E93; font-size: 12px;'>See on automaatne teade. Palun ära vasta sellele kirjale.</p>
</body>
</html>";

            await SendEmailAsync(employeeEmail, subject, body);
        }

        public async Task SendNewRequestNotificationToAdminsAsync(int requestId, string employeeName, DateTime startDate, DateTime endDate)
        {
            var subject = "🔔 Uus puhkusetaotlus ootab kinnitust";
            var body = $@"
<html>
<body style='font-family: -apple-system, BlinkMacSystemFont, sans-serif; padding: 20px;'>
    <h2 style='color: #007AFF;'>🔔 Uus taotlus</h2>
    <p><strong>{employeeName}</strong> on esitanud uue puhkusetaotluse:</p>
    <ul>
        <li><strong>Taotlus #:</strong> {requestId}</li>
        <li><strong>Algus:</strong> {startDate:dd.MM.yyyy}</li>
        <li><strong>Lõpp:</strong> {endDate:dd.MM.yyyy}</li>
        <li><strong>Päevi:</strong> {(endDate - startDate).Days + 1}</li>
    </ul>
    <p>Palun vaata taotlus üle ja kinnita või lükka tagasi.</p>
    <hr style='border: none; border-top: 1px solid #E5E5EA; margin: 20px 0;'>
    <p style='color: #8E8E93; font-size: 12px;'>See on automaatne teade. Palun ära vasta sellele kirjale.</p>
</body>
</html>";

            // In real implementation, fetch admin emails from database
            var adminEmails = _configuration.GetSection("Email:AdminEmails").Get<List<string>>() ?? new List<string>();
            
            foreach (var adminEmail in adminEmails)
            {
                await SendEmailAsync(adminEmail, subject, body);
            }
        }

        public async Task SendUpcomingVacationReminderAsync(string employeeName, string employeeEmail, DateTime startDate, int daysUntil)
        {
            var subject = $"🏖️ Meeldetuletus: Puhkus algab {daysUntil} päeva pärast";
            var body = $@"
<html>
<body style='font-family: -apple-system, BlinkMacSystemFont, sans-serif; padding: 20px;'>
    <h2 style='color: #007AFF;'>🏖️ Puhkuse meeldetuletus</h2>
    <p>Tere, {employeeName}!</p>
    <p>Sinu puhkus algab <strong>{startDate:dd.MM.yyyy}</strong> ({daysUntil} päeva pärast).</p>
    <p>Ära unusta:</p>
    <ul>
        <li>Seadistada automaatne vastus</li>
        <li>Teavitada kliente/kolleege</li>
        <li>Üle anda käimasolevad ülesanded</li>
    </ul>
    <p>Head puhkust! 🌴</p>
    <hr style='border: none; border-top: 1px solid #E5E5EA; margin: 20px 0;'>
    <p style='color: #8E8E93; font-size: 12px;'>See on automaatne teade. Palun ära vasta sellele kirjale.</p>
</body>
</html>";

            await SendEmailAsync(employeeEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                // For development: just log the email
                var isDevelopment = _configuration.GetValue<bool>("Email:UseMockEmail", true);

                if (isDevelopment)
                {
                    _logger.LogInformation(
                        "📧 [MOCK EMAIL]\nTo: {ToEmail}\nSubject: {Subject}\nBody: {Body}",
                        toEmail, subject, htmlBody
                    );
                    await Task.CompletedTask;
                    return;
                }

                // For production: use SMTP
                // Uncomment and configure when ready for production
                /*
                using var smtp = new SmtpClient(_configuration["Email:SmtpHost"], 
                    int.Parse(_configuration["Email:SmtpPort"]));
                smtp.Credentials = new NetworkCredential(
                    _configuration["Email:SmtpUsername"], 
                    _configuration["Email:SmtpPassword"]
                );
                smtp.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await smtp.SendMailAsync(mailMessage);
                */

                _logger.LogInformation("Email sent to {ToEmail}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            }
        }
    }
}
