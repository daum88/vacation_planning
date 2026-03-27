using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.Extensions;
using VacationRequestApi.Middleware;
using VacationRequestApi.BackgroundJobs;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════
// SERVICES CONFIGURATION
// ══════════════════════════════════════════════════════════════════

// Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Strongly-typed configuration
builder.Services.AddAppConfiguration(builder.Configuration);

// Application services
builder.Services.AddApplicationServices();

// FluentValidation
builder.Services.AddValidators();

// AutoMapper
builder.Services.AddAutoMapperProfiles();

// Database
builder.Services.AddDbContext<VacationRequestContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddConfiguredCors(builder.Configuration);

// Rate Limiting
builder.Services.AddConfiguredRateLimiting();

// Swagger
builder.Services.AddSwaggerDocumentation();

// Health Checks
builder.Services.AddConfiguredHealthChecks(builder.Configuration);

// Background Jobs
builder.Services.AddHostedService<ScheduledJobsService>();

var app = builder.Build();

// ══════════════════════════════════════════════════════════════════
// DATABASE INITIALIZATION
// ══════════════════════════════════════════════════════════════════

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VacationRequestContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Check schema integrity
    bool schemaOk = true;
    try
    {
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM BlackoutPeriods");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM DepartmentCapacities");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM RequestComments");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM NotificationLogs");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM Organizations");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM JoinRequests");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM AuditLogs");
    }
    catch
    {
        schemaOk = false;
    }

    if (!schemaOk)
    {
        logger.LogWarning("Schema out of date — recreating database with seed data.");
        context.Database.EnsureDeleted();
    }

    context.Database.EnsureCreated();
    logger.LogInformation("✅ Database ready.");
}

// ══════════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE
// ══════════════════════════════════════════════════════════════════

// Development tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Puhkusetaotlused API v1");
        options.RoutePrefix = "swagger";
    });
}

// Custom middleware (order matters!)
app.UseExceptionHandling();       // 1. Global exception handling
app.UseRequestLogging();          // 2. Request/response logging

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Powered-By"] = "ASP.NET Core"; // Remove version info
    await next();
});

// Rate limiting (before routing)
app.UseIpRateLimiting();

// CORS must come before auth (and before https redirect in dev)
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowLocalhostDev");
}
else
{
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
}

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

// ══════════════════════════════════════════════════════════════════
// RUN APPLICATION
// ══════════════════════════════════════════════════════════════════

app.Logger.LogInformation("🚀 Application starting...");
app.Logger.LogInformation("📝 Swagger UI: http://localhost:5000/swagger");
app.Logger.LogInformation("💚 Health Check: http://localhost:5000/health");

app.Run();
