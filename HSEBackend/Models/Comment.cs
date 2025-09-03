using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = "";

        // ===== RELATIONS =====
        public int ReportId { get; set; }
        public virtual Report Report { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = "";
        public virtual ApplicationUser User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsInternal { get; set; } = true; // Toujours true pour HSE/Admin
    }
}