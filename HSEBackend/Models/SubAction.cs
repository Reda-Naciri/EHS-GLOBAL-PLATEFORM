using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class SubAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Not Started";

        public DateTime? DueDate { get; set; }

        public bool Overdue { get; set; } = false; // Automatically set to true when DueDate passes and status is not Completed/Canceled

        // ===== RELATIONS =====
        public int ActionId { get; set; }
        public virtual Models.Action Action { get; set; } = null!;

        public int? CorrectiveActionId { get; set; }
        public virtual CorrectiveAction? CorrectiveAction { get; set; }

        public string? AssignedToId { get; set; }
        public virtual ApplicationUser? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}