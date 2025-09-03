using HSEBackend.DTOs;
using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager, ILogger<AuthController> logger)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// User login endpoint
        /// </summary>
        /// <param name="loginDto">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid login data", errors = ModelState });
                }

                var result = await _authService.LoginAsync(loginDto);
                
                if (!result.Success)
                {
                    return Unauthorized(new { message = result.Message });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    token = result.Token,
                    user = result.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "Internal server error during login" });
            }
        }

        /// <summary>
        /// User logout endpoint
        /// </summary>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                var result = await _authService.LogoutAsync(userId);
                
                return Ok(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Internal server error during logout" });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid user context" });
                }

                // Get full user data from database to include avatar and other updated fields
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "",
                    Department = user.Department ?? "",
                    Zone = user.Zone ?? "",
                    Position = user.Position ?? "",
                    CompanyId = user.CompanyId,
                    Avatar = user.Avatar,
                    DateOfBirth = user.DateOfBirth,
                    IsActive = user.IsActive,
                    AccountCreatedAt = user.AccountCreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    FullName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
                        ? $"{user.FirstName} {user.LastName}".Trim()
                        : user.FirstName ?? user.LastName ?? ""
                };

                _logger.LogInformation($"Profile fetched for user {user.Email} with avatar: {user.Avatar}");

                return Ok(new
                {
                    success = true,
                    user = userDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "Internal server error getting profile" });
            }
        }

        /// <summary>
        /// Validate current token
        /// </summary>
        /// <returns>Token validation result</returns>
        [HttpGet("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Invalid token", valid = false });
                }

                var isValid = await _authService.ValidateUserAsync(userId);
                
                if (!isValid)
                {
                    return Unauthorized(new { message = "User not found", valid = false });
                }

                return Ok(new
                {
                    valid = true,
                    message = "Token is valid",
                    userId = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, new { message = "Internal server error during validation" });
            }
        }

        /// <summary>
        /// Refresh JWT token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshToken);
                
                if (!result.Success)
                {
                    return Unauthorized(new { message = result.Message });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    token = result.Token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "Internal server error during token refresh" });
            }
        }

        /// <summary>
        /// Create a new user account (Admin only)
        /// </summary>
        /// <param name="createUserDto">User creation data</param>
        /// <returns>Created user information</returns>
        [HttpPost("create-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid user data", errors = ModelState });
                }

                var result = await _authService.CreateUserAsync(createUserDto);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    user = result.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "Internal server error during user creation" });
            }
        }

        /// <summary>
        /// Create first admin account (No authentication required - one-time use)
        /// </summary>
        /// <param name="createUserDto">Admin creation data</param>
        /// <returns>Created admin information</returns>
        [HttpPost("create-first-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFirstAdmin([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid admin data", errors = ModelState });
                }

                // Force role to Admin
                createUserDto.Role = "Admin";

                var result = await _authService.CreateFirstAdminAsync(createUserDto);
                
                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    user = result.User
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating first admin");
                return StatusCode(500, new { message = "Internal server error during admin creation" });
            }
        }

        /// <summary>
        /// Test authentication (Admin only)
        /// </summary>
        /// <returns>Authentication test result</returns>
        [HttpGet("test-auth")]
        [Authorize(Roles = "Admin")]
        public IActionResult TestAuth()
        {
            var userClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            
            return Ok(new
            {
                success = true,
                message = "Authentication successful",
                userId = User.FindFirst("UserId")?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                roles = roles,
                allClaims = userClaims
            });
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                return Ok(new
                {
                    success = true,
                    users = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { message = "Internal server error getting users" });
            }
        }
    }
}