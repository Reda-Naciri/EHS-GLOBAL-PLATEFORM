using System.ComponentModel.DataAnnotations;

namespace HSEBackend.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        // Unique tracking number for public reference (e.g., RPT-2024-001234)
        [Required]
        [StringLength(20)]
        public string TrackingNumber { get; set; } = "";

        // ===== REPORTER INFORMATION =====
        [Required]
        [StringLength(50)]
        public string ReporterCompanyId { get; set; } = ""; // Company ID that needs instant validation

        [Required]
        public DateTime ReportDateTime { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(20)]
        public string WorkShift { get; set; } = ""; // Day, Afternoon, Night

        // ===== REPORT DETAILS =====
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = ""; // D�termin� depuis la page d'accueil

        [Required]
        [StringLength(100)]
        public string Zone { get; set; } = "";

        public int? ZoneId { get; set; }
        public virtual Zone? ZoneRef { get; set; }

        [Required]
        public DateTime IncidentDateTime { get; set; }

        [Required]
        public string Description { get; set; } = "";

        // ===== INJURED PERSONS (Seulement pour Incident-Management) =====
        public int InjuredPersonsCount { get; set; } = 0;

        // ===== ACTIONS TAKEN (Section OPTIONNELLE) =====
        // Ces champs peuvent �tre vides si aucune action n'a �t� prise
        public string? ImmediateActionsTaken { get; set; } // NULLABLE - optionnel

        [StringLength(50)]
        public string? ActionStatus { get; set; } // NULLABLE - optionnel

        [StringLength(100)]
        public string? PersonInChargeOfActions { get; set; } // NULLABLE - optionnel

        public DateTime? DateActionsCompleted { get; set; } // NULLABLE - optionnel

        // ===== SYSTEM FIELDS =====
        [StringLength(20)]
        public string Status { get; set; } = "Unopened"; // Unopened, Opened, Closed

        public DateTime? OpenedAt { get; set; } // When HSE first accessed the report
        public string? OpenedByHSEId { get; set; } // HSE who first opened the report
        public virtual ApplicationUser? OpenedByHSE { get; set; }

        public string? AssignedHSEId { get; set; }
        public virtual ApplicationUser? AssignedHSE { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ===== RELATIONS =====
        public virtual ICollection<InjuredPerson> InjuredPersons { get; set; } = new List<InjuredPerson>();
        public virtual ICollection<ReportAttachment> Attachments { get; set; } = new List<ReportAttachment>();
        public virtual ICollection<Models.Action> Actions { get; set; } = new List<Models.Action>();

        // Actions Correctives cr��es par HSE (syst�me s�par�)
        public virtual ICollection<CorrectiveAction> CorrectiveActions { get; set; } = new List<CorrectiveAction>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}