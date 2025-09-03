using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class ReportAttachment
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
        public int ReportId { get; set; }
        public virtual Report Report { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}