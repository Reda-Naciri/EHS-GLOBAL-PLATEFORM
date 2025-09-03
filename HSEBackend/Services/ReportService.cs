using HSEBackend.Data;
using HSEBackend.DTOs;
using HSEBackend.Interfaces;
using HSEBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace HSEBackend.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReportService> _logger;
        private readonly IHSEAccessControlService _accessControl;
        private readonly INotificationService _notificationService;
        private readonly IEnhancedEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportService(
            AppDbContext context, 
            ILogger<ReportService> logger, 
            IHSEAccessControlService accessControl,
            INotificationService notificationService,
            IEnhancedEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _accessControl = accessControl;
            _notificationService = notificationService;
            _emailService = emailService;
            _userManager = userManager;
        }

        public async Task<Report> CreateReportAsync(CreateReportDto dto)
        {
            try
            {
                _logger.LogInformation($"🔧 Creating report - InjuredPersonsCount: {dto.InjuredPersonsCount}");
                _logger.LogInformation($"🔧 InjuredPersons array count: {dto.InjuredPersons?.Count ?? 0}");
                _logger.LogInformation($"🔧 InjuredPersonsJson: {dto.InjuredPersonsJson}");
                
                var report = new Report
                {
                    TrackingNumber = await GenerateTrackingNumberAsync(),
                    ReporterCompanyId = dto.ReporterId,
                    WorkShift = dto.WorkShift,
                    Title = dto.Title,
                    Type = dto.Type,
                    Zone = dto.Zone,
                    IncidentDateTime = dto.IncidentDateTime,
                    Description = dto.Description,
                    InjuredPersonsCount = dto.InjuredPersonsCount,
                    ImmediateActionsTaken = dto.ImmediateActionsTaken,
                    ActionStatus = dto.ActionStatus ?? "Non commencé",
                    PersonInChargeOfActions = dto.PersonInChargeOfActions,
                    DateActionsCompleted = dto.DateActionsCompleted,
                    Status = "Unopened",
                    CreatedAt = DateTime.UtcNow
                };

                // Process injured persons - try JSON string first, then array
                List<CreateInjuredPersonDto> injuredPersonsToProcess = new();
                
                if (!string.IsNullOrEmpty(dto.InjuredPersonsJson))
                {
                    try
                    {
                        _logger.LogInformation("🔧 Deserializing injured persons from JSON string");
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        injuredPersonsToProcess = JsonSerializer.Deserialize<List<CreateInjuredPersonDto>>(dto.InjuredPersonsJson, options) ?? new();
                        _logger.LogInformation($"🔧 Deserialized {injuredPersonsToProcess.Count} injured persons from JSON");
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "🔧 Failed to deserialize injured persons JSON");
                    }
                }
                else if (dto.InjuredPersons != null && dto.InjuredPersons.Any())
                {
                    _logger.LogInformation("🔧 Using injured persons from direct binding");
                    injuredPersonsToProcess = dto.InjuredPersons;
                }

                // Add injured persons
                if (injuredPersonsToProcess.Any())
                {
                    _logger.LogInformation($"🔧 Processing {injuredPersonsToProcess.Count} injured persons...");
                    
                    foreach (var injuredPersonDto in injuredPersonsToProcess)
                    {
                        _logger.LogInformation($"🔧 Processing injured person: {injuredPersonDto.Name}");
                        
                        var injuredPerson = new InjuredPerson
                        {
                            Name = injuredPersonDto.Name,
                            Department = injuredPersonDto.Department,
                            ZoneOfPerson = injuredPersonDto.ZoneOfPerson,
                            Gender = injuredPersonDto.Gender,
                            SelectedBodyPart = injuredPersonDto.SelectedBodyPart,
                            InjuryType = injuredPersonDto.InjuryType,
                            Severity = injuredPersonDto.Severity,
                            InjuryDescription = injuredPersonDto.InjuryDescription,
                            CreatedAt = DateTime.UtcNow
                        };

                        // Add injuries
                        if (injuredPersonDto.Injuries != null && injuredPersonDto.Injuries.Any())
                        {
                            _logger.LogInformation($"🔧 Adding {injuredPersonDto.Injuries.Count} injuries for {injuredPersonDto.Name}");
                            
                            foreach (var injuryDto in injuredPersonDto.Injuries)
                            {
                                var injury = new Injury
                                {
                                    BodyPartId = injuryDto.BodyPartId,
                                    FractureTypeId = injuryDto.FractureTypeId,
                                    Severity = injuryDto.Severity,
                                    Description = injuryDto.Description,
                                    CreatedAt = DateTime.UtcNow
                                };
                                injuredPerson.Injuries.Add(injury);
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"🔧 No injuries found for injured person: {injuredPersonDto.Name}");
                        }

                        report.InjuredPersons.Add(injuredPerson);
                    }
                    
                    _logger.LogInformation($"🔧 Successfully added {report.InjuredPersons.Count} injured persons to report");
                }
                else
                {
                    _logger.LogWarning("🔧 No injured persons data received in DTO");
                }

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                // Process and save attachments if any
                if (dto.Attachments != null && dto.Attachments.Any())
                {
                    await ProcessAttachmentsAsync(report.Id, dto.Attachments);
                }

                // Auto-assign HSE user based on zone after report is created
                await AutoAssignHSEUserAsync(report);

                // Send notifications and emails after HSE assignment
                await SendNewReportNotificationsAsync(report, dto.ReporterId);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }

        private async Task ProcessAttachmentsAsync(int reportId, List<IFormFile> attachments)
        {
            try
            {
                _logger.LogInformation($"Processing {attachments.Count} attachments for report {reportId}");

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "attachments");
                Directory.CreateDirectory(uploadsPath);

                foreach (var file in attachments)
                {
                    if (file.Length > 0)
                    {
                        // Generate unique filename
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var filePath = Path.Combine(uploadsPath, fileName);

                        // Save file to disk
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Create database record
                        var attachment = new ReportAttachment
                        {
                            ReportId = reportId,
                            FileName = file.FileName,
                            FilePath = filePath,
                            FileType = file.ContentType,
                            FileSize = file.Length,
                            UploadedAt = DateTime.UtcNow
                        };

                        _context.ReportAttachments.Add(attachment);
                        _logger.LogInformation($"Saved attachment: {file.FileName} ({file.Length} bytes)");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully processed {attachments.Count} attachments for report {reportId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing attachments for report {reportId}");
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(UpdateStatusDto dto)
        {
            try
            {
                var report = await _context.Reports.FindAsync(dto.ReportId);
                if (report == null)
                    return false;

                report.Status = dto.NewStatus;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report status");
                throw;
            }
        }

        public async Task<Comment> AddCommentAsync(CreateCommentDto dto)
        {
            try
            {
                var comment = new Comment
                {
                    Content = dto.Content,
                    ReportId = dto.ReportId,
                    UserId = dto.Author,
                    CreatedAt = DateTime.UtcNow,
                    IsInternal = true
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                throw;
            }
        }

        public async Task<IEnumerable<Report>> GetAllReportsAsync()
        {
            try
            {
                return await _context.Reports
                    .Include(r => r.InjuredPersons)
                        .ThenInclude(ip => ip.Injuries)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.CreatedBy)
                    .Include(r => r.CorrectiveActions)
                    .Include(r => r.Comments)
                        .ThenInclude(c => c.User)
                    .Include(r => r.Attachments)
                    .Include(r => r.AssignedHSE)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all reports");
                throw;
            }
        }

        public async Task<Report?> GetReportByIdAsync(int id)
        {
            try
            {
                return await _context.Reports
                    .Include(r => r.InjuredPersons)
                        .ThenInclude(ip => ip.Injuries)
                            .ThenInclude(i => i.BodyPart)
                    .Include(r => r.InjuredPersons)
                        .ThenInclude(ip => ip.Injuries)
                            .ThenInclude(i => i.FractureType)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.CreatedBy)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.SubActions)
                    .Include(r => r.CorrectiveActions)
                        .ThenInclude(ca => ca.SubActions)
                    .Include(r => r.CorrectiveActions)
                        .ThenInclude(ca => ca.CreatedByHSE)
                    .Include(r => r.Comments)
                        .ThenInclude(c => c.User)
                    .Include(r => r.Attachments)
                    .Include(r => r.AssignedHSE)
                    .FirstOrDefaultAsync(r => r.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report by id: {Id}", id);
                throw;
            }
        }

        public async Task<Report?> GetReportByTrackingNumberAsync(string trackingNumber)
        {
            try
            {
                _logger.LogInformation($"🔍 Getting report by tracking number: {trackingNumber}");
                
                return await _context.Reports
                    .Include(r => r.InjuredPersons)
                        .ThenInclude(ip => ip.Injuries)
                            .ThenInclude(i => i.BodyPart)
                    .Include(r => r.InjuredPersons)
                        .ThenInclude(ip => ip.Injuries)
                            .ThenInclude(i => i.FractureType)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.CreatedBy)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.SubActions)
                    .Include(r => r.CorrectiveActions)
                        .ThenInclude(ca => ca.SubActions)
                            .ThenInclude(sa => sa.AssignedTo)
                    .Include(r => r.CorrectiveActions)
                        .ThenInclude(ca => ca.AssignedToProfile)
                    .Include(r => r.Comments)
                        .ThenInclude(c => c.User)
                    .Include(r => r.Attachments)
                    .Include(r => r.AssignedHSE)
                    .FirstOrDefaultAsync(r => r.TrackingNumber == trackingNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report by tracking number: {TrackingNumber}", trackingNumber);
                throw;
            }
        }

        public async Task<IEnumerable<ReportSummaryDto>> GetReportsAsync(string userId, string? type = null, string? zone = null, string? status = null)
        {
            try
            {
                // SIMPLE approach - get all reports and filter later
                var query = _context.Reports
                    .Include(r => r.InjuredPersons)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.CreatedBy)
                    .Include(r => r.Actions)
                        .ThenInclude(a => a.SubActions)
                    .Include(r => r.CorrectiveActions)
                        .ThenInclude(ca => ca.SubActions)
                    .Include(r => r.Attachments)
                    .Include(r => r.AssignedHSE)
                    .Include(r => r.ZoneRef)
                    .AsQueryable();

                // Apply basic filters
                if (!string.IsNullOrEmpty(type))
                    query = query.Where(r => r.Type == type);

                if (!string.IsNullOrEmpty(zone))
                    query = query.Where(r => r.Zone == zone);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(r => r.Status == status);

                var visibleReports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

                // For now, everyone sees all reports (we'll add access control later)

                return visibleReports.Select(r => new ReportSummaryDto
                {
                    Id = r.Id,
                    TrackingNumber = r.TrackingNumber,
                    Title = r.Title,
                    Type = r.Type,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ReporterId = r.ReporterCompanyId,
                    Zone = r.Zone,
                    AssignedHSE = r.AssignedHSE?.FullName, // Return the user's full name instead of ID
                    InjuredPersonsCount = r.InjuredPersonsCount,
                    HasAttachments = r.Attachments.Any(),
                    ActionsCount = CalculateActionsCount(r),
                    CorrectiveActionsCount = r.CorrectiveActions.Count(ca => ca.Status != "Aborted" && ca.Status != "Canceled"),
                    InjurySeverity = r.InjuredPersons.Any() ? r.InjuredPersons.Max(ip => ip.Severity) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports with filters");
                throw;
            }
        }

        public async Task<bool> UpdateReportStatusAsync(int reportId, string status)
        {
            try
            {
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                    return false;

                report.Status = status;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report status");
                throw;
            }
        }

        public async Task<bool> OpenReportAsync(int reportId, string userId)
        {
            try
            {
                // Check if user can open the report
                if (!await _accessControl.CanOpenReportAsync(userId, reportId))
                    return false;

                var report = await _context.Reports.FindAsync(reportId);
                if (report == null || report.Status != "Unopened")
                    return false;

                // Open the report
                report.Status = "Opened";
                report.OpenedAt = DateTime.UtcNow;
                report.OpenedByHSEId = userId;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening report {ReportId} by user {UserId}", reportId, userId);
                throw;
            }
        }

        public async Task<IEnumerable<RecentReportDto>> GetRecentReportsAsync(string userId, int limit = 10)
        {
            try
            {
                // SIMPLE approach - get recent reports
                var reports = await _context.Reports
                    .Include(r => r.InjuredPersons)
                    .Include(r => r.AssignedHSE)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                return reports.Select(r => new RecentReportDto
                {
                    Id = r.Id,
                    TrackingNumber = r.TrackingNumber,
                    Title = r.Title,
                    Type = r.Type,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ReporterId = r.ReporterCompanyId,
                    Zone = r.Zone,
                    AssignedHSE = r.AssignedHSE?.FullName, // Return the user's full name instead of ID
                    InjurySeverity = r.InjuredPersons.Any() ? r.InjuredPersons.Max(ip => ip.Severity) : null,
                    IsUrgent = r.Status == "Unopened" && r.CreatedAt > DateTime.UtcNow.AddDays(-1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent reports");
                throw;
            }
        }

        /// <summary>
        /// Automatically assigns an HSE user to a report based on the zone
        /// Handles zone responsibilities and delegations
        /// </summary>
        private async Task AutoAssignHSEUserAsync(Report report)
        {
            try
            {
                _logger.LogInformation($"🔍 Auto-assigning HSE user for report {report.Id} in zone: {report.Zone}");

                // Step 1: Find the zone by name or code
                var zone = await _context.Zones
                    .FirstOrDefaultAsync(z => z.Name == report.Zone || z.Code == report.Zone);

                if (zone == null)
                {
                    _logger.LogWarning($"⚠️ Zone '{report.Zone}' not found in database for report {report.Id}");
                    return;
                }

                _logger.LogInformation($"✅ Found zone: {zone.Name} (ID: {zone.Id})");

                // Step 2: Check for active delegations first (delegated users take priority)
                var activeDelegation = await _context.HSEZoneDelegations
                    .Include(d => d.ToHSEUser)
                    .Where(d => d.ZoneId == zone.Id && 
                               d.IsActive && 
                               d.StartDate <= DateTime.UtcNow && 
                               d.EndDate >= DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (activeDelegation != null)
                {
                    _logger.LogInformation($"🔄 Found active delegation: Zone {zone.Name} delegated to {activeDelegation.ToHSEUser.Email}");
                    
                    // Assign to delegated HSE user
                    report.AssignedHSEId = activeDelegation.ToHSEUserId;
                    report.ZoneId = zone.Id;
                    report.UpdatedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"✅ Report {report.Id} assigned to delegated HSE user: {activeDelegation.ToHSEUser.Email}");
                    return;
                }

                // Step 3: Find HSE users responsible for this zone
                var zoneResponsibilities = await _context.HSEZoneResponsibilities
                    .Include(r => r.HSEUser)
                    .Where(r => r.ZoneId == zone.Id && r.IsActive)
                    .ToListAsync();

                if (!zoneResponsibilities.Any())
                {
                    _logger.LogWarning($"⚠️ No HSE users assigned to zone '{zone.Name}' for report {report.Id}");
                    return;
                }

                // Step 4: Select the HSE user to assign (for now, take the first one)
                // TODO: Could implement more sophisticated assignment logic (round-robin, workload balancing, etc.)
                var selectedResponsibility = zoneResponsibilities.First();
                
                _logger.LogInformation($"✅ Found {zoneResponsibilities.Count} HSE user(s) responsible for zone {zone.Name}");
                _logger.LogInformation($"📋 Assigning to: {selectedResponsibility.HSEUser.Email}");

                // Step 5: Assign the report
                report.AssignedHSEId = selectedResponsibility.HSEUserId;
                report.ZoneId = zone.Id;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Report {report.Id} successfully assigned to HSE user: {selectedResponsibility.HSEUser.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error auto-assigning HSE user for report {report.Id}");
                // Don't throw - report creation should still succeed even if assignment fails
            }
        }

        /// <summary>
        /// Generates a unique tracking number for the report (e.g., RPT-2024-001234)
        /// </summary>
        private async Task<string> GenerateTrackingNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"RPT-{year}-";
            
            // Get the highest tracking number for this year
            var lastReport = await _context.Reports
                .Where(r => r.TrackingNumber.StartsWith(prefix))
                .OrderByDescending(r => r.TrackingNumber)
                .FirstOrDefaultAsync();
            
            int nextNumber = 1;
            
            if (lastReport != null)
            {
                // Extract the number part from the tracking number
                var numberPart = lastReport.TrackingNumber.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }
            
            // Format as 6-digit number with leading zeros
            var trackingNumber = $"{prefix}{nextNumber:D6}";
            
            _logger.LogInformation($"Generated tracking number: {trackingNumber}");
            
            return trackingNumber;
        }

        /// <summary>
        /// Calculate the total count of non-canceled sub-actions for a report
        /// </summary>
        private int CalculateActionsCount(Report report)
        {
            int actionSubActionsCount = 0;
            int correctiveActionSubActionsCount = 0;

            // Count sub-actions from Actions (only exclude canceled sub-actions)
            foreach (var action in report.Actions)
            {
                var count = action.SubActions.Count(sa => sa.Status != "Canceled");
                actionSubActionsCount += count;
                _logger.LogInformation($"Action {action.Id} has {count} non-canceled sub-actions");
            }

            // Count sub-actions from CorrectiveActions (only exclude canceled sub-actions)
            foreach (var correctiveAction in report.CorrectiveActions)
            {
                var count = correctiveAction.SubActions.Count(sa => sa.Status != "Canceled");
                correctiveActionSubActionsCount += count;
                _logger.LogInformation($"CorrectiveAction {correctiveAction.Id} has {count} non-canceled sub-actions");
            }

            var totalCount = actionSubActionsCount + correctiveActionSubActionsCount;
            _logger.LogInformation($"Report {report.Id} total ActionsCount: {totalCount} (Actions: {actionSubActionsCount}, CorrectiveActions: {correctiveActionSubActionsCount})");

            return totalCount;
        }

        /// <summary>
        /// Sends notifications and emails when a new report is submitted
        /// </summary>
        private async Task SendNewReportNotificationsAsync(Report report, string reporterCompanyId)
        {
            try
            {
                _logger.LogInformation($"🔔 Starting notifications for new report {report.Id}");

                // Find the reporter user for notifications
                var reporter = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.CompanyId == reporterCompanyId && u.IsActive);

                if (reporter == null)
                {
                    _logger.LogWarning($"⚠️ Reporter with CompanyId {reporterCompanyId} not found for notifications");
                    return;
                }

                // 1. Notify assigned HSE user(s) via notification system
                if (!string.IsNullOrEmpty(report.AssignedHSEId))
                {
                    _logger.LogInformation($"🔔 Sending notification to assigned HSE user: {report.AssignedHSEId}");
                    await _notificationService.NotifyHSEOnNewReportSubmissionAsync(report.Id, reporter.Id);
                }

                // 2. Send instant email notification to HSE users if enabled
                _logger.LogInformation($"📧 Sending instant email notification for report {report.Id}");
                await _emailService.SendInstantReportNotificationToHSEAsync(report.Id);

                _logger.LogInformation($"✅ Successfully sent notifications for new report {report.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending notifications for new report {report.Id}");
                // Don't throw - report creation should still succeed even if notifications fail
            }
        }
    }
}