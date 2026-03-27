using VacationRequestApi.Models;

namespace VacationRequestApi.Repositories
{
    /// <summary>
    /// Vacation request specific repository operations
    /// </summary>
    public interface IVacationRequestRepository : IRepository<VacationRequest>
    {
        Task<List<VacationRequest>> GetByUserIdAsync(int userId, int? year = null);
        Task<List<VacationRequest>> GetPendingAsync();
        Task<List<VacationRequest>> GetOverlappingAsync(int userId, DateTime startDate, DateTime endDate, int? excludeId = null);
        Task<int> GetUsedDaysAsync(int userId, int year);
    }

    /// <summary>
    /// User specific repository operations
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<List<User>> GetAdminsAsync();
        Task<List<User>> GetByOrganizationAsync(int organizationId);
    }
}
