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
    public class ZonesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ZonesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Zone>>> GetZones()
        {
            return await _context.Zones
                .Where(z => z.IsActive)
                .OrderBy(z => z.Name)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Zone>> GetZone(int id)
        {
            var zone = await _context.Zones.FindAsync(id);

            if (zone == null)
            {
                return NotFound();
            }

            return zone;
        }

        [HttpPost]
        public async Task<ActionResult<Zone>> PostZone(Zone zone)
        {
            if (await _context.Zones.AnyAsync(z => z.Code == zone.Code))
            {
                return BadRequest(new { message = "Zone code already exists" });
            }

            zone.CreatedAt = DateTime.UtcNow;
            _context.Zones.Add(zone);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetZone", new { id = zone.Id }, zone);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutZone(int id, Zone zone)
        {
            if (id != zone.Id)
            {
                return BadRequest();
            }

            var existingZone = await _context.Zones.FindAsync(id);
            if (existingZone == null)
            {
                return NotFound();
            }

            if (await _context.Zones.AnyAsync(z => z.Code == zone.Code && z.Id != id))
            {
                return BadRequest(new { message = "Zone code already exists" });
            }

            existingZone.Name = zone.Name;
            existingZone.Code = zone.Code;
            existingZone.Description = zone.Description;
            existingZone.IsActive = zone.IsActive;
            existingZone.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ZoneExists(id))
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
        public async Task<IActionResult> DeleteZone(int id)
        {
            var zone = await _context.Zones.FindAsync(id);
            if (zone == null)
            {
                return NotFound();
            }

            var hasUsers = await _context.Users.AnyAsync(u => u.ZoneId == id);
            if (hasUsers)
            {
                return BadRequest(new { message = "Cannot delete zone because it has associated users" });
            }

            zone.IsActive = false;
            zone.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ZoneExists(int id)
        {
            return _context.Zones.Any(e => e.Id == id);
        }
    }
}