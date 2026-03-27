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
    public class LeaveTypesController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<LeaveTypesController> _logger;

        public LeaveTypesController(
            VacationRequestContext context,
            IUserService userService,
            ILogger<LeaveTypesController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        // GET: api/LeaveTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetLeaveTypes(
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.LeaveTypes.AsQueryable();
                if (!includeInactive) query = query.Where(lt => lt.IsActive);

                var leaveTypes = await query
                    .OrderBy(lt => lt.DisplayOrder)
                    .ThenBy(lt => lt.Name)
                    .ToListAsync();

                return Ok(leaveTypes.Select(ToDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave types");
                return StatusCode(500, new { message = "Viga puhkusetüüpide laadimisel." });
            }
        }

        // GET: api/LeaveTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveTypeDto>> GetLeaveType(int id)
        {
            var lt = await _context.LeaveTypes.FindAsync(id);
            if (lt == null) return NotFound(new { message = "Puhkusetüüpi ei leitud." });
            return Ok(ToDto(lt));
        }

        // POST: api/LeaveTypes  (admin only)
        [HttpPost]
        public async Task<ActionResult<LeaveTypeDto>> CreateLeaveType([FromBody] LeaveTypeCreateUpdateDto dto)
        {
            if (!_userService.IsAdmin()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var lt = new LeaveType
                {
                    Name               = dto.Name.Trim(),
                    Description        = dto.Description?.Trim(),
                    Color              = dto.Color,
                    RequiresApproval   = dto.RequiresApproval,
                    RequiresAttachment = dto.RequiresAttachment,
                    MaxDaysPerYear     = dto.MaxDaysPerYear,
                    AdvanceNoticeDays  = dto.AdvanceNoticeDays,
                    IsPaid             = dto.IsPaid,
                    IsActive           = dto.IsActive,
                    DisplayOrder       = dto.DisplayOrder,
                    CreatedAt          = DateTime.UtcNow,
                    UpdatedAt          = DateTime.UtcNow,
                };

                _context.LeaveTypes.Add(lt);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Leave type {Id} '{Name}' created", lt.Id, lt.Name);
                return CreatedAtAction(nameof(GetLeaveType), new { id = lt.Id }, ToDto(lt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave type");
                return StatusCode(500, new { message = "Viga puhkusetüübi loomisel." });
            }
        }

        // PUT: api/LeaveTypes/5  (admin only)
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveTypeDto>> UpdateLeaveType(
            int id, [FromBody] LeaveTypeCreateUpdateDto dto)
        {
            if (!_userService.IsAdmin()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var lt = await _context.LeaveTypes.FindAsync(id);
                if (lt == null) return NotFound(new { message = "Puhkusetüüpi ei leitud." });

                lt.Name               = dto.Name.Trim();
                lt.Description        = dto.Description?.Trim();
                lt.Color              = dto.Color;
                lt.RequiresApproval   = dto.RequiresApproval;
                lt.RequiresAttachment = dto.RequiresAttachment;
                lt.MaxDaysPerYear     = dto.MaxDaysPerYear;
                lt.AdvanceNoticeDays  = dto.AdvanceNoticeDays;
                lt.IsPaid             = dto.IsPaid;
                lt.IsActive           = dto.IsActive;
                lt.DisplayOrder       = dto.DisplayOrder;
                lt.UpdatedAt          = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Leave type {Id} '{Name}' updated", lt.Id, lt.Name);
                return Ok(ToDto(lt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating leave type {Id}", id);
                return StatusCode(500, new { message = "Viga puhkusetüübi uuendamisel." });
            }
        }

        // DELETE: api/LeaveTypes/5  (admin only — soft delete via IsActive=false)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLeaveType(int id)
        {
            if (!_userService.IsAdmin()) return Forbid();

            try
            {
                var lt = await _context.LeaveTypes.FindAsync(id);
                if (lt == null) return NotFound(new { message = "Puhkusetüüpi ei leitud." });

                var inUse = await _context.VacationRequests.AnyAsync(vr => vr.LeaveTypeId == id);
                if (inUse)
                {
                    // Soft-delete: keep for history
                    lt.IsActive  = false;
                    lt.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Puhkusetüüp deaktiveeritud (kasutusel olevatelt taotlustelt ei kustutata)." });
                }

                _context.LeaveTypes.Remove(lt);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Leave type {Id} '{Name}' deleted", id, lt.Name);
                return Ok(new { message = "Puhkusetüüp kustutatud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting leave type {Id}", id);
                return StatusCode(500, new { message = "Viga puhkusetüübi kustutamisel." });
            }
        }

        private static LeaveTypeDto ToDto(LeaveType lt) => new()
        {
            Id                 = lt.Id,
            Name               = lt.Name,
            Description        = lt.Description,
            Color              = lt.Color,
            RequiresApproval   = lt.RequiresApproval,
            RequiresAttachment = lt.RequiresAttachment,
            MaxDaysPerYear     = lt.MaxDaysPerYear,
            AdvanceNoticeDays  = lt.AdvanceNoticeDays,
            IsPaid             = lt.IsPaid,
            IsActive           = lt.IsActive,
            DisplayOrder       = lt.DisplayOrder,
        };
    }
}
