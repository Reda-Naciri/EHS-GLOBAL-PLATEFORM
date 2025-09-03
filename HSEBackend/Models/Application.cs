using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class Application
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Icon { get; set; } = string.Empty; // emoji or icon class

        [Required]
        [StringLength(500)]
        public string RedirectUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int Order { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}