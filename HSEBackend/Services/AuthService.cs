using HSEBackend.DTOs;
using HSEBackend.Models;
using HSEBackend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HSEBackend.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly AppDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Check if user account is active
                if (!user.IsActive)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Your account has been deactivated. Please contact an administrator."
                    };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
                if (!result.Succeeded)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                var roles = await _userManager.GetRolesAsync(user);
                var token = GenerateJwtToken(user, roles);

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                user.IsOnline = true;
                user.CurrentStatus = "Online";
                await _userManager.UpdateAsync(user);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = roles.FirstOrDefault() ?? "Profil",
                        Department = user.Department ?? "",
                        Zone = user.Zone ?? "",
                        Position = user.Position ?? "",
                        CompanyId = user.CompanyId ?? "",
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        DateOfBirth = user.DateOfBirth,
                        DepartmentId = user.DepartmentId,
                        ZoneId = user.ZoneId,
                        ShiftId = user.ShiftId,
                        AccountCreatedAt = user.AccountCreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        LastActivityAt = user.LastActivityAt,
                        IsOnline = user.IsOnline,
                        CurrentStatus = user.CurrentStatus,
                        IsActive = user.IsActive,
                        DeactivatedAt = user.DeactivatedAt,
                        DeactivationReason = user.DeactivationReason
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<AuthResponseDto> LogoutAsync(string userId)
        {
            try
            {
                _logger.LogInformation("🔓 Starting logout process for user: {UserId}", userId);
                
                // Update user status
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    _logger.LogInformation("🔓 Found user {Email}, updating status to offline", user.Email);
                    
                    user.IsOnline = false;
                    user.CurrentStatus = "Offline";
                    user.LastActivityAt = DateTime.UtcNow;
                    
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        _logger.LogInformation("✅ Successfully updated user {Email} status to offline", user.Email);
                    }
                    else
                    {
                        _logger.LogError("❌ Failed to update user {Email} status: {Errors}", user.Email, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ User not found for logout: {UserId}", userId);
                }

                await _signInManager.SignOutAsync();
                _logger.LogInformation("✅ Logout completed successfully for user: {UserId}", userId);
                
                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Logout successful"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during logout"
                };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // For now, return not implemented
            // In a production system, you would implement refresh token logic
            return new AuthResponseDto
            {
                Success = false,
                Message = "Refresh token not implemented"
            };
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> ValidateUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null;
        }

        public async Task<AuthResponseDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
                if (existingUser != null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                // Parse full name into first and last names
                var nameParts = createUserDto.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                var user = new ApplicationUser
                {
                    UserName = createUserDto.Email,
                    Email = createUserDto.Email,
                    FirstName = firstName,
                    LastName = lastName,
                    CompanyId = createUserDto.CompanyId,
                    Department = createUserDto.Department,
                    Zone = createUserDto.Zone,
                    Position = createUserDto.Position,
                    DepartmentId = createUserDto.DepartmentId,
                    ZoneId = createUserDto.ZoneId,
                    ShiftId = createUserDto.ShiftId,
                    LocalJobTitle = "",
                    LaborIndicator = "",
                    DateOfBirth = createUserDto.DateOfBirth ?? new DateTime(1990, 1, 1),
                    EmailConfirmed = true,
                    AccountCreatedAt = DateTime.UtcNow,
                    IsOnline = false,
                    CurrentStatus = "Offline",
                    IsActive = true
                };

                IdentityResult result;
                string? generatedPassword = null;
                
                // Smart role-based processing
                if (createUserDto.Role == "Profil")
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
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}"
                    };
                }

                // Add role
                var roleResult = await _userManager.AddToRoleAsync(user, createUserDto.Role);
                if (!roleResult.Succeeded)
                {
                    // Cleanup user if role assignment fails
                    await _userManager.DeleteAsync(user);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = $"Role assignment failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}"
                    };
                }

                // Send appropriate email notification
                await SendUserCreationEmail(user, createUserDto.Role, generatedPassword);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "User created successfully",
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = createUserDto.Role,
                        Department = user.Department,
                        Zone = user.Zone,
                        Position = user.Position,
                        CompanyId = user.CompanyId,
                        FullName = createUserDto.FullName,
                        DateOfBirth = user.DateOfBirth,
                        DepartmentId = user.DepartmentId,
                        ZoneId = user.ZoneId,
                        ShiftId = user.ShiftId,
                        AccountCreatedAt = user.AccountCreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        LastActivityAt = user.LastActivityAt,
                        IsOnline = user.IsOnline,
                        CurrentStatus = user.CurrentStatus,
                        IsActive = user.IsActive,
                        DeactivatedAt = user.DeactivatedAt,
                        DeactivationReason = user.DeactivationReason
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", createUserDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during user creation"
                };
            }
        }

        public async Task<AuthResponseDto> CreateFirstAdminAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Check if any admin exists
                var existingAdmins = await _userManager.GetUsersInRoleAsync("Admin");
                if (existingAdmins.Any())
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Admin users already exist. Use the regular create-user endpoint."
                    };
                }

                // Force admin role
                createUserDto.Role = "Admin";
                return await CreateUserAsync(createUserDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating first admin: {Email}", createUserDto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during first admin creation"
                };
            }
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.DepartmentRef)
                    .Include(u => u.ZoneRef)
                    .Include(u => u.ShiftRef)
                    .ToListAsync();
                
                var userDtos = new List<UserDto>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = roles.FirstOrDefault() ?? "Profil",
                        Department = user.DepartmentRef?.Name ?? user.Department ?? "",
                        Zone = user.ZoneRef?.Name ?? user.Zone ?? "",
                        Position = user.Position ?? "",
                        CompanyId = user.CompanyId ?? "",
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        DateOfBirth = user.DateOfBirth,
                        DepartmentId = user.DepartmentId,
                        ZoneId = user.ZoneId,
                        ShiftId = user.ShiftId,
                        AccountCreatedAt = user.AccountCreatedAt,
                        LastLoginAt = user.LastLoginAt,
                        LastActivityAt = user.LastActivityAt,
                        IsOnline = user.IsOnline,
                        CurrentStatus = user.CurrentStatus,
                        IsActive = user.IsActive,
                        DeactivatedAt = user.DeactivatedAt,
                        DeactivationReason = user.DeactivationReason
                    });
                }

                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("UserId", user.Id),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? ""),
                new Claim("Department", user.Department ?? ""),
                new Claim("Zone", user.Zone ?? ""),
                new Claim("Position", user.Position ?? "")
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7), // 7 days expiration
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateSecurePassword()
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string special = "!@#$%^&*";
            
            var random = new Random();
            var password = new StringBuilder();
            
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

        private async Task SendUserCreationEmail(ApplicationUser user, string role, string? generatedPassword)
        {
            try
            {
                // You'll need to inject IEmailService - for now using console output
                if (role == "Profil")
                {
                    // Profile user email
                    var profileMessage = $"Hello {user.FirstName} {user.LastName},<br><br>" +
                        $"Your profile has been created in the HSE system.<br><br>" +
                        $"<b>Account Details:</b><br>" +
                        $"Company ID: {user.CompanyId ?? "Will be assigned"}<br>" +
                        $"Position: {user.Position}<br>" +
                        $"Department: {user.Department}<br><br>" +
                        $"You can now submit safety reports using your TE ID.<br>" +
                        $"Note: This is a profile-only account. You cannot login to the system.<br><br>" +
                        $"Thank you,<br>HSE Team";
                    
                    Console.WriteLine($"EMAIL TO {user.Email}: Profile Created");
                    Console.WriteLine(profileMessage);
                }
                else
                {
                    // HSE/Admin user email
                    var loginMessage = $"Hello {user.FirstName} {user.LastName},<br><br>" +
                        $"Your {role} account has been created in the HSE system.<br><br>" +
                        $"<b>Login Credentials:</b><br>" +
                        $"Email: {user.Email}<br>" +
                        $"Password: {generatedPassword}<br><br>" +
                        $"<b>IMPORTANT:</b> Please change your password after your first login.<br><br>" +
                        $"You can now access the HSE management system.<br><br>" +
                        $"Thank you,<br>HSE Team";
                    
                    Console.WriteLine($"EMAIL TO {user.Email}: {role} Account Created");
                    Console.WriteLine(loginMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send user creation email to {Email}", user.Email);
            }
        }
    }
}