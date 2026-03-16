namespace VacationRequestApi.Services
{
    public interface IUserService
    {
        int GetCurrentUserId();
        bool IsAdmin();
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
            // Check query parameter for user role simulation
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var userIdParam = context.Request.Query["userId"].FirstOrDefault();
                if (!string.IsNullOrEmpty(userIdParam) && int.TryParse(userIdParam, out int userId))
                {
                    return userId;
                }
            }

            // Default employee user
            return 1;
        }

        public bool IsAdmin()
        {
            // Check query parameter for admin role simulation
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var roleParam = context.Request.Query["role"].FirstOrDefault();
                return roleParam?.ToLower() == "admin";
            }

            return false;
        }
    }
}
