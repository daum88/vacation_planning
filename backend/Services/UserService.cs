using System.Security.Claims;

namespace VacationRequestApi.Services
{
    public interface IUserService
    {
        int GetCurrentUserId();
        bool IsAdmin();
        string? GetCurrentUserEmail();
        string? GetCurrentUserDepartment();
    }

    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User not authenticated");
        }

        public bool IsAdmin()
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Role)?.Value;
            
            return roleClaim == "Admin";
        }

        public string? GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Email)?.Value;
        }

        public string? GetCurrentUserDepartment()
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirst("Department")?.Value;
        }
    }
}
