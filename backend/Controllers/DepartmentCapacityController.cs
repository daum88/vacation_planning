using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentCapacityController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<DepartmentCapacityController> _logger;

        public DepartmentCapacityController(VacationRequestContext context, ILogger<DepartmentCapacityController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/DepartmentCapacity
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentCapacityDto>>> GetAll()
        {
            var caps = await _context.DepartmentCapacities
                .OrderBy(d => d.Department)
                .ToListAsync();
            return Ok(caps.Select(MapToDto));
        }

        // GET: api/DepartmentCapacity/check?department=IT&startDate=...&endDate=...
        [HttpGet("check")]
        public async Task<ActionResult<DepartmentCapacityCheckDto>> Check(
            [FromQuery] string department,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int? excludeUserId = null)
        {
            var cap = await _context.DepartmentCapacities
                .FirstOrDefaultAsync(d => d.Department == department && d.IsActive);

            if (cap == null)
                return Ok(new DepartmentCapacityCheckDto { HasLimit = false, Department = department });

            var query = _context.VacationRequests
                .Include(r => r.User)
                .Where(r => r.User != null
                    && r.User.Department == department
                    && r.Status == VacationRequestStatus.Approved
                    && r.StartDate <= endDate.Date
                    && r.EndDate >= startDate.Date);

            if (excludeUserId.HasValue)
                query = query.Where(r => r.UserId != excludeUserId.Value);

            var currentCount = await query.CountAsync();

            return Ok(new DepartmentCapacityCheckDto
            {
                HasLimit = true,
                MaxConcurrent = cap.MaxConcurrent,
                CurrentCount = currentCount,
                WouldExceed = currentCount >= cap.MaxConcurrent,
                Department = department
            });
        }

        // POST: api/DepartmentCapacity
        [HttpPost]
        public async Task<ActionResult<DepartmentCapacityDto>> Create([FromBody] DepartmentCapacityCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Department))
                return BadRequest(new { message = "Osakond on kohustuslik." });

            var existing = await _context.DepartmentCapacities
                .FirstOrDefaultAsync(d => d.Department == dto.Department);

            if (existing != null)
            {
                existing.MaxConcurrent = Math.Max(1, dto.MaxConcurrent);
                existing.IsActive = true;
                await _context.SaveChangesAsync();
                return Ok(MapToDto(existing));
            }

            var cap = new DepartmentCapacity
            {
                Department = dto.Department.Trim(),
                MaxConcurrent = Math.Max(1, dto.MaxConcurrent),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.DepartmentCapacities.Add(cap);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created capacity limit for {Dept}: max {Max}", cap.Department, cap.MaxConcurrent);
            return CreatedAtAction(nameof(GetAll), MapToDto(cap));
        }

        // PUT: api/DepartmentCapacity/5
        [HttpPut("{id}")]
        public async Task<ActionResult<DepartmentCapacityDto>> Update(int id, [FromBody] DepartmentCapacityCreateDto dto)
        {
            var cap = await _context.DepartmentCapacities.FindAsync(id);
            if (cap == null) return NotFound();

            cap.Department = dto.Department.Trim();
            cap.MaxConcurrent = Math.Max(1, dto.MaxConcurrent);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(cap));
        }

        // DELETE: api/DepartmentCapacity/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cap = await _context.DepartmentCapacities.FindAsync(id);
            if (cap == null) return NotFound();

            _context.DepartmentCapacities.Remove(cap);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted capacity limit for {Dept}", cap.Department);
            return NoContent();
        }

        private static DepartmentCapacityDto MapToDto(DepartmentCapacity c) => new DepartmentCapacityDto
        {
            Id = c.Id,
            Department = c.Department,
            MaxConcurrent = c.MaxConcurrent,
            IsActive = c.IsActive
        };
    }
}
