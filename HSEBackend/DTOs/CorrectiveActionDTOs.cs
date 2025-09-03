using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    public class CreateCorrectiveActionDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = "";

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        [Required]
        [StringLength(100)]
        public string Hierarchy { get; set; } = "";

        public string? CreatedByHSEId { get; set; }
        public int? ReportId { get; set; } = null; // Nullable to support standalone corrective actions
    }

    public class UpdateCorrectiveActionDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "";
        public string Hierarchy { get; set; } = "";
        public string? AssignedTo { get; set; }
        public string Status { get; set; } = "";
        public bool? IsCompleted { get; set; }
    }

    public class CorrectiveActionDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Priority { get; set; } = "";
        public string Hierarchy { get; set; } = "";
        public string? AssignedTo { get; set; }
        public string? CreatedByHSEId { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsCompleted { get; set; }
        public bool Overdue { get; set; } = false;
        public int? ReportId { get; set; }
        public string? ReportTitle { get; set; }
        public string? ReportTrackingNumber { get; set; }
        
        // Abort tracking fields
        public string? AbortedById { get; set; }
        public string? AbortedByName { get; set; }
        public DateTime? AbortedAt { get; set; }
        public string? AbortReason { get; set; }
        
        public List<SubActionDetailDto> SubActions { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class UpdateCorrectiveActionStatusDto
    {
        [Required]
        public string Status { get; set; } = "";
    }

    public class AbortCorrectiveActionDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = "";
    }
}