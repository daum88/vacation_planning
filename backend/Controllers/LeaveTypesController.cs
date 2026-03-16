using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacationRequestApi.Data;
using VacationRequestApi.DTOs;

namespace VacationRequestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveTypesController : ControllerBase
    {
        private readonly VacationRequestContext _context;
        private readonly ILogger<LeaveTypesController> _logger;

        public LeaveTypesController(VacationRequestContext context, ILogger<LeaveTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/LeaveTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetLeaveTypes([FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.LeaveTypes.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(lt => lt.IsActive);
                }

                var leaveTypes = await query
                    .OrderBy(lt => lt.DisplayOrder)
                    .ThenBy(lt => lt.Name)
                    .ToListAsync();

                var dtos = leaveTypes.Select(lt => new LeaveTypeDto
                {
                    Id = lt.Id,
                    Name = lt.Name,
                    Description = lt.Description,
                    Color = lt.Color,
                    RequiresApproval = lt.RequiresApproval,
                    RequiresAttachment = lt.RequiresAttachment,
                    MaxDaysPerYear = lt.MaxDaysPerYear,
                    IsPaid = lt.IsPaid,
                    IsActive = lt.IsActive
                }).ToList();

                _logger.LogInformation("Retrieved {Count} leave types", dtos.Count);
                return Ok(dtos);
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
            try
            {
                var leaveType = await _context.LeaveTypes.FindAsync(id);

                if (leaveType == null)
                {
                    return NotFound(new { message = "Puhkusetüüpi ei leitud." });
                }

                var dto = new LeaveTypeDto
                {
                    Id = leaveType.Id,
                    Name = leaveType.Name,
                    Description = leaveType.Description,
                    Color = leaveType.Color,
                    RequiresApproval = leaveType.RequiresApproval,
                    RequiresAttachment = leaveType.RequiresAttachment,
                    MaxDaysPerYear = leaveType.MaxDaysPerYear,
                    IsPaid = leaveType.IsPaid,
                    IsActive = leaveType.IsActive
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave type {LeaveTypeId}", id);
                return StatusCode(500, new { message = "Viga puhkusetüübi laadimisel." });
            }
        }
    }
}
