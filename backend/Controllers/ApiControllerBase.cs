using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VacationRequestApi.Common;
using VacationRequestApi.Constants;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    /// <summary>
    /// Base controller with shared helpers: current user, Try-Execute, standard responses.
    /// All API controllers should inherit from this.
    /// </summary>
    [ApiController]
    [Authorize]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected readonly IUserService _userService;
        protected readonly ILogger _logger;

        protected ApiControllerBase(IUserService userService, ILogger logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Current user helpers
        // ──────────────────────────────────────────────────────────────────────

        protected int CurrentUserId => _userService.GetCurrentUserId();

        protected bool CurrentUserIsAdmin => _userService.IsAdmin();

        // ──────────────────────────────────────────────────────────────────────
        // Response helpers
        // ──────────────────────────────────────────────────────────────────────

        protected IActionResult Ok<T>(T data, string? message = null)
            => base.Ok(ApiResponse<T>.SuccessResponse(data, message));

        protected IActionResult Created<T>(string routeName, object routeValues, T data, string? message = null)
            => base.CreatedAtRoute(routeName, routeValues, ApiResponse<T>.SuccessResponse(data, message));

        protected IActionResult BadRequest(string message)
            => base.BadRequest(ApiResponse<object>.ErrorResponse(message));

        protected new IActionResult NotFound(string? message = null)
            => base.NotFound(ApiResponse<object>.ErrorResponse(message ?? ErrorMessages.NotFound));

        protected IActionResult Forbidden(string message = ErrorMessages.Unauthorized)
            => base.StatusCode(403, ApiResponse<object>.ErrorResponse(message));

        protected IActionResult ServerError(string message = ErrorMessages.ServerError)
            => base.StatusCode(500, ApiResponse<object>.ErrorResponse(message));

        // ──────────────────────────────────────────────────────────────────────
        // Try-Execute — wraps action in try/catch with consistent logging
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Execute an action, catching and logging any exception.
        /// Returns 500 on unhandled error.
        /// </summary>
        protected async Task<IActionResult> TryExecuteAsync(
            Func<Task<IActionResult>> action,
            string errorMessage = ErrorMessages.ServerError,
            string? operationName = null)
        {
            try
            {
                return await action();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access in {Operation}", operationName ?? "unknown");
                return Forbidden();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found in {Operation}", operationName ?? "unknown");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Operation}: {Message}", operationName ?? "unknown", ex.Message);
                return ServerError(errorMessage);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Require admin — returns 403 Forbidden if caller is not admin
        // ──────────────────────────────────────────────────────────────────────

        protected IActionResult? RequireAdmin()
        {
            if (!CurrentUserIsAdmin) return Forbidden();
            return null; // null = OK to proceed
        }
    }
}
