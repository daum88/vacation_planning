using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Extensions;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;

        public UsersController(
            VacationRequestContext context, 
            ILogger<UsersController> logger,
            IUserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Manager)
                    .AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(u => u.IsActive);
                }

                var users = await query
                    .OrderBy(u => u.Department)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                var userDtos = users.Select(MapToDto).ToList();

                _logger.LogInformation("Retrieved {Count} users", userDtos.Count);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "Viga kasutajate laadimisel." });
            }
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Manager)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { message = "Viga kasutaja laadimisel." });
            }
        }

        // GET: api/Users/current
        [HttpGet("current")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                return await GetUser(userId);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Kasutaja pole autenditud." });
            }
        }

        // GET: api/Users/5/balance
        [HttpGet("{id}/balance")]
        public async Task<ActionResult<UserBalanceDto>> GetUserBalance(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                var currentYear = DateTime.Now.Year;
                var requests = await _context.VacationRequests
                    .Include(vr => vr.LeaveType)
                    .Where(vr => vr.UserId == id && vr.StartDate.Year == currentYear)
                    .ToListAsync();

                var pendingDays = requests
                    .Where(r => r.Status == VacationRequestStatus.Pending)
                    .Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1);

                var approvedDays = requests
                    .Where(r => r.Status == VacationRequestStatus.Approved)
                    .Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1);

                var balance = new UserBalanceDto
                {
                    AnnualLeaveDays = user.AnnualLeaveDays,
                    UsedLeaveDays = user.UsedLeaveDays,
                    CarryOverDays = user.CarryOverDays,
                    RemainingLeaveDays = user.RemainingLeaveDays,
                    PendingDays = pendingDays,
                    ApprovedDays = approvedDays
                };

                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving balance for user {UserId}", id);
                return StatusCode(500, new { message = "Viga saldo laadimisel." });
            }
        }

        // MapToDto replaced by MappingExtensions.ToDto() — kept as forwarding shim
        private static UserDto MapToDto(User user) => user.ToDto();

        // PUT: api/Users/5/carryover
        [HttpPut("{id}/carryover")]
        public async Task<IActionResult> UpdateCarryOver(int id, [FromBody] CarryOverUpdateDto dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();

                user.CarryOverDays = Math.Max(0, dto.CarryOverDays);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(MapToDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating carry-over for user {Id}", id);
                return StatusCode(500, new { message = "Viga ülekandemise uuendamisel." });
            }
        }

        // POST: api/Users/annual-reset
        [HttpPost("annual-reset")]
        public async Task<ActionResult<AnnualResetResultDto>> AnnualReset([FromQuery] int maxCarryOverDays = 5)
        {
            try
            {
                maxCarryOverDays = Math.Max(0, Math.Min(30, maxCarryOverDays));
                var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
                var details = new List<AnnualResetUserDto>();

                foreach (var user in users)
                {
                    var remaining = user.RemainingLeaveDays;
                    var newCarry = Math.Min(remaining, maxCarryOverDays);

                    details.Add(new AnnualResetUserDto
                    {
                        UserId = user.Id,
                        UserName = user.FullName,
                        PreviousUsedDays = user.UsedLeaveDays,
                        PreviousCarryOver = user.CarryOverDays,
                        NewCarryOver = newCarry
                    });

                    user.UsedLeaveDays = 0;
                    user.CarryOverDays = newCarry;
                    user.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Annual reset performed for {Count} users, maxCarryOver={Max}", users.Count, maxCarryOverDays);

                return Ok(new AnnualResetResultDto
                {
                    UsersReset = users.Count,
                    Year = DateTime.Now.Year,
                    MaxCarryOverDays = maxCarryOverDays,
                    Details = details
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing annual reset");
                return StatusCode(500, new { message = "Viga aastase lähtestamise tegemisel." });
            }
        }
    }
}
