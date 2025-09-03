using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HSEBackend.Data;
using HSEBackend.Models;
using HSEBackend.DTOs;

namespace HSEBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllUsersWithStatus()
        {
            var users = await _context.Users
                .Include(u => u.DepartmentRef)
                .Include(u => u.ZoneRef)
                .Include(u => u.ShiftRef)
                .ToListAsync();

            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Role = roles.FirstOrDefault() ?? "Profil",
                    user.CompanyId,
                    Department = user.DepartmentRef?.Name ?? user.Department ?? "",
                    Zone = user.ZoneRef?.Name ?? user.Zone ?? "",
                    user.Position,
                    user.DateOfBirth,
                    user.AccountCreatedAt,
                    user.LastLoginAt,
                    user.LastActivityAt,
                    user.IsOnline,
                    user.CurrentStatus,
                    user.IsActive,
                    user.DeactivatedAt,
                    user.DeactivationReason
                });
            }

            return Ok(userList);
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<object>>> GetActiveUsers()
        {
            var users = await _context.Users
                .Include(u => u.DepartmentRef)
                .Include(u => u.ZoneRef)
                .Where(u => u.IsActive)
                .ToListAsync();

            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Role = roles.FirstOrDefault() ?? "Profil",
                    user.CompanyId,
                    Department = user.DepartmentRef?.Name ?? user.Department ?? "",
                    Zone = user.ZoneRef?.Name ?? user.Zone ?? "",
                    user.Position
                });
            }

            return Ok(userList);
        }

        [HttpGet("inactive")]
        public async Task<ActionResult<IEnumerable<object>>> GetInactiveUsers()
        {
            var users = await _context.Users
                .Include(u => u.DepartmentRef)
                .Include(u => u.ZoneRef)
                .Where(u => !u.IsActive)
                .ToListAsync();

            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Role = roles.FirstOrDefault() ?? "Profil",
                    user.CompanyId,
                    Department = user.DepartmentRef?.Name ?? user.Department ?? "",
                    Zone = user.ZoneRef?.Name ?? user.Zone ?? "",
                    user.Position,
                    user.DeactivatedAt,
                    user.DeactivationReason
                });
            }

            return Ok(userList);
        }

        [HttpPost("{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(string userId, [FromBody] DeactivateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Prevent admin from deactivating themselves
            var currentUserId = User.FindFirst("UserId")?.Value;
            if (userId == currentUserId)
            {
                return BadRequest(new { message = "You cannot deactivate your own account" });
            }

            // Check if user is already inactive
            if (!user.IsActive)
            {
                return BadRequest(new { message = "User is already inactive" });
            }

            user.IsActive = false;
            user.DeactivatedAt = DateTime.UtcNow;
            user.DeactivationReason = dto.Reason;
            user.IsOnline = false;
            user.CurrentStatus = "Deactivated";

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to deactivate user", errors = result.Errors });
            }

            return Ok(new { message = "User deactivated successfully" });
        }

        [HttpPost("{userId}/activate")]
        public async Task<IActionResult> ActivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if user is already active
            if (user.IsActive)
            {
                return BadRequest(new { message = "User is already active" });
            }

            user.IsActive = true;
            user.DeactivatedAt = null;
            user.DeactivationReason = null;
            user.CurrentStatus = "Offline";

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Failed to activate user", errors = result.Errors });
            }

            return Ok(new { message = "User activated successfully" });
        }

        [HttpGet("{userId}/status")]
        public async Task<ActionResult<object>> GetUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Role = roles.FirstOrDefault() ?? "Profil",
                user.IsActive,
                user.DeactivatedAt,
                user.DeactivationReason,
                user.IsOnline,
                user.CurrentStatus,
                user.LastLoginAt,
                user.LastActivityAt
            });
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetUserStatistics()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var inactiveUsers = totalUsers - activeUsers;
            var onlineUsers = await _context.Users.CountAsync(u => u.IsOnline && u.IsActive);

            // Count by roles
            var adminUsers = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r })
                .CountAsync(x => x.r.Name == "Admin" && x.u.IsActive);

            var hseUsers = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r })
                .CountAsync(x => x.r.Name == "HSE" && x.u.IsActive);

            var profileUsers = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r })
                .CountAsync(x => x.r.Name == "Profil" && x.u.IsActive);

            return Ok(new
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                OnlineUsers = onlineUsers,
                UsersByRole = new
                {
                    Admin = adminUsers,
                    HSE = hseUsers,
                    Profile = profileUsers
                }
            });
        }
    }

    public class DeactivateUserDto
    {
        public string? Reason { get; set; }
    }
}