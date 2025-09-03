using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class Action
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        public DateTime? DueDate { get; set; }

        [StringLength(100)]
        public string Hierarchy { get; set; } = ""; // Elimination, Substitution, Engineering Controls, etc.

        [StringLength(20)]
        public string Status { get; set; } = "Not Started"; // Not Started, In Progress, Completed

        public bool Overdue { get; set; } = false; // Automatically set to true when DueDate passes and status is not Completed/Canceled

        // ===== RELATIONS =====
        public string? AssignedToId { get; set; } // User ID qui doit ex�cuter l'action
        public virtual ApplicationUser? AssignedTo { get; set; }

        public int? ReportId { get; set; } // Peut �tre null pour les actions ind�pendantes
        public virtual Report? Report { get; set; }

        [Required]
        public string CreatedById { get; set; } = ""; // HSE qui a cr�� l'action
        public virtual ApplicationUser? CreatedBy { get; set; }

        public string? AbortedById { get; set; } // User ID who aborted the action (for traceability)
        public virtual ApplicationUser? AbortedBy { get; set; }
        public DateTime? AbortedAt { get; set; }
        public string? AbortReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ===== SUB-ACTIONS =====
        public virtual ICollection<SubAction> SubActions { get; set; } = new List<SubAction>();
        public virtual ICollection<ActionAttachment> Attachments { get; set; } = new List<ActionAttachment>();
    }
}