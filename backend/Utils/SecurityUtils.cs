using System.Net;
using System.Text.RegularExpressions;

namespace VacationRequestApi.Utils
{
    public static class SecurityUtils
    {
        /// <summary>
        /// Sanitizes user input to prevent XSS attacks.
        /// Uses HTML encoding and removes dangerous patterns.
        /// </summary>
        public static string? SanitizeInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            // Trim whitespace
            var sanitized = input.Trim();

            // Limit length to prevent DoS
            if (sanitized.Length > 5000)
            {
                sanitized = sanitized.Substring(0, 5000);
            }

            // HTML encode to prevent XSS
            sanitized = WebUtility.HtmlEncode(sanitized);

            // Remove null bytes
            sanitized = sanitized.Replace("\0", "");

            return sanitized;
        }

        /// <summary>
        /// Validates file path to prevent directory traversal attacks.
        /// </summary>
        public static bool IsValidFilePath(string filePath, string allowedBasePath)
        {
            try
            {
                var fullPath = Path.GetFullPath(filePath);
                var basePath = Path.GetFullPath(allowedBasePath);
                
                // Ensure the file is within the allowed directory
                return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sanitizes filename to prevent path traversal.
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            // Remove any path characters
            fileName = Path.GetFileName(fileName);
            
            // Remove invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            // Remove potentially dangerous characters
            fileName = Regex.Replace(fileName, @"[<>:""/\\|?*]", "");
            
            // Limit length
            if (fileName.Length > 255)
            {
                var extension = Path.GetExtension(fileName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                fileName = nameWithoutExt.Substring(0, 255 - extension.Length) + extension;
            }

            return fileName;
        }

        /// <summary>
        /// Validates email format.
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks password strength (minimum requirements).
        /// </summary>
        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            // At least one uppercase, one lowercase, one digit
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);

            return hasUpper && hasLower && hasDigit;
        }
    }
}
