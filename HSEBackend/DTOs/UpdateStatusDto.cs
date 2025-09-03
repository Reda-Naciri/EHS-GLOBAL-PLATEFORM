using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    public class UpdateStatusDto
    {
        [Required]
        public int ReportId { get; set; }

        [Required]
        [StringLength(50)]
        public string NewStatus { get; set; } = "";
        
        public string Status => NewStatus;
    }

    public class UpdateRegistrationStatusDto
    {
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "";
    }
}