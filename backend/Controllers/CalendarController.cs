using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<CalendarController> _logger;
        private readonly IPublicHolidayService _publicHolidayService;

        public CalendarController(VacationRequestContext context, ILogger<CalendarController> logger, IPublicHolidayService publicHolidayService)
        {
            _context = context;
            _logger = logger;
            _publicHolidayService = publicHolidayService;
        }

        // GET: api/Calendar/team?startDate=2026-01-01&endDate=2026-12-31
        [HttpGet("team")]
        public async Task<ActionResult<TeamCalendarDto>> GetTeamCalendar(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? department = null)
        {
            try
            {
                var start = startDate ?? DateTime.Now.Date;
                var end = endDate ?? start.AddMonths(3);

                var query = _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .Where(vr => vr.Status == VacationRequestStatus.Approved &&
                                 vr.EndDate >= start &&
                                 vr.StartDate <= end);

                if (!string.IsNullOrWhiteSpace(department))
                {
                    query = query.Where(vr => vr.User != null && vr.User.Department == department);
                }

                var requests = await query.ToListAsync();

                var events = requests.Select(vr => new CalendarEventDto
                {
                    Id = vr.Id,
                    Title = $"{vr.User?.FullName} - {vr.LeaveType?.Name}",
                    Start = vr.StartDate,
                    End = vr.EndDate,
                    Color = vr.LeaveType?.Color ?? "#007AFF",
                    Status = vr.Status.ToString(),
                    UserName = vr.User?.FullName ?? "Unknown",
                    Department = vr.User?.Department ?? "",
                    LeaveType = vr.LeaveType?.Name ?? ""
                }).ToList();

                // Calculate daily absence count
                var dailyAbsenceCount = new Dictionary<DateTime, int>();
                var currentDate = start.Date;
                while (currentDate <= end.Date)
                {
                    var count = requests.Count(vr =>
                        currentDate >= vr.StartDate.Date &&
                        currentDate <= vr.EndDate.Date);
                    dailyAbsenceCount[currentDate] = count;
                    currentDate = currentDate.AddDays(1);
                }

                var calendar = new TeamCalendarDto
                {
                    StartDate = start,
                    EndDate = end,
                    Events = events,
                    DailyAbsenceCount = dailyAbsenceCount
                };

                _logger.LogInformation(
                    "Retrieved team calendar with {EventCount} events from {Start} to {End}",
                    events.Count, start, end
                );

                return Ok(calendar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team calendar");
                return StatusCode(500, new { message = "Viga kalendri laadimisel." });
            }
        }

        // GET: api/Calendar/conflicts?startDate=2026-01-01&endDate=2026-01-10&excludeRequestId=5
        [HttpGet("conflicts")]
        public async Task<ActionResult<object>> CheckConflicts(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? excludeRequestId = null,
            [FromQuery] string? department = null)
        {
            try
            {
                var query = _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .Where(vr =>
                        (vr.Status == VacationRequestStatus.Approved || vr.Status == VacationRequestStatus.Pending) &&
                        vr.StartDate <= endDate &&
                        vr.EndDate >= startDate);

                if (excludeRequestId.HasValue)
                {
                    query = query.Where(vr => vr.Id != excludeRequestId.Value);
                }

                if (!string.IsNullOrWhiteSpace(department))
                {
                    query = query.Where(vr => vr.User != null && vr.User.Department == department);
                }

                var conflicts = await query.ToListAsync();

                var result = new
                {
                    HasConflicts = conflicts.Any(),
                    ConflictCount = conflicts.Count,
                    Conflicts = conflicts.Select(vr => new
                    {
                        RequestId = vr.Id,
                        UserName = vr.User?.FullName,
                        Department = vr.User?.Department,
                        StartDate = vr.StartDate,
                        EndDate = vr.EndDate,
                        Status = vr.Status.ToString(),
                        LeaveType = vr.LeaveType?.Name
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking calendar conflicts");
                return StatusCode(500, new { message = "Viga konfliktide kontrollimisel." });
            }
        }

        // GET: api/Calendar/departments
        [HttpGet("departments")]
        public async Task<ActionResult<IEnumerable<string>>> GetDepartments()
        {
            try
            {
                var departments = await _context.Users
                    .Where(u => u.IsActive)
                    .Select(u => u.Department)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return StatusCode(500, new { message = "Viga osakondade laadimisel." });
            }
        }

        // GET: api/Calendar/holidays?year=2026
        [HttpGet("holidays")]
        public ActionResult<IEnumerable<PublicHolidayDto>> GetPublicHolidays([FromQuery] int? year = null)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var holidays = _publicHolidayService.GetEstonianPublicHolidaysNamed(targetYear);
            return Ok(holidays.Select(h => new PublicHolidayDto { Date = h.Date, Name = h.Name }));
        }

        // GET: api/Calendar/blackouts
        [HttpGet("blackouts")]
        public async Task<ActionResult<IEnumerable<BlackoutPeriodDto>>> GetBlackouts()
        {
            try
            {
                var blackouts = await _context.BlackoutPeriods
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.StartDate)
                    .ToListAsync();

                return Ok(blackouts.Select(b => new BlackoutPeriodDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching blackouts");
                return StatusCode(500, new { message = "Viga laadimsel." });
            }
        }
    }
}
