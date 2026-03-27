using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class ManagerDelegationsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<ManagerDelegationsController> _logger;

        public ManagerDelegationsController(
            VacationRequestContext context,
            IUserService userService,
            ILogger<ManagerDelegationsController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>GET /api/ManagerDelegations/my — delegations created by the current user</summary>
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<ManagerDelegationDto>>> GetMyDelegations()
        {
            var userId = _userService.GetCurrentUserId();
            var now = DateTime.UtcNow;
            var list = await _context.ManagerDelegations
                .Include(d => d.Manager)
                .Include(d => d.Delegate)
                .Where(d => d.ManagerId == userId && d.IsActive)
                .OrderByDescending(d => d.StartDate)
                .ToListAsync();

            return Ok(list.Select(d => ToDto(d, now)));
        }

        /// <summary>GET /api/ManagerDelegations — admin: all delegations</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ManagerDelegationDto>>> GetAll()
        {
            if (!_userService.IsAdmin()) return Forbid();
            var now = DateTime.UtcNow;
            var list = await _context.ManagerDelegations
                .Include(d => d.Manager)
                .Include(d => d.Delegate)
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.StartDate)
                .ToListAsync();
            return Ok(list.Select(d => ToDto(d, now)));
        }

        /// <summary>POST /api/ManagerDelegations — current user delegates their approvals</summary>
        [HttpPost]
        public async Task<ActionResult<ManagerDelegationDto>> Create([FromBody] ManagerDelegationCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = _userService.GetCurrentUserId();

            if (dto.EndDate < dto.StartDate)
                return BadRequest(new { message = "Lõppkuupäev peab olema alguskuupäevast hiljem." });
            if (dto.DelegateId == userId)
                return BadRequest(new { message = "Sa ei saa ennast asendajaks määrata." });

            var delegate_ = await _context.Users.FindAsync(dto.DelegateId);
            if (delegate_ == null || !delegate_.IsActive)
                return BadRequest(new { message = "Asendajat ei leitud." });

            // Check for overlapping active delegation
            var overlap = await _context.ManagerDelegations.AnyAsync(d =>
                d.ManagerId == userId &&
                d.IsActive &&
                d.StartDate <= dto.EndDate.Date &&
                d.EndDate >= dto.StartDate.Date);
            if (overlap)
                return Conflict(new { message = "Sellel perioodil on juba aktiivne delegeerimine." });

            var delegation = new ManagerDelegation
            {
                ManagerId  = userId,
                DelegateId = dto.DelegateId,
                StartDate  = dto.StartDate.Date,
                EndDate    = dto.EndDate.Date,
                Reason     = dto.Reason?.Trim(),
                IsActive   = true,
                CreatedAt  = DateTime.UtcNow,
            };
            _context.ManagerDelegations.Add(delegation);
            await _context.SaveChangesAsync();

            // Re-load with navigation
            delegation = await _context.ManagerDelegations
                .Include(d => d.Manager)
                .Include(d => d.Delegate)
                .FirstAsync(d => d.Id == delegation.Id);

            _logger.LogInformation("Manager {ManagerId} delegated to {DelegateId} from {Start} to {End}",
                userId, dto.DelegateId, dto.StartDate, dto.EndDate);

            return CreatedAtAction(nameof(GetMyDelegations), ToDto(delegation, DateTime.UtcNow));
        }

        /// <summary>DELETE /api/ManagerDelegations/{id} — cancel a delegation</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var isAdmin = _userService.IsAdmin();

            var d = await _context.ManagerDelegations.FindAsync(id);
            if (d == null) return NotFound(new { message = "Delegeerimist ei leitud." });
            if (d.ManagerId != userId && !isAdmin) return Forbid();

            d.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Delegeerimine tühistatud." });
        }

        /// <summary>Helper: find active delegate for a manager on a given date (used by approval logic)</summary>
        public static async Task<int?> GetActiveDelegateId(VacationRequestContext ctx, int managerId, DateTime date)
        {
            var d = await ctx.ManagerDelegations.FirstOrDefaultAsync(x =>
                x.ManagerId == managerId &&
                x.IsActive &&
                x.StartDate <= date.Date &&
                x.EndDate >= date.Date);
            return d?.DelegateId;
        }

        private static ManagerDelegationDto ToDto(ManagerDelegation d, DateTime now) => new()
        {
            Id                = d.Id,
            ManagerId         = d.ManagerId,
            ManagerName       = d.Manager?.FullName ?? "",
            DelegateId        = d.DelegateId,
            DelegateName      = d.Delegate?.FullName ?? "",
            StartDate         = d.StartDate,
            EndDate           = d.EndDate,
            Reason            = d.Reason,
            IsActive          = d.IsActive,
            IsCurrentlyActive = d.IsActive && d.StartDate <= now.Date && d.EndDate >= now.Date,
        };
    }
}
