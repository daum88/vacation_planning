using System.Diagnostics;

namespace VacationRequestApi.Middleware
{
    /// <summary>
    /// Request logging middleware for monitoring and debugging
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString();
            context.Items["RequestId"] = requestId;

            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            _logger.LogInformation(
                "Request {RequestId} started: {Method} {Path} from {IP}",
                requestId,
                request.Method,
                request.Path,
                context.Connection.RemoteIpAddress
            );

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                
                var response = context.Response;
                var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                              response.StatusCode >= 400 ? LogLevel.Warning :
                              LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "Request {RequestId} completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
                    requestId,
                    request.Method,
                    request.Path,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds
                );
            }
        }
    }

    /// <summary>
    /// Extension method to register request logging middleware
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
