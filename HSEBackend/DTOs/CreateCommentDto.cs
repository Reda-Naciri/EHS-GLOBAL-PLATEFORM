using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public int ReportId { get; set; }

        [Required]
        [StringLength(300)]
        public string Content { get; set; } = "";

        [Required]
        public string Author { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
