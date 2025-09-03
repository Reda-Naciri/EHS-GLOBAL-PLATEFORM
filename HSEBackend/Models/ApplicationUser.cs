using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Basic Information
        [StringLength(100)]
        public string FirstName { get; set; } = "";

        [StringLength(100)]
        public string LastName { get; set; } = "";

        [StringLength(50)]
        public string CompanyId { get; set; } = ""; // TE001234

        public string FullName => $"{FirstName} {LastName}".Trim();

        [Required]
        public DateTime DateOfBirth { get; set; } = DateTime.Parse("1990-01-01");

        // Profile picture
        [StringLength(255)]
        public string? Avatar { get; set; }

        // Reference to lookup tables
        public int? DepartmentId { get; set; }
        public int? ZoneId { get; set; }
        public int? ShiftId { get; set; }

        // Legacy fields for backward compatibility
        [StringLength(100)]
        public string Department { get; set; } = "";

        [StringLength(100)]
        public string Zone { get; set; } = "";

        [StringLength(100)]
        public string LocalJobTitle { get; set; } = "";

        [StringLength(50)]
        public string LaborIndicator { get; set; } = "";

        [StringLength(100)]
        public string Position { get; set; } = "";

        // Account tracking
        public DateTime AccountCreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AccountUpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastActivityAt { get; set; }

        // Status tracking
        public bool IsOnline { get; set; } = false;
        public string? CurrentStatus { get; set; } // "Online", "Offline", "Away", "Busy"
        
        // Account status (Admin can activate/deactivate users)
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedAt { get; set; }
        public string? DeactivationReason { get; set; }

        // Navigation properties
        public virtual Department? DepartmentRef { get; set; }
        public virtual Zone? ZoneRef { get; set; }
        public virtual Shift? ShiftRef { get; set; }

        // Activity tracking
        public virtual ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

        // HSE Zone Responsibilities (only for HSE users)
        public virtual ICollection<HSEZoneResponsibility> ResponsibleZones { get; set; } = new List<HSEZoneResponsibility>();

        // HSE Zone Delegations
        public virtual ICollection<HSEZoneDelegation> DelegatedZones { get; set; } = new List<HSEZoneDelegation>(); // Zones delegated TO this user
        public virtual ICollection<HSEZoneDelegation> DelegatorZones { get; set; } = new List<HSEZoneDelegation>(); // Zones delegated BY this user
        public virtual ICollection<HSEZoneDelegation> CreatedDelegations { get; set; } = new List<HSEZoneDelegation>(); // Delegations created by this admin
    }
}
