using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;
using VacationRequestApi.Models;
using VacationRequestApi.Services;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VacationRequestsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<VacationRequestsController> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUserService _userService;

        public VacationRequestsController(
            VacationRequestContext context,
            ILogger<VacationRequestsController> logger,
            IEmailService emailService,
            IAuditService auditService,
            IFileStorageService fileStorageService,
            IUserService userService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _auditService = auditService;
            _fileStorageService = fileStorageService;
            _userService = userService;
        }

        // GET: api/VacationRequests - with filtering
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

                // Non-admin users can only see their own requests
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

        // GET: api/VacationRequests/5
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

                // Check permissions
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

        // POST: api/VacationRequests
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

                // Validate dates
                var validationError = ValidateDates(dto.StartDate, dto.EndDate, userId);
                if (validationError != null)
                {
                    return BadRequest(new { message = validationError });
                }

                // Check for overlapping requests
                var hasOverlap = await HasOverlappingRequests(userId, dto.StartDate, dto.EndDate);
                if (hasOverlap)
                {
                    return Conflict(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
                }

                // Sanitize comment
                var sanitizedComment = SanitizeInput(dto.Comment);

                // Check leave type
                var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
                if (leaveType == null || !leaveType.IsActive)
                {
                    return BadRequest(new { message = "Vale puhkuse tüüp." });
                }

                // Check balance
                var daysRequested = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;
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
                    Status = leaveType.RequiresApproval ? VacationRequestStatus.Pending : VacationRequestStatus.Approved,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.VacationRequests.Add(vacationRequest);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogActionAsync(
                    vacationRequest.Id,
                    userId,
                    AuditAction.Created,
                    $"Created {leaveType.Name} request for {daysRequested} days",
                    null,
                    new { dto.StartDate, dto.EndDate, dto.LeaveTypeId, dto.Comment },
                    GetIpAddress(),
                    GetUserAgent()
                );

                // Send emails
                await _emailService.SendRequestSubmittedEmailAsync(vacationRequest.Id, user.FullName, user.Email);
                
                if (leaveType.RequiresApproval)
                {
                    await _emailService.SendNewRequestNotificationToAdminsAsync(
                        vacationRequest.Id, user.FullName, dto.StartDate, dto.EndDate);
                }

                _logger.LogInformation("Vacation request {Id} created by user {UserId}", vacationRequest.Id, userId);

                // Reload with includes
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

        // PUT: api/VacationRequests/5
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

                // Check permissions
                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                // Can only edit pending requests
                if (vacationRequest.Status != VacationRequestStatus.Pending && !isAdmin)
                {
                    return BadRequest(new { message = "Ainult ootel taotlusi saab muuta." });
                }

                // Store old values for audit
                var oldValues = new
                {
                    vacationRequest.StartDate,
                    vacationRequest.EndDate,
                    vacationRequest.LeaveTypeId,
                    vacationRequest.Comment
                };

                // Validate new dates
                var validationError = ValidateDates(dto.StartDate, dto.EndDate, userId, id);
                if (validationError != null)
                {
                    return BadRequest(new { message = validationError });
                }

                // Check for overlapping requests (excluding current)
                var hasOverlap = await HasOverlappingRequests(userId, dto.StartDate, dto.EndDate, id);
                if (hasOverlap)
                {
                    return Conflict(new { message = "Sellel perioodil on juba puhkusetaotlus olemas." });
                }

                // Update fields
                vacationRequest.LeaveTypeId = dto.LeaveTypeId;
                vacationRequest.StartDate = dto.StartDate.Date;
                vacationRequest.EndDate = dto.EndDate.Date;
                vacationRequest.Comment = SanitizeInput(dto.Comment);
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();

                    // Audit log
                    await _auditService.LogActionAsync(
                        id,
                        userId,
                        AuditAction.Updated,
                        "Request updated",
                        oldValues,
                        new { dto.StartDate, dto.EndDate, dto.LeaveTypeId, dto.Comment },
                        GetIpAddress(),
                        GetUserAgent()
                    );

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

        // DELETE: api/VacationRequests/5
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

                // Check permissions
                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                // Delete associated files
                foreach (var attachment in vacationRequest.Attachments)
                {
                    await _fileStorageService.DeleteFileAsync(attachment.FilePath);
                }

                _context.VacationRequests.Remove(vacationRequest);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogActionAsync(
                    id,
                    userId,
                    AuditAction.Deleted,
                    "Request deleted",
                    new { vacationRequest.StartDate, vacationRequest.EndDate, vacationRequest.Status },
                    null,
                    GetIpAddress(),
                    GetUserAgent()
                );

                _logger.LogInformation("Vacation request {Id} deleted by user {UserId}", id, userId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse kustutamisel." });
            }
        }

        // POST: api/VacationRequests/5/withdraw
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

                // Only owner can withdraw
                if (vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                // Can only withdraw pending or approved requests
                if (vacationRequest.Status != VacationRequestStatus.Pending &&
                    vacationRequest.Status != VacationRequestStatus.Approved)
                {
                    return BadRequest(new { message = "Seda taotlust ei saa tagasi võtta." });
                }

                var oldStatus = vacationRequest.Status;
                vacationRequest.Status = VacationRequestStatus.Withdrawn;
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogActionAsync(
                    id,
                    userId,
                    AuditAction.Withdrawn,
                    $"Request withdrawn (was {oldStatus})",
                    new { Status = oldStatus.ToString() },
                    new { Status = "Withdrawn" },
                    GetIpAddress(),
                    GetUserAgent()
                );

                _logger.LogInformation("Vacation request {Id} withdrawn by user {UserId}", id, userId);

                return Ok(new { message = "Taotlus tagasi võetud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing vacation request {Id}", id);
                return StatusCode(500, new { message = "Viga taotluse tagasivõtmisel." });
            }
        }

        // GET: api/VacationRequests/5/audit
        [HttpGet("{id}/audit")]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(int id)
        {
            try
            {
                var userId = _userService.GetCurrentUserId();
                var isAdmin = _userService.IsAdmin();

                var vacationRequest = await _context.VacationRequests.FindAsync(id);
                if (vacationRequest == null)
                {
                    return NotFound();
                }

                // Check permissions
                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                var auditLogs = await _auditService.GetAuditLogsForRequestAsync(id);

                var dtos = auditLogs.Select(log => new AuditLogDto
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    UserName = log.User?.FullName,
                    Action = log.Action.ToString(),
                    Details = log.Details,
                    Timestamp = log.Timestamp,
                    IpAddress = log.IpAddress
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs for request {Id}", id);
                return StatusCode(500, new { message = "Viga auditi logide laadimisel." });
            }
        }

        // ADMIN ENDPOINTS

        // GET: api/VacationRequests/admin/all
        [HttpGet("admin/all")]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetAllVacationRequestsAdmin(
            [FromQuery] VacationRequestFilterDto? filter = null)
        {
            // Just call the main GET with admin context
            return await GetVacationRequests(filter);
        }

        // GET: api/VacationRequests/admin/pending
        [HttpGet("admin/pending")]
        public async Task<ActionResult<IEnumerable<VacationRequestResponseDto>>> GetPendingVacationRequestsAdmin()
        {
            var filter = new VacationRequestFilterDto { Status = "Pending", SortBy = "StartDate", SortDescending = false };
            return await GetVacationRequests(filter);
        }

        // POST: api/VacationRequests/admin/approve/5
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

                var oldStatus = vacationRequest.Status;
                vacationRequest.Status = dto.Approved ? VacationRequestStatus.Approved : VacationRequestStatus.Rejected;
                vacationRequest.ApprovedByUserId = adminUserId;
                vacationRequest.ApprovedAt = DateTime.UtcNow;
                vacationRequest.AdminComment = dto.AdminComment?.Trim();
                vacationRequest.UpdatedAt = DateTime.UtcNow;

                // Update user's used days if approved
                if (dto.Approved && vacationRequest.User != null && vacationRequest.LeaveType?.IsPaid == true)
                {
                    var daysCount = (vacationRequest.EndDate.Date - vacationRequest.StartDate.Date).Days + 1;
                    vacationRequest.User.UsedLeaveDays += daysCount;
                }

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogActionAsync(
                    id,
                    adminUserId,
                    dto.Approved ? AuditAction.Approved : AuditAction.Rejected,
                    dto.AdminComment,
                    new { Status = oldStatus.ToString() },
                    new { Status = vacationRequest.Status.ToString(), AdminComment = dto.AdminComment },
                    GetIpAddress(),
                    GetUserAgent()
                );

                // Send email notification
                if (vacationRequest.User != null)
                {
                    if (dto.Approved)
                    {
                        await _emailService.SendRequestApprovedEmailAsync(
                            id, vacationRequest.User.FullName, vacationRequest.User.Email, dto.AdminComment);
                    }
                    else
                    {
                        await _emailService.SendRequestRejectedEmailAsync(
                            id, vacationRequest.User.FullName, vacationRequest.User.Email, dto.AdminComment);
                    }
                }

                var statusText = dto.Approved ? "kinnitatud" : "tagasi lükatud";
                _logger.LogInformation("Vacation request {Id} was {Status} by admin {AdminId}", id, statusText, adminUserId);

                // Reload with includes
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

        // DELETE: api/VacationRequests/admin/5
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

        // ATTACHMENT ENDPOINTS

        // POST: api/VacationRequests/5/attachments
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

                // Check permissions
                if (!isAdmin && vacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                // Validate file
                if (!_fileStorageService.IsValidFileType(file.ContentType))
                {
                    return BadRequest(new { message = "Failitüüp ei ole lubatud." });
                }

                if (!_fileStorageService.IsValidFileSize(file.Length))
                {
                    return BadRequest(new { message = "Fail on liiga suur." });
                }

                // Save file
                var filePath = await _fileStorageService.SaveFileAsync(file, id);

                // Create attachment record
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

                // Audit log
                await _auditService.LogActionAsync(
                    id,
                    userId,
                    AuditAction.AttachmentAdded,
                    $"Uploaded file: {file.FileName}",
                    null,
                    new { FileName = file.FileName, FileSize = file.Length },
                    GetIpAddress(),
                    GetUserAgent()
                );

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

        // GET: api/VacationRequests/5/attachments/3
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

                // Check permissions
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

        // DELETE: api/VacationRequests/5/attachments/3
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

                // Check permissions
                if (!isAdmin && attachment.VacationRequest.UserId != userId)
                {
                    return Forbid();
                }

                // Delete physical file
                await _fileStorageService.DeleteFileAsync(attachment.FilePath);

                // Delete record
                _context.VacationRequestAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogActionAsync(
                    id,
                    userId,
                    AuditAction.AttachmentDeleted,
                    $"Deleted file: {attachment.FileName}",
                    new { FileName = attachment.FileName },
                    null,
                    GetIpAddress(),
                    GetUserAgent()
                );

                _logger.LogInformation("Attachment {AttachmentId} deleted from request {RequestId}", attachmentId, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId}", attachmentId);
                return StatusCode(500, new { message = "Viga faili kustutamisel." });
            }
        }

        // STATISTICS AND EXPORT (keeping existing + enhanced)

        // GET: api/VacationRequests/statistics
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

        // GET: api/VacationRequests/export/csv
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

        // GET: api/VacationRequests/export/ical
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

                var ical = new StringBuilder();
                ical.AppendLine("BEGIN:VCALENDAR");
                ical.AppendLine("VERSION:2.0");
                ical.AppendLine("PRODID:-//Puhkusetaotluste süsteem//ET");
                ical.AppendLine("CALSCALE:GREGORIAN");
                ical.AppendLine("METHOD:PUBLISH");

                foreach (var request in requests)
                {
                    var uid = $"{request.Id}@vacationrequests.example.com";
                    var summary = $"Puhkus - {request.LeaveType?.Name ?? ""}";
                    var description = !string.IsNullOrWhiteSpace(request.Comment)
                        ? request.Comment.Replace("\n", "\\n")
                        : "Puhkus";

                    ical.AppendLine("BEGIN:VEVENT");
                    ical.AppendLine($"UID:{uid}");
                    ical.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd}T{DateTime.UtcNow:HHmmss}Z");
                    ical.AppendLine($"DTSTART;VALUE=DATE:{request.StartDate:yyyyMMdd}");
                    ical.AppendLine($"DTEND;VALUE=DATE:{request.EndDate.AddDays(1):yyyyMMdd}");
                    ical.AppendLine($"SUMMARY:{summary}");
                    ical.AppendLine($"DESCRIPTION:{description}");
                    ical.AppendLine("STATUS:CONFIRMED");
                    ical.AppendLine("END:VEVENT");
                }

                ical.AppendLine("END:VCALENDAR");

                var fileName = $"puhkused_{DateTime.Now:yyyyMMdd}.ics";
                var bytes = Encoding.UTF8.GetBytes(ical.ToString());

                _logger.LogInformation("Exported {Count} requests to iCal for user {UserId}", requests.Count, userId);

                return File(bytes, "text/calendar", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to iCal");
                return StatusCode(500, new { message = "Viga iCal eksportimisel." });
            }
        }

        // HELPER METHODS

        private VacationRequestResponseDto MapToResponseDto(VacationRequest request, int currentUserId, bool isAdmin)
        {
            var canEdit = request.Status == VacationRequestStatus.Pending &&
                         (request.UserId == currentUserId || isAdmin);
            var canDelete = request.UserId == currentUserId || isAdmin;
            var canWithdraw = request.UserId == currentUserId &&
                            (request.Status == VacationRequestStatus.Pending ||
                             request.Status == VacationRequestStatus.Approved);

            return new VacationRequestResponseDto
            {
                Id = request.Id,
                UserId = request.UserId,
                UserName = request.User?.FullName,
                UserEmail = request.User?.Email,
                Department = request.User?.Department,
                LeaveTypeId = request.LeaveTypeId,
                LeaveTypeName = request.LeaveType?.Name,
                LeaveTypeColor = request.LeaveType?.Color,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Comment = request.Comment,
                Status = request.Status.ToString(),
                ApprovedByUserId = request.ApprovedByUserId,
                ApprovedByName = request.ApprovedBy?.FullName,
                ApprovedAt = request.ApprovedAt,
                AdminComment = request.AdminComment,
                DaysCount = (request.EndDate.Date - request.StartDate.Date).Days + 1,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt,
                Attachments = request.Attachments.Select(a => new AttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedAt = a.UploadedAt
                }).ToList(),
                CanEdit = canEdit,
                CanDelete = canDelete,
                CanWithdraw = canWithdraw
            };
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

        private static string? SanitizeInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var sanitized = input.Trim();
            sanitized = Regex.Replace(sanitized, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"<iframe[^>]*>.*?</iframe>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"javascript:", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=", "", RegexOptions.IgnoreCase);

            return sanitized;
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

        private string? GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].ToString();
        }
    }
}
