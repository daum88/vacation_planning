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
    public class BlackoutPeriodsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<BlackoutPeriodsController> _logger;

        public BlackoutPeriodsController(VacationRequestContext context, IUserService userService, ILogger<BlackoutPeriodsController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        // GET: api/BlackoutPeriods
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlackoutPeriodDto>>> GetBlackoutPeriods([FromQuery] bool activeOnly = true)
        {
            try
            {
                var query = _context.BlackoutPeriods.AsQueryable();
                if (activeOnly) query = query.Where(b => b.IsActive);

                var periods = await query.OrderBy(b => b.StartDate).ToListAsync();

                return Ok(periods.Select(b => new BlackoutPeriodDto
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
                _logger.LogError(ex, "Error fetching blackout periods");
                return StatusCode(500, new { message = "Viga blokeerimisel." });
            }
        }

        // POST: api/BlackoutPeriods
        [HttpPost]
        public async Task<ActionResult<BlackoutPeriodDto>> CreateBlackoutPeriod([FromBody] BlackoutPeriodCreateDto dto)
        {
            if (!_userService.IsAdmin()) return Forbid();

            try
            {
                if (dto.EndDate < dto.StartDate)
                    return BadRequest(new { message = "Lõppkuupäev ei saa olla enne alguskuupäeva." });

                var period = new BlackoutPeriod
                {
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    StartDate = dto.StartDate.Date,
                    EndDate = dto.EndDate.Date,
                    IsActive = true,
                    CreatedByUserId = _userService.GetCurrentUserId(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.BlackoutPeriods.Add(period);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBlackoutPeriods), new BlackoutPeriodDto
                {
                    Id = period.Id,
                    Name = period.Name,
                    Description = period.Description,
                    StartDate = period.StartDate,
                    EndDate = period.EndDate,
                    IsActive = period.IsActive,
                    CreatedAt = period.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating blackout period");
                return StatusCode(500, new { message = "Viga blokeerimisperioodi loomisel." });
            }
        }

        // DELETE: api/BlackoutPeriods/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlackoutPeriod(int id)
        {
            if (!_userService.IsAdmin()) return Forbid();

            try
            {
                var period = await _context.BlackoutPeriods.FindAsync(id);
                if (period == null) return NotFound();

                _context.BlackoutPeriods.Remove(period);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blackout period {Id}", id);
                return StatusCode(500, new { message = "Viga kustutamisel." });
            }
        }
    }
}
