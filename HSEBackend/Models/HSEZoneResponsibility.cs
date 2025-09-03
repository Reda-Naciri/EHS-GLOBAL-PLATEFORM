using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class HSEZoneResponsibility
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string HSEUserId { get; set; } = "";

        [Required]
        public int ZoneId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ApplicationUser HSEUser { get; set; } = null!;
        public virtual Zone Zone { get; set; } = null!;
    }
}