using HSEBackend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Tables existantes
        public DbSet<RegistrationRequest> RegistrationRequests { get; set; }
        public DbSet<PendingUser> PendingUsers { get; set; }

        // Reference Tables
        public DbSet<Department> Departments { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Shift> Shifts { get; set; }

        // User Activity Tracking
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        // HSE Zone Responsibilities
        public DbSet<HSEZoneResponsibility> HSEZoneResponsibilities { get; set; }
        public DbSet<HSEZoneDelegation> HSEZoneDelegations { get; set; }
        public DbSet<ReportAssignment> ReportAssignments { get; set; }

        // Reference Tables for Reports
        public DbSet<BodyPart> BodyParts { get; set; }
        public DbSet<FractureType> FractureTypes { get; set; }

        // Notifications and Email System
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmailConfiguration> EmailConfigurations { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }

        // Tables HSE
        public DbSet<Report> Reports { get; set; }
        public DbSet<InjuredPerson> InjuredPersons { get; set; }
        public DbSet<Injury> Injuries { get; set; }
        public DbSet<ReportAttachment> ReportAttachments { get; set; }
        public DbSet<Models.Action> Actions { get; set; }
        public DbSet<CorrectiveAction> CorrectiveActions { get; set; }
        public DbSet<SubAction> SubActions { get; set; }
        public DbSet<ActionAttachment> ActionAttachments { get; set; }
        public DbSet<Comment> Comments { get; set; }
        
        // Applications for Home Page
        public DbSet<Models.Application> Applications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser relations with reference tables
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.DepartmentRef)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.ZoneRef)
                .WithMany(z => z.Users)
                .HasForeignKey(u => u.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.ShiftRef)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.ShiftId)
                .OnDelete(DeleteBehavior.SetNull);

            // User Activity relations
            builder.Entity<UserActivity>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserSession>()
                .HasOne(us => us.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // HSE Zone Responsibility relations
            builder.Entity<HSEZoneResponsibility>()
                .HasOne(hzr => hzr.HSEUser)
                .WithMany(u => u.ResponsibleZones)
                .HasForeignKey(hzr => hzr.HSEUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<HSEZoneResponsibility>()
                .HasOne(hzr => hzr.Zone)
                .WithMany(z => z.HSEResponsibilities)
                .HasForeignKey(hzr => hzr.ZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            // HSE Zone Delegation relations
            builder.Entity<HSEZoneDelegation>()
                .HasOne(hzd => hzd.FromHSEUser)
                .WithMany(u => u.DelegatorZones)
                .HasForeignKey(hzd => hzd.FromHSEUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<HSEZoneDelegation>()
                .HasOne(hzd => hzd.ToHSEUser)
                .WithMany(u => u.DelegatedZones)
                .HasForeignKey(hzd => hzd.ToHSEUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<HSEZoneDelegation>()
                .HasOne(hzd => hzd.Zone)
                .WithMany()
                .HasForeignKey(hzd => hzd.ZoneId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<HSEZoneDelegation>()
                .HasOne(hzd => hzd.CreatedByAdmin)
                .WithMany(u => u.CreatedDelegations)
                .HasForeignKey(hzd => hzd.CreatedByAdminId)
                .OnDelete(DeleteBehavior.NoAction);

            // Report Assignment relations
            builder.Entity<ReportAssignment>()
                .HasOne(ra => ra.Report)
                .WithMany()
                .HasForeignKey(ra => ra.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReportAssignment>()
                .HasOne(ra => ra.AssignedHSEUser)
                .WithMany()
                .HasForeignKey(ra => ra.AssignedHSEUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ReportAssignment>()
                .HasOne(ra => ra.AssignedByAdmin)
                .WithMany()
                .HasForeignKey(ra => ra.AssignedByAdminId)
                .OnDelete(DeleteBehavior.NoAction);

            // Report relations
            builder.Entity<Report>()
                .HasOne(r => r.AssignedHSE)
                .WithMany()
                .HasForeignKey(r => r.AssignedHSEId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Report>()
                .HasOne(r => r.ZoneRef)
                .WithMany(z => z.Reports)
                .HasForeignKey(r => r.ZoneId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Report>()
                .HasOne(r => r.OpenedByHSE)
                .WithMany()
                .HasForeignKey(r => r.OpenedByHSEId)
                .OnDelete(DeleteBehavior.NoAction);

            // InjuredPerson relations  
            builder.Entity<InjuredPerson>()
                .HasOne(ip => ip.Report)
                .WithMany(r => r.InjuredPersons)
                .HasForeignKey(ip => ip.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // Injury relations
            builder.Entity<Injury>()
                .HasOne(i => i.InjuredPerson)
                .WithMany(ip => ip.Injuries)
                .HasForeignKey(i => i.InjuredPersonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Injury>()
                .HasOne(i => i.BodyPart)
                .WithMany(bp => bp.Injuries)
                .HasForeignKey(i => i.BodyPartId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Injury>()
                .HasOne(i => i.FractureType)
                .WithMany(ft => ft.Injuries)
                .HasForeignKey(i => i.FractureTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ReportAttachment relations
            builder.Entity<ReportAttachment>()
                .HasOne(ra => ra.Report)
                .WithMany(r => r.Attachments)
                .HasForeignKey(ra => ra.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ CorrectiveAction relations
            builder.Entity<CorrectiveAction>()
                .HasOne(ca => ca.Report)
                .WithMany(r => r.CorrectiveActions)
                .HasForeignKey(ca => ca.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CorrectiveAction>()
                .HasOne(ca => ca.AssignedToProfile)
                .WithMany()
                .HasForeignKey(ca => ca.AssignedToProfileId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CorrectiveAction>()
                .HasOne(ca => ca.CreatedByHSE)
                .WithMany()
                .HasForeignKey(ca => ca.CreatedByHSEId)
                .OnDelete(DeleteBehavior.NoAction);

            // SubAction relations
            builder.Entity<SubAction>()
                .HasOne(sa => sa.CorrectiveAction)
                .WithMany(ca => ca.SubActions)
                .HasForeignKey(sa => sa.CorrectiveActionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SubAction>()
                .HasOne(sa => sa.AssignedTo)
                .WithMany()
                .HasForeignKey(sa => sa.AssignedToId)
                .OnDelete(DeleteBehavior.NoAction);

            // ✅ ActionAttachment relations - CORRIGÉ
            builder.Entity<ActionAttachment>()
                .HasOne(aa => aa.CorrectiveAction)
                .WithMany(ca => ca.Attachments)
                .HasForeignKey(aa => aa.CorrectiveActionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Comment relations
            builder.Entity<Comment>()
                .HasOne(c => c.Report)
                .WithMany(r => r.Comments)
                .HasForeignKey(c => c.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification relations
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.TriggeredByUser)
                .WithMany()
                .HasForeignKey(n => n.TriggeredByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.RelatedReport)
                .WithMany()
                .HasForeignKey(n => n.RelatedReportId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.RelatedAction)
                .WithMany()
                .HasForeignKey(n => n.RelatedActionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.RelatedCorrectiveAction)
                .WithMany()
                .HasForeignKey(n => n.RelatedCorrectiveActionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Email Configuration relations
            builder.Entity<EmailConfiguration>()
                .HasOne(ec => ec.UpdatedByUser)
                .WithMany()
                .HasForeignKey(ec => ec.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Email Template relations
            builder.Entity<EmailTemplate>()
                .HasOne(et => et.UpdatedByUser)
                .WithMany()
                .HasForeignKey(et => et.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Email Log relations
            builder.Entity<EmailLog>()
                .HasOne(el => el.RecipientUser)
                .WithMany()
                .HasForeignKey(el => el.RecipientUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<EmailLog>()
                .HasOne(el => el.RelatedNotification)
                .WithMany()
                .HasForeignKey(el => el.RelatedNotificationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed data
            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            // Departments
            builder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "IT Administration", Code = "IT", Description = "Information Technology Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 2, Name = "Health Safety Environment", Code = "HSE", Description = "Health, Safety and Environment Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 3, Name = "Production", Code = "PROD", Description = "Production Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 4, Name = "Quality", Code = "QUA", Description = "Quality Control Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 5, Name = "Logistics", Code = "LOG", Description = "Logistics Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 6, Name = "Engineering", Code = "ENG", Description = "Engineering Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 7, Name = "Operations", Code = "OPS", Description = "Operations Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Department { Id = 8, Name = "Maintenance", Code = "MAINT", Description = "Maintenance Department", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Zones
            builder.Entity<Zone>().HasData(
                new Zone { Id = 1, Name = "Production Area A", Code = "PROD-A", Description = "Main production area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Zone { Id = 2, Name = "Production Area B", Code = "PROD-B", Description = "Secondary production area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Zone { Id = 3, Name = "Warehouse A", Code = "WH-A", Description = "Main warehouse", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Zone { Id = 4, Name = "Warehouse B", Code = "WH-B", Description = "Secondary warehouse", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Zone { Id = 5, Name = "Office Building", Code = "OFFICE", Description = "Administrative offices", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Zone { Id = 6, Name = "Laboratory", Code = "LAB", Description = "Quality testing laboratory", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Zone { Id = 7, Name = "Loading Dock", Code = "DOCK", Description = "Loading and unloading area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Zone { Id = 8, Name = "All Areas", Code = "ALL", Description = "Access to all areas", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Shifts
            builder.Entity<Shift>().HasData(
                new Shift { Id = 1, Name = "Day Shift", Code = "DAY", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(14, 0, 0), Description = "6:00 AM - 2:00 PM", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Shift { Id = 2, Name = "Afternoon Shift", Code = "AFT", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(22, 0, 0), Description = "2:00 PM - 10:00 PM", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Shift { Id = 3, Name = "Night Shift", Code = "NIGHT", StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(6, 0, 0), Description = "10:00 PM - 6:00 AM", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Shift { Id = 4, Name = "Office Hours", Code = "OFFICE", StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 0, 0), Description = "8:00 AM - 5:00 PM", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Body Parts
            builder.Entity<BodyPart>().HasData(
                new BodyPart { Id = 1, Name = "Head", Code = "HEAD", Description = "Head and skull area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 2, Name = "Eyes", Code = "EYES", Description = "Eye area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 3, Name = "Face", Code = "FACE", Description = "Facial area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 4, Name = "Neck", Code = "NECK", Description = "Neck area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 5, Name = "Left Shoulder", Code = "L_SHOULDER", Description = "Left shoulder", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 6, Name = "Right Shoulder", Code = "R_SHOULDER", Description = "Right shoulder", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 7, Name = "Left Arm", Code = "L_ARM", Description = "Left arm", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 8, Name = "Right Arm", Code = "R_ARM", Description = "Right arm", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 9, Name = "Left Hand", Code = "L_HAND", Description = "Left hand and fingers", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 10, Name = "Right Hand", Code = "R_HAND", Description = "Right hand and fingers", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 11, Name = "Chest", Code = "CHEST", Description = "Chest area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 12, Name = "Back", Code = "BACK", Description = "Back area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 13, Name = "Abdomen", Code = "ABDOMEN", Description = "Abdominal area", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 14, Name = "Left Leg", Code = "L_LEG", Description = "Left leg", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 15, Name = "Right Leg", Code = "R_LEG", Description = "Right leg", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 16, Name = "Left Foot", Code = "L_FOOT", Description = "Left foot and toes", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new BodyPart { Id = 17, Name = "Right Foot", Code = "R_FOOT", Description = "Right foot and toes", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Fracture Types
            builder.Entity<FractureType>().HasData(
                new FractureType { Id = 1, Name = "Cut/Laceration", Code = "CUT", Category = "Cut", Description = "Cuts and lacerations", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 2, Name = "Bruise/Contusion", Code = "BRUISE", Category = "Bruise", Description = "Bruises and contusions", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 3, Name = "Burn", Code = "BURN", Category = "Burn", Description = "Thermal burns", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 4, Name = "Chemical Burn", Code = "CHEM_BURN", Category = "Burn", Description = "Chemical burns", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 5, Name = "Simple Fracture", Code = "SIMPLE_FRAC", Category = "Fracture", Description = "Simple bone fracture", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 6, Name = "Compound Fracture", Code = "COMPOUND_FRAC", Category = "Fracture", Description = "Compound bone fracture", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 7, Name = "Sprain", Code = "SPRAIN", Category = "Sprain", Description = "Joint sprain", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 8, Name = "Strain", Code = "STRAIN", Category = "Strain", Description = "Muscle strain", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 9, Name = "Puncture Wound", Code = "PUNCTURE", Category = "Cut", Description = "Puncture wounds", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new FractureType { Id = 10, Name = "Abrasion/Scrape", Code = "ABRASION", Category = "Cut", Description = "Abrasions and scrapes", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Applications
            builder.Entity<Models.Application>().HasData(
                new Models.Application { Id = 1, Title = "Chemical Product", Icon = "🧪", RedirectUrl = "http://162.109.85.69:778/app/products", IsActive = true, Order = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Models.Application { Id = 2, Title = "App 2", Icon = "🖥️", RedirectUrl = "#", IsActive = true, Order = 2, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Models.Application { Id = 3, Title = "App 3", Icon = "💼", RedirectUrl = "#", IsActive = true, Order = 3, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Models.Application { Id = 4, Title = "App 4", Icon = "📝", RedirectUrl = "#", IsActive = true, Order = 4, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Rôles
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().HasData(
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "1",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "admin-role-stamp"
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "2",
                    Name = "HSE",
                    NormalizedName = "HSE",
                    ConcurrencyStamp = "hse-role-stamp"
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "3",
                    Name = "Profil",
                    NormalizedName = "PROFIL",
                    ConcurrencyStamp = "profil-role-stamp"
                }
            );

            // Utilisateurs
            var adminUserId = "admin-default-id";
            var hseUserId = "hse-default-id";
            var profileUser1Id = "profile-user-1";
            var profileUser2Id = "profile-user-2";
            
            // Static password hashes for Admin123! and Hse123! and Profile123!
            var adminPasswordHash = "AQAAAAIAAYagAAAAECCFEZqGRq8/9qTFZpMEBwGkNpRHOYqOyqUiJjgRhiJRPpUbqLJSJJJgQl5wSPbzBw==";
            var hsePasswordHash = "AQAAAAIAAYagAAAAECCFEZqGRq8/9qTFZpMEBwGkNpRHOYqOyqUiJjgRhiJRPpUbqLJSJJJgQl5wSPbzBw==";
            var profilePasswordHash = "AQAAAAIAAYagAAAAECCFEZqGRq8/9qTFZpMEBwGkNpRHOYqOyqUiJjgRhiJRPpUbqLJSJJJgQl5wSPbzBw==";

            builder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = adminUserId,
                    UserName = "admin@te.com",
                    NormalizedUserName = "ADMIN@TE.COM",
                    Email = "admin@te.com",
                    NormalizedEmail = "ADMIN@TE.COM",
                    EmailConfirmed = true,
                    PasswordHash = adminPasswordHash,
                    SecurityStamp = "static-security-stamp",
                    ConcurrencyStamp = "static-concurrency-stamp",
                    FirstName = "System",
                    LastName = "Administrator",
                    CompanyId = "ADMIN001",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    Department = "IT",
                    Zone = "All",
                    DepartmentId = 1, // IT Administration
                    ZoneId = 8, // All Areas
                    ShiftId = 4, // Office Hours
                    Position = "System Administrator",
                    IsActive = true,
                    AccountCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ApplicationUser
                {
                    Id = hseUserId,
                    UserName = "hse@te.com",
                    NormalizedUserName = "HSE@TE.COM",
                    Email = "hse@te.com",
                    NormalizedEmail = "HSE@TE.COM",
                    EmailConfirmed = true,
                    PasswordHash = hsePasswordHash,
                    SecurityStamp = "static-security-stamp",
                    ConcurrencyStamp = "static-concurrency-stamp",
                    FirstName = "HSE",
                    LastName = "Manager",
                    CompanyId = "HSE001",
                    DateOfBirth = new DateTime(1985, 5, 15),
                    Department = "Health Safety Environment",
                    Zone = "Production Area A",
                    DepartmentId = 2, // HSE
                    ZoneId = 1, // Production Area A
                    ShiftId = 4, // Office Hours
                    Position = "HSE Manager",
                    IsActive = true,
                    AccountCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ApplicationUser
                {
                    Id = profileUser1Id,
                    UserName = "john.doe@te.com",
                    NormalizedUserName = "JOHN.DOE@TE.COM",
                    Email = "john.doe@te.com",
                    NormalizedEmail = "JOHN.DOE@TE.COM",
                    EmailConfirmed = true,
                    PasswordHash = null, // Profile users cannot login
                    SecurityStamp = "static-security-stamp",
                    ConcurrencyStamp = "static-concurrency-stamp",
                    FirstName = "John",
                    LastName = "Doe",
                    CompanyId = "TE001234",
                    DateOfBirth = new DateTime(1992, 3, 20),
                    Department = "Production",
                    Zone = "Production Area A",
                    DepartmentId = 3, // Production
                    ZoneId = 1, // Production Area A
                    ShiftId = 1, // Day Shift
                    Position = "Production Operator",
                    IsActive = true,
                    AccountCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ApplicationUser
                {
                    Id = profileUser2Id,
                    UserName = "jane.smith@te.com",
                    NormalizedUserName = "JANE.SMITH@TE.COM",
                    Email = "jane.smith@te.com",
                    NormalizedEmail = "JANE.SMITH@TE.COM",
                    EmailConfirmed = true,
                    PasswordHash = null, // Profile users cannot login
                    SecurityStamp = "static-security-stamp",
                    ConcurrencyStamp = "static-concurrency-stamp",
                    FirstName = "Jane",
                    LastName = "Smith",
                    CompanyId = "TE005678",
                    DateOfBirth = new DateTime(1988, 8, 10),
                    Department = "Quality",
                    Zone = "Warehouse B",
                    DepartmentId = 4, // Quality
                    ZoneId = 4, // Warehouse B
                    ShiftId = 2, // Afternoon Shift
                    Position = "Quality Inspector",
                    IsActive = true,
                    AccountCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Assignations de rôles
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().HasData(
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = adminUserId,
                    RoleId = "1"
                },
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = hseUserId,
                    RoleId = "2"
                },
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = profileUser1Id,
                    RoleId = "3"
                },
                new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                {
                    UserId = profileUser2Id,
                    RoleId = "3"
                }
            );
        }
    }
}