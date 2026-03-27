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
    public class OrganizationsController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<OrganizationsController> _logger;

        public OrganizationsController(
            VacationRequestContext context,
            IAuditLogService auditLogService,
            ILogger<OrganizationsController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/Organizations - List all active organizations (public for registration)
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrganizationDto>>> GetOrganizations(
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.Organizations.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(o => o.IsActive);
                }

                var organizations = await query
                    .Select(o => new OrganizationDto
                    {
                        Id = o.Id,
                        Name = o.Name,
                        Description = o.Description,
                        Address = o.Address,
                        ContactEmail = o.ContactEmail,
                        ContactPhone = o.ContactPhone,
                        IsActive = o.IsActive,
                        MemberCount = o.Users.Count(u => u.IsActive),
                        CreatedAt = o.CreatedAt
                    })
                    .OrderBy(o => o.Name)
                    .ToListAsync();

                return Ok(organizations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organizations");
                return StatusCode(500, new { message = "Viga organisatsioonide laadimisel." });
            }
        }

        /// <summary>
        /// GET: api/Organizations/5 - Get organization details
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<OrganizationDto>> GetOrganization(int id)
        {
            try
            {
                var organization = await _context.Organizations
                    .Where(o => o.Id == id)
                    .Select(o => new OrganizationDto
                    {
                        Id = o.Id,
                        Name = o.Name,
                        Description = o.Description,
                        Address = o.Address,
                        ContactEmail = o.ContactEmail,
                        ContactPhone = o.ContactPhone,
                        IsActive = o.IsActive,
                        MemberCount = o.Users.Count(u => u.IsActive),
                        CreatedAt = o.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (organization == null)
                {
                    return NotFound(new { message = "Organisatsiooni ei leitud." });
                }

                return Ok(organization);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization {Id}", id);
                return StatusCode(500, new { message = "Viga organisatsiooni laadimisel." });
            }
        }

        /// <summary>
        /// POST: api/Organizations - Create new organization (admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<OrganizationDto>> CreateOrganization(
            [FromBody] OrganizationCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Check if organization name already exists
                if (await _context.Organizations.AnyAsync(o => o.Name == dto.Name))
                {
                    return BadRequest(new { message = "Selle nimega organisatsioon on juba olemas." });
                }

                var organization = new Organization
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Address = dto.Address,
                    ContactEmail = dto.ContactEmail,
                    ContactPhone = dto.ContactPhone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.OrganizationCreated,
                    userId: userId,
                    entityType: "Organization",
                    entityId: organization.Id,
                    details: $"Created organization: {organization.Name}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("Organization {OrgId} created by user {UserId}", 
                    organization.Id, userId);

                var result = new OrganizationDto
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    Description = organization.Description,
                    Address = organization.Address,
                    ContactEmail = organization.ContactEmail,
                    ContactPhone = organization.ContactPhone,
                    IsActive = organization.IsActive,
                    MemberCount = 0,
                    CreatedAt = organization.CreatedAt
                };

                return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating organization");
                return StatusCode(500, new { message = "Viga organisatsiooni loomisel." });
            }
        }

        /// <summary>
        /// PUT: api/Organizations/5 - Update organization (admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrganization(int id, [FromBody] OrganizationCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var organization = await _context.Organizations.FindAsync(id);

                if (organization == null)
                {
                    return NotFound(new { message = "Organisatsiooni ei leitud." });
                }

                // Check if name is taken by another organization
                if (await _context.Organizations.AnyAsync(o => o.Name == dto.Name && o.Id != id))
                {
                    return BadRequest(new { message = "Selle nimega organisatsioon on juba olemas." });
                }

                organization.Name = dto.Name;
                organization.Description = dto.Description;
                organization.Address = dto.Address;
                organization.ContactEmail = dto.ContactEmail;
                organization.ContactPhone = dto.ContactPhone;
                organization.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.OrganizationUpdated,
                    userId: userId,
                    entityType: "Organization",
                    entityId: organization.Id,
                    details: $"Updated organization: {organization.Name}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("Organization {OrgId} updated by user {UserId}", id, userId);

                return Ok(new { message = "Organisatsioon uuendatud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organization {Id}", id);
                return StatusCode(500, new { message = "Viga organisatsiooni uuendamisel." });
            }
        }

        /// <summary>
        /// DELETE: api/Organizations/5 - Deactivate organization (admin only)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var organization = await _context.Organizations.FindAsync(id);

                if (organization == null)
                {
                    return NotFound(new { message = "Organisatsiooni ei leitud." });
                }

                // Don't actually delete, just deactivate
                organization.IsActive = false;
                organization.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    AuditEventType.OrganizationUpdated,
                    userId: userId,
                    entityType: "Organization",
                    entityId: organization.Id,
                    details: $"Deactivated organization: {organization.Name}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogInformation("Organization {OrgId} deactivated by user {UserId}", id, userId);

                return Ok(new { message = "Organisatsioon deaktiveeritud." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating organization {Id}", id);
                return StatusCode(500, new { message = "Viga organisatsiooni deaktiveerimisel." });
            }
        }
    }
}
