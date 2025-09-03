using HSEBackend.DTOs;
using HSEBackend.Models;
using HSEBackend.Data;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context,
            ILogger<UsersController> logger,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get all users with pagination and filtering
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? department = null,
            [FromQuery] string? zone = null)
        {
            try
            {
                // Use direct database context query like validateCompanyId endpoint
                var query = _context.Users.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => u.FirstName.Contains(search) || 
                                           u.LastName.Contains(search) || 
                                           u.Email.Contains(search));
                }

                if (!string.IsNullOrEmpty(department))
                    query = query.Where(u => u.Department == department);

                if (!string.IsNullOrEmpty(zone))
                    query = query.Where(u => u.Zone == zone);

                // Get all users first to apply role-based sorting
                var allUsers = await query.ToListAsync();
                
                // Get roles for all users in batch
                var currentUserId = User.FindFirst("UserId")?.Value;
                var usersWithRoles = new List<(ApplicationUser User, string Role)>();
                
                foreach (var user in allUsers)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    var userRole = userRoles.FirstOrDefault() ?? "Profil";
                    usersWithRoles.Add((user, userRole));
                }
                
                // Apply role-based sorting with current user first
                var sortedUsersWithRoles = usersWithRoles
                    .OrderBy(ur => ur.User.Id == currentUserId ? 0 : 1) // Current user first
                    .ThenBy(ur => GetRolePriority(ur.Role)) // Role priority: Admin(1) -> HSE(2) -> Profil(3)
                    .ThenBy(ur => ur.User.FirstName ?? "") // Then by first name
                    .ThenBy(ur => ur.User.LastName ?? "") // Then by last name
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                var users = sortedUsersWithRoles.Select(ur => ur.User).ToList();

                // Debug log to check if CompanyId exists in database
                foreach (var user in users)
                {
                    Console.WriteLine($"Direct DB Query - User: {user.Email}, CompanyId: '{user.CompanyId}', UserName: '{user.UserName}'");
                }

                var userDtos = new List<UserDto>();

                foreach (var user in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    var userRole = userRoles.FirstOrDefault() ?? "Profil";

                    // Filter by role if specified
                    if (!string.IsNullOrEmpty(role) && userRole != role)
                        continue;

                    var companyId = user.CompanyId;
                    if (string.IsNullOrEmpty(companyId))
                    {
                        companyId = user.UserName ?? "";
                    }
                    
                    Console.WriteLine($"Mapping user {user.Email}: CompanyId='{user.CompanyId}' -> Final CompanyId='{companyId}'");

                    userDtos.Add(new UserDto
                    {
                        Id = user.Id,
                        CompanyId = companyId,
                        Email = user.Email!,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        Role = userRole,
                        Department = user.Department ?? "",
                        Zone = user.Zone ?? "",
                        Position = user.Position ?? "",
                        DateOfBirth = user.DateOfBirth,
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        Avatar = user.Avatar,
                        AccountCreatedAt = user.AccountCreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        LastActivityAt = user.LastActivityAt,
                        IsOnline = user.IsOnline,
                        CurrentStatus = user.CurrentStatus,
                        IsActive = user.IsActive
                    });
                }

                var totalCount = allUsers.Count;

                return Ok(new
                {
                    users = userDtos,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { message = "Error retrieving users" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                // Use direct database context query
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                var roles = await _userManager.GetRolesAsync(user);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    CompanyId = !string.IsNullOrEmpty(user.CompanyId) ? user.CompanyId : user.UserName ?? "",
                    Email = user.Email!,
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Role = roles.FirstOrDefault() ?? "Profil",
                    Department = user.Department ?? "",
                    Zone = user.Zone ?? "",
                    Position = user.Position ?? "",
                    DateOfBirth = user.DateOfBirth,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Avatar = user.Avatar
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {Id}", id);
                return StatusCode(500, new { message = "Error retrieving user" });
            }
        }

        /// <summary>
        /// Create new user with enhanced role-based processing
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                _logger.LogInformation($"📥 Backend: Received CreateUser request");
                _logger.LogInformation($"🔐 Backend: User authenticated: {User.Identity.IsAuthenticated}");
                if (User.Identity.IsAuthenticated)
                {
                    _logger.LogInformation($"🔐 Backend: User email: {User.Identity.Name}");
                    _logger.LogInformation($"🔐 Backend: User roles: {string.Join(", ", User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value))}");
                }
                _logger.LogInformation($"📥 Backend: DTO Email: '{dto.Email}' (null: {dto.Email == null})");
                _logger.LogInformation($"📥 Backend: DTO FullName: '{dto.FullName}' (null: {dto.FullName == null})");
                _logger.LogInformation($"📥 Backend: DTO Role: '{dto.Role}' (null: {dto.Role == null})");
                _logger.LogInformation($"📥 Backend: DTO CompanyId: '{dto.CompanyId}' (null: {dto.CompanyId == null})");
                _logger.LogInformation($"📥 Backend: DTO Department: '{dto.Department}' (null: {dto.Department == null})");
                _logger.LogInformation($"📥 Backend: DTO Position: '{dto.Position}' (null: {dto.Position == null})");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"❌ Backend: Model validation failed: {string.Join(", ", errors)}");
                    
                    // Create detailed error dictionary
                    var errorDict = new Dictionary<string, List<string>>();
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        if (state.Errors.Count > 0)
                        {
                            var fieldErrors = state.Errors.Select(e => e.ErrorMessage).ToList();
                            errorDict[key] = fieldErrors;
                            _logger.LogWarning($"❌ Backend: Field '{key}' errors: {string.Join(", ", fieldErrors)}");
                        }
                    }
                    
                    return BadRequest(new { 
                        message = "Validation failed", 
                        errors = errorDict,
                        fields = errorDict.Keys.ToArray()
                    });
                }

                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                    return BadRequest(new { message = "User with this email already exists" });

                // Parse full name into first and last names
                var nameParts = dto.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    FirstName = firstName,
                    LastName = lastName,
                    CompanyId = dto.CompanyId,
                    Department = dto.Department,
                    Zone = dto.Zone,
                    Position = dto.Position,
                    LocalJobTitle = "",
                    LaborIndicator = "",
                    DateOfBirth = dto.DateOfBirth ?? new DateTime(1990, 1, 1),
                    EmailConfirmed = true,
                    AccountCreatedAt = DateTime.UtcNow,
                    IsOnline = false,
                    CurrentStatus = "Offline",
                    IsActive = true
                };

                IdentityResult result;
                string? generatedPassword = null;
                
                // Smart role-based processing
                if (dto.Role == "Profil")
                {
                    // Profile users: no password, cannot login
                    result = await _userManager.CreateAsync(user);
                }
                else
                {
                    // HSE/Admin users: generate secure password
                    generatedPassword = GenerateSecurePassword();
                    result = await _userManager.CreateAsync(user, generatedPassword);
                }
                if (!result.Succeeded)
                {
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"User creation failed: {errorMessages}");
                    return BadRequest(new { message = $"User creation failed: {errorMessages}" });
                }

                // Add role
                var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
                if (!roleResult.Succeeded)
                {
                    // Cleanup user if role assignment fails
                    await _userManager.DeleteAsync(user);
                    var roleErrorMessages = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Role assignment failed: {roleErrorMessages}");
                    return BadRequest(new { message = $"Role assignment failed: {roleErrorMessages}" });
                }

                // Send appropriate email notification
                await _emailService.SendUserCreationEmailAsync(
                    user.Email!, 
                    user.FirstName ?? "", 
                    user.LastName ?? "", 
                    dto.Role, 
                    generatedPassword, 
                    user.CompanyId, 
                    user.Department, 
                    user.Position
                );

                _logger.LogInformation($"User created successfully: {user.Email} with role: {dto.Role}");
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { 
                    id = user.Id, 
                    message = "User created successfully",
                    role = dto.Role,
                    passwordGenerated = generatedPassword != null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "Error creating user" });
            }
        }

        /// <summary>
        /// Test endpoint to verify Company ID routing works
        /// </summary>
        [HttpGet("company/{companyId}/test")]
        [Authorize]
        public async Task<IActionResult> TestCompanyIdRouting(string companyId)
        {
            try
            {
                _logger.LogInformation($"TestCompanyIdRouting called with Company ID: '{companyId}'");
                return Ok(new { message = $"Company ID routing works for: {companyId}", receivedId = companyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test endpoint");
                return StatusCode(500, new { message = "Test endpoint error" });
            }
        }

        /// <summary>
        /// Update user profile information (for profile editing modal) - uses Company ID
        /// </summary>
        [HttpPut("company/{companyId}/profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile(string companyId, [FromForm] UpdateUserProfileDto dto)
        {
            try
            {
                _logger.LogInformation($"UpdateUserProfile called with Company ID: '{companyId}'");
                _logger.LogInformation($"DTO FullName: '{dto.FullName}' (null: {dto.FullName == null})");
                _logger.LogInformation($"DTO Position: '{dto.Position}' (null: {dto.Position == null})");
                _logger.LogInformation($"DTO Department: '{dto.Department}' (null: {dto.Department == null})");
                _logger.LogInformation($"DTO DateOfBirth: '{dto.DateOfBirth}' (null: {dto.DateOfBirth == null})");
                
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Log all Company IDs in database for debugging
                var allCompanyIds = await _context.Users
                    .Select(u => u.CompanyId)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToListAsync();
                _logger.LogInformation($"All Company IDs in database: [{string.Join(", ", allCompanyIds)}]");

                // Find user by company ID
                var user = await _context.Users.FirstOrDefaultAsync(u => u.CompanyId == companyId);
                _logger.LogInformation($"User found for Company ID '{companyId}': {user != null}");
                
                if (user == null)
                {
                    _logger.LogWarning($"No user found with Company ID: '{companyId}'");
                    return NotFound(new { message = $"User with Company ID '{companyId}' not found" });
                }

                _logger.LogInformation($"Before update - User Position: '{user.Position}', Department: '{user.Department}', FirstName: '{user.FirstName}', LastName: '{user.LastName}'");

                // Allow users to update their own profile or HSE/Admin to update any
                if (currentUserId != user.Id && currentUserRole != "Admin" && currentUserRole != "HSE")
                {
                    return StatusCode(403, new { message = "You can only update your own profile or must be HSE/Admin" });
                }

                // Update full name (split into first and last names)
                if (!string.IsNullOrEmpty(dto.FullName))
                {
                    var nameParts = dto.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var newFirstName = nameParts.Length > 0 ? nameParts[0] : "";
                    var newLastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
                    _logger.LogInformation($"Updating FullName from '{user.FirstName} {user.LastName}' to '{newFirstName} {newLastName}'");
                    user.FirstName = newFirstName;
                    user.LastName = newLastName;
                }

                // Update date of birth
                if (dto.DateOfBirth.HasValue)
                    user.DateOfBirth = dto.DateOfBirth.Value;

                // Update position and department (allow clearing by setting to empty string)
                if (dto.Position != null)
                {
                    _logger.LogInformation($"Updating Position from '{user.Position}' to '{dto.Position}'");
                    user.Position = dto.Position;
                }

                if (dto.Department != null)
                {
                    _logger.LogInformation($"Updating Department from '{user.Department}' to '{dto.Department}'");
                    user.Department = dto.Department;
                }

                // Handle avatar upload
                if (dto.Avatar != null && dto.Avatar.Length > 0)
                {
                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(dto.Avatar.ContentType.ToLower()))
                    {
                        return BadRequest(new { message = "Invalid file type. Only JPG, PNG, and GIF are allowed." });
                    }

                    // Validate file size (2MB max)
                    if (dto.Avatar.Length > 2 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "File size must be less than 2MB." });
                    }

                    // Save avatar (for now, we'll just store the filename - in production you'd save to blob storage)
                    var fileName = $"{user.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(dto.Avatar.FileName)}";
                    var uploadsPath = Path.Combine("wwwroot", "uploads", "avatars");
                    Directory.CreateDirectory(uploadsPath);
                    var filePath = Path.Combine(uploadsPath, fileName);
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.Avatar.CopyToAsync(stream);
                    }
                    
                    user.Avatar = $"/uploads/avatars/{fileName}";
                }

                _logger.LogInformation($"Before UserManager.UpdateAsync - Department: '{user.Department}', Position: '{user.Position}'");
                
                var result = await _userManager.UpdateAsync(user);
                
                _logger.LogInformation($"UserManager.UpdateAsync result: Success={result.Succeeded}");
                
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Error updating user profile", errors = result.Errors });
                }

                _logger.LogInformation($"After update - User Position: '{user.Position}', Department: '{user.Department}', FirstName: '{user.FirstName}', LastName: '{user.LastName}'");

                return Ok(new { message = "User profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for Company ID {CompanyId}", companyId);
                return StatusCode(500, new { message = "Error updating user profile" });
            }
        }

        /// <summary>
        /// Update user information (legacy endpoint)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Allow users to update their own profile or HSE/Admin to update any
                if (currentUserId != id && currentUserRole != "Admin" && currentUserRole != "HSE")
                {
                    return StatusCode(403, new { message = "You can only update your own profile or must be HSE/Admin" });
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                if (!string.IsNullOrEmpty(dto.FirstName))
                    user.FirstName = dto.FirstName;

                if (!string.IsNullOrEmpty(dto.LastName))
                    user.LastName = dto.LastName;

                // Always update Department and Position, even if empty (allow clearing)
                if (dto.Department != null)
                {
                    _logger.LogInformation($"Updating Department from '{user.Department}' to '{dto.Department}'");
                    user.Department = dto.Department;
                }

                if (!string.IsNullOrEmpty(dto.Zone))
                    user.Zone = dto.Zone;

                if (dto.Position != null)
                {
                    _logger.LogInformation($"Updating Position from '{user.Position}' to '{dto.Position}'");
                    user.Position = dto.Position;
                }

                if (dto.DateOfBirth.HasValue)
                    user.DateOfBirth = dto.DateOfBirth.Value;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Error updating user", errors = result.Errors });
                }

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id}", id);
                return StatusCode(500, new { message = "Error updating user" });
            }
        }

        /// <summary>
        /// Delete user - uses Company ID
        /// </summary>
        [HttpDelete("company/{companyId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string companyId)
        {
            try
            {
                // Find user by company ID
                var user = await _context.Users.FirstOrDefaultAsync(u => u.CompanyId == companyId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Error deleting user", errors = result.Errors });
                }

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with Company ID {CompanyId}", companyId);
                return StatusCode(500, new { message = "Error deleting user" });
            }
        }

        /// <summary>
        /// Update user role with smart email notifications
        /// </summary>
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateUserRoleDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                if (!await _roleManager.RoleExistsAsync(dto.Role))
                    return BadRequest(new { message = "Role does not exist" });

                // Get current role for comparison
                var currentRoles = await _userManager.GetRolesAsync(user);
                var oldRole = currentRoles.FirstOrDefault() ?? "Unknown";
                
                _logger.LogInformation($"Changing user {user.Email} role from {oldRole} to {dto.Role}");

                // Remove current roles
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Add new role
                await _userManager.AddToRoleAsync(user, dto.Role);

                // Handle password and email based on role change
                string? newPassword = null;
                
                if (dto.Role == "Profil" && (oldRole == "HSE" || oldRole == "Admin"))
                {
                    // Downgrade to Profile: Remove password (disable login)
                    user.PasswordHash = null;
                    await _userManager.UpdateAsync(user);
                    
                    // Send downgrade email
                    await _emailService.SendRoleChangeEmailAsync(
                        user.Email!, 
                        user.FirstName ?? "", 
                        user.LastName ?? "", 
                        oldRole, 
                        dto.Role, 
                        null, 
                        user.CompanyId
                    );
                }
                else if ((dto.Role == "HSE" || dto.Role == "Admin") && oldRole == "Profil")
                {
                    // Upgrade from Profile: Generate new password
                    newPassword = GenerateSecurePassword();
                    await _userManager.RemovePasswordAsync(user);
                    await _userManager.AddPasswordAsync(user, newPassword);
                    
                    // Send upgrade email
                    await _emailService.SendRoleChangeEmailAsync(
                        user.Email!, 
                        user.FirstName ?? "", 
                        user.LastName ?? "", 
                        oldRole, 
                        dto.Role, 
                        newPassword, 
                        user.CompanyId
                    );
                }
                else if ((dto.Role == "HSE" || dto.Role == "Admin") && (oldRole == "HSE" || oldRole == "Admin"))
                {
                    // Role change between login roles: Generate new password
                    newPassword = GenerateSecurePassword();
                    await _userManager.RemovePasswordAsync(user);
                    await _userManager.AddPasswordAsync(user, newPassword);
                    
                    // Send role change email
                    await _emailService.SendRoleChangeEmailAsync(
                        user.Email!, 
                        user.FirstName ?? "", 
                        user.LastName ?? "", 
                        oldRole, 
                        dto.Role, 
                        newPassword, 
                        user.CompanyId
                    );
                }

                return Ok(new { 
                    message = "User role updated successfully", 
                    oldRole = oldRole,
                    newRole = dto.Role,
                    passwordGenerated = newPassword != null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role for {Id}", id);
                return StatusCode(500, new { message = "Error updating user role" });
            }
        }

        /// <summary>
        /// Change user password - uses Company ID
        /// </summary>
        [HttpPut("company/{companyId}/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string companyId, [FromBody] ChangePasswordDto dto)
        {
            try
            {
                _logger.LogInformation($"ChangePassword called with Company ID: '{companyId}'");
                _logger.LogInformation($"DTO CurrentPassword provided: {!string.IsNullOrEmpty(dto.CurrentPassword)}");
                _logger.LogInformation($"DTO NewPassword provided: {!string.IsNullOrEmpty(dto.NewPassword)}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"ChangePassword model validation failed: {string.Join(", ", errors)}");
                    return BadRequest(new { message = "Validation failed", errors = errors });
                }
                var currentUserId = User.FindFirst("UserId")?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Find user by company ID
                var user = await _context.Users.FirstOrDefaultAsync(u => u.CompanyId == companyId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                // Allow users to change their own password or Admin to change any
                if (currentUserId != user.Id && currentUserRole != "Admin")
                {
                    return StatusCode(403, new { message = "You can only change your own password or must be Admin" });
                }

                _logger.LogInformation($"Attempting to change password for user: {user.Email}");
                var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
                
                if (!result.Succeeded)
                {
                    var errorMessages = result.Errors.Select(e => e.Description);
                    _logger.LogWarning($"Password change failed for {user.Email}: {string.Join(", ", errorMessages)}");
                    return BadRequest(new { message = "Error changing password", errors = result.Errors.Select(e => e.Description) });
                }
                
                _logger.LogInformation($"Password changed successfully for user: {user.Email}");

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user with Company ID {CompanyId}", companyId);
                return StatusCode(500, new { message = "Error changing password" });
            }
        }

        /// <summary>
        /// Admin reset user password - uses Company ID (no current password required)
        /// </summary>
        [HttpPut("company/{companyId}/reset-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminResetPassword(string companyId, [FromBody] AdminResetPasswordDto dto)
        {
            try
            {
                _logger.LogInformation($"AdminResetPassword called with Company ID: '{companyId}'");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning($"AdminResetPassword model validation failed: {string.Join(", ", errors)}");
                    return BadRequest(new { message = "Validation failed", errors = errors });
                }

                // Find user by company ID
                var user = await _context.Users.FirstOrDefaultAsync(u => u.CompanyId == companyId);
                if (user == null)
                {
                    _logger.LogWarning($"AdminResetPassword: No user found with Company ID: '{companyId}'");
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogInformation($"Admin resetting password for user: {user.Email}");

                // Remove current password and set new one (admin override)
                await _userManager.RemovePasswordAsync(user);
                var result = await _userManager.AddPasswordAsync(user, dto.NewPassword);
                
                if (!result.Succeeded)
                {
                    var errorMessages = result.Errors.Select(e => e.Description);
                    _logger.LogWarning($"Admin password reset failed for {user.Email}: {string.Join(", ", errorMessages)}");
                    return BadRequest(new { message = "Error resetting password", errors = result.Errors.Select(e => e.Description) });
                }
                
                _logger.LogInformation($"Password reset successfully by admin for user: {user.Email}");

                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user with Company ID {CompanyId}", companyId);
                return StatusCode(500, new { message = "Error resetting password" });
            }
        }

        /// <summary>
        /// Get all departments
        /// </summary>
        [HttpGet("departments")]
        [Authorize]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.Department))
                    .Select(u => u.Department)
                    .Distinct()
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
        /// Get all zones
        /// </summary>
        [HttpGet("zones")]
        [Authorize]
        public async Task<IActionResult> GetZones()
        {
            try
            {
                var zones = await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.Zone))
                    .Select(u => u.Zone)
                    .Distinct()
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
        /// Get all available roles
        /// </summary>
        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return StatusCode(500, new { message = "Error retrieving roles" });
            }
        }

        /// <summary>
        /// Assign CompanyId to users who don't have one
        /// </summary>
        [HttpPost("assign-company-ids")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignCompanyIds()
        {
            try
            {
                var usersWithoutCompanyId = await _userManager.Users
                    .Where(u => string.IsNullOrEmpty(u.CompanyId))
                    .ToListAsync();

                if (!usersWithoutCompanyId.Any())
                {
                    return Ok(new { message = "All users already have CompanyId assigned" });
                }

                var lastCompanyId = await _userManager.Users
                    .Where(u => !string.IsNullOrEmpty(u.CompanyId) && u.CompanyId.StartsWith("TE"))
                    .OrderByDescending(u => u.CompanyId)
                    .Select(u => u.CompanyId)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (!string.IsNullOrEmpty(lastCompanyId) && lastCompanyId.StartsWith("TE"))
                {
                    if (int.TryParse(lastCompanyId.Substring(2), out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                var assignedCount = 0;
                foreach (var user in usersWithoutCompanyId)
                {
                    // Skip admin users, they should have special CompanyIds
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                    {
                        user.CompanyId = $"ADMIN{nextNumber:D3}";
                    }
                    else if (roles.Contains("HSE"))
                    {
                        user.CompanyId = $"HSE{nextNumber:D3}";
                    }
                    else
                    {
                        user.CompanyId = $"TE{nextNumber:D4}";
                    }

                    await _userManager.UpdateAsync(user);
                    assignedCount++;
                    nextNumber++;
                }

                return Ok(new { message = $"Assigned CompanyId to {assignedCount} users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning company IDs");
                return StatusCode(500, new { message = "Error assigning company IDs" });
            }
        }
        
        private string GenerateSecurePassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string special = "!@#$%^&*";
            
            var random = new Random();
            var password = new System.Text.StringBuilder();
            
            // Ensure at least one character from each category
            password.Append(uppercase[random.Next(uppercase.Length)]);
            password.Append(lowercase[random.Next(lowercase.Length)]);
            password.Append(numbers[random.Next(numbers.Length)]);
            password.Append(special[random.Next(special.Length)]);
            
            // Fill the rest with random characters
            string allChars = uppercase + lowercase + numbers + special;
            for (int i = 4; i < 12; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }
            
            // Shuffle the password
            return new string(password.ToString().OrderBy(x => random.Next()).ToArray());
        }
        
        private int GetRolePriority(string role)
        {
            return role switch
            {
                "Admin" => 1,
                "HSE" => 2,
                "Profil" => 3,
                _ => 4
            };
        }

        /// <summary>
        /// Temporary endpoint to update admin user Company ID
        /// </summary>
        [HttpPost("update-admin-company-id")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdminCompanyId([FromBody] UpdateCompanyIdDto dto)
        {
            try
            {
                _logger.LogInformation($"UpdateAdminCompanyId called for email: {dto.Email} with Company ID: {dto.CompanyId}");

                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with email: {dto.Email}");
                    return NotFound(new { message = "User not found" });
                }

                // Update Company ID
                user.CompanyId = dto.CompanyId;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errorMessages = result.Errors.Select(e => e.Description);
                    _logger.LogWarning($"Failed to update Company ID for {dto.Email}: {string.Join(", ", errorMessages)}");
                    return BadRequest(new { message = "Error updating Company ID", errors = result.Errors.Select(e => e.Description) });
                }

                _logger.LogInformation($"Successfully updated Company ID for {dto.Email} to {dto.CompanyId}");
                return Ok(new { message = $"Successfully updated Company ID for {dto.Email} to {dto.CompanyId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Company ID for email {Email}", dto.Email);
                return StatusCode(500, new { message = "Error updating Company ID" });
            }
        }

        /// <summary>
        /// Temporary endpoint to assign test avatar to admin user
        /// </summary>
        [HttpPost("assign-test-avatar")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignTestAvatar([FromBody] AssignTestAvatarDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.Avatar = dto.AvatarFilename;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update avatar", errors = result.Errors });
                }

                _logger.LogInformation($"Test avatar assigned to {dto.Email}: {dto.AvatarFilename}");
                return Ok(new { message = $"Test avatar assigned: {dto.AvatarFilename}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning test avatar");
                return StatusCode(500, new { message = "Error assigning test avatar" });
            }
        }

        // =======================
        // ZONE MANAGEMENT SECTION  
        // =======================

        /// <summary>
        /// Get user's assigned zones (for HSE users only)
        /// </summary>
        [HttpGet("{userId}/zones")]
        [Authorize(Roles = "Admin,HSE")]
        public async Task<IActionResult> GetUserZones(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("HSE"))
                    return BadRequest(new { message = "Zone assignments are only available for HSE users" });

                var zones = await _context.HSEZoneResponsibilities
                    .Where(hzr => hzr.HSEUserId == userId && hzr.IsActive)
                    .Include(hzr => hzr.Zone)
                    .Select(hzr => new ZoneAssignmentDto
                    {
                        ZoneId = hzr.ZoneId,
                        ZoneName = hzr.Zone.Name,
                        ZoneCode = hzr.Zone.Code,
                        AssignedAt = hzr.AssignedAt,
                        IsActive = hzr.IsActive
                    })
                    .ToListAsync();

                return Ok(zones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user zones for {UserId}", userId);
                return StatusCode(500, new { message = "Error retrieving user zones" });
            }
        }

        /// <summary>
        /// Assign zone to an HSE user
        /// </summary>
        [HttpPost("{userId}/zones/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignZoneToUser(string userId, [FromBody] AssignZoneDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("HSE"))
                    return BadRequest(new { message = "Zones can only be assigned to HSE users" });

                var zone = await _context.Zones.FindAsync(dto.ZoneId);
                if (zone == null)
                    return NotFound(new { message = "Zone not found" });

                // Check if zone is already assigned to ANY HSE user (exclusivity constraint)
                var existingZoneAssignment = await _context.HSEZoneResponsibilities
                    .Include(hzr => hzr.HSEUser)
                    .FirstOrDefaultAsync(hzr => hzr.ZoneId == dto.ZoneId && hzr.IsActive);

                if (existingZoneAssignment != null && existingZoneAssignment.HSEUserId != userId)
                {
                    return BadRequest(new { message = $"Zone '{zone.Name}' is already assigned to {existingZoneAssignment.HSEUser.FirstName} {existingZoneAssignment.HSEUser.LastName}. A zone can only be assigned to one HSE agent." });
                }

                // Check if this specific user is already assigned to this zone
                var existingUserAssignment = await _context.HSEZoneResponsibilities
                    .FirstOrDefaultAsync(hzr => hzr.HSEUserId == userId && hzr.ZoneId == dto.ZoneId);

                if (existingUserAssignment != null)
                {
                    if (existingUserAssignment.IsActive)
                        return BadRequest(new { message = "Zone is already assigned to this user" });
                    
                    // Reactivate existing assignment
                    existingUserAssignment.IsActive = true;
                    existingUserAssignment.AssignedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new assignment
                    var newAssignment = new HSEZoneResponsibility
                    {
                        HSEUserId = userId,
                        ZoneId = dto.ZoneId,
                        AssignedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.HSEZoneResponsibilities.Add(newAssignment);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Zone '{zone.Name}' assigned to user successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning zone to user {UserId}", userId);
                return StatusCode(500, new { message = "Error assigning zone to user" });
            }
        }

        /// <summary>
        /// Remove zone assignment from an HSE user
        /// </summary>
        [HttpDelete("{userId}/zones/{zoneId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveZoneFromUser(string userId, int zoneId)
        {
            try
            {
                var assignment = await _context.HSEZoneResponsibilities
                    .Include(hzr => hzr.Zone)
                    .FirstOrDefaultAsync(hzr => hzr.HSEUserId == userId && hzr.ZoneId == zoneId && hzr.IsActive);

                if (assignment == null)
                    return NotFound(new { message = "Zone assignment not found" });

                // Check if there are active delegations for this zone
                var activeDelegations = await _context.HSEZoneDelegations
                    .Where(hzd => hzd.FromHSEUserId == userId && hzd.ZoneId == zoneId && hzd.IsActive)
                    .ToListAsync();

                if (activeDelegations.Any())
                {
                    return BadRequest(new { message = "Cannot remove zone assignment while there are active delegations. Please end all delegations first." });
                }

                assignment.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Zone '{assignment.Zone.Name}' removed from user successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing zone from user {UserId}", userId);
                return StatusCode(500, new { message = "Error removing zone from user" });
            }
        }

        /// <summary>
        /// Get all available zones
        /// </summary>
        [HttpGet("available-zones")]
        [Authorize(Roles = "Admin,HSE")]
        public async Task<IActionResult> GetAvailableZones()
        {
            try
            {
                _logger.LogInformation("🔍 GetAvailableZones called");
                
                var zones = await _context.Zones
                    .Select(z => new
                    {
                        id = z.Id,
                        name = z.Name,
                        code = z.Code,
                        description = z.Description
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ Found {zones.Count} zones in database");
                foreach (var zone in zones)
                {
                    _logger.LogInformation($"  - Zone: {zone.name} ({zone.code})");
                }

                return Ok(zones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting available zones");
                return StatusCode(500, new { message = "Error retrieving available zones" });
            }
        }

        /// <summary>
        /// Get all HSE users (for delegation assignments)
        /// </summary>
        [HttpGet("hse-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetHSEUsers()
        {
            try
            {
                var hseUsers = new List<object>();

                var allUsers = await _userManager.Users.ToListAsync();
                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("HSE"))
                    {
                        hseUsers.Add(new
                        {
                            id = user.Id,
                            email = user.Email,
                            fullName = $"{user.FirstName} {user.LastName}".Trim(),
                            department = user.Department,
                            companyId = user.CompanyId
                        });
                    }
                }

                return Ok(hseUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting HSE users");
                return StatusCode(500, new { message = "Error retrieving HSE users" });
            }
        }

        /// <summary>
        /// Create zone delegation
        /// </summary>
        [HttpPost("zone-delegations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateZoneDelegation([FromBody] CreateZoneDelegationDto dto)
        {
            try
            {
                var currentAdminId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(currentAdminId))
                    return Unauthorized(new { message = "Unable to identify current admin user" });

                // Validate from HSE user
                var fromUser = await _userManager.FindByIdAsync(dto.FromHSEUserId);
                if (fromUser == null)
                    return NotFound(new { message = "From HSE user not found" });

                var fromUserRoles = await _userManager.GetRolesAsync(fromUser);
                if (!fromUserRoles.Contains("HSE"))
                    return BadRequest(new { message = "From user must be an HSE user" });

                // Validate to HSE user
                var toUser = await _userManager.FindByIdAsync(dto.ToHSEUserId);
                if (toUser == null)
                    return NotFound(new { message = "To HSE user not found" });

                var toUserRoles = await _userManager.GetRolesAsync(toUser);
                if (!toUserRoles.Contains("HSE"))
                    return BadRequest(new { message = "To user must be an HSE user" });

                // Validate zone
                var zone = await _context.Zones.FindAsync(dto.ZoneId);
                if (zone == null)
                    return NotFound(new { message = "Zone not found" });

                // Check if from user is responsible for this zone
                var zoneResponsibility = await _context.HSEZoneResponsibilities
                    .FirstOrDefaultAsync(hzr => hzr.HSEUserId == dto.FromHSEUserId && hzr.ZoneId == dto.ZoneId && hzr.IsActive);

                if (zoneResponsibility == null)
                    return BadRequest(new { message = "From HSE user is not responsible for this zone" });

                // Validate dates
                if (dto.EndDate <= dto.StartDate)
                    return BadRequest(new { message = "End date must be after start date" });

                if (dto.StartDate < DateTime.UtcNow.Date)
                    return BadRequest(new { message = "Start date cannot be in the past" });

                // Check for overlapping delegations from the same user
                var overlappingDelegation = await _context.HSEZoneDelegations
                    .Where(hzd => hzd.FromHSEUserId == dto.FromHSEUserId && 
                                  hzd.ZoneId == dto.ZoneId && 
                                  hzd.IsActive &&
                                  ((dto.StartDate >= hzd.StartDate && dto.StartDate <= hzd.EndDate) ||
                                   (dto.EndDate >= hzd.StartDate && dto.EndDate <= hzd.EndDate) ||
                                   (dto.StartDate <= hzd.StartDate && dto.EndDate >= hzd.EndDate)))
                    .FirstOrDefaultAsync();

                if (overlappingDelegation != null)
                    return BadRequest(new { message = "There is already an active delegation for this zone during the specified period" });

                // Check if zone is already delegated to ANY other HSE user during this period (exclusivity constraint)
                var existingZoneDelegation = await _context.HSEZoneDelegations
                    .Include(hzd => hzd.ToHSEUser)
                    .Where(hzd => hzd.ZoneId == dto.ZoneId && 
                                  hzd.IsActive &&
                                  ((dto.StartDate >= hzd.StartDate && dto.StartDate <= hzd.EndDate) ||
                                   (dto.EndDate >= hzd.StartDate && dto.EndDate <= hzd.EndDate) ||
                                   (dto.StartDate <= hzd.StartDate && dto.EndDate >= hzd.EndDate)))
                    .FirstOrDefaultAsync();

                if (existingZoneDelegation != null && existingZoneDelegation.ToHSEUserId != dto.ToHSEUserId)
                {
                    return BadRequest(new { message = $"Zone '{zone.Name}' is already delegated to {existingZoneDelegation.ToHSEUser.FirstName} {existingZoneDelegation.ToHSEUser.LastName} during this period. A zone can only be delegated to one HSE agent at a time." });
                }

                // Create delegation
                var delegation = new HSEZoneDelegation
                {
                    FromHSEUserId = dto.FromHSEUserId,
                    ToHSEUserId = dto.ToHSEUserId,
                    ZoneId = dto.ZoneId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Reason = dto.Reason,
                    CreatedByAdminId = currentAdminId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.HSEZoneDelegations.Add(delegation);
                await _context.SaveChangesAsync();

                // Send notifications to both parties
                try
                {
                    _logger.LogInformation("🔔 Attempting to send delegation creation notifications for delegation {DelegationId} (via UsersController)", delegation.Id);
                    await _notificationService.NotifyOnZoneDelegationCreatedAsync(delegation.Id);
                    _logger.LogInformation("✅ Successfully sent delegation creation notifications for delegation {DelegationId} (via UsersController)", delegation.Id);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the delegation creation
                    // The delegation was successfully created, notification failure shouldn't break the flow
                    _logger.LogError(ex, "❌ Failed to send delegation creation notifications for delegation {DelegationId} (via UsersController): {ErrorMessage}", delegation.Id, ex.Message);
                }

                return Ok(new { message = "Zone delegation created successfully", delegationId = delegation.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating zone delegation");
                return StatusCode(500, new { message = "Error creating zone delegation" });
            }
        }

        /// <summary>
        /// Get all zone delegations with filtering
        /// </summary>
        [HttpGet("zone-delegations")]
        [Authorize(Roles = "Admin,HSE")]
        public async Task<IActionResult> GetZoneDelegations(
            [FromQuery] bool activeOnly = true,
            [FromQuery] string? fromUserId = null,
            [FromQuery] string? toUserId = null)
        {
            try
            {
                var query = _context.HSEZoneDelegations
                    .Include(hzd => hzd.FromHSEUser)
                    .Include(hzd => hzd.ToHSEUser)
                    .Include(hzd => hzd.Zone)
                    .Include(hzd => hzd.CreatedByAdmin)
                    .AsQueryable();

                if (activeOnly)
                    query = query.Where(hzd => hzd.IsActive);

                if (!string.IsNullOrEmpty(fromUserId))
                    query = query.Where(hzd => hzd.FromHSEUserId == fromUserId);

                if (!string.IsNullOrEmpty(toUserId))
                    query = query.Where(hzd => hzd.ToHSEUserId == toUserId);

                var delegations = await query
                    .Select(hzd => new ZoneDelegationDto
                    {
                        Id = hzd.Id,
                        FromHSEUserName = $"{hzd.FromHSEUser.FirstName} {hzd.FromHSEUser.LastName}".Trim(),
                        FromHSEUserEmail = hzd.FromHSEUser.Email!,
                        ToHSEUserName = $"{hzd.ToHSEUser.FirstName} {hzd.ToHSEUser.LastName}".Trim(),
                        ToHSEUserEmail = hzd.ToHSEUser.Email!,
                        ZoneName = hzd.Zone.Name,
                        ZoneCode = hzd.Zone.Code,
                        StartDate = hzd.StartDate,
                        EndDate = hzd.EndDate,
                        Reason = hzd.Reason,
                        IsActive = hzd.IsActive,
                        CreatedAt = hzd.CreatedAt,
                        CreatedByAdminName = $"{hzd.CreatedByAdmin.FirstName} {hzd.CreatedByAdmin.LastName}".Trim()
                    })
                    .ToListAsync();

                return Ok(delegations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting zone delegations");
                return StatusCode(500, new { message = "Error retrieving zone delegations" });
            }
        }


        /// <summary>
        /// Deactivate (end) zone delegation
        /// </summary>
        [HttpDelete("zone-delegations/{delegationId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateZoneDelegation(int delegationId)
        {
            try
            {
                var delegation = await _context.HSEZoneDelegations
                    .Include(hzd => hzd.Zone)
                    .Include(hzd => hzd.FromHSEUser)
                    .Include(hzd => hzd.ToHSEUser)
                    .FirstOrDefaultAsync(hzd => hzd.Id == delegationId);

                if (delegation == null)
                    return NotFound(new { message = "Zone delegation not found" });

                if (!delegation.IsActive)
                    return BadRequest(new { message = "Zone delegation is already inactive" });

                delegation.IsActive = false;
                delegation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Zone delegation ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating zone delegation {DelegationId}", delegationId);
                return StatusCode(500, new { message = "Error ending zone delegation" });
            }
        }
        
    }
}