using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class EmailConfiguration
    {
        [Key]
        public int Id { get; set; }

        // General email settings
        [Required]
        public bool IsEmailingEnabled { get; set; } = true;

        // Profile user assignment emails
        [Required]
        public bool SendProfileAssignmentEmails { get; set; } = true;

        // HSE email settings
        [Required]
        public bool SendHSEUpdateEmails { get; set; } = true;
        
        [Required]
        public int HSEUpdateIntervalMinutes { get; set; } = 360; // Default 6 hours in minutes

        [Required]
        public bool SendHSEInstantReportEmails { get; set; } = true;

        // Admin email settings
        [Required]
        public bool SendAdminOverviewEmails { get; set; } = true;
        
        [Required]
        public int AdminOverviewIntervalMinutes { get; set; } = 360; // Default 6 hours in minutes

        // Super admin emails (comma-separated list of user IDs)
        [StringLength(2000)]
        public string? SuperAdminUserIds { get; set; }

        // Configuration metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }

        // Navigation properties
        public virtual ApplicationUser? UpdatedByUser { get; set; }
    }

    public class EmailTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TemplateName { get; set; } = ""; // ProfileAssignment, HSEUpdate, AdminOverview, etc.

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = "";

        [Required]
        public string HtmlContent { get; set; } = "";

        [Required]
        public string PlainTextContent { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }

        // Navigation properties
        public virtual ApplicationUser? UpdatedByUser { get; set; }
    }

    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string RecipientEmail { get; set; } = "";

        [StringLength(450)]
        public string? RecipientUserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string EmailType { get; set; } = ""; // ProfileAssignment, HSEUpdate, AdminOverview, etc.

        [Required]
        public string Status { get; set; } = ""; // Pending, Sent, Failed

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }

        public int? RelatedNotificationId { get; set; }

        // Navigation properties
        public virtual ApplicationUser? RecipientUser { get; set; }
        public virtual Notification? RelatedNotification { get; set; }
    }
}