using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    // Base DTO for all parameter types
    public class ParameterDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Zone DTOs
    public class ZoneDto : ParameterDto
    {
        public string Code { get; set; } = "";
    }

    public class CreateZoneDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }

    public class UpdateZoneDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Code { get; set; }

        public bool? IsActive { get; set; }
    }

    // Department DTOs
    public class DepartmentDto : ParameterDto
    {
        public string Code { get; set; } = "";
    }

    public class CreateDepartmentDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }

    public class UpdateDepartmentDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Code { get; set; }

        public bool? IsActive { get; set; }
    }

    // FractureType (Injury Type) DTOs
    public class InjuryTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Code { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateInjuryTypeDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Category { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }

    public class UpdateInjuryTypeDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(20)]
        public string? Category { get; set; }

        public bool? IsActive { get; set; }
    }

    // Shift DTOs
    public class ShiftDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Code { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateShiftDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }

    public class UpdateShiftDto
    {
        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [StringLength(20)]
        public string? Code { get; set; }

        public bool? IsActive { get; set; }
    }
}