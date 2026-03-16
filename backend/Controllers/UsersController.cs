using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(VacationRequestContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
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
        public async Task<ActionResult<UserDto>> GetCurrentUser([FromQuery] int userId = 1)
        {
            return await GetUser(userId);
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

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                Department = user.Department,
                Position = user.Position,
                ManagerId = user.ManagerId,
                ManagerName = user.Manager?.FullName,
                IsActive = user.IsActive,
                IsAdmin = user.IsAdmin,
                AnnualLeaveDays = user.AnnualLeaveDays,
                UsedLeaveDays = user.UsedLeaveDays,
                CarryOverDays = user.CarryOverDays,
                RemainingLeaveDays = user.RemainingLeaveDays,
                HireDate = user.HireDate
            };
        }

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
    }
}
