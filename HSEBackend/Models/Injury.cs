using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class Injury
    {
        [Key]
        public int Id { get; set; }

        // Body part reference
        public int BodyPartId { get; set; }
        public virtual BodyPart BodyPart { get; set; } = null!;

        // Fracture/Injury type reference
        public int FractureTypeId { get; set; }
        public virtual FractureType FractureType { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = ""; // Minor, Moderate, Severe

        [Required]
        public string Description { get; set; } = "";

        // ===== RELATION =====
        public int InjuredPersonId { get; set; }
        public virtual InjuredPerson InjuredPerson { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}