using System.ComponentModel.DataAnnotations;

namespace HSEBackend.DTOs
{
    public class CreateReportDto
    {
        // ===== REPORTER INFORMATION (Section bleue) =====
        [Required(ErrorMessage = "Reporter ID is required")]
        public string ReporterId { get; set; } = "";

        // Note: ReportDateTime sera généré automatiquement au backend

        [Required(ErrorMessage = "Work shift is required")]
        public string WorkShift { get; set; } = ""; // Day, Afternoon, Night

        // ===== INCIDENT DETAILS (Section jaune) =====
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; } = ""; // Vient de la sélection page d'accueil

        [Required(ErrorMessage = "Zone is required")]
        public string Zone { get; set; } = "";

        [Required(ErrorMessage = "Incident date and time is required")]
        public DateTime IncidentDateTime { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters")]
        public string Description { get; set; } = "";

        // ===== INJURED PERSONS (Section rouge - seulement si Incident-Management) =====
        public int InjuredPersonsCount { get; set; } = 0;
        public List<CreateInjuredPersonDto> InjuredPersons { get; set; } = new();
        
        // JSON string version for FormData compatibility
        public string? InjuredPersonsJson { get; set; }

        // ===== ACTIONS TAKEN (Section verte - OPTIONNELLE) =====
        // Ces champs sont optionnels - peuvent être vides
        public string? ImmediateActionsTaken { get; set; } // Peut être null/vide

        public string? ActionStatus { get; set; } = "Non commencé"; // Peut être null

        public string? PersonInChargeOfActions { get; set; } // Peut être null/vide

        public DateTime? DateActionsCompleted { get; set; } // Peut être null

        // ===== ATTACHMENTS =====
        public List<IFormFile>? Attachments { get; set; }
    }

    // ===== DTO POUR INJURED PERSON =====
    public class CreateInjuredPersonDto
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = "";

        public string? Department { get; set; } // Correspond au champ "Department" dans le frontend

        public string? ZoneOfPerson { get; set; } // "Zone of Person"

        public string? Gender { get; set; } // Male, Female, Other, Prefer not to say

        // ===== BODY MAP =====
        public string? SelectedBodyPart { get; set; } // Données du body-map component

        public string? InjuryType { get; set; } // Fracture, Burn, etc.

        public string? Severity { get; set; } // Minor (low), Moderate (medium), Severe (high)

        public string? InjuryDescription { get; set; } // Description libre

        // ===== INJURIES LIST (pour le bouton "Add Injury") =====
        public List<CreateInjuryDto> Injuries { get; set; } = new();
    }

    public class CreateInjuryDto
    {
        [Required]
        public int BodyPartId { get; set; }

        [Required]
        public int FractureTypeId { get; set; }

        [Required]
        public string Severity { get; set; } = ""; // Minor, Moderate, Severe

        [Required]
        public string Description { get; set; } = "";

        // For backward compatibility, keep BodyPart as optional
        public string? BodyPart { get; set; }
    }

    // ===== DTO POUR VALIDATION REPORTER ID =====
    public class ValidateReporterDto
    {
        [Required]
        public string ReporterId { get; set; } = "";
    }

    public class ReporterValidationResponseDto
    {
        public bool IsValid { get; set; }
        public string? ReporterName { get; set; }
        public string? Department { get; set; }
        public string? Zone { get; set; }
        public string Message { get; set; } = "";
    }

    // ===== DTO POUR RÉPONSE DE SOUMISSION =====
    public class ReportSubmissionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int? ReportId { get; set; }
        public string? TrackingNumber { get; set; }
    }

    // ===== DTO POUR DÉTAILS RAPPORT (Vue HSE) =====
    public class ReportDetailDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = "";
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string ReporterId { get; set; } = "";
        public string Zone { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime IncidentDateTime { get; set; }
        public string? AssignedHSE { get; set; } // HSE User Full Name

        // Détails complets
        public DateTime ReportDateTime { get; set; }
        public string WorkShift { get; set; } = "";
        public string? Shift { get; set; }
        public string Description { get; set; } = "";

        // Injured Person Details
        public string? InjuredPersonName { get; set; }
        public string? InjuredPersonDepartment { get; set; }
        public string? InjuredPersonZone { get; set; }
        public string? BodyMapData { get; set; }
        public string? InjuryType { get; set; }
        public string? InjurySeverity { get; set; }

        // Actions Taken (peut être vide si aucune action prise)
        public string? ImmediateActionsTaken { get; set; }
        public string? ImmediateActions { get; set; }
        public string? ActionStatus { get; set; }
        public string? PersonInChargeOfActions { get; set; }
        public DateTime? DateActionsCompleted { get; set; }
        public bool HasActionsTaken => !string.IsNullOrEmpty(ImmediateActionsTaken);

        // Relations
        public int InjuredPersonsCount { get; set; }
        public List<InjuredPersonDto> InjuredPersons { get; set; } = new();

        // Actions Correctives (créées par HSE - système séparé)
        public List<CorrectiveActionSummaryDto> CorrectiveActions { get; set; } = new();
        public List<ActionSummaryDto> Actions { get; set; } = new();

        public List<CommentDto> Comments { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    // ===== DTO POUR INJURED PERSON (AFFICHAGE) =====
    public class InjuredPersonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Department { get; set; }
        public string? ZoneOfPerson { get; set; }
        public string? Gender { get; set; }
        public string? SelectedBodyPart { get; set; }
        public string? InjuryType { get; set; }
        public string? Severity { get; set; }
        public string? InjuryDescription { get; set; }
        public List<InjuryDto> Injuries { get; set; } = new();
    }

    public class InjuryDto
    {
        public int Id { get; set; }
        public string BodyPart { get; set; } = "";
        public string InjuryType { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    // ===== DTO POUR ACTIONS CORRECTIVES (Système HSE séparé) =====
    public class CorrectiveActionSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "";
        public string Hierarchy { get; set; } = "";
        public string? AssignedTo { get; set; }
        public string? CreatedByHSEId { get; set; }
        public string? CreatedByName { get; set; }
        public int SubActionsCount { get; set; }
        public int ProgressPercentage { get; set; } = 0; // Progress based on sub-actions
        public bool Overdue { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }

    // ===== DTOs UTILITAIRES =====
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = "";
        public string? Avatar { get; set; }
    }

    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string? FileType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string DownloadUrl { get; set; } = "";
    }

    // ===== DTO FOR REPORT SUMMARY (LIST VIEW) =====
    public class ReportSummaryDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = "";
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string ReporterId { get; set; } = "";
        public string Zone { get; set; } = "";
        public string? AssignedHSE { get; set; } // HSE User Full Name
        public int InjuredPersonsCount { get; set; }
        public bool HasAttachments { get; set; }
        public int ActionsCount { get; set; }
        public int CorrectiveActionsCount { get; set; }
        public string? InjurySeverity { get; set; }
    }

    // ===== DTO FOR ACTION SUMMARY =====
    public class ActionSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "";
        public string Hierarchy { get; set; } = "";
        public string? AssignedTo { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public int SubActionsCount { get; set; }
        public int ProgressPercentage { get; set; } = 0; // Progress based on sub-actions
        public DateTime CreatedAt { get; set; }
    }

    // ===== DTO FOR RECENT REPORTS =====
    public class RecentReportDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = "";
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string ReporterId { get; set; } = "";
        public string Zone { get; set; } = "";
        public string? AssignedHSE { get; set; } // HSE User Full Name
        public string? InjurySeverity { get; set; }
        public bool IsUrgent { get; set; }
    }

    // ===== DTO FOR UPDATING ASSIGNED HSE =====
    public class UpdateAssignedHSEDto
    {
        public string? AssignedHSEId { get; set; } // Can be null to unassign
    }

    
}