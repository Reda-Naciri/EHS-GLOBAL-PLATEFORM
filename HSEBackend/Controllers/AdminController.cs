using HSEBackend.Data;
using HSEBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all departments for frontend dropdowns
        /// </summary>
        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .Select(d => new { d.Id, d.Name, d.Code })
                    .ToListAsync();

                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments");
                return StatusCode(500, new { message = "Error retrieving departments" });
            }
        }

        /// <summary>
        /// Get all zones for frontend dropdowns
        /// </summary>
        [HttpGet("zones")]
        public async Task<IActionResult> GetZones()
        {
            try
            {
                var zones = await _context.Zones
                    .Where(z => z.IsActive)
                    .OrderBy(z => z.Name)
                    .Select(z => new { z.Id, z.Name, z.Code })
                    .ToListAsync();

                return Ok(zones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting zones");
                return StatusCode(500, new { message = "Error retrieving zones" });
            }
        }

        /// <summary>
        /// Get all shifts for frontend dropdowns
        /// </summary>
        [HttpGet("shifts")]
        public async Task<IActionResult> GetShifts()
        {
            try
            {
                var shifts = await _context.Shifts
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(s => new { s.Id, s.Name, s.Code, s.Description })
                    .ToListAsync();

                return Ok(shifts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shifts");
                return StatusCode(500, new { message = "Error retrieving shifts" });
            }
        }

        /// <summary>
        /// Get all fracture/injury types for frontend dropdowns
        /// </summary>
        [HttpGet("fracture-types")]
        public async Task<IActionResult> GetFractureTypes()
        {
            try
            {
                var fractureTypes = await _context.FractureTypes
                    .Where(ft => ft.IsActive)
                    .OrderBy(ft => ft.Category)
                    .ThenBy(ft => ft.Name)
                    .Select(ft => new { ft.Id, ft.Name, ft.Code, ft.Category })
                    .ToListAsync();

                return Ok(fractureTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fracture types");
                return StatusCode(500, new { message = "Error retrieving fracture types" });
            }
        }

        /// <summary>
        /// Validate company ID (for instant validation during report submission)
        /// </summary>
        [HttpPost("validate-company-id")]
        public async Task<IActionResult> ValidateCompanyId([FromBody] ValidateCompanyIdRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.DepartmentRef)
                    .Include(u => u.ZoneRef)
                    .Where(u => u.CompanyId == request.CompanyId && u.IsActive)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return Ok(new
                    {
                        isValid = false,
                        message = "Company ID not found or inactive"
                    });
                }

                return Ok(new
                {
                    isValid = true,
                    userId = user.Id,
                    reporterName = user.FullName,
                    department = user.DepartmentRef?.Name ?? user.Department,
                    position = user.Position ?? user.LocalJobTitle ?? "Position not specified",
                    companyId = user.CompanyId,
                    message = "Company ID validated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating company ID: {CompanyId}", request.CompanyId);
                return StatusCode(500, new { message = "Error validating company ID" });
            }
        }
    }

    public class ValidateCompanyIdRequest
    {
        public string CompanyId { get; set; } = "";
    }
}