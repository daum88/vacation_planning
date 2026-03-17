using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// Register custom services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IPublicHolidayService, PublicHolidayService>();

// Configure SQLite database
builder.Services.AddDbContext<VacationRequestContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS configuration - Allow all for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize database — auto-recreate if schema is outdated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VacationRequestContext>();
    var logger  = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Check whether all required tables exist. If any are missing (stale schema),
    // drop and recreate the whole DB so EnsureCreated seeds fresh data.
    bool schemaOk = true;
    try
    {
        // Quick probe: query each new table — throws if the table doesn't exist
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM BlackoutPeriods");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM DepartmentCapacities");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM RequestComments");
        context.Database.ExecuteSqlRaw("SELECT COUNT(*) FROM NotificationLogs");
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
    logger.LogInformation("Database ready.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
