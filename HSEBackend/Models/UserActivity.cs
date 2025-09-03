using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class UserActivity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; } = ""; // Login, Logout, PageView, Action

        [StringLength(200)]
        public string? Details { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }

    public class UserSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string SessionToken { get; set; } = "";

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime? LogoutTime { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }
}