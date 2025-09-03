using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HSEBackend.Data;
using HSEBackend.Models;
using HSEBackend.Services;

namespace HSEBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class HSEZoneDelegationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<HSEZoneDelegationController> _logger;

        public HSEZoneDelegationController(AppDbContext context, INotificationService notificationService, ILogger<HSEZoneDelegationController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetZoneDelegations()
        {
            var delegations = await _context.HSEZoneDelegations
                .Include(hzd => hzd.FromHSEUser)
                .Include(hzd => hzd.ToHSEUser)
                .Include(hzd => hzd.Zone)
                .Include(hzd => hzd.CreatedByAdmin)
                .Where(hzd => hzd.IsActive)
                .Select(hzd => new
                {
                    hzd.Id,
                    hzd.FromHSEUserId,
                    FromHSEUserName = hzd.FromHSEUser.FirstName + " " + hzd.FromHSEUser.LastName,
                    FromHSEUserEmail = hzd.FromHSEUser.Email,
                    hzd.ToHSEUserId,
                    ToHSEUserName = hzd.ToHSEUser.FirstName + " " + hzd.ToHSEUser.LastName,
                    ToHSEUserEmail = hzd.ToHSEUser.Email,
                    hzd.ZoneId,
                    ZoneName = hzd.Zone.Name,
                    ZoneCode = hzd.Zone.Code,
                    hzd.StartDate,
                    hzd.EndDate,
                    hzd.Reason,
                    hzd.CreatedAt,
                    CreatedByAdminName = hzd.CreatedByAdmin.FirstName + " " + hzd.CreatedByAdmin.LastName,
                    IsCurrentlyActive = hzd.StartDate <= DateTime.UtcNow && hzd.EndDate >= DateTime.UtcNow
                })
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(delegations);
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<object>>> GetActiveDelegations()
        {
            var currentDate = DateTime.UtcNow;
            var activeDelegations = await _context.HSEZoneDelegations
                .Include(hzd => hzd.FromHSEUser)
                .Include(hzd => hzd.ToHSEUser)
                .Include(hzd => hzd.Zone)
                .Where(hzd => hzd.IsActive && 
                             hzd.StartDate <= currentDate && 
                             hzd.EndDate >= currentDate)
                .Select(hzd => new
                {
                    hzd.Id,
                    FromHSEUserName = hzd.FromHSEUser.FirstName + " " + hzd.FromHSEUser.LastName,
                    ToHSEUserName = hzd.ToHSEUser.FirstName + " " + hzd.ToHSEUser.LastName,
                    ZoneName = hzd.Zone.Name,
                    hzd.StartDate,
                    hzd.EndDate,
                    hzd.Reason,
                    DaysRemaining = (hzd.EndDate - currentDate).Days
                })
                .ToListAsync();

            return Ok(activeDelegations);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserDelegations(string userId)
        {
            var delegations = await _context.HSEZoneDelegations
                .Include(hzd => hzd.FromHSEUser)
                .Include(hzd => hzd.ToHSEUser)
                .Include(hzd => hzd.Zone)
                .Where(hzd => (hzd.FromHSEUserId == userId || hzd.ToHSEUserId == userId) && hzd.IsActive)
                .Select(hzd => new
                {
                    hzd.Id,
                    hzd.ZoneId,
                    ZoneName = hzd.Zone.Name,
                    ZoneCode = hzd.Zone.Code,
                    hzd.StartDate,
                    hzd.EndDate,
                    hzd.Reason,
                    DelegationType = hzd.FromHSEUserId == userId ? "Delegated Out" : "Delegated To",
                    OtherUserName = hzd.FromHSEUserId == userId ? 
                        hzd.ToHSEUser.FirstName + " " + hzd.ToHSEUser.LastName :
                        hzd.FromHSEUser.FirstName + " " + hzd.FromHSEUser.LastName,
                    IsCurrentlyActive = hzd.StartDate <= DateTime.UtcNow && hzd.EndDate >= DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(delegations);
        }

        [HttpPost("delegate")]
        public async Task<ActionResult> CreateZoneDelegation([FromBody] CreateZoneDelegationDto dto)
        {
            // Validate dates
            if (dto.StartDate >= dto.EndDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            if (dto.StartDate < DateTime.UtcNow.Date)
            {
                return BadRequest(new { message = "Start date cannot be in the past" });
            }

            // Validate users exist and have HSE role
            var fromUser = await _context.Users.FindAsync(dto.FromHSEUserId);
            var toUser = await _context.Users.FindAsync(dto.ToHSEUserId);
            
            if (fromUser == null || toUser == null)
            {
                return NotFound(new { message = "One or both HSE users not found" });
            }

            // Check if both users have HSE role
            var fromUserRoles = await _context.UserRoles
                .Where(ur => ur.UserId == dto.FromHSEUserId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            var toUserRoles = await _context.UserRoles
                .Where(ur => ur.UserId == dto.ToHSEUserId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            if (!fromUserRoles.Contains("HSE") || !toUserRoles.Contains("HSE"))
            {
                return BadRequest(new { message = "Both users must have HSE role" });
            }

            // Check if zone exists
            var zone = await _context.Zones.FindAsync(dto.ZoneId);
            if (zone == null)
            {
                return NotFound(new { message = "Zone not found" });
            }

            // Check if fromUser actually has responsibility for this zone
            var hasZoneResponsibility = await _context.HSEZoneResponsibilities
                .AnyAsync(hzr => hzr.HSEUserId == dto.FromHSEUserId && hzr.ZoneId == dto.ZoneId && hzr.IsActive);

            if (!hasZoneResponsibility)
            {
                return BadRequest(new { message = "From HSE user does not have responsibility for this zone" });
            }

            // Check for overlapping delegations
            var hasOverlappingDelegation = await _context.HSEZoneDelegations
                .AnyAsync(hzd => hzd.ZoneId == dto.ZoneId && 
                                hzd.IsActive &&
                                ((hzd.StartDate <= dto.EndDate && hzd.EndDate >= dto.StartDate)));

            if (hasOverlappingDelegation)
            {
                return BadRequest(new { message = "There is already an overlapping delegation for this zone and time period" });
            }

            var adminId = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized(new { message = "Invalid admin context" });
            }

            var delegation = new HSEZoneDelegation
            {
                FromHSEUserId = dto.FromHSEUserId,
                ToHSEUserId = dto.ToHSEUserId,
                ZoneId = dto.ZoneId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Reason = dto.Reason,
                CreatedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.HSEZoneDelegations.Add(delegation);
            await _context.SaveChangesAsync();

            // Send notifications to both parties
            try
            {
                _logger.LogInformation("🔔 Attempting to send delegation creation notifications for delegation {DelegationId}", delegation.Id);
                await _notificationService.NotifyOnZoneDelegationCreatedAsync(delegation.Id);
                _logger.LogInformation("✅ Successfully sent delegation creation notifications for delegation {DelegationId}", delegation.Id);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the delegation creation
                // The delegation was successfully created, notification failure shouldn't break the flow
                _logger.LogError(ex, "❌ Failed to send delegation creation notifications for delegation {DelegationId}: {ErrorMessage}", delegation.Id, ex.Message);
            }

            return Ok(new { message = "Zone delegation created successfully", delegationId = delegation.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateZoneDelegation(int id, [FromBody] UpdateZoneDelegationDto dto)
        {
            var delegation = await _context.HSEZoneDelegations.FindAsync(id);
            if (delegation == null)
            {
                return NotFound();
            }

            // Validate dates if provided
            var startDate = dto.StartDate ?? delegation.StartDate;
            var endDate = dto.EndDate ?? delegation.EndDate;

            if (startDate >= endDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            // Update fields
            if (dto.StartDate.HasValue) delegation.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) delegation.EndDate = dto.EndDate.Value;
            if (!string.IsNullOrEmpty(dto.Reason)) delegation.Reason = dto.Reason;
            
            delegation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelZoneDelegation(int id)
        {
            var delegation = await _context.HSEZoneDelegations.FindAsync(id);
            if (delegation == null)
            {
                return NotFound();
            }

            delegation.IsActive = false;
            delegation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send end notifications to both parties
            try
            {
                _logger.LogInformation("🔔 Attempting to send delegation end notifications for delegation {DelegationId}", delegation.Id);
                await _notificationService.NotifyOnZoneDelegationEndedAsync(delegation.Id);
                _logger.LogInformation("✅ Successfully sent delegation end notifications for delegation {DelegationId}", delegation.Id);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the delegation cancellation
                _logger.LogError(ex, "❌ Failed to send delegation end notifications for delegation {DelegationId}: {ErrorMessage}", delegation.Id, ex.Message);
            }

            return NoContent();
        }
    }

    public class CreateZoneDelegationDto
    {
        public string FromHSEUserId { get; set; } = "";
        public string ToHSEUserId { get; set; } = "";
        public int ZoneId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
    }

    public class UpdateZoneDelegationDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
    }
}