using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Extensions;
using VacationRequestApi.Models;
using VacationRequestApi.Services;
using VacationRequestApi.Utils;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VacationRequestsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<VacationRequestsController> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUserService _userService;
        private readonly IPublicHolidayService _publicHolidayService;

        public VacationRequestsController(
            VacationRequestContext context,
            ILogger<VacationRequestsController> logger,
            IEmailService emailService,
            IAuditService auditService,
            IFileStorageService fileStorageService,
            IUserService userService,
            IPublicHolidayService publicHolidayService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _auditService = auditService;
            _fileStorageService = fileStorageService;
            _userService = userService;
            _publicHolidayService = publicHolidayService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetVacationRequests(
            [FromQuery] VacationRequestFilterDto? filter = null)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var query = _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .Include(vr => vr.ApprovedBy)
                    .Include(vr => vr.Attachments)
                    .AsQueryable();

                if (!isAdmin)
                {
                    query = query.Where(vr => vr.UserId == userId);
                }

                // Apply filters
                if (filter != null)
                {
                    if (filter.UserId.HasValue && isAdmin)
                    {
                        query = query.Where(vr => vr.UserId == filter.UserId.Value);
                    }

                    if (filter.LeaveTypeId.HasValue)
                    {
                        query = query.Where(vr => vr.LeaveTypeId == filter.LeaveTypeId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Status))
                    {
                        if (Enum.TryParse<VacationRequestStatus>(filter.Status, true, out var status))
                        {
                            query = query.Where(vr => vr.Status == status);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Department) && isAdmin)
                    {
                        query = query.Where(vr => vr.User != null && vr.User.Department == filter.Department);
                    }

                    if (filter.StartDateFrom.HasValue)
                    {
                        query = query.Where(vr => vr.StartDate >= filter.StartDateFrom.Value);
                    }

                    if (filter.StartDateTo.HasValue)
                    {
                        query = query.Where(vr => vr.StartDate <= filter.StartDateTo.Value);
                    }

                    if (filter.EndDateFrom.HasValue)
                    {
                        query = query.Where(vr => vr.EndDate >= filter.EndDateFrom.Value);
                    }

                    if (filter.EndDateTo.HasValue)
                    {
                        query = query.Where(vr => vr.EndDate <= filter.EndDateTo.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.SearchTerm) && isAdmin)
                    {
                        var searchLower = filter.SearchTerm.ToLower();
                        query = query.Where(vr =>
                            (vr.User != null && (vr.User.FirstName.ToLower().Contains(searchLower) ||
                             vr.User.LastName.ToLower().Contains(searchLower) ||
                             vr.User.Email.ToLower().Contains(searchLower))) ||
                            (vr.Comment != null && vr.Comment.ToLower().Contains(searchLower)));
                    }

                    // Sorting
                    query = (filter.SortBy?.ToLower()) switch
                    {
                        "enddate" => filter.SortDescending
                            ? query.OrderByDescending(vr => vr.EndDate)
                            : query.OrderBy(vr => vr.EndDate),
                        "createdat" => filter.SortDescending
                            ? query.OrderByDescending(vr => vr.CreatedAt)
                            : query.OrderBy(vr => vr.CreatedAt),
                        "status" => filter.SortDescending
                            ? query.OrderByDescending(vr => vr.Status)
                            : query.OrderBy(vr => vr.Status),
                        _ => filter.SortDescending
                            ? query.OrderByDescending(vr => vr.StartDate)
                            : query.OrderBy(vr => vr.StartDate)
                    };
                }
                else
                {
                    query = query.OrderByDescending(vr => vr.CreatedAt);
                }

                var requests = await query.ToListAsync();

                var dtos = requests.Select(r => MapToResponseDto(r, userId, isAdmin)).ToList();

                _logger.LogInformation("Retrieved {Count} vacation requests for user {UserId}", dtos.Count, userId);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacation requests");
                return StatusCode(500, new { message = "Viga andmete laadimisel." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VacationRequestResponseDto>> GetVacationRequest(int id)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var vacationRequest = await _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .Include(vr => vr.ApprovedBy)
                    .Include(vr => vr.Attachments)
                    .FirstOrDefaultAsync(vr => vr.Id == id);

                if (vacationRequest == null)
                {
                    return NotFound();
                }

                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                return Ok(MapToResponseDto(vacationRequest, userId, isAdmin));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga andmete laadimisel." });
            }
        }

        /// <summary>GET /api/VacationRequests/{id}/history — change log visible to requester and admins</summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<IEnumerable<RequestHistoryItemDto>>> GetHistory(int id)
        {
            try
            {
                var userId  = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var req = await _context.VacationRequests.FindAsync(id);
                if (req == null) return NotFound();
                if (!isAdmin && req.UserId != userId) return Forbid();

                var items = await _context.RequestHistories
                    .Include(h => h.Actor)
                    .Where(h => h.VacationRequestId == id)
                    .OrderBy(h => h.CreatedAt)
                    .Select(h => new RequestHistoryItemDto
                    {
                        Id          = h.Id,
                        EventType   = h.EventType,
                        Description = h.Description,
                        OldValue    = h.OldValue,
                        NewValue    = h.NewValue,
                        ActorName   = h.Actor != null ? h.Actor.FullName : null,
                        ActorIsAdmin= h.Actor != null && h.Actor.IsAdmin,
                        CreatedAt   = h.CreatedAt,
                    })
                    .ToListAsync();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching history for request {Id}", id);
                return StatusCode(500, new { message = "Viga ajaloo laadimisel." });
            }
        }

        [HttpPost]
        public async Task<ActionResult<VacationRequestResponseDto>> PostVacationRequest(
            [FromBody] VacationRequestCreateDto dto)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Kasutajat ei leitud." });
                }

                var validationError = ValidateDates(dto.StartDate, dto.EndDate, userId);
                if (validationError != null)
                {
                    return BadRequest(new { message = validationError });
                }

                var hasOverlap = await HasOverlappingRequests(userId, dto.StartDate, dto.EndDate);
                if (hasOverlap)
                {
                    return Conflict(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
                }

                // Check department capacity
                var deptCapacity = await _context.DepartmentCapacities
                    .FirstOrDefaultAsync(dc => dc.Department == user.Department && dc.IsActive);
                if (deptCapacity != null)
                {
                    var concurrentCount = await _context.VacationRequests
                        .Include(r => r.User)
                        .CountAsync(r => r.User != null
                            && r.User.Department == user.Department
                            && r.Status == VacationRequestStatus.Approved
                            && r.StartDate <= dto.EndDate.Date
                            && r.EndDate >= dto.StartDate.Date
                            && r.UserId != userId);
                    if (concurrentCount >= deptCapacity.MaxConcurrent)
                    {
                        return Conflict(new
                        {
                            message = $"Osakonna '{user.Department}' puhkuse limiit on täis " +
                                      $"({deptCapacity.MaxConcurrent} inimest korraga). Vali teised kuupäevad.",
                            capacityExceeded = true
                        });
                    }
                }

                var sanitizedComment = SecurityUtils.SanitizeInput(dto.Comment);

                var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
                if (leaveType == null || !leaveType.IsActive)
                {
                    return BadRequest(new { message = "Vale puhkuse tüüp." });
                }

                var daysRequested = _publicHolidayService.CountWorkingDays(dto.StartDate.Date, dto.EndDate.Date);

                // ── Advance notice check ─────────────────────────────────
                if (leaveType.AdvanceNoticeDays > 0)
                {
                    var workingDaysUntilStart = _publicHolidayService.CountWorkingDays(DateTime.UtcNow.Date, dto.StartDate.Date.AddDays(-1));
                    if (workingDaysUntilStart < leaveType.AdvanceNoticeDays)
                    {
                        return BadRequest(new
                        {
                            message = $"'{leaveType.Name}' nõuab vähemalt {leaveType.AdvanceNoticeDays} tööpäevast etteteatamist. " +
                                      $"Varaseim võimalik alguskuupäev on rohkem kui {leaveType.AdvanceNoticeDays} tööpäeva tulevikus."
                        });
                    }
                }

                // ── MaxDaysPerYear check ─────────────────────────────────
                if (leaveType.MaxDaysPerYear > 0)
                {
                    var year = dto.StartDate.Year;
                    var usedThisYear = await _context.VacationRequests
                        .Where(vr => vr.UserId == userId
                            && vr.LeaveTypeId == dto.LeaveTypeId
                            && vr.StartDate.Year == year
                            && (vr.Status == VacationRequestStatus.Pending || vr.Status == VacationRequestStatus.Approved))
                        .ToListAsync();
                    var usedDays = usedThisYear.Sum(vr =>
                        _publicHolidayService.CountWorkingDays(vr.StartDate.Date, vr.EndDate.Date));
                    if (usedDays + daysRequested > leaveType.MaxDaysPerYear)
                    {
                        return BadRequest(new
                        {
                            message = $"'{leaveType.Name}' aastane limiit on {leaveType.MaxDaysPerYear} tööpäeva. " +
                                      $"Oled juba kasutanud {usedDays} päeva, taotlus ületaks limiiti."
                        });
                    }
                }

                if (user.RemainingLeaveDays < daysRequested && leaveType.IsPaid)
                {
                    return BadRequest(new
                    {
                        message = $"Sul ei ole piisavalt puhkusepäevi. Jääk: {user.RemainingLeaveDays} päeva."
                    });
                }

                var vacationRequest = new VacationRequest
                {
                    UserId = userId,
                    LeaveTypeId = dto.LeaveTypeId,
                    StartDate = dto.StartDate.Date,
                    EndDate = dto.EndDate.Date,
                    Comment = sanitizedComment,
                    SubstituteName = dto.SubstituteName?.Trim(),
                    Status = leaveType.RequiresApproval ? VacationRequestStatus.Pending : VacationRequestStatus.Approved,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.VacationRequests.Add(vacationRequest);
                await _context.SaveChangesAsync();
                await WriteHistory(vacationRequest.Id, userId, "created",
                    $"Taotlus esitatud: {vacationRequest.StartDate:dd.MM.yyyy}–{vacationRequest.EndDate:dd.MM.yyyy} ({daysRequested} tööpäeva)");
                await _emailService.SendVacationRequestSubmittedEmailAsync(user.Email, user.FullName, vacationRequest.StartDate, vacationRequest.EndDate);

                if (leaveType.RequiresApproval)
                {
                    // In-app notification: add a comment visible to the manager/admins
                    // so it appears in their notification bell under "my" notifications
                    var managerId = user.ManagerId;
                    if (managerId.HasValue)
                    {
                        var managerComment = new RequestComment
                        {
                            VacationRequestId = vacationRequest.Id,
                            AuthorUserId      = userId,
                            Text              = $"Taotlus esitatud kinnitamiseks: {vacationRequest.StartDate:dd.MM.yyyy}–{vacationRequest.EndDate:dd.MM.yyyy} ({daysRequested} tööpäeva).",
                            IsAdmin           = false,
                            CreatedAt         = DateTime.UtcNow,
                        };
                        _context.RequestComments.Add(managerComment);
                        await _context.SaveChangesAsync();
                    }

                    _logger.LogInformation("New vacation request {Id} submitted by {User}", vacationRequest.Id, user.FullName);
                }

                _logger.LogInformation("Vacation request {Id} created by user {UserId}", vacationRequest.Id, userId);

                vacationRequest = await _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .Include(vr => vr.Attachments)
                    .FirstOrDefaultAsync(vr => vr.Id == vacationRequest.Id);

                return CreatedAtAction(nameof(GetVacationRequest), new { id = vacationRequest!.Id },
                    MapToResponseDto(vacationRequest, userId, false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vacation request");
                return StatusCode(500, new { message = "Viga taotluse loomisel." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutVacationRequest(int id, [FromBody] VacationRequestUpdateDto dto)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var vacationRequest = await _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .FirstOrDefaultAsync(vr => vr.Id == id);

                if (vacationRequest == null)
                {
                    return NotFound();
                }

                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                if (vacationRequest.Status != VacationRequestStatus.Pending && !isAdmin)
                {
                    return BadRequest(new { message = "Ainult ootel taotlusi saab muuta." });
                }

                var oldValues = new
                {
                    vacationRequest.StartDate,
                    vacationRequest.EndDate,
                    vacationRequest.LeaveTypeId,
                    vacationRequest.Comment
                };

                var validationError = ValidateDates(dto.StartDate, dto.EndDate, userId, id);
                if (validationError != null)
                {
                    return BadRequest(new { message = validationError });
                }

                var hasOverlap = await HasOverlappingRequests(userId, dto.StartDate, dto.EndDate, id);
                if (hasOverlap)
                {
                    return Conflict(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
                }

                vacationRequest.LeaveTypeId = dto.LeaveTypeId;
                vacationRequest.StartDate = dto.StartDate.Date;
                vacationRequest.EndDate = dto.EndDate.Date;
                vacationRequest.Comment = SecurityUtils.SanitizeInput(dto.Comment);
                vacationRequest.SubstituteName = dto.SubstituteName?.Trim();
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();

                    // Audit log
                    _logger.LogInformation("Vacation request {Id} updated by user {UserId}", id, userId);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict updating vacation request {Id}", id);
                    return Conflict(new
                    {
                        message = "Taotlus on vahepeal muudetud. Palun laadi leht uuesti ja proovi uuesti."
                    });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse uuendamisel." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVacationRequest(int id)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var vacationRequest = await _context.VacationRequests
                    .Include(vr => vr.Attachments)
                    .FirstOrDefaultAsync(vr => vr.Id == id);

                if (vacationRequest == null)
                {
                    return NotFound();
                }

                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                foreach (var attachment in vacationRequest.Attachments)
                {
                    await _fileStorageService.DeleteFileAsync(attachment.FilePath);
                }

                _context.VacationRequests.Remove(vacationRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Vacation request {Id} deleted by user {UserId}", id, userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse kustutamisel." });
            }
        }

        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> WithdrawVacationRequest(int id)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();

                var vacationRequest = await _context.VacationRequests
                    .Include(vr => vr.User)
                    .FirstOrDefaultAsync(vr => vr.Id == id);

                if (vacationRequest == null)
                {
                    return NotFound();
                }

                if (vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                if (vacationRequest.Status != VacationRequestStatus.Pending &&
                    vacationRequest.Status != VacationRequestStatus.Approved)
                {
                    return BadRequest(new { message = "Seda taotlust ei saa tagasi võtta." });
                }

                var oldStatus = vacationRequest.Status;
                vacationRequest.Status = VacationRequestStatus.Withdrawn;
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Vacation request {Id} withdrawn by user {UserId}", id, userId);

                return Ok(new { message = "Taotlus tagasi võetud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse tagasivõtmisel." });
            }
        }

        [HttpGet("admin/all")]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetAllVacationRequestsAdmin(
            [FromQuery] VacationRequestFilterDto? filter = null)
        {
            // Just call the main GET with admin context
            return await GetVacationRequests(filter);
        }

        [HttpGet("admin/pending")]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetPendingVacationRequestsAdmin()
        {
            var filter = new VacationRequestFilterDto { Status = "Pending", SortBy = "StartDate", SortDescending = false };
            return await GetVacationRequests(filter);
        }

        [HttpPost("admin/approve/{id}")]
        public async Task<IActionResult> ApproveVacationRequest(int id, [FromBody] ApprovalDto dto)
        {
            try
            {
                var adminUserId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                if (!isAdmin)
                {
                    return Forbid();
                }

                var vacationRequest = await _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .FirstOrDefaultAsync(vr => vr.Id == id);

                if (vacationRequest == null)
                {
                    return NotFound(new { message = "Taotlust ei leitud." });
                }

                if (vacationRequest.Status != VacationRequestStatus.Pending)
                {
                    return BadRequest(new { message = "Ainult ootel taotlusi saab kinnitada või tagasi lükata." });
                }

                // Admin cannot approve their own request
                if (vacationRequest.UserId == adminUserId)
                {
                    return BadRequest(new { message = "Admin ei saa oma puhkusetaotlust kinnitada." });
                }

                var oldStatus = vacationRequest.Status;
                vacationRequest.Status = dto.Approved ? VacationRequestStatus.Approved : VacationRequestStatus.Rejected;
                vacationRequest.ApprovedByUserId = adminUserId;
                vacationRequest.ApprovedAt = DateTime.UtcNow;
                vacationRequest.AdminComment = dto.AdminComment?.Trim();
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                if (dto.Approved && vacationRequest.User != null && vacationRequest.LeaveType?.IsPaid == true)
                {
                    var daysCount = _publicHolidayService.CountWorkingDays(
                        vacationRequest.StartDate.Date, vacationRequest.EndDate.Date);
                    vacationRequest.User.UsedLeaveDays += daysCount;
                }

                await _context.SaveChangesAsync();
                var statusLabel = dto.Approved ? "Kinnitatud" : "Tagasi lükatud";
                await WriteHistory(vacationRequest.Id, adminUserId, "status_changed",
                    string.IsNullOrWhiteSpace(dto.AdminComment)
                        ? statusLabel
                        : $"{statusLabel}: {dto.AdminComment}",
                    oldValue: "Ootel", newValue: statusLabel);
                if (vacationRequest.User != null)
                {
                    if (dto.Approved)
                    {
                        await _emailService.SendVacationRequestApprovedEmailAsync(
                            vacationRequest.User.Email, vacationRequest.User.FullName,
                            vacationRequest.StartDate, vacationRequest.EndDate, dto.AdminComment);
                    }
                    else
                    {
                        await _emailService.SendVacationRequestRejectedEmailAsync(
                            vacationRequest.User.Email, vacationRequest.User.FullName,
                            vacationRequest.StartDate, vacationRequest.EndDate, dto.AdminComment);
                    }
                }

                var statusText = dto.Approved ? "kinnitatud" : "tagasi lükatud";
                _logger.LogInformation("Vacation request {Id} was {Status} by admin {AdminId}", id, statusText, adminUserId);

                vacationRequest = await _context.VacationRequests
                    .Include(vr => vr.User)
                    .Include(vr => vr.LeaveType)
                    .Include(vr => vr.ApprovedBy)
                    .Include(vr => vr.Attachments)
                    .FirstOrDefaultAsync(vr => vr.Id == id);

                return Ok(MapToResponseDto(vacationRequest!, adminUserId, true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving/rejecting vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse kinnitamisel." });
            }
        }

        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> DeleteVacationRequestAdmin(int id)
        {
            // Check admin permission
            if (!_userService.IsAdmin())
            {
                return Forbid();
            }

            // Use the same delete logic
            return await DeleteVacationRequest(id);
        }


        [HttpPost("{id}/attachments")]
        public async Task<ActionResult<AttachmentDto>> UploadAttachment(int id, IFormFile file)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var vacationRequest = await _context.VacationRequests.FindAsync(id);
                if (vacationRequest == null)
                {
                    return NotFound(new { message = "Taotlust ei leitud." });
                }

                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                if (!_fileStorageService.IsValidFileType(file.ContentType))
                {
                    return BadRequest(new { message = "Failitüüp ei ole lubatud." });
                }

                if (!_fileStorageService.IsValidFileSize(file.Length))
                {
                    return BadRequest(new { message = "Fail on liiga suur." });
                }

                var filePath = await _fileStorageService.SaveFileAsync(file, id);

                var attachment = new VacationRequestAttachment
                {
                    VacationRequestId = id,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FilePath = filePath,
                    UploadedByUserId = userId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.VacationRequestAttachments.Add(attachment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Attachment {AttachmentId} uploaded for request {RequestId}", attachment.Id, id);

                var dto = new AttachmentDto
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    FileSize = attachment.FileSize,
                    UploadedByUserId = attachment.UploadedByUserId,
                    UploadedAt = attachment.UploadedAt
                };

                return CreatedAtAction(nameof(GetAttachment), new { id, attachmentId = attachment.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment for request {Id}", id);
                return StatusCode(500, new { message = "Viga faili üleslaadimisel." });
            }
        }

        [HttpGet("{id}/attachments/{attachmentId}")]
        public async Task<IActionResult> GetAttachment(int id, int attachmentId)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var attachment = await _context.VacationRequestAttachments
                    .Include(a => a.VacationRequest)
                    .FirstOrDefaultAsync(a => a.Id == attachmentId && a.VacationRequestId == id);

                if (attachment == null)
                {
                    return NotFound();
                }

                if (!isAdmin && attachment.VacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                var (fileBytes, contentType, fileName) = await _fileStorageService.GetFileAsync(attachment.FilePath);

                return File(fileBytes, contentType, attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attachment {AttachmentId}", attachmentId);
                return StatusCode(500, new { message = "Viga faili laadimisel." });
            }
        }

        [HttpDelete("{id}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var attachment = await _context.VacationRequestAttachments
                    .Include(a => a.VacationRequest)
                    .FirstOrDefaultAsync(a => a.Id == attachmentId && a.VacationRequestId == id);

                if (attachment == null)
                {
                    return NotFound();
                }

                if (!isAdmin && attachment.VacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                await _fileStorageService.DeleteFileAsync(attachment.FilePath);

                _context.VacationRequestAttachments.Remove(attachment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Attachment {AttachmentId} deleted from request {RequestId}", attachmentId, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
                return StatusCode(500, new { message = "Viga faili kustutamisel." });
            }
        }


        [HttpGet("statistics")]
        public async Task<ActionResult<VacationStatisticsDto>> GetStatistics()
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var user = await _context.Users.FindAsync(userId);

                var currentYear = DateTime.Now.Year;
                var requests = await _context.VacationRequests
                    .Include(vr => vr.LeaveType)
                    .Where(vr => vr.UserId == userId)
                    .OrderBy(vr => vr.StartDate)
                    .ToListAsync();

                var totalRequests = requests.Count;
                var totalDays = requests.Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1);
                var currentYearDays = requests
                    .Where(r => r.StartDate.Year == currentYear)
                    .Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1);

                var upcomingVacations = requests
                    .Where(r => r.StartDate > DateTime.Now && r.Status == VacationRequestStatus.Approved)
                    .OrderBy(r => r.StartDate)
                    .ToList();

                var upcomingCount = upcomingVacations.Count;
                var nextVacationDate = upcomingVacations.FirstOrDefault()?.StartDate;

                // Monthly breakdown
                var monthlyGroups = requests
                    .SelectMany(r => GetMonthsSpanned(r.StartDate, r.EndDate)
                        .Select(month => new
                        {
                            Year = month.Year,
                            Month = month.Month,
                            Request = r
                        }))
                    .GroupBy(x => new { x.Year, x.Month })
                    .Select(g => new MonthlyBreakdown
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", new CultureInfo("et-EE")),
                        DaysCount = g.Sum(x => CalculateDaysInMonth(x.Request.StartDate, x.Request.EndDate, g.Key.Year, g.Key.Month)),
                        RequestsCount = g.Select(x => x.Request.Id).Distinct().Count()
                    })
                    .OrderBy(m => m.Year)
                    .ThenBy(m => m.Month)
                    .ToList();

                // Leave type usage
                var leaveTypeUsage = requests
                    .GroupBy(r => r.LeaveType)
                    .Select(g => new LeaveTypeUsageDto
                    {
                        LeaveTypeId = g.Key?.Id ?? 0,
                        LeaveTypeName = g.Key?.Name ?? "Unknown",
                        Color = g.Key?.Color ?? "#007AFF",
                        DaysUsed = g.Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1),
                        RequestsCount = g.Count()
                    })
                    .OrderByDescending(x => x.DaysUsed)
                    .ToList();

                // User balance
                var userBalance = user != null ? new UserBalanceDto
                {
                    AnnualLeaveDays = user.AnnualLeaveDays,
                    UsedLeaveDays = user.UsedLeaveDays,
                    CarryOverDays = user.CarryOverDays,
                    RemainingLeaveDays = user.RemainingLeaveDays,
                    PendingDays = requests
                        .Where(r => r.Status == VacationRequestStatus.Pending && r.LeaveType?.IsPaid == true)
                        .Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1),
                    ApprovedDays = requests
                        .Where(r => r.Status == VacationRequestStatus.Approved && r.LeaveType?.IsPaid == true)
                        .Sum(r => (r.EndDate.Date - r.StartDate.Date).Days + 1)
                } : null;

                var statistics = new VacationStatisticsDto
                {
                    TotalRequests = totalRequests,
                    TotalDays = totalDays,
                    CurrentYearDays = currentYearDays,
                    UpcomingVacationsCount = upcomingCount,
                    NextVacationDate = nextVacationDate,
                    MonthlyBreakdown = monthlyGroups,
                    UserBalance = userBalance,
                    LeaveTypeUsage = leaveTypeUsage
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating statistics");
                return StatusCode(500, new { message = "Viga statistika arvutamisel." });
            }
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv()
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var requests = await _context.VacationRequests
                    .Include(vr => vr.LeaveType)
                    .Where(vr => vr.UserId == userId)
                    .OrderBy(vr => vr.StartDate)
                    .ToListAsync();

                var csv = new StringBuilder();
                csv.AppendLine("ID,Alguskuupäev,Lõppkuupäev,Päevi,Tüüp,Staatus,Kommentaar,Loodud");

                foreach (var request in requests)
                {
                    var daysCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
                    csv.AppendLine(
                        $"{request.Id}," +
                        $"{request.StartDate:dd.MM.yyyy}," +
                        $"{request.EndDate:dd.MM.yyyy}," +
                        $"{daysCount}," +
                        $"\"{request.LeaveType?.Name ?? ""}\"," +
                        $"{request.Status}," +
                        $"\"{request.Comment?.Replace("\"", "\"\"")}\"," +
                        $"{request.CreatedAt:dd.MM.yyyy HH:mm}"
                    );
                }

                var fileName = $"puhkusetaotlused_{DateTime.Now:yyyyMMdd}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                var bomBytes = Encoding.UTF8.GetPreamble().Concat(bytes).ToArray();

                _logger.LogInformation("Exported {Count} requests to CSV for user {UserId}", requests.Count, userId);

                return File(bomBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                return StatusCode(500, new { message = "Viga CSV eksportimisel." });
            }
        }

        [HttpGet("export/ical")]
        public async Task<IActionResult> ExportToICal()
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var requests = await _context.VacationRequests
                    .Include(vr => vr.LeaveType)
                    .Where(vr => vr.UserId == userId && vr.Status == VacationRequestStatus.Approved)
                    .OrderBy(vr => vr.StartDate)
                    .ToListAsync();

                var fileName = $"puhkused_{DateTime.Now:yyyyMMdd}.ics";
                var bytes = Encoding.UTF8.GetBytes(BuildVCalendar(requests, "Export"));
                _logger.LogInformation("Exported {Count} requests to iCal for user {UserId}", requests.Count, userId);
                return File(bytes, "text/calendar", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to iCal");
                return StatusCode(500, new { message = "Viga iCal eksportimisel." });
            }
        }

        [HttpPost("admin/bulk-approve")]
        public async Task<ActionResult<BulkApproveResultDto>> BulkApprove([FromBody] List<BulkApproveItemDto> items)
        {
            if (!_userService.IsAdmin()) return Forbid();

            var adminUserId = _userService.GetCurrentUserId();
            var result = new BulkApproveResultDto { Processed = items.Count };

            foreach (var item in items)
            {
                try
                {
                    var vr = await _context.VacationRequests
                        .Include(r => r.User)
                        .Include(r => r.LeaveType)
                        .FirstOrDefaultAsync(r => r.Id == item.Id);

                    if (vr == null) { result.Failed++; result.Errors.Add($"#{item.Id}: ei leitud"); continue; }
                    if (vr.Status != VacationRequestStatus.Pending) { result.Failed++; result.Errors.Add($"#{item.Id}: pole ootel"); continue; }
                    if (vr.UserId == adminUserId) { result.Failed++; result.Errors.Add($"#{item.Id}: oma taotlust ei saa kinnitada"); continue; }

                    vr.Status = item.Approved ? VacationRequestStatus.Approved : VacationRequestStatus.Rejected;
                    vr.ApprovedByUserId = adminUserId;
                    vr.ApprovedAt = DateTime.UtcNow;
                    vr.AdminComment = item.AdminComment?.Trim();
                    vr.UpdatedAt = DateTime.UtcNow;

                    if (item.Approved && vr.User != null && vr.LeaveType?.IsPaid == true)
                    {
                        var days = _publicHolidayService.CountWorkingDays(vr.StartDate.Date, vr.EndDate.Date);
                        vr.User.UsedLeaveDays += days;
                    }

                    await _context.SaveChangesAsync();
                    if (vr.User != null)
                    {
                        if (item.Approved)
                            await _emailService.SendVacationRequestApprovedEmailAsync(vr.User.Email, vr.User.FullName, vr.StartDate, vr.EndDate, item.AdminComment);
                        else
                            await _emailService.SendVacationRequestRejectedEmailAsync(vr.User.Email, vr.User.FullName, vr.StartDate, vr.EndDate, item.AdminComment);
                    }

                    result.Succeeded++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bulk approve error for request {Id}", item.Id);
                    result.Failed++;
                    result.Errors.Add($"#{item.Id}: {ex.Message}");
                }
            }

            return Ok(result);
        }

        [HttpGet("ical/user/{userId}")]
        public async Task<IActionResult> GetICalFeed(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound();

                var requests = await _context.VacationRequests
                    .Include(vr => vr.LeaveType)
                    .Where(vr => vr.UserId == userId && vr.Status == VacationRequestStatus.Approved)
                    .OrderBy(vr => vr.StartDate)
                    .ToListAsync();

                Response.Headers["Content-Disposition"] = $"attachment; filename=\"puhkused-{userId}.ics\"";
                return Content(BuildVCalendar(requests, user.FullName, $"{user.FullName} puhkused"), "text/calendar; charset=utf-8");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating iCal feed for user {UserId}", userId);
                return StatusCode(500, new { message = "Viga iCal voo genereerimisel." });
            }
        }

        [HttpGet("{id}/comments")]
        public async Task<ActionResult<IEnumerable<RequestCommentDto>>> GetComments(int id)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var vr = await _context.VacationRequests.FindAsync(id);
                if (vr == null) return NotFound();
                if (!isAdmin && vr.UserId != userId) return Forbid();

                var comments = await _context.RequestComments
                    .Include(c => c.Author)
                    .Where(c => c.VacationRequestId == id)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(comments.Select(c => new RequestCommentDto
                {
                    Id = c.Id,
                    VacationRequestId = c.VacationRequestId,
                    AuthorUserId = c.AuthorUserId,
                    AuthorName = c.Author?.FullName ?? "Kasutaja",
                    Text = c.Text,
                    IsAdmin = c.IsAdmin,
                    CreatedAt = c.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading comments for request {Id}", id);
                return StatusCode(500, new { message = "Viga kommentaaride laadimisel." });
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<RequestCommentDto>> PostComment(int id, [FromBody] CreateCommentDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Text) || dto.Text.Length > 1000)
                    return BadRequest(new { message = "Kommentaar on kohustuslik ja max 1000 tähemärki." });

                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var vr = await _context.VacationRequests.FindAsync(id);
                if (vr == null) return NotFound();
                if (!isAdmin && vr.UserId != userId) return Forbid();

                var comment = new RequestComment
                {
                    VacationRequestId = id,
                    AuthorUserId = userId,
                    Text = dto.Text.Trim(),
                    IsAdmin = isAdmin,
                    CreatedAt = DateTime.UtcNow
                };

                _context.RequestComments.Add(comment);
                await _context.SaveChangesAsync();

                await _context.Entry(comment).Reference(c => c.Author).LoadAsync();

                return Ok(new RequestCommentDto
                {
                    Id = comment.Id,
                    VacationRequestId = comment.VacationRequestId,
                    AuthorUserId = comment.AuthorUserId,
                    AuthorName = comment.Author?.FullName ?? "Kasutaja",
                    Text = comment.Text,
                    IsAdmin = comment.IsAdmin,
                    CreatedAt = comment.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting comment for request {Id}", id);
                return StatusCode(500, new { message = "Viga kommentaari salvestamisel." });
            }
        }


        private VacationRequestResponseDto MapToResponseDto(VacationRequest request, int currentUserId, bool isAdmin)
        {
            var canEdit     = request.Status == VacationRequestStatus.Pending &&
                              (request.UserId == currentUserId || isAdmin);
            var canDelete   = request.UserId == currentUserId || isAdmin;
            var canWithdraw = request.UserId == currentUserId &&
                              (request.Status == VacationRequestStatus.Pending ||
                               request.Status == VacationRequestStatus.Approved);

            var daysCount = _publicHolidayService.CountWorkingDays(request.StartDate.Date, request.EndDate.Date);

            return request.ToDto(daysCount, canEdit, canDelete, canWithdraw);
        }

        private string? ValidateDates(DateTime startDate, DateTime endDate, int userId, int? excludeRequestId = null)
        {
            startDate = startDate.Date;
            endDate = endDate.Date;

            if (endDate < startDate)
            {
                return "Lõppkuupäev ei saa olla enne alguskuupäeva.";
            }

            var daysCount = (endDate - startDate).Days + 1;
            if (daysCount > 90)
            {
                return "Puhkus ei saa olla pikem kui 90 päeva.";
            }

            // Allow editing past dates for existing requests, but not for new ones
            if (excludeRequestId == null && startDate < DateTime.Today)
            {
                return "Alguskuupäev ei saa olla minevikus.";
            }

            return null;
        }

        private async Task<bool> HasOverlappingRequests(int userId, DateTime startDate, DateTime endDate, int? excludeRequestId = null)
        {
            var query = _context.VacationRequests
                .Where(vr => vr.UserId == userId &&
                            vr.Status != VacationRequestStatus.Rejected &&
                            vr.Status != VacationRequestStatus.Withdrawn &&
                            vr.StartDate <= endDate &&
                            vr.EndDate >= startDate);

            if (excludeRequestId.HasValue)
            {
                query = query.Where(vr => vr.Id != excludeRequestId.Value);
            }

            return await query.AnyAsync();
        }

        private static IEnumerable<DateTime> GetMonthsSpanned(DateTime startDate, DateTime endDate)
        {
            var months = new List<DateTime>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);
            var end = new DateTime(endDate.Year, endDate.Month, 1);

            while (current <= end)
            {
                months.Add(current);
                current = current.AddMonths(1);
            }

            return months;
        }

        private static int CalculateDaysInMonth(DateTime startDate, DateTime endDate, int year, int month)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var rangeStart = startDate > monthStart ? startDate : monthStart;
            var rangeEnd = endDate < monthEnd ? endDate : monthEnd;

            if (rangeStart > rangeEnd)
            {
                return 0;
            }

            return (rangeEnd - rangeStart).Days + 1;
        }

        private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
        private string? GetUserAgent() => HttpContext.Request.Headers["User-Agent"].ToString();

        private static string BuildVEvent(VacationRequest req, string uid)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{uid}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{req.StartDate:yyyyMMdd}");
            sb.AppendLine($"DTEND;VALUE=DATE:{req.EndDate.AddDays(1):yyyyMMdd}");
            sb.AppendLine($"SUMMARY:{req.LeaveType?.Name ?? "Puhkus"}");
            if (!string.IsNullOrWhiteSpace(req.Comment))
                sb.AppendLine($"DESCRIPTION:{req.Comment.Replace("\n", "\\n")}");
            sb.AppendLine("STATUS:CONFIRMED");
            sb.AppendLine("TRANSP:OPAQUE");
            sb.AppendLine("END:VEVENT");
            return sb.ToString();
        }

        private static string BuildVCalendar(IEnumerable<VacationRequest> requests, string prodId, string? calName = null)
        {
            var ical = new StringBuilder();
            ical.AppendLine("BEGIN:VCALENDAR");
            ical.AppendLine("VERSION:2.0");
            ical.AppendLine($"PRODID:-//Puhkusetaotluste süsteem//{prodId}//ET");
            ical.AppendLine("CALSCALE:GREGORIAN");
            ical.AppendLine("METHOD:PUBLISH");
            if (calName != null)
            {
                ical.AppendLine($"X-WR-CALNAME:{calName}");
                ical.AppendLine("X-WR-TIMEZONE:Europe/Tallinn");
                ical.AppendLine("REFRESH-INTERVAL;VALUE=DURATION:PT1H");
            }
            foreach (var req in requests)
                ical.Append(BuildVEvent(req, $"vacation-{req.Id}@vacationapp.local"));
            ical.AppendLine("END:VCALENDAR");
            return ical.ToString();
        }

        private async Task WriteHistory(int requestId, int? actorId, string eventType, string description,
            string? oldValue = null, string? newValue = null)
        {
            _context.RequestHistories.Add(new RequestHistory
            {
                VacationRequestId = requestId,
                ActorUserId       = actorId,
                EventType         = eventType,
                Description       = description,
                OldValue          = oldValue,
                NewValue          = newValue,
                CreatedAt         = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();
        }
    }
}
