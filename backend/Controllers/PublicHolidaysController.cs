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
    public class PublicHolidaysController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<PublicHolidaysController> _logger;

        public PublicHolidaysController(
            VacationRequestContext context,
            IUserService userService,
            ILogger<PublicHolidaysController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>GET api/PublicHolidays?year=2026 — all holidays for a year (recurring + year-specific)</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PublicHolidayDto>>> GetHolidays([FromQuery] int? year = null)
        {
            try
            {
                var targetYear = year ?? DateTime.UtcNow.Year;
                var holidays = await _context.PublicHolidays
                    .Where(h => h.IsRecurring || h.Year == targetYear)
                    .OrderBy(h => h.IsRecurring ? h.Date.Month * 100 + h.Date.Day : h.Date.Month * 100 + h.Date.Day)
                    .ToListAsync();

                // For recurring entries, substitute the target year into the date
                var dtos = holidays.Select(h => new PublicHolidayDto
                {
                    Id          = h.Id,
                    Date        = h.IsRecurring ? new DateTime(targetYear, h.Date.Month, h.Date.Day) : h.Date,
                    Name        = h.Name,
                    IsRecurring = h.IsRecurring,
                    Year        = h.Year,
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching public holidays");
                return StatusCode(500, new { message = "Viga riigipühade laadimisel." });
            }
        }

        /// <summary>GET api/PublicHolidays/range?start=2026-01-01&end=2026-12-31</summary>
        [HttpGet("range")]
        public async Task<ActionResult<IEnumerable<PublicHolidayDto>>> GetHolidaysInRange(
            [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (end < start) return BadRequest(new { message = "Lõppkuupäev peab olema alguskuupäevast hiljem." });
            try
            {
                var result = new List<PublicHolidayDto>();
                for (var y = start.Year; y <= end.Year; y++)
                {
                    var holidays = await _context.PublicHolidays
                        .Where(h => h.IsRecurring || h.Year == y)
                        .ToListAsync();

                    foreach (var h in holidays)
                    {
                        var date = h.IsRecurring ? new DateTime(y, h.Date.Month, h.Date.Day) : h.Date;
                        if (date >= start.Date && date <= end.Date)
                            result.Add(new PublicHolidayDto { Id = h.Id, Date = date, Name = h.Name, IsRecurring = h.IsRecurring, Year = h.Year });
                    }
                }
                return Ok(result.OrderBy(d => d.Date));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching holidays in range");
                return StatusCode(500, new { message = "Viga riigipühade laadimisel." });
            }
        }

        /// <summary>GET api/PublicHolidays/all — admin: raw DB rows (no year substitution)</summary>
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<PublicHolidayDto>>> GetAllRaw()
        {
            if (!_userService.IsAdmin()) return Forbid();
            var all = await _context.PublicHolidays.OrderBy(h => h.Date).ToListAsync();
            return Ok(all.Select(h => new PublicHolidayDto
            {
                Id = h.Id, Date = h.Date, Name = h.Name, IsRecurring = h.IsRecurring, Year = h.Year
            }));
        }

        [HttpPost]
        public async Task<ActionResult<PublicHolidayDto>> Create([FromBody] PublicHolidayCreateUpdateDto dto)
        {
            if (!_userService.IsAdmin()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var h = new PublicHoliday
                {
                    Date        = dto.Date.Date,
                    Name        = dto.Name.Trim(),
                    IsRecurring = dto.IsRecurring,
                    Year        = dto.IsRecurring ? null : (dto.Year ?? dto.Date.Year),
                    CreatedAt   = DateTime.UtcNow,
                };
                _context.PublicHolidays.Add(h);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Public holiday '{Name}' on {Date} created", h.Name, h.Date);
                return CreatedAtAction(nameof(GetAllRaw), new { }, new PublicHolidayDto
                {
                    Id = h.Id, Date = h.Date, Name = h.Name, IsRecurring = h.IsRecurring, Year = h.Year
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating public holiday");
                return StatusCode(500, new { message = "Viga riigipüha loomisel." });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<PublicHolidayDto>> Update(int id, [FromBody] PublicHolidayCreateUpdateDto dto)
        {
            if (!_userService.IsAdmin()) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var h = await _context.PublicHolidays.FindAsync(id);
            if (h == null) return NotFound(new { message = "Riigipüha ei leitud." });

            h.Date        = dto.Date.Date;
            h.Name        = dto.Name.Trim();
            h.IsRecurring = dto.IsRecurring;
            h.Year        = dto.IsRecurring ? null : (dto.Year ?? dto.Date.Year);
            await _context.SaveChangesAsync();

            return Ok(new PublicHolidayDto { Id = h.Id, Date = h.Date, Name = h.Name, IsRecurring = h.IsRecurring, Year = h.Year });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!_userService.IsAdmin()) return Forbid();
            var h = await _context.PublicHolidays.FindAsync(id);
            if (h == null) return NotFound(new { message = "Riigipüha ei leitud." });
            _context.PublicHolidays.Remove(h);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Kustutatud." });
        }
    }
}
