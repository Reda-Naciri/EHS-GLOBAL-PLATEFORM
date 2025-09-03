using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class HSEZoneDelegation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string FromHSEUserId { get; set; } = ""; // Original HSE user (absent)

        [Required]
        [StringLength(450)]
        public string ToHSEUserId { get; set; } = ""; // Temporary delegate HSE user

        [Required]
        public int ZoneId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; } // e.g., "Vacation", "Sick leave", "Training"

        [Required]
        [StringLength(450)]
        public string CreatedByAdminId { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ApplicationUser FromHSEUser { get; set; } = null!;
        public virtual ApplicationUser ToHSEUser { get; set; } = null!;
        public virtual Zone Zone { get; set; } = null!;
        public virtual ApplicationUser CreatedByAdmin { get; set; } = null!;
    }
}