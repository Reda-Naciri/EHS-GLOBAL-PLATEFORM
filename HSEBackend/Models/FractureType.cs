using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class FractureType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(50)]
        public string Code { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string Category { get; set; } = ""; // Fracture, Cut, Burn, Bruise, etc.

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Injury> Injuries { get; set; } = new List<Injury>();
    }
}