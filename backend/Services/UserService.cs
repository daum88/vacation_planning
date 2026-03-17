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
        private readonly Data.VacationRequestContext _context;

        public UserService(IHttpContextAccessor httpContextAccessor, Data.VacationRequestContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public int GetCurrentUserId()
        {
            var param = _httpContextAccessor.HttpContext?.Request.Query["userId"].FirstOrDefault();
            return (!string.IsNullOrEmpty(param) && int.TryParse(param, out int id)) ? id : 1;
        }

        public bool IsAdmin()
        {
            var userId = GetCurrentUserId();
            return _context.Users.Find(userId)?.IsAdmin ?? false;
        }
    }
}
