using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;

namespace VacationRequestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VacationRequestsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private const int HardcodedUserId = 1; // Hardcoded user ID as per requirements

        public VacationRequestsController(VacationRequestContext context)
        {
            _context = context;
        }

        // GET: api/VacationRequests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetVacationRequests()
        {
            var requests = await _context.VacationRequests
                .Where(vr => vr.UserId == HardcodedUserId)
                .OrderByDescending(vr => vr.CreatedAt)
                .ToListAsync();

            return Ok(requests.Select(MapToResponseDto));
        }

        // GET: api/VacationRequests/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VacationRequestResponseDto>> GetVacationRequest(int id)
        {
            var vacationRequest = await _context.VacationRequests.FindAsync(id);

            if (vacationRequest == null || vacationRequest.UserId != HardcodedUserId)
            {
                return NotFound();
            }

            return Ok(MapToResponseDto(vacationRequest));
        }

        // POST: api/VacationRequests
        [HttpPost]
        public async Task<ActionResult<VacationRequestResponseDto>> PostVacationRequest(VacationRequestDto dto)
        {
            // Validation: Start date must be before end date
            if (dto.StartDate >= dto.EndDate)
            {
                return BadRequest(new { message = "Alguskuupäev peab olema enne lõppkuupäeva." });
            }

            // Check for overlapping vacation requests
            var hasOverlap = await _context.VacationRequests
                .Where(vr => vr.UserId == HardcodedUserId)
                .AnyAsync(vr =>
                    (dto.StartDate >= vr.StartDate && dto.StartDate < vr.EndDate) ||
                    (dto.EndDate > vr.StartDate && dto.EndDate <= vr.EndDate) ||
                    (dto.StartDate <= vr.StartDate && dto.EndDate >= vr.EndDate)
                );

            if (hasOverlap)
            {
                return BadRequest(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
            }

            var vacationRequest = new VacationRequest
            {
                UserId = HardcodedUserId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Comment = dto.Comment
            };

            _context.VacationRequests.Add(vacationRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVacationRequest), new { id = vacationRequest.Id }, MapToResponseDto(vacationRequest));
        }

        // PUT: api/VacationRequests/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVacationRequest(int id, VacationRequestDto dto)
        {
            var vacationRequest = await _context.VacationRequests.FindAsync(id);

            if (vacationRequest == null || vacationRequest.UserId != HardcodedUserId)
            {
                return NotFound();
            }

            // Validation: Start date must be before end date
            if (dto.StartDate >= dto.EndDate)
            {
                return BadRequest(new { message = "Alguskuupäev peab olema enne lõppkuupäeva." });
            }

            // Check for overlapping vacation requests (excluding current request)
            var hasOverlap = await _context.VacationRequests
                .Where(vr => vr.UserId == HardcodedUserId && vr.Id != id)
                .AnyAsync(vr =>
                    (dto.StartDate >= vr.StartDate && dto.StartDate < vr.EndDate) ||
                    (dto.EndDate > vr.StartDate && dto.EndDate <= vr.EndDate) ||
                    (dto.StartDate <= vr.StartDate && dto.EndDate >= vr.EndDate)
                );

            if (hasOverlap)
            {
                return BadRequest(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
            }

            vacationRequest.StartDate = dto.StartDate;
            vacationRequest.EndDate = dto.EndDate;
            vacationRequest.Comment = dto.Comment;
            vacationRequest.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VacationRequestExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/VacationRequests/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVacationRequest(int id)
        {
            var vacationRequest = await _context.VacationRequests.FindAsync(id);
            if (vacationRequest == null || vacationRequest.UserId != HardcodedUserId)
            {
                return NotFound();
            }

            _context.VacationRequests.Remove(vacationRequest);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VacationRequestExists(int id)
        {
            return _context.VacationRequests.Any(e => e.Id == id);
        }

        private static VacationRequestResponseDto MapToResponseDto(VacationRequest request)
        {
            return new VacationRequestResponseDto
            {
                Id = request.Id,
                UserId = request.UserId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Comment = request.Comment,
                DaysCount = (request.EndDate.Date - request.StartDate.Date).Days,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }
    }
}
