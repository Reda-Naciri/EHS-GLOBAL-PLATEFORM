using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = "";

        [Required]
        public string Message { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = ""; // ActionAssigned, StatusChanged, AccountRequest, ReportSubmitted, etc.

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = ""; // User who should receive the notification

        [StringLength(450)]
        public string? TriggeredByUserId { get; set; } // User who triggered the notification

        public int? RelatedReportId { get; set; }
        public int? RelatedActionId { get; set; }
        public int? RelatedCorrectiveActionId { get; set; }

        [StringLength(500)]
        public string? RedirectUrl { get; set; } // Direct link to related section

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        
        public bool IsEmailSent { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationUser? TriggeredByUser { get; set; }
        public virtual Report? RelatedReport { get; set; }
        public virtual Models.Action? RelatedAction { get; set; }
        public virtual CorrectiveAction? RelatedCorrectiveAction { get; set; }
    }
}