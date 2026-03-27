using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.Models;

namespace VacationRequestApi.Repositories
{
    /// <summary>
    /// EF Core implementation of VacationRequest repository
    /// </summary>
    public class VacationRequestRepository : Repository<VacationRequest>, IVacationRequestRepository
    {
        public VacationRequestRepository(VacationRequestContext context) : base(context) { }

        public async Task<List<VacationRequest>> GetByUserIdAsync(int userId, int? year = null)
        {
            var query = _dbSet
                .Include(vr => vr.LeaveType)
                .Include(vr => vr.ApprovedBy)
                .Where(vr => vr.UserId == userId);

            if (year.HasValue)
            {
                query = query.Where(vr => vr.StartDate.Year == year.Value || vr.EndDate.Year == year.Value);
            }

            return await query.OrderByDescending(vr => vr.StartDate).ToListAsync();
        }

        public async Task<List<VacationRequest>> GetPendingAsync()
        {
            return await _dbSet
                .Include(vr => vr.User)
                .Include(vr => vr.LeaveType)
                .Where(vr => vr.Status == VacationRequestStatus.Pending)
                .OrderBy(vr => vr.StartDate)
                .ToListAsync();
        }

        public async Task<List<VacationRequest>> GetOverlappingAsync(
            int userId, DateTime startDate, DateTime endDate, int? excludeId = null)
        {
            var query = _dbSet.Where(vr =>
                vr.UserId == userId &&
                vr.Status != VacationRequestStatus.Rejected &&
                vr.StartDate <= endDate &&
                vr.EndDate >= startDate);

            if (excludeId.HasValue)
            {
                query = query.Where(vr => vr.Id != excludeId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<int> GetUsedDaysAsync(int userId, int year)
        {
            var requests = await _dbSet
                .Where(vr =>
                    vr.UserId == userId &&
                    vr.Status == VacationRequestStatus.Approved &&
                    (vr.StartDate.Year == year || vr.EndDate.Year == year))
                .ToListAsync();

            return requests.Sum(vr => (vr.EndDate.Date - vr.StartDate.Date).Days + 1);
        }
    }

    /// <summary>
    /// EF Core implementation of User repository
    /// </summary>
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(VacationRequestContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
            => await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        public async Task<List<User>> GetAdminsAsync()
            => await _dbSet.Where(u => u.IsAdmin && u.IsActive).ToListAsync();

        public async Task<List<User>> GetByOrganizationAsync(int organizationId)
            => await _dbSet.Where(u => u.OrganizationId == organizationId && u.IsActive).ToListAsync();
    }

    /// <summary>
    /// Unit of Work implementation
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly VacationRequestContext _context;
        private IVacationRequestRepository? _vacationRequests;
        private IUserRepository? _users;

        public UnitOfWork(VacationRequestContext context)
        {
            _context = context;
        }

        public IVacationRequestRepository VacationRequests
            => _vacationRequests ??= new VacationRequestRepository(_context);

        public IUserRepository Users
            => _users ??= new UserRepository(_context);

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
