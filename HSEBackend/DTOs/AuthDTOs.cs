using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? Token { get; set; }
        public UserDto? User { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Role { get; set; } = "";
        public string Department { get; set; } = "";
        public string Zone { get; set; } = "";
        public string Position { get; set; } = "";
        public string FullName { get; set; } = "";
        public string CompanyId { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string? Avatar { get; set; }
        public int? DepartmentId { get; set; }
        public int? ZoneId { get; set; }
        public int? ShiftId { get; set; }
        public DateTime AccountCreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public bool IsOnline { get; set; }
        public string? CurrentStatus { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public string? DeactivationReason { get; set; }
    }

    public class CreateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        // Password is now optional - will be generated for HSE/Admin roles
        public string? Password { get; set; }

        [Required]
        public string FullName { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Profil";

        [Required]
        public string CompanyId { get; set; } = "";
        
        [Required]
        public string Department { get; set; } = "";
        
        public string Zone { get; set; } = "";
        
        [Required]
        public string Position { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public int? DepartmentId { get; set; }
        public int? ZoneId { get; set; }
        public int? ShiftId { get; set; }
        
        // Legacy fields for backward compatibility (not required)
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class UpdateUserDto
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string CompanyId { get; set; } = "";
        public string Department { get; set; } = "";
        public string Zone { get; set; } = "";
        public string Position { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public int? DepartmentId { get; set; }
        public int? ZoneId { get; set; }
        public int? ShiftId { get; set; }
    }

    public class UpdateUserRoleDto
    {
        [Required]
        public string Role { get; set; } = "";
    }

    public class UpdateUserProfileDto
    {
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public IFormFile? Avatar { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";
    }

    public class AdminResetPasswordDto
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";
    }

    public class UpdateCompanyIdDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string CompanyId { get; set; } = "";
    }

    public class AssignTestAvatarDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string AvatarFilename { get; set; } = "";
    }

    public class AssignZoneDto
    {
        [Required]
        public string UserId { get; set; } = "";

        [Required]
        public int ZoneId { get; set; }
    }

    public class RemoveZoneDto
    {
        [Required]
        public string UserId { get; set; } = "";

        [Required]
        public int ZoneId { get; set; }
    }

    public class CreateZoneDelegationDto
    {
        [Required]
        public string FromHSEUserId { get; set; } = "";

        [Required]
        public string ToHSEUserId { get; set; } = "";

        [Required]
        public int ZoneId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class UpdateZoneDelegationDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ZoneAssignmentDto
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = "";
        public string ZoneCode { get; set; } = "";
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class ZoneDelegationDto
    {
        public int Id { get; set; }
        public string FromHSEUserName { get; set; } = "";
        public string FromHSEUserEmail { get; set; } = "";
        public string ToHSEUserName { get; set; } = "";
        public string ToHSEUserEmail { get; set; } = "";
        public string ZoneName { get; set; } = "";
        public string ZoneCode { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByAdminName { get; set; } = "";
    }
}