using HSEBackend.DTOs;
using HSEBackend.Models;

namespace HSEBackend.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> LogoutAsync(string userId);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<bool> ValidateUserAsync(string userId);
        Task<AuthResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<AuthResponseDto> CreateFirstAdminAsync(CreateUserDto createUserDto);
        Task<List<UserDto>> GetAllUsersAsync();
        string GenerateJwtToken(ApplicationUser user, IList<string> roles);
    }
}