using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class InjuredPerson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(100)]
        public string Department { get; set; } = "";

        [StringLength(100)]
        public string ZoneOfPerson { get; set; } = "";

        [StringLength(20)]
        public string? Gender { get; set; } // Male, Female, Other, Prefer not to say

        // ===== BODY MAP DATA =====
        public string? SelectedBodyPart { get; set; } // JSON ou texte simple

        [StringLength(100)]
        public string? InjuryType { get; set; } // Fracture, Burn, etc.

        [StringLength(20)]
        public string? Severity { get; set; } // Minor, Moderate, Severe

        public string? InjuryDescription { get; set; }

        // ===== RELATION =====
        public int ReportId { get; set; }
        public virtual Report Report { get; set; } = null!;

        // ===== INJURIES LIST =====
        public virtual ICollection<Injury> Injuries { get; set; } = new List<Injury>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}