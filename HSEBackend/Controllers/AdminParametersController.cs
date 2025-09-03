using HSEBackend.Data;
using HSEBackend.DTOs;
using HSEBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/admin-parameters")]
    [Authorize(Roles = "Admin")]
    public class AdminParametersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminParametersController> _logger;

        public AdminParametersController(AppDbContext context, ILogger<AdminParametersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Zones CRUD

        [HttpGet("zones")]
        public async Task<IActionResult> GetZones()
        {
            try
            {
                var zones = await _context.Zones
                    .OrderBy(z => z.Name)
                    .Select(z => new ZoneDto
                    {
                        Id = z.Id,
                        Name = z.Name,
                        Description = z.Description,
                        Code = z.Code,
                        IsActive = z.IsActive,
                        CreatedAt = z.CreatedAt,
                        UpdatedAt = z.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(zones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving zones");
                return StatusCode(500, new { message = "Error retrieving zones" });
            }
        }

        [HttpGet("zones/{id}")]
        public async Task<IActionResult> GetZone(int id)
        {
            try
            {
                var zone = await _context.Zones.FindAsync(id);
                if (zone == null)
                    return NotFound(new { message = "Zone not found" });

                var zoneDto = new ZoneDto
                {
                    Id = zone.Id,
                    Name = zone.Name,
                    Description = zone.Description,
                    Code = zone.Code,
                    IsActive = zone.IsActive,
                    CreatedAt = zone.CreatedAt,
                    UpdatedAt = zone.UpdatedAt
                };

                return Ok(zoneDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving zone {Id}", id);
                return StatusCode(500, new { message = "Error retrieving zone" });
            }
        }

        [HttpPost("zones")]
        public async Task<IActionResult> CreateZone([FromBody] CreateZoneDto dto)
        {
            try
            {
                // Check for duplicate name
                if (await _context.Zones.AnyAsync(z => z.Name.ToLower() == dto.Name.ToLower()))
                    return BadRequest(new { message = "Zone with this name already exists" });

                // Check for duplicate code
                if (await _context.Zones.AnyAsync(z => z.Code.ToLower() == dto.Code.ToLower()))
                    return BadRequest(new { message = "Zone with this code already exists" });

                var zone = new Zone
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Code = dto.Code,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Zones.Add(zone);
                await _context.SaveChangesAsync();

                var zoneDto = new ZoneDto
                {
                    Id = zone.Id,
                    Name = zone.Name,
                    Description = zone.Description,
                    Code = zone.Code,
                    IsActive = zone.IsActive,
                    CreatedAt = zone.CreatedAt,
                    UpdatedAt = zone.UpdatedAt
                };

                return CreatedAtAction(nameof(GetZone), new { id = zone.Id }, zoneDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating zone");
                return StatusCode(500, new { message = "Error creating zone" });
            }
        }

        [HttpPut("zones/{id}")]
        public async Task<IActionResult> UpdateZone(int id, [FromBody] UpdateZoneDto dto)
        {
            try
            {
                var zone = await _context.Zones.FindAsync(id);
                if (zone == null)
                    return NotFound(new { message = "Zone not found" });

                // Check for duplicate name (excluding current zone)
                if (!string.IsNullOrEmpty(dto.Name) && 
                    await _context.Zones.AnyAsync(z => z.Name.ToLower() == dto.Name.ToLower() && z.Id != id))
                    return BadRequest(new { message = "Zone with this name already exists" });

                // Check for duplicate code (excluding current zone)
                if (!string.IsNullOrEmpty(dto.Code) && 
                    await _context.Zones.AnyAsync(z => z.Code.ToLower() == dto.Code.ToLower() && z.Id != id))
                    return BadRequest(new { message = "Zone with this code already exists" });

                // Update fields
                if (!string.IsNullOrEmpty(dto.Name)) zone.Name = dto.Name;
                if (dto.Description != null) zone.Description = dto.Description;
                if (!string.IsNullOrEmpty(dto.Code)) zone.Code = dto.Code;
                if (dto.IsActive.HasValue) zone.IsActive = dto.IsActive.Value;
                
                zone.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Zone updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating zone {Id}", id);
                return StatusCode(500, new { message = "Error updating zone" });
            }
        }

        [HttpDelete("zones/{id}")]
        public async Task<IActionResult> DeleteZone(int id)
        {
            try
            {
                var zone = await _context.Zones.FindAsync(id);
                if (zone == null)
                    return NotFound(new { message = "Zone not found" });

                // Check if zone is being used by users or reports
                var hasUsers = await _context.Users.AnyAsync(u => u.ZoneId == id);
                var hasReports = await _context.Reports.AnyAsync(r => r.ZoneId == id);

                if (hasUsers || hasReports)
                    return BadRequest(new { message = "Cannot delete zone. It is being used by users or reports." });

                _context.Zones.Remove(zone);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Zone deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting zone {Id}", id);
                return StatusCode(500, new { message = "Error deleting zone" });
            }
        }

        #endregion

        #region Departments CRUD

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    .OrderBy(d => d.Name)
                    .Select(d => new DepartmentDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Description = d.Description,
                        Code = d.Code,
                        IsActive = d.IsActive,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving departments");
                return StatusCode(500, new { message = "Error retrieving departments" });
            }
        }

        [HttpGet("departments/{id}")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return NotFound(new { message = "Department not found" });

                var departmentDto = new DepartmentDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    Description = department.Description,
                    Code = department.Code,
                    IsActive = department.IsActive,
                    CreatedAt = department.CreatedAt,
                    UpdatedAt = department.UpdatedAt
                };

                return Ok(departmentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving department {Id}", id);
                return StatusCode(500, new { message = "Error retrieving department" });
            }
        }

        [HttpPost("departments")]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto dto)
        {
            try
            {
                // Check for duplicate name
                if (await _context.Departments.AnyAsync(d => d.Name.ToLower() == dto.Name.ToLower()))
                    return BadRequest(new { message = "Department with this name already exists" });

                // Check for duplicate code
                if (await _context.Departments.AnyAsync(d => d.Code.ToLower() == dto.Code.ToLower()))
                    return BadRequest(new { message = "Department with this code already exists" });

                var department = new Department
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Code = dto.Code,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                var departmentDto = new DepartmentDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    Description = department.Description,
                    Code = department.Code,
                    IsActive = department.IsActive,
                    CreatedAt = department.CreatedAt,
                    UpdatedAt = department.UpdatedAt
                };

                return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, departmentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                return StatusCode(500, new { message = "Error creating department" });
            }
        }

        [HttpPut("departments/{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return NotFound(new { message = "Department not found" });

                // Check for duplicate name (excluding current department)
                if (!string.IsNullOrEmpty(dto.Name) && 
                    await _context.Departments.AnyAsync(d => d.Name.ToLower() == dto.Name.ToLower() && d.Id != id))
                    return BadRequest(new { message = "Department with this name already exists" });

                // Check for duplicate code (excluding current department)
                if (!string.IsNullOrEmpty(dto.Code) && 
                    await _context.Departments.AnyAsync(d => d.Code.ToLower() == dto.Code.ToLower() && d.Id != id))
                    return BadRequest(new { message = "Department with this code already exists" });

                // Update fields
                if (!string.IsNullOrEmpty(dto.Name)) department.Name = dto.Name;
                if (dto.Description != null) department.Description = dto.Description;
                if (!string.IsNullOrEmpty(dto.Code)) department.Code = dto.Code;
                if (dto.IsActive.HasValue) department.IsActive = dto.IsActive.Value;
                
                department.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Department updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department {Id}", id);
                return StatusCode(500, new { message = "Error updating department" });
            }
        }

        [HttpDelete("departments/{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return NotFound(new { message = "Department not found" });

                // Check if department is being used by users
                var hasUsers = await _context.Users.AnyAsync(u => u.DepartmentId == id);

                if (hasUsers)
                    return BadRequest(new { message = "Cannot delete department. It is being used by users." });

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department {Id}", id);
                return StatusCode(500, new { message = "Error deleting department" });
            }
        }

        #endregion

        #region Injury Types CRUD

        [HttpGet("injury-types")]
        public async Task<IActionResult> GetInjuryTypes()
        {
            try
            {
                var injuryTypes = await _context.FractureTypes
                    .OrderBy(f => f.Name)
                    .Select(f => new InjuryTypeDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Description = f.Description,
                        Code = f.Code,
                        Category = f.Category,
                        IsActive = f.IsActive,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(injuryTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving injury types");
                return StatusCode(500, new { message = "Error retrieving injury types" });
            }
        }

        [HttpGet("injury-types/{id}")]
        public async Task<IActionResult> GetInjuryType(int id)
        {
            try
            {
                var injuryType = await _context.FractureTypes.FindAsync(id);
                if (injuryType == null)
                    return NotFound(new { message = "Injury type not found" });

                var injuryTypeDto = new InjuryTypeDto
                {
                    Id = injuryType.Id,
                    Name = injuryType.Name,
                    Description = injuryType.Description,
                    Code = injuryType.Code,
                    Category = injuryType.Category,
                    IsActive = injuryType.IsActive,
                    CreatedAt = injuryType.CreatedAt,
                    UpdatedAt = injuryType.UpdatedAt
                };

                return Ok(injuryTypeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving injury type {Id}", id);
                return StatusCode(500, new { message = "Error retrieving injury type" });
            }
        }

        [HttpPost("injury-types")]
        public async Task<IActionResult> CreateInjuryType([FromBody] CreateInjuryTypeDto dto)
        {
            try
            {
                // Check for duplicate name
                if (await _context.FractureTypes.AnyAsync(f => f.Name.ToLower() == dto.Name.ToLower()))
                    return BadRequest(new { message = "Injury type with this name already exists" });

                // Check for duplicate code
                if (await _context.FractureTypes.AnyAsync(f => f.Code.ToLower() == dto.Code.ToLower()))
                    return BadRequest(new { message = "Injury type with this code already exists" });

                var injuryType = new FractureType
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Code = dto.Code,
                    Category = dto.Category,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FractureTypes.Add(injuryType);
                await _context.SaveChangesAsync();

                var injuryTypeDto = new InjuryTypeDto
                {
                    Id = injuryType.Id,
                    Name = injuryType.Name,
                    Description = injuryType.Description,
                    Code = injuryType.Code,
                    Category = injuryType.Category,
                    IsActive = injuryType.IsActive,
                    CreatedAt = injuryType.CreatedAt,
                    UpdatedAt = injuryType.UpdatedAt
                };

                return CreatedAtAction(nameof(GetInjuryType), new { id = injuryType.Id }, injuryTypeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating injury type");
                return StatusCode(500, new { message = "Error creating injury type" });
            }
        }

        [HttpPut("injury-types/{id}")]
        public async Task<IActionResult> UpdateInjuryType(int id, [FromBody] UpdateInjuryTypeDto dto)
        {
            try
            {
                var injuryType = await _context.FractureTypes.FindAsync(id);
                if (injuryType == null)
                    return NotFound(new { message = "Injury type not found" });

                // Check for duplicate name (excluding current injury type)
                if (!string.IsNullOrEmpty(dto.Name) && 
                    await _context.FractureTypes.AnyAsync(f => f.Name.ToLower() == dto.Name.ToLower() && f.Id != id))
                    return BadRequest(new { message = "Injury type with this name already exists" });

                // Check for duplicate code (excluding current injury type)
                if (!string.IsNullOrEmpty(dto.Code) && 
                    await _context.FractureTypes.AnyAsync(f => f.Code.ToLower() == dto.Code.ToLower() && f.Id != id))
                    return BadRequest(new { message = "Injury type with this code already exists" });

                // Update fields
                if (!string.IsNullOrEmpty(dto.Name)) injuryType.Name = dto.Name;
                if (dto.Description != null) injuryType.Description = dto.Description;
                if (!string.IsNullOrEmpty(dto.Code)) injuryType.Code = dto.Code;
                if (!string.IsNullOrEmpty(dto.Category)) injuryType.Category = dto.Category;
                if (dto.IsActive.HasValue) injuryType.IsActive = dto.IsActive.Value;
                
                injuryType.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Injury type updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating injury type {Id}", id);
                return StatusCode(500, new { message = "Error updating injury type" });
            }
        }

        [HttpDelete("injury-types/{id}")]
        public async Task<IActionResult> DeleteInjuryType(int id)
        {
            try
            {
                var injuryType = await _context.FractureTypes.FindAsync(id);
                if (injuryType == null)
                    return NotFound(new { message = "Injury type not found" });

                // Check if injury type is being used by injuries
                var hasInjuries = await _context.Injuries.AnyAsync(i => i.FractureTypeId == id);

                if (hasInjuries)
                    return BadRequest(new { message = "Cannot delete injury type. It is being used by injury records." });

                _context.FractureTypes.Remove(injuryType);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Injury type deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting injury type {Id}", id);
                return StatusCode(500, new { message = "Error deleting injury type" });
            }
        }

        #endregion

        #region Shifts CRUD

        [HttpGet("shifts")]
        public async Task<IActionResult> GetShifts()
        {
            try
            {
                var shifts = await _context.Shifts
                    .OrderBy(s => s.StartTime)
                    .Select(s => new ShiftDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Code = s.Code,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(shifts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shifts");
                return StatusCode(500, new { message = "Error retrieving shifts" });
            }
        }

        [HttpGet("shifts/{id}")]
        public async Task<IActionResult> GetShift(int id)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                    return NotFound(new { message = "Shift not found" });

                var shiftDto = new ShiftDto
                {
                    Id = shift.Id,
                    Name = shift.Name,
                    Description = shift.Description,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    Code = shift.Code,
                    IsActive = shift.IsActive,
                    CreatedAt = shift.CreatedAt,
                    UpdatedAt = shift.UpdatedAt
                };

                return Ok(shiftDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving shift {Id}", id);
                return StatusCode(500, new { message = "Error retrieving shift" });
            }
        }

        [HttpPost("shifts")]
        public async Task<IActionResult> CreateShift([FromBody] CreateShiftDto dto)
        {
            try
            {
                // Check for duplicate name
                if (await _context.Shifts.AnyAsync(s => s.Name.ToLower() == dto.Name.ToLower()))
                    return BadRequest(new { message = "Shift with this name already exists" });

                // Check for duplicate code
                if (await _context.Shifts.AnyAsync(s => s.Code.ToLower() == dto.Code.ToLower()))
                    return BadRequest(new { message = "Shift with this code already exists" });

                // Validate time range
                if (dto.StartTime >= dto.EndTime)
                    return BadRequest(new { message = "Start time must be before end time" });

                var shift = new Shift
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    Code = dto.Code,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Shifts.Add(shift);
                await _context.SaveChangesAsync();

                var shiftDto = new ShiftDto
                {
                    Id = shift.Id,
                    Name = shift.Name,
                    Description = shift.Description,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    Code = shift.Code,
                    IsActive = shift.IsActive,
                    CreatedAt = shift.CreatedAt,
                    UpdatedAt = shift.UpdatedAt
                };

                return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, shiftDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shift");
                return StatusCode(500, new { message = "Error creating shift" });
            }
        }

        [HttpPut("shifts/{id}")]
        public async Task<IActionResult> UpdateShift(int id, [FromBody] UpdateShiftDto dto)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                    return NotFound(new { message = "Shift not found" });

                // Check for duplicate name (excluding current shift)
                if (!string.IsNullOrEmpty(dto.Name) && 
                    await _context.Shifts.AnyAsync(s => s.Name.ToLower() == dto.Name.ToLower() && s.Id != id))
                    return BadRequest(new { message = "Shift with this name already exists" });

                // Check for duplicate code (excluding current shift)
                if (!string.IsNullOrEmpty(dto.Code) && 
                    await _context.Shifts.AnyAsync(s => s.Code.ToLower() == dto.Code.ToLower() && s.Id != id))
                    return BadRequest(new { message = "Shift with this code already exists" });

                // Validate time range if both times are provided
                var startTime = dto.StartTime ?? shift.StartTime;
                var endTime = dto.EndTime ?? shift.EndTime;
                
                if (startTime >= endTime)
                    return BadRequest(new { message = "Start time must be before end time" });

                // Update fields
                if (!string.IsNullOrEmpty(dto.Name)) shift.Name = dto.Name;
                if (dto.Description != null) shift.Description = dto.Description;
                if (!string.IsNullOrEmpty(dto.Code)) shift.Code = dto.Code;
                if (dto.StartTime.HasValue) shift.StartTime = dto.StartTime.Value;
                if (dto.EndTime.HasValue) shift.EndTime = dto.EndTime.Value;
                if (dto.IsActive.HasValue) shift.IsActive = dto.IsActive.Value;
                
                shift.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Shift updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shift {Id}", id);
                return StatusCode(500, new { message = "Error updating shift" });
            }
        }

        [HttpDelete("shifts/{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            try
            {
                var shift = await _context.Shifts.FindAsync(id);
                if (shift == null)
                    return NotFound(new { message = "Shift not found" });

                // Check if shift is being used by users
                var hasUsers = await _context.Users.AnyAsync(u => u.ShiftId == id);
                
                // Check if shift name is being used in reports (Reports use WorkShift as string)
                var hasReports = await _context.Reports.AnyAsync(r => r.WorkShift == shift.Name);

                if (hasUsers || hasReports)
                    return BadRequest(new { message = "Cannot delete shift. It is being used by users or reports." });

                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Shift deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shift {Id}", id);
                return StatusCode(500, new { message = "Error deleting shift" });
            }
        }

        #endregion
    }
}