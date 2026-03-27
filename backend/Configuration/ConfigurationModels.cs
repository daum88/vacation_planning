namespace VacationRequestApi.Configuration
{
    /// <summary>
    /// JWT configuration settings
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";
        
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationHours { get; set; } = 8;
    }

    /// <summary>
    /// CORS configuration settings
    /// </summary>
    public class CorsSettings
    {
        public const string SectionName = "Cors";
        
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Email configuration settings
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "Email";
        
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool Enabled { get; set; } = false; // Disabled by default for development
    }

    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppSettings
    {
        public const string SectionName = "AppSettings";
        
        public string ApplicationName { get; set; } = "Puhkusetaotlused";
        public string CompanyName { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;
        public string FrontendUrl { get; set; } = "http://localhost:3000";
        public bool EnableSwagger { get; set; } = true;
    }

    /// <summary>
    /// File storage configuration
    /// </summary>
    public class FileStorageSettings
    {
        public const string SectionName = "FileStorage";
        
        public string BasePath { get; set; } = "./uploads";
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB
        public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
    }
}
