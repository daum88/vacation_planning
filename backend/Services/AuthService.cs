using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VacationRequestApi.Data;
using VacationRequestApi.Models;
using BCrypt.Net;

namespace VacationRequestApi.Services
{
    public interface IAuthService
    {
        Task<(string token, User user)?> AuthenticateAsync(string email, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        string GenerateJwtToken(User user);
        string GenerateTemporaryPassword();
    }

    public class AuthService : IAuthService
    {
        private readonly VacationRequestContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            VacationRequestContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(string token, User user)?> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            
            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent user: {Email}", email);
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for user: {Email}", email);
                return null;
            }

            var token = GenerateJwtToken(user);
            _logger.LogInformation("User {UserId} authenticated successfully", user.Id);
            
            return (token, user);
        }

        public string HashPassword(string password)
        {
            // BCrypt automatically generates and manages salt per-password
            // WorkFactor 12 is a good balance between security and performance
            // Higher = more secure but slower (each +1 doubles the time)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                // BCrypt.Verify handles the salt extraction automatically
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // Invalid hash format or other error
                return false;
            }
        }

        public string GenerateTemporaryPassword()
        {
            // Generate a random 12-character password (easy to type but secure)
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public string GenerateJwtToken(User user)
        {
            var secretKey = _configuration["Jwt:SecretKey"] ?? "your-secret-key-min-32-chars-long-change-in-production";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
                new Claim("Department", user.Department ?? ""),
                new Claim("IsTemporaryPassword", user.IsTemporaryPassword.ToString()),
                new Claim("IsProfileComplete", user.IsProfileComplete.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
