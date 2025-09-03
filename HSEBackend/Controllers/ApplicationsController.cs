using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HSEBackend.Data;
using HSEBackend.Models;

namespace HSEBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApplicationsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/applications
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Application>>> GetApplications()
        {
            return await _context.Applications
                .OrderBy(a => a.Order)
                .ToListAsync();
        }

        // GET: api/applications/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Application>>> GetActiveApplications()
        {
            return await _context.Applications
                .Where(a => a.IsActive)
                .OrderBy(a => a.Order)
                .ToListAsync();
        }

        // GET: api/applications/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Application>> GetApplication(int id)
        {
            var application = await _context.Applications.FindAsync(id);

            if (application == null)
            {
                return NotFound();
            }

            return application;
        }

        // POST: api/applications
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Application>> PostApplication(CreateApplicationDto createDto)
        {
            // Check for duplicate order
            if (await _context.Applications.AnyAsync(a => a.Order == createDto.Order))
            {
                return BadRequest(new { message = "An application with this order already exists" });
            }

            var application = new Application
            {
                Title = createDto.Title,
                Icon = createDto.Icon,
                RedirectUrl = createDto.RedirectUrl,
                IsActive = createDto.IsActive,
                Order = createDto.Order,
                CreatedAt = DateTime.UtcNow
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, application);
        }

        // PUT: api/applications/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutApplication(int id, UpdateApplicationDto updateDto)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(updateDto.Title))
                application.Title = updateDto.Title;
            
            if (!string.IsNullOrEmpty(updateDto.Icon))
                application.Icon = updateDto.Icon;
                
            if (!string.IsNullOrEmpty(updateDto.RedirectUrl))
                application.RedirectUrl = updateDto.RedirectUrl;
                
            if (updateDto.IsActive.HasValue)
                application.IsActive = updateDto.IsActive.Value;
                
            if (updateDto.Order.HasValue && updateDto.Order.Value != application.Order)
            {
                // Check for duplicate order
                if (await _context.Applications.AnyAsync(a => a.Order == updateDto.Order.Value && a.Id != id))
                {
                    return BadRequest(new { message = "An application with this order already exists" });
                }
                application.Order = updateDto.Order.Value;
            }

            application.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(application);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApplicationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // PATCH: api/applications/5/toggle-status
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Application>> ToggleApplicationStatus(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            application.IsActive = !application.IsActive;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(application);
        }

        // DELETE: api/applications/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PATCH: api/applications/reorder
        [HttpPatch("reorder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReorderApplications(ReorderApplicationsDto reorderDto)
        {
            foreach (var item in reorderDto.Applications)
            {
                var application = await _context.Applications.FindAsync(item.Id);
                if (application != null)
                {
                    application.Order = item.Order;
                    application.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.Id == id);
        }
    }

    // DTOs
    public class CreateApplicationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int Order { get; set; } = 1;
    }

    public class UpdateApplicationDto
    {
        public string? Title { get; set; }
        public string? Icon { get; set; }
        public string? RedirectUrl { get; set; }
        public bool? IsActive { get; set; }
        public int? Order { get; set; }
    }

    public class ReorderApplicationsDto
    {
        public List<ApplicationOrderDto> Applications { get; set; } = new();
    }

    public class ApplicationOrderDto
    {
        public int Id { get; set; }
        public int Order { get; set; }
    }
}