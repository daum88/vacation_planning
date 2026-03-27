namespace VacationRequestApi.Constants
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class AppConstants
    {
        // Authentication
        public const string BearerScheme = "Bearer";
        public const int JwtExpirationHours = 8;
        public const int PasswordMinLength = 8;
        public const int PasswordMaxLength = 100;
        
        // Rate Limiting
        public const int MaxLoginAttemptsPerMinute = 5;
        public const int MaxRegistrationAttemptsPerHour = 3;
        public const int MaxApiRequestsPerMinute = 100;
        
        // Pagination
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        
        // Vacation
        public const int MinAnnualLeaveDays = 10;
        public const int MaxAnnualLeaveDays = 50;
        public const int DefaultAnnualLeaveDays = 25;
        
        // Audit
        public const int DefaultAuditLogLimit = 100;
        public const int MaxAuditLogLimit = 1000;
        public const int AuditLogRetentionDays = 365;
        
        // File Upload
        public const int MaxFileSize = 10 * 1024 * 1024; // 10MB
        public static readonly string[] AllowedFileExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
    }

    /// <summary>
    /// User roles
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    /// <summary>
    /// Claim types
    /// </summary>
    public static class ClaimTypes
    {
        public const string UserId = System.Security.Claims.ClaimTypes.NameIdentifier;
        public const string Email = System.Security.Claims.ClaimTypes.Email;
        public const string FullName = System.Security.Claims.ClaimTypes.Name;
        public const string Role = System.Security.Claims.ClaimTypes.Role;
        public const string Department = "Department";
        public const string IsTemporaryPassword = "IsTemporaryPassword";
        public const string IsProfileComplete = "IsProfileComplete";
    }

    /// <summary>
    /// Cache keys
    /// </summary>
    public static class CacheKeys
    {
        public const string Organizations = "organizations:all";
        public const string PublicHolidays = "holidays:{0}"; // {year}
        public const string UserBalance = "user:{0}:balance"; // {userId}
        public const string LeaveTypes = "leavetypes:active";
    }

    /// <summary>
    /// Error messages
    /// </summary>
    public static class ErrorMessages
    {
        // Authentication
        public const string InvalidCredentials = "Vale email või parool.";
        public const string EmailAlreadyExists = "Email on juba registreeritud.";
        public const string UserNotFound = "Kasutajat ei leitud.";
        public const string Unauthorized = "Sul pole õigust seda toimingut teha.";
        public const string TokenExpired = "Sessioon on aegunud. Palun logi uuesti sisse.";
        
        // Validation
        public const string RequiredField = "Väli on kohustuslik.";
        public const string InvalidEmail = "Vigane email formaat.";
        public const string PasswordTooShort = "Parool peab olema vähemalt {0} tähemärki.";
        public const string InvalidDateRange = "Alguskuupäev peab olema enne lõppkuupäeva.";
        
        // Business logic
        public const string InsufficientLeaveDays = "Sul ei ole piisavalt puhkusepäevi.";
        public const string OverlappingRequest = "Sul on juba puhkuse taotlus sellel perioodil.";
        public const string CannotDeleteWithRequests = "Kasutajat ei saa kustutada, kuna tal on puhkuse taotlusi.";
        
        // General
        public const string ServerError = "Serveri viga. Palun proovi hiljem uuesti.";
        public const string NotFound = "Ressurssi ei leitud.";
    }

    /// <summary>
    /// Success messages
    /// </summary>
    public static class SuccessMessages
    {
        public const string LoginSuccess = "Sisselogimine õnnestus.";
        public const string RegistrationSuccess = "Registreerimine õnnestus! Ootame administraatori kinnitust.";
        public const string ProfileCompleted = "Profiil edukalt täidetud!";
        public const string PasswordChanged = "Parool edukalt muudetud.";
        public const string RequestCreated = "Taotlus edukalt loodud.";
        public const string RequestUpdated = "Taotlus edukalt uuendatud.";
        public const string RequestApproved = "Taotlus kinnitatud.";
        public const string RequestRejected = "Taotlus tagasi lükatud.";
    }
}
