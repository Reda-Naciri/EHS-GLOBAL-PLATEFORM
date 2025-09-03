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
    public class ShiftsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShiftsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shift>>> GetShifts()
        {
            return await _context.Shifts
                .Where(s => s.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Shift>> GetShift(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);

            if (shift == null)
            {
                return NotFound();
            }

            return shift;
        }

        [HttpPost]
        public async Task<ActionResult<Shift>> PostShift(Shift shift)
        {
            if (await _context.Shifts.AnyAsync(s => s.Code == shift.Code))
            {
                return BadRequest(new { message = "Shift code already exists" });
            }

            shift.CreatedAt = DateTime.UtcNow;
            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetShift", new { id = shift.Id }, shift);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutShift(int id, Shift shift)
        {
            if (id != shift.Id)
            {
                return BadRequest();
            }

            var existingShift = await _context.Shifts.FindAsync(id);
            if (existingShift == null)
            {
                return NotFound();
            }

            if (await _context.Shifts.AnyAsync(s => s.Code == shift.Code && s.Id != id))
            {
                return BadRequest(new { message = "Shift code already exists" });
            }

            existingShift.Name = shift.Name;
            existingShift.Code = shift.Code;
            existingShift.Description = shift.Description;
            existingShift.StartTime = shift.StartTime;
            existingShift.EndTime = shift.EndTime;
            existingShift.IsActive = shift.IsActive;
            existingShift.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShiftExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
            {
                return NotFound();
            }

            var hasUsers = await _context.Users.AnyAsync(u => u.ShiftId == id);
            if (hasUsers)
            {
                return BadRequest(new { message = "Cannot delete shift because it has associated users" });
            }

            shift.IsActive = false;
            shift.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ShiftExists(int id)
        {
            return _context.Shifts.Any(e => e.Id == id);
        }
    }
}