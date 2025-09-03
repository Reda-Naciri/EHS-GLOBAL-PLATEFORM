using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class RegistrationRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = "";

        [Required]
        public string CompanyId { get; set; } = "";

        [Required]
        public string Email { get; set; } = "";

        [Required]
        public string Department { get; set; } = "";

        [Required]
        public string Position { get; set; } = "";

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending"; // Possible values: Pending, Approved, Rejected
    }
}
