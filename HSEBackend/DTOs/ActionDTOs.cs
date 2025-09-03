using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    public class UpdateProfileSubActionDto
    {
        [StringLength(50)]
        public string? Status { get; set; }

        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
    public class CreateActionDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [StringLength(100)]
        public string Hierarchy { get; set; } = ""; // Elimination, Substitution, etc.

        [Required]
        public string AssignedToId { get; set; } = "";

        [Required]
        public string CreatedById { get; set; } = "";

        public int? ReportId { get; set; }
    }

    public class UpdateActionDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public string Hierarchy { get; set; } = "";
        public string AssignedToId { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class CreateSubActionDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string? AssignedToId { get; set; }
        // Status is automatically set to "Not Started" on creation
    }

    public class UpdateSubActionDto
    {
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string? AssignedToId { get; set; }
        public string Status { get; set; } = "";
    }

    public class ActionDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "";
        public string Hierarchy { get; set; } = "";
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string CreatedById { get; set; } = "";
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? ReportId { get; set; }
        public string? ReportTitle { get; set; }
        public string? ReportTrackingNumber { get; set; }
        public bool Overdue { get; set; } = false;
        public List<SubActionDetailDto> SubActions { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class SubActionDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "";
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool Overdue { get; set; } = false;
        
        // Corrective Action details (if this sub-action belongs to a corrective action)
        public int? CorrectiveActionId { get; set; }
        public string? CorrectiveActionTitle { get; set; }
        public string? CorrectiveActionDescription { get; set; }
        public DateTime? CorrectiveActionDueDate { get; set; }
        public string? CorrectiveActionPriority { get; set; }
        public string? CorrectiveActionHierarchy { get; set; }
        public string? CorrectiveActionStatus { get; set; }
        public string? CorrectiveActionAuthor { get; set; }
        public DateTime? CorrectiveActionCreatedAt { get; set; }
    }

    public class UpdateActionStatusDto
    {
        [Required]
        public string Status { get; set; } = "";
    }

    public class AbortActionDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = "";
    }
}