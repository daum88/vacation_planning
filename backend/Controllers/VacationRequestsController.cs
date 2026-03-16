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
        private readonly ILogger<VacationRequestsController> _logger;
        private const int HardcodedUserId = 1; // Hardcoded user ID as per requirements
        private const int MaxVacationDays = 90; // Maximum vacation duration in days

        public VacationRequestsController(VacationRequestContext context, ILogger<VacationRequestsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/VacationRequests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetVacationRequests()
        {
            try
            {
                var requests = await _context.VacationRequests
                    .Where(vr => vr.UserId == HardcodedUserId)
                    .OrderByDescending(vr => vr.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} vacation requests for user {UserId}", requests.Count, HardcodedUserId);
                return Ok(requests.Select(MapToResponseDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacation requests for user {UserId}", HardcodedUserId);
                return StatusCode(500, new { message = "Viga andmete laadimisel." });
            }
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
            try
            {
                // Normalize dates to date-only (remove time component)
                var startDate = dto.StartDate.Date;
                var endDate = dto.EndDate.Date;

                // Validation 1: Dates must be valid range (not too old or far in future)
                if (startDate.Year < 2020 || startDate.Year > 2100 || endDate.Year < 2020 || endDate.Year > 2100)
                {
                    return BadRequest(new { message = "Vigane kuupäev." });
                }

                // Validation 2: Start date cannot be in the past
                if (startDate < DateTime.Today)
                {
                    return BadRequest(new { message = "Alguskuupäev ei saa olla minevikus." });
                }

                // Validation 3: Start date must be before or equal to end date (allow single day vacation)
                if (startDate > endDate)
                {
                    return BadRequest(new { message = "Alguskuupäev peab olema enne või võrdne lõppkuupäevaga." });
                }

                // Validation 4: Check maximum duration
                var daysCount = (endDate - startDate).Days + 1; // +1 to include both start and end day
                if (daysCount > MaxVacationDays)
                {
                    return BadRequest(new { message = $"Puhkus ei saa olla pikem kui {MaxVacationDays} päeva." });
                }

                // Validation 5: Check for overlapping vacation requests
                var hasOverlap = await _context.VacationRequests
                    .Where(vr => vr.UserId == HardcodedUserId)
                    .AnyAsync(vr =>
                        (startDate >= vr.StartDate.Date && startDate <= vr.EndDate.Date) ||
                        (endDate >= vr.StartDate.Date && endDate <= vr.EndDate.Date) ||
                        (startDate <= vr.StartDate.Date && endDate >= vr.EndDate.Date)
                    );

                if (hasOverlap)
                {
                    return BadRequest(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
                }

                // Sanitize comment (basic XSS prevention)
                var sanitizedComment = dto.Comment?.Trim();
                if (!string.IsNullOrEmpty(sanitizedComment))
                {
                    // Remove potential script tags and dangerous content
                    sanitizedComment = System.Text.RegularExpressions.Regex.Replace(
                        sanitizedComment, 
                        @"<script[^>]*>.*?</script>|<iframe[^>]*>.*?</iframe>|javascript:|on\w+\s*=", 
                        "", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
                    );
                }

                var vacationRequest = new VacationRequest
                {
                    UserId = HardcodedUserId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Comment = sanitizedComment
                };

                _context.VacationRequests.Add(vacationRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created vacation request {Id} for user {UserId} from {StartDate} to {EndDate}", 
                    vacationRequest.Id, HardcodedUserId, startDate, endDate);

                return CreatedAtAction(nameof(GetVacationRequest), new { id = vacationRequest.Id }, MapToResponseDto(vacationRequest));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vacation request for user {UserId}", HardcodedUserId);
                return StatusCode(500, new { message = "Viga taotluse loomisel." });
            }
        }

        // PUT: api/VacationRequests/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVacationRequest(int id, VacationRequestDto dto)
        {
            try
            {
                var vacationRequest = await _context.VacationRequests.FindAsync(id);

                if (vacationRequest == null || vacationRequest.UserId != HardcodedUserId)
                {
                    return NotFound();
                }

                // Normalize dates to date-only
                var startDate = dto.StartDate.Date;
                var endDate = dto.EndDate.Date;

                // Validation 1: Dates must be valid range
                if (startDate.Year < 2020 || startDate.Year > 2100 || endDate.Year < 2020 || endDate.Year > 2100)
                {
                    return BadRequest(new { message = "Vigane kuupäev." });
                }

                // Validation 2: Start date cannot be in the past (unless updating existing past request)
                if (startDate < DateTime.Today && vacationRequest.StartDate.Date >= DateTime.Today)
                {
                    return BadRequest(new { message = "Alguskuupäev ei saa olla minevikus." });
                }

                // Validation 3: Start date must be before or equal to end date
                if (startDate > endDate)
                {
                    return BadRequest(new { message = "Alguskuupäev peab olema enne või võrdne lõppkuupäevaga." });
                }

                // Validation 4: Check maximum duration
                var daysCount = (endDate - startDate).Days + 1;
                if (daysCount > MaxVacationDays)
                {
                    return BadRequest(new { message = $"Puhkus ei saa olla pikem kui {MaxVacationDays} päeva." });
                }

                // Validation 5: Check for overlapping vacation requests (excluding current request)
                var hasOverlap = await _context.VacationRequests
                    .Where(vr => vr.UserId == HardcodedUserId && vr.Id != id)
                    .AnyAsync(vr =>
                        (startDate >= vr.StartDate.Date && startDate <= vr.EndDate.Date) ||
                        (endDate >= vr.StartDate.Date && endDate <= vr.EndDate.Date) ||
                        (startDate <= vr.StartDate.Date && endDate >= vr.EndDate.Date)
                    );

                if (hasOverlap)
                {
                    return BadRequest(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
                }

                // Sanitize comment
                var sanitizedComment = dto.Comment?.Trim();
                if (!string.IsNullOrEmpty(sanitizedComment))
                {
                    sanitizedComment = System.Text.RegularExpressions.Regex.Replace(
                        sanitizedComment, 
                        @"<script[^>]*>.*?</script>|<iframe[^>]*>.*?</iframe>|javascript:|on\w+\s*=", 
                        "", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
                    );
                }

                vacationRequest.StartDate = startDate;
                vacationRequest.EndDate = endDate;
                vacationRequest.Comment = sanitizedComment;
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated vacation request {Id} for user {UserId}", id, HardcodedUserId);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!VacationRequestExists(id))
                {
                    _logger.LogWarning("Attempted to update non-existent vacation request {Id}", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating vacation request {Id}", id);
                    return StatusCode(409, new { message = "Taotlus on vahepeal muudetud. Palun laadige leht uuesti." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse uuendamisel." });
            }
        }

        // DELETE: api/VacationRequests/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVacationRequest(int id)
        {
            try
            {
                var vacationRequest = await _context.VacationRequests.FindAsync(id);
                if (vacationRequest == null || vacationRequest.UserId != HardcodedUserId)
                {
                    return NotFound();
                }

                _context.VacationRequests.Remove(vacationRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted vacation request {Id} for user {UserId}", id, HardcodedUserId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse kustutamisel." });
            }
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
                DaysCount = (request.EndDate.Date - request.StartDate.Date).Days + 1, // +1 to include both days
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }
    }
}
