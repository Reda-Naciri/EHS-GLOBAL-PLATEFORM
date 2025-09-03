using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class CorrectiveAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = "";

        [Required]
        public DateTime DueDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Not Started"; // Not Started, In Progress, Completed, Aborted

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [StringLength(100)]
        public string Hierarchy { get; set; } = "";

        [StringLength(450)]
        public string? AssignedToProfileId { get; set; } // Profile user assigned to this action
        public virtual ApplicationUser? AssignedToProfile { get; set; }

        [StringLength(450)]
        public string? CreatedByHSEId { get; set; } // HSE user who created this action
        public virtual ApplicationUser? CreatedByHSE { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsCompleted { get; set; }

        public bool Overdue { get; set; } = false; // Automatically set to true when DueDate passes and status is not Completed/Canceled/Aborted

        // Abort tracking fields (for traceability)
        public string? AbortedById { get; set; } // User ID who aborted the corrective action
        public virtual ApplicationUser? AbortedBy { get; set; }
        public DateTime? AbortedAt { get; set; }
        public string? AbortReason { get; set; }

        // Clé étrangère vers le rapport concerné (nullable for standalone corrective actions)
        public int? ReportId { get; set; }
        public virtual Report? Report { get; set; }

        // Relations
        public virtual ICollection<SubAction> SubActions { get; set; } = new List<SubAction>();
        public virtual ICollection<ActionAttachment> Attachments { get; set; } = new List<ActionAttachment>();
    }
}