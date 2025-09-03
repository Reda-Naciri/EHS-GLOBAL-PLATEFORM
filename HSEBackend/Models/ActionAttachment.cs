using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class ActionAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = "";

        [Required]
        public string FilePath { get; set; } = "";

        [StringLength(100)]
        public string? FileType { get; set; }

        public long FileSize { get; set; }

        // ===== RELATION =====
        public int CorrectiveActionId { get; set; }
        public virtual CorrectiveAction CorrectiveAction { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}