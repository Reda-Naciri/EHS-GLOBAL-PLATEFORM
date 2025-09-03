using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class ReportAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReportId { get; set; }

        [Required]
        [StringLength(450)]
        public string AssignedHSEUserId { get; set; } = "";

        [StringLength(500)]
        public string? AssignmentReason { get; set; } // e.g., "Special expertise", "Urgent case", "Workload balance"

        [Required]
        [StringLength(450)]
        public string AssignedByAdminId { get; set; } = "";

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Report Report { get; set; } = null!;
        public virtual ApplicationUser AssignedHSEUser { get; set; } = null!;
        public virtual ApplicationUser AssignedByAdmin { get; set; } = null!;
    }
}