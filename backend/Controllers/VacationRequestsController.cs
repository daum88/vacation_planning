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

        // GET: api/VacationRequests/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<VacationStatisticsDto>> GetStatistics()
        {
            try
            {
                var requests = await _context.VacationRequests
                    .Where(vr => vr.UserId == HardcodedUserId)
                    .ToListAsync();

                var today = DateTime.Today;
                var currentYear = DateTime.Now.Year;

                var statistics = new VacationStatisticsDto
                {
                    TotalRequests = requests.Count,
                    TotalDays = requests.Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1),
                    UpcomingRequests = requests.Count(r => r.StartDate.Date > today),
                    PastRequests = requests.Count(r => r.EndDate.Date < today),
                    CurrentYearDays = requests
                        .Where(r => r.StartDate.Year == currentYear || r.EndDate.Year == currentYear)
                        .Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1),
                    NextVacationStart = requests
                        .Where(r => r.StartDate.Date > today)
                        .OrderBy(r => r.StartDate)
                        .Select(r => (DateTime?)r.StartDate)
                        .FirstOrDefault(),
                    MonthlyBreakdown = requests
                        .SelectMany(r =>
                        {
                            var months = new List<(int Year, int Month)>();
                            var current = r.StartDate;
                            while (current <= r.EndDate)
                            {
                                months.Add((current.Year, current.Month));
                                current = current.AddMonths(1);
                                current = new DateTime(current.Year, current.Month, 1);
                            }
                            return months.Distinct().Select(m => new { Year = m.Year, Month = m.Month, Request = r });
                        })
                        .GroupBy(x => new { x.Year, x.Month })
                        .Select(g => new MonthlyStatistic
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("et-EE")),
                            DaysCount = g.Sum(x =>
                            {
                                var monthStart = new DateTime(g.Key.Year, g.Key.Month, 1);
                                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                                var overlapStart = x.Request.StartDate > monthStart ? x.Request.StartDate : monthStart;
                                var overlapEnd = x.Request.EndDate < monthEnd ? x.Request.EndDate : monthEnd;
                                return (overlapEnd.Date - overlapStart.Date).Days + 1;
                            }),
                            RequestsCount = g.Select(x => x.Request.Id).Distinct().Count()
                        })
                        .OrderByDescending(m => m.Year)
                        .ThenByDescending(m => m.Month)
                        .Take(12)
                        .ToList()
                };

                _logger.LogInformation("Retrieved statistics for user {UserId}: {TotalDays} total days", HardcodedUserId, statistics.TotalDays);

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for user {UserId}", HardcodedUserId);
                return StatusCode(500, new { message = "Viga statistika laadimisel." });
            }
        }

        // GET: api/VacationRequests/export/csv
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv()
        {
            try
            {
                var requests = await _context.VacationRequests
                    .Where(vr => vr.UserId == HardcodedUserId)
                    .OrderBy(vr => vr.StartDate)
                    .ToListAsync();

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("ID,Alguskuupäev,Lõppkuupäev,Päevi,Kommentaar,Loodud,Uuendatud");

                foreach (var request in requests)
                {
                    var daysCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
                    csv.AppendLine($"{request.Id},{request.StartDate:yyyy-MM-dd},{request.EndDate:yyyy-MM-dd},{daysCount},\"{request.Comment?.Replace("\"", "\"\"")}\",{request.CreatedAt:yyyy-MM-dd HH:mm:ss},{request.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                var fileName = $"puhkusetaotlused_{DateTime.Now:yyyyMMdd}.csv";

                _logger.LogInformation("Exported {Count} vacation requests to CSV for user {UserId}", requests.Count, HardcodedUserId);

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV for user {UserId}", HardcodedUserId);
                return StatusCode(500, new { message = "Viga CSV eksportimisel." });
            }
        }

        // GET: api/VacationRequests/export/ical
        [HttpGet("export/ical")]
        public async Task<IActionResult> ExportToICal()
        {
            try
            {
                var requests = await _context.VacationRequests
                    .Where(vr => vr.UserId == HardcodedUserId)
                    .OrderBy(vr => vr.StartDate)
                    .ToListAsync();

                var ical = new System.Text.StringBuilder();
                ical.AppendLine("BEGIN:VCALENDAR");
                ical.AppendLine("VERSION:2.0");
                ical.AppendLine("PRODID:-//Puhkusetaotluste süsteem//ET");
                ical.AppendLine("CALSCALE:GREGORIAN");
                ical.AppendLine("METHOD:PUBLISH");
                ical.AppendLine("X-WR-CALNAME:Puhkused");
                ical.AppendLine("X-WR-TIMEZONE:Europe/Tallinn");

                foreach (var request in requests)
                {
                    var daysCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
                    var summary = $"Puhkus ({daysCount} {(daysCount == 1 ? "päev" : "päeva")})";
                    var description = !string.IsNullOrEmpty(request.Comment) ? request.Comment.Replace("\n", "\\n") : "";

                    ical.AppendLine("BEGIN:VEVENT");
                    ical.AppendLine($"UID:{request.Id}@puhkused.ee");
                    ical.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
                    ical.AppendLine($"DTSTART;VALUE=DATE:{request.StartDate:yyyyMMdd}");
                    ical.AppendLine($"DTEND;VALUE=DATE:{request.EndDate.AddDays(1):yyyyMMdd}");
                    ical.AppendLine($"SUMMARY:{summary}");
                    if (!string.IsNullOrEmpty(description))
                    {
                        ical.AppendLine($"DESCRIPTION:{description}");
                    }
                    ical.AppendLine("STATUS:CONFIRMED");
                    ical.AppendLine("TRANSP:OPAQUE");
                    ical.AppendLine("END:VEVENT");
                }

                ical.AppendLine("END:VCALENDAR");

                var bytes = System.Text.Encoding.UTF8.GetBytes(ical.ToString());
                var fileName = $"puhkused_{DateTime.Now:yyyyMMdd}.ics";

                _logger.LogInformation("Exported {Count} vacation requests to iCal for user {UserId}", requests.Count, HardcodedUserId);

                return File(bytes, "text/calendar", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to iCal for user {UserId}", HardcodedUserId);
                return StatusCode(500, new { message = "Viga iCal eksportimisel." });
            }
        }

        // ADMIN ENDPOINTS

        // GET: api/VacationRequests/admin/all
        [HttpGet("admin/all")]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetAllVacationRequestsAdmin()
        {
            try
            {
                var requests = await _context.VacationRequests
                    .OrderByDescending(vr => vr.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Admin retrieved {Count} vacation requests", requests.Count);
                return Ok(requests.Select(MapToResponseDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all vacation requests for admin");
                return StatusCode(500, new { message = "Viga andmete laadimisel." });
            }
        }

        // GET: api/VacationRequests/admin/pending
        [HttpGet("admin/pending")]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetPendingVacationRequestsAdmin()
        {
            try
            {
                var requests = await _context.VacationRequests
                    .Where(vr => vr.Status == VacationRequestStatus.Pending)
                    .OrderBy(vr => vr.StartDate)
                    .ToListAsync();

                _logger.LogInformation("Admin retrieved {Count} pending vacation requests", requests.Count);
                return Ok(requests.Select(MapToResponseDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending vacation requests for admin");
                return StatusCode(500, new { message = "Viga andmete laadimisel." });
            }
        }

        // POST: api/VacationRequests/admin/approve/{id}
        [HttpPost("admin/approve/{id}")]
        public async Task<IActionResult> ApproveVacationRequest(int id, [FromBody] ApprovalDto dto)
        {
            try
            {
                var vacationRequest = await _context.VacationRequests.FindAsync(id);

                if (vacationRequest == null)
                {
                    return NotFound(new { message = "Taotlust ei leitud." });
                }

                if (vacationRequest.Status != VacationRequestStatus.Pending)
                {
                    return BadRequest(new { message = "Ainult ootel taotlusi saab kinnitada või tagasi lükata." });
                }

                vacationRequest.Status = dto.Approved ? VacationRequestStatus.Approved : VacationRequestStatus.Rejected;
                vacationRequest.ApprovedByUserId = 999; // Admin user ID (hardcoded)
                vacationRequest.ApprovedAt = DateTime.UtcNow;
                vacationRequest.AdminComment = dto.AdminComment?.Trim();
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var statusText = dto.Approved ? "kinnitatud" : "tagasi lükatud";
                _logger.LogInformation("Vacation request {Id} was {Status} by admin", id, statusText);

                return Ok(MapToResponseDto(vacationRequest));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving/rejecting vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse kinnitamisel." });
            }
        }

        // DELETE: api/VacationRequests/admin/{id}
        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> DeleteVacationRequestAdmin(int id)
        {
            try
            {
                var vacationRequest = await _context.VacationRequests.FindAsync(id);
                if (vacationRequest == null)
                {
                    return NotFound();
                }

                _context.VacationRequests.Remove(vacationRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Admin deleted vacation request {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vacation request {Id} (admin)", id);
                return StatusCode(500, new { message = "Viga taotluse kustutamisel." });
            }
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
                Status = request.Status.ToString(),
                ApprovedByUserId = request.ApprovedByUserId,
                ApprovedAt = request.ApprovedAt,
                AdminComment = request.AdminComment,
                DaysCount = (request.EndDate.Date - request.StartDate.Date).Days + 1, // +1 to include both days
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }
    }
}

