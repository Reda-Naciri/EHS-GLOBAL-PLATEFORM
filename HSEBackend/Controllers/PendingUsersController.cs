using HSEBackend.Data;
using HSEBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/pending-users")]
    public class PendingUsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PendingUsersController(AppDbContext context)
        {
            _context = context;
        }

        // 🔒 HSE/Admin peuvent voir les utilisateurs validés mais non enregistrés
        [HttpGet]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetPendingUsers()
        {
            var users = await _context.PendingUsers.ToListAsync();
            return Ok(users);
        }

        // Approve a pending user request
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var pendingUser = await _context.PendingUsers.FindAsync(id);
            if (pendingUser == null)
            {
                return NotFound(new { message = "Pending user not found" });
            }

            // For now, just remove the pending user as approved
            // TODO: Later we can add a proper approval flow
            _context.PendingUsers.Remove(pendingUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User request approved successfully", user = pendingUser });
        }

        // Reject a pending user request  
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> RejectUser(int id)
        {
            var pendingUser = await _context.PendingUsers.FindAsync(id);
            if (pendingUser == null)
            {
                return NotFound(new { message = "Pending user not found" });
            }

            // For now, just remove the pending user as rejected
            // TODO: Later we can add a proper rejection flow
            _context.PendingUsers.Remove(pendingUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User request rejected successfully", user = pendingUser });
        }
    }
}
