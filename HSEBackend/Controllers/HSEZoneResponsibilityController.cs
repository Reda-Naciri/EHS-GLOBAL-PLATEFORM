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
    public class HSEZoneResponsibilityController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HSEZoneResponsibilityController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetHSEZoneResponsibilities()
        {
            var responsibilities = await _context.HSEZoneResponsibilities
                .Include(hzr => hzr.HSEUser)
                .Include(hzr => hzr.Zone)
                .Where(hzr => hzr.IsActive)
                .Select(hzr => new
                {
                    hzr.Id,
                    hzr.HSEUserId,
                    HSEUserName = hzr.HSEUser.FirstName + " " + hzr.HSEUser.LastName,
                    HSEUserEmail = hzr.HSEUser.Email,
                    hzr.ZoneId,
                    ZoneName = hzr.Zone.Name,
                    ZoneCode = hzr.Zone.Code,
                    hzr.AssignedAt,
                    hzr.IsActive
                })
                .ToListAsync();

            return Ok(responsibilities);
        }

        [HttpGet("hse/{hseUserId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetHSEUserZones(string hseUserId)
        {
            var zones = await _context.HSEZoneResponsibilities
                .Include(hzr => hzr.Zone)
                .Where(hzr => hzr.HSEUserId == hseUserId && hzr.IsActive)
                .Select(hzr => new
                {
                    hzr.Id,
                    hzr.ZoneId,
                    ZoneName = hzr.Zone.Name,
                    ZoneCode = hzr.Zone.Code,
                    ZoneDescription = hzr.Zone.Description,
                    hzr.AssignedAt
                })
                .ToListAsync();

            return Ok(zones);
        }

        [HttpPost("assign")]
        public async Task<ActionResult> AssignZoneToHSE([FromBody] AssignZoneToHSEDto dto)
        {
            // Check if HSE user exists and has HSE role
            var hseUser = await _context.Users.FindAsync(dto.HSEUserId);
            if (hseUser == null)
            {
                return NotFound(new { message = "HSE user not found" });
            }

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == dto.HSEUserId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            if (!userRoles.Contains("HSE"))
            {
                return BadRequest(new { message = "User must have HSE role" });
            }

            // Check if zone exists
            var zone = await _context.Zones.FindAsync(dto.ZoneId);
            if (zone == null)
            {
                return NotFound(new { message = "Zone not found" });
            }

            // Check if assignment already exists
            var existingAssignment = await _context.HSEZoneResponsibilities
                .FirstOrDefaultAsync(hzr => hzr.HSEUserId == dto.HSEUserId && hzr.ZoneId == dto.ZoneId && hzr.IsActive);

            if (existingAssignment != null)
            {
                return BadRequest(new { message = "HSE user is already assigned to this zone" });
            }

            var responsibility = new HSEZoneResponsibility
            {
                HSEUserId = dto.HSEUserId,
                ZoneId = dto.ZoneId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.HSEZoneResponsibilities.Add(responsibility);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Zone assigned to HSE user successfully", responsibilityId = responsibility.Id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveZoneResponsibility(int id)
        {
            var responsibility = await _context.HSEZoneResponsibilities.FindAsync(id);
            if (responsibility == null)
            {
                return NotFound();
            }

            responsibility.IsActive = false;
            responsibility.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("hse-users")]
        public async Task<ActionResult<IEnumerable<object>>> GetHSEUsers()
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

    public class AssignZoneToHSEDto
    {
        public string HSEUserId { get; set; } = "";
        public int ZoneId { get; set; }
    }
}