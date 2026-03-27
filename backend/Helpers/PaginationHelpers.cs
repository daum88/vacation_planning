using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Constants;

namespace VacationRequestApi.Helpers
{
    /// <summary>
    /// Pagination parameters
    /// </summary>
    public class PaginationParams
    {
        private int _pageNumber = 1;
        private int _pageSize = AppConstants.DefaultPageSize;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > AppConstants.MaxPageSize ? AppConstants.MaxPageSize : 
                              value < 1 ? AppConstants.DefaultPageSize : value;
        }

        public int Skip => (PageNumber - 1) * PageSize;
    }

    /// <summary>
    /// Extension methods for pagination
    /// </summary>
    public static class PaginationExtensions
    {
        /// <summary>
        /// Apply pagination to an IQueryable
        /// </summary>
        public static async Task<(List<T> items, int totalCount)> PaginateAsync<T>(
            this IQueryable<T> query, 
            PaginationParams pagination,
            CancellationToken cancellationToken = default)
        {
            var totalCount = await query.CountAsync(cancellationToken);
            
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        /// <summary>
        /// Apply pagination and return PagedResponse
        /// </summary>
        public static async Task<Common.PagedResponse<T>> ToPagedResponseAsync<T>(
            this IQueryable<T> query,
            PaginationParams pagination,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await query.PaginateAsync(pagination, cancellationToken);
            
            return Common.PagedResponse<T>.Create(
                items,
                pagination.PageNumber,
                pagination.PageSize,
                totalCount
            );
        }
    }
}
