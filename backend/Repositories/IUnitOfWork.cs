namespace VacationRequestApi.Repositories
{
    /// <summary>
    /// Unit of Work pattern - wraps all repositories and provides single SaveChanges
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IVacationRequestRepository VacationRequests { get; }
        IUserRepository Users { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
