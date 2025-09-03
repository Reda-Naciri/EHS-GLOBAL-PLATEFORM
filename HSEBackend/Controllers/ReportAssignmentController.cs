using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HSEBackend.Data;
using HSEBackend.Models;

namespace HSEBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportAssignmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportAssignmentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetReportAssignments()
        {
            var assignments = await _context.ReportAssignments
                .Include(ra => ra.Report)
                .Include(ra => ra.AssignedHSEUser)
                .Include(ra => ra.AssignedByAdmin)
                .Where(ra => ra.IsActive)
                .Select(ra => new
                {
                    ra.Id,
                    ra.ReportId,
                    ReportTitle = ra.Report.Title,
                    ReportType = ra.Report.Type,
                    ReportZone = ra.Report.Zone,
                    ReportStatus = ra.Report.Status,
                    ra.AssignedHSEUserId,
                    AssignedHSEUserName = ra.AssignedHSEUser.FirstName + " " + ra.AssignedHSEUser.LastName,
                    AssignedHSEUserEmail = ra.AssignedHSEUser.Email,
                    ra.AssignmentReason,
                    ra.AssignedAt,
                    AssignedByAdminName = ra.AssignedByAdmin.FirstName + " " + ra.AssignedByAdmin.LastName
                })
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpGet("report/{reportId}")]
        public async Task<ActionResult<object>> GetReportAssignment(int reportId)
        {
            var assignment = await _context.ReportAssignments
                .Include(ra => ra.AssignedHSEUser)
                .Include(ra => ra.AssignedByAdmin)
                .Where(ra => ra.ReportId == reportId && ra.IsActive)
                .Select(ra => new
                {
                    ra.Id,
                    ra.ReportId,
                    ra.AssignedHSEUserId,
                    AssignedHSEUserName = ra.AssignedHSEUser.FirstName + " " + ra.AssignedHSEUser.LastName,
                    AssignedHSEUserEmail = ra.AssignedHSEUser.Email,
                    ra.AssignmentReason,
                    ra.AssignedAt,
                    AssignedByAdminName = ra.AssignedByAdmin.FirstName + " " + ra.AssignedByAdmin.LastName
                })
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return NotFound(new { message = "No assignment found for this report" });
            }

            return Ok(assignment);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserAssignments(string userId)
        {
            var assignments = await _context.ReportAssignments
                .Include(ra => ra.Report)
                .Include(ra => ra.AssignedByAdmin)
                .Where(ra => ra.AssignedHSEUserId == userId && ra.IsActive)
                .Select(ra => new
                {
                    ra.Id,
                    ra.ReportId,
                    ReportTitle = ra.Report.Title,
                    ReportType = ra.Report.Type,
                    ReportZone = ra.Report.Zone,
                    ReportStatus = ra.Report.Status,
                    ReportCreatedAt = ra.Report.CreatedAt,
                    ra.AssignmentReason,
                    ra.AssignedAt,
                    AssignedByAdminName = ra.AssignedByAdmin.FirstName + " " + ra.AssignedByAdmin.LastName
                })
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpPost("assign")]
        public async Task<ActionResult> AssignReportToHSE([FromBody] CreateReportAssignmentDto dto)
        {
            // Check if report exists
            var report = await _context.Reports.FindAsync(dto.ReportId);
            if (report == null)
            {
                return NotFound(new { message = "Report not found" });
            }

            // Check if HSE user exists and has HSE role
            var hseUser = await _context.Users.FindAsync(dto.AssignedHSEUserId);
            if (hseUser == null)
            {
                return NotFound(new { message = "HSE user not found" });
            }

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == dto.AssignedHSEUserId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            if (!userRoles.Contains("HSE"))
            {
                return BadRequest(new { message = "User must have HSE role" });
            }

            // Check if there's already an active assignment for this report
            var existingAssignment = await _context.ReportAssignments
                .FirstOrDefaultAsync(ra => ra.ReportId == dto.ReportId && ra.IsActive);

            if (existingAssignment != null)
            {
                // Update existing assignment
                existingAssignment.AssignedHSEUserId = dto.AssignedHSEUserId;
                existingAssignment.AssignmentReason = dto.AssignmentReason;
                existingAssignment.AssignedAt = DateTime.UtcNow;
                existingAssignment.AssignedByAdminId = User.FindFirst("UserId")?.Value ?? "";
                existingAssignment.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new assignment
                var adminId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized(new { message = "Invalid admin context" });
                }

                var assignment = new ReportAssignment
                {
                    ReportId = dto.ReportId,
                    AssignedHSEUserId = dto.AssignedHSEUserId,
                    AssignmentReason = dto.AssignmentReason,
                    AssignedByAdminId = adminId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ReportAssignments.Add(assignment);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Report assigned successfully" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReportAssignment(int id, [FromBody] UpdateReportAssignmentDto dto)
        {
            var assignment = await _context.ReportAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            // Validate new HSE user if provided
            if (!string.IsNullOrEmpty(dto.AssignedHSEUserId))
            {
                var hseUser = await _context.Users.FindAsync(dto.AssignedHSEUserId);
                if (hseUser == null)
                {
                    return NotFound(new { message = "HSE user not found" });
                }

                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == dto.AssignedHSEUserId)
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();

                if (!userRoles.Contains("HSE"))
                {
                    return BadRequest(new { message = "User must have HSE role" });
                }

                assignment.AssignedHSEUserId = dto.AssignedHSEUserId;
            }

            if (!string.IsNullOrEmpty(dto.AssignmentReason))
            {
                assignment.AssignmentReason = dto.AssignmentReason;
            }

            assignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveReportAssignment(int id)
        {
            var assignment = await _context.ReportAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            assignment.IsActive = false;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("report/{reportId}")]
        public async Task<IActionResult> RemoveReportAssignmentByReport(int reportId)
        {
            var assignment = await _context.ReportAssignments
                .FirstOrDefaultAsync(ra => ra.ReportId == reportId && ra.IsActive);

            if (assignment == null)
            {
                return NotFound(new { message = "No active assignment found for this report" });
            }

            assignment.IsActive = false;
            assignment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("available-hse-users")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableHSEUsers()
        {
            var hseUsers = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { User = u, ur.RoleId })
                .Join(_context.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.User, Role = r })
                .Where(x => x.Role.Name == "HSE")
                .Select(x => new
                {
                    x.User.Id,
                    x.User.Email,
                    x.User.FirstName,
                    x.User.LastName,
                    FullName = x.User.FirstName + " " + x.User.LastName,
                    x.User.CompanyId
                })
                .ToListAsync();

            return Ok(hseUsers);
        }
    }

    public class CreateReportAssignmentDto
    {
        public int ReportId { get; set; }
        public string AssignedHSEUserId { get; set; } = "";
        public string? AssignmentReason { get; set; }
    }

    public class UpdateReportAssignmentDto
    {
        public string? AssignedHSEUserId { get; set; }
        public string? AssignmentReason { get; set; }
    }
}