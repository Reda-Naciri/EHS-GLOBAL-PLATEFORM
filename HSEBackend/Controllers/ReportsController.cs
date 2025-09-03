using HSEBackend.Data;
using HSEBackend.Models;
using HSEBackend.Services;
using HSEBackend.Interfaces;
using HSEBackend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IReportService _reportService;
        private readonly IHSEAccessControlService _hseAccessControl;
        private readonly ILogger<ReportsController> _logger;
        private readonly ProgressCalculationService _progressService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public ReportsController(
            AppDbContext context,
            IEmailService emailService,
            IReportService reportService,
            IHSEAccessControlService hseAccessControl,
            ILogger<ReportsController> logger,
            ProgressCalculationService progressService,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _reportService = reportService;
            _hseAccessControl = hseAccessControl;
            _logger = logger;
            _progressService = progressService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        /// <summary>
        /// PUBLIC: Submit report (accessible to all, no authentication required)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitReport([FromForm] CreateReportDto reportDto)
        {
            _logger.LogInformation($"📋 Report submission received - ReporterID: {reportDto.ReporterId}, Title: {reportDto.Title}");
            _logger.LogInformation($"📎 Attachments count: {reportDto.Attachments?.Count ?? 0}");
            _logger.LogInformation($"🔧 InjuredPersonsCount: {reportDto.InjuredPersonsCount}");
            _logger.LogInformation($"🔧 InjuredPersons array count: {reportDto.InjuredPersons?.Count ?? 0}");
            
            // Handle JSON deserialization manually if needed
            if (!string.IsNullOrEmpty(reportDto.InjuredPersonsJson))
            {
                _logger.LogInformation($"🔧 Received InjuredPersonsJson: {reportDto.InjuredPersonsJson}");
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    reportDto.InjuredPersons = JsonSerializer.Deserialize<List<CreateInjuredPersonDto>>(reportDto.InjuredPersonsJson, options) ?? new List<CreateInjuredPersonDto>();
                    _logger.LogInformation($"🔧 Successfully deserialized {reportDto.InjuredPersons.Count} injured persons from JSON");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "🔧 Failed to deserialize InjuredPersonsJson");
                    return BadRequest(new { message = "Invalid injured persons data format" });
                }
            }
            
            if (reportDto.InjuredPersons != null && reportDto.InjuredPersons.Any())
            {
                for (int i = 0; i < reportDto.InjuredPersons.Count; i++)
                {
                    var person = reportDto.InjuredPersons[i];
                    _logger.LogInformation($"🔧 InjuredPerson[{i}]: Name={person.Name}, Department={person.Department}, InjuriesCount={person.Injuries?.Count ?? 0}");
                }
            }
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("📋 Model state invalid: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            try
            {
                // First validate the reporter company ID
                var reporter = await _context.Users
                    .Where(u => u.CompanyId == reportDto.ReporterId && u.IsActive)
                    .FirstOrDefaultAsync();

                if (reporter == null)
                {
                    return BadRequest(new { message = "Invalid Company ID. Please verify your Company ID." });
                }

                var report = await _reportService.CreateReportAsync(reportDto);

                _logger.LogInformation($"Report {report.Id} submitted successfully by {reportDto.ReporterId}");

                // Send notifications to all admins about new report submission
                try
                {
                    await _notificationService.NotifyAdminOnNewReportSubmissionAsync(report.Id, reportDto.ReporterId);
                    _logger.LogInformation($"✅ Sent admin notifications for new report {report.Id}");
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, $"❌ Failed to send admin notifications for report {report.Id}");
                }

                return Ok(new
                {
                    success = true,
                    message = "Report submitted successfully",
                    reportId = report.Id,
                    trackingNumber = report.TrackingNumber,
                    status = "Your report has been sent to the HSE team for review"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting report from Company ID: {CompanyId}", reportDto.ReporterId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error submitting report. Please try again later." 
                });
            }
        }

        // 🔒 HSE/Admin: Voir tous les rapports (HSE can access all reports for reading and commenting)
        [HttpGet]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetReports(
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            [FromQuery] string? zone = null)
        {
            try
            {
                // Get current user ID from JWT token
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                // HSE and Admin can access all reports for reading and commenting
                var reports = await _reportService.GetReportsAsync(userId, type, zone, status);

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports");
                return StatusCode(500, new { message = "Error fetching reports" });
            }
        }

        // 🔒 HSE/Admin: Détails d'un rapport par tracking number
        [HttpGet("tracking/{trackingNumber}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetReportByTrackingNumber(string trackingNumber)
        {
            try
            {
                var report = await _reportService.GetReportByTrackingNumberAsync(trackingNumber);
                if (report == null)
                    return NotFound(new { message = "Report not found" });

                var firstInjuredPerson = report.InjuredPersons.FirstOrDefault();
                
                var reportDto = new ReportDetailDto
                {
                    Id = report.Id,
                    TrackingNumber = report.TrackingNumber,
                    Title = report.Title,
                    Type = report.Type,
                    Zone = report.Zone,
                    ReporterId = report.ReporterCompanyId,
                    WorkShift = report.WorkShift,
                    Shift = report.WorkShift,
                    IncidentDateTime = report.IncidentDateTime,
                    ReportDateTime = report.ReportDateTime,
                    Description = report.Description,
                    
                    // Injured person details from first injured person
                    InjuredPersonName = firstInjuredPerson?.Name,
                    InjuredPersonDepartment = firstInjuredPerson?.Department,
                    InjuredPersonZone = firstInjuredPerson?.ZoneOfPerson,
                    BodyMapData = firstInjuredPerson?.SelectedBodyPart,
                    InjuryType = firstInjuredPerson?.InjuryType,
                    InjurySeverity = firstInjuredPerson?.Severity,
                    
                    // Actions taken
                    ImmediateActionsTaken = report.ImmediateActionsTaken,
                    ImmediateActions = report.ImmediateActionsTaken,
                    ActionStatus = report.ActionStatus,
                    PersonInChargeOfActions = report.PersonInChargeOfActions,
                    DateActionsCompleted = report.DateActionsCompleted,
                    
                    Status = report.Status,
                    CreatedAt = report.CreatedAt,
                    AssignedHSE = report.AssignedHSE?.FullName, // Return the user's full name instead of ID
                    InjuredPersonsCount = report.InjuredPersonsCount,
                    
                    // Relations
                    InjuredPersons = report.InjuredPersons.Select(ip => new InjuredPersonDto
                    {
                        Id = ip.Id,
                        Name = ip.Name,
                        Department = ip.Department,
                        ZoneOfPerson = ip.ZoneOfPerson,
                        Gender = ip.Gender,
                        SelectedBodyPart = ip.SelectedBodyPart,
                        InjuryType = ip.InjuryType,
                        Severity = ip.Severity,
                        InjuryDescription = ip.InjuryDescription,
                        Injuries = ip.Injuries.Select(i => new InjuryDto
                        {
                            Id = i.Id,
                            BodyPart = i.BodyPart?.Name ?? "",
                            InjuryType = i.FractureType?.Name ?? "",
                            Severity = i.Severity,
                            Description = i.Description,
                            CreatedAt = i.CreatedAt
                        }).ToList()
                    }).ToList(),
                    
                    Actions = report.Actions?.Select(a => new ActionSummaryDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description,
                        Status = a.Status,
                        DueDate = a.DueDate,
                        Priority = "Medium", // Default since not in Action model
                        Hierarchy = a.Hierarchy,
                        AssignedTo = a.AssignedTo?.FullName,
                        CreatedById = a.CreatedById,
                        CreatedByName = a.CreatedBy?.FullName,
                        SubActionsCount = a.SubActions.Count(sa => 
                            (sa.Action == null || sa.Action.Status != "Aborted") && 
                            (sa.CorrectiveAction == null || sa.CorrectiveAction.Status != "Aborted")),
                        ProgressPercentage = _progressService.CalculateSubActionsProgressPercentage(a.SubActions.ToList()),
                        CreatedAt = a.CreatedAt
                    }).ToList() ?? new List<ActionSummaryDto>(),
                    
                    CorrectiveActions = report.CorrectiveActions?.Select(ca => new CorrectiveActionSummaryDto
                    {
                        Id = ca.Id,
                        Title = ca.Title,
                        Description = ca.Description,
                        Status = ca.Status,
                        DueDate = ca.DueDate,
                        Priority = ca.Priority,
                        Hierarchy = ca.Hierarchy,
                        AssignedTo = ca.AssignedToProfile?.FullName,
                        CreatedByHSEId = ca.CreatedByHSEId,
                        CreatedByName = ca.CreatedByHSE?.FullName,
                        SubActionsCount = ca.SubActions.Count(sa => 
                            (sa.Action == null || sa.Action.Status != "Aborted") && 
                            (sa.CorrectiveAction == null || sa.CorrectiveAction.Status != "Aborted")),
                        ProgressPercentage = ca.Status == "Aborted" ? 0 : _progressService.CalculateSubActionsProgressPercentage(ca.SubActions.ToList()),
                        CreatedAt = ca.CreatedAt
                    }).ToList() ?? new List<CorrectiveActionSummaryDto>(),
                    
                    Comments = report.Comments?.Select(c => new CommentDto
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UserName = c.User?.FullName ?? "Unknown",
                        Avatar = c.User?.Avatar
                    }).ToList() ?? new List<CommentDto>(),
                    
                    Attachments = report.Attachments?.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileSize = a.FileSize,
                        FileType = a.FileType,
                        UploadedAt = a.UploadedAt,
                        DownloadUrl = $"/api/reports/{report.Id}/attachments/{a.Id}"
                    }).ToList() ?? new List<AttachmentDto>()
                };

                return Ok(reportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching report by tracking number {trackingNumber}");
                return StatusCode(500, new { message = "Error fetching report details" });
            }
        }

        // 🔒 HSE/Admin: Détails d'un rapport (HSE can access all reports for reading and commenting)
        [HttpGet("{id}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetReport(int id)
        {
            try
            {
                // HSE and Admin can access all report details for reading and commenting
                var report = await _reportService.GetReportByIdAsync(id);
                if (report == null)
                    return NotFound(new { message = "Report not found" });

                var firstInjuredPerson = report.InjuredPersons.FirstOrDefault();
                
                var reportDto = new ReportDetailDto
                {
                    Id = report.Id,
                    TrackingNumber = report.TrackingNumber,
                    Title = report.Title,
                    Type = report.Type,
                    Zone = report.Zone,
                    ReporterId = report.ReporterCompanyId,
                    WorkShift = report.WorkShift,
                    Shift = report.WorkShift,
                    IncidentDateTime = report.IncidentDateTime,
                    ReportDateTime = report.ReportDateTime,
                    Description = report.Description,
                    
                    // Injured person details from first injured person
                    InjuredPersonName = firstInjuredPerson?.Name,
                    InjuredPersonDepartment = firstInjuredPerson?.Department,
                    InjuredPersonZone = firstInjuredPerson?.ZoneOfPerson,
                    BodyMapData = firstInjuredPerson?.SelectedBodyPart,
                    InjuryType = firstInjuredPerson?.InjuryType,
                    InjurySeverity = firstInjuredPerson?.Severity,
                    
                    // Actions taken
                    ImmediateActionsTaken = report.ImmediateActionsTaken,
                    ImmediateActions = report.ImmediateActionsTaken,
                    ActionStatus = report.ActionStatus,
                    PersonInChargeOfActions = report.PersonInChargeOfActions,
                    DateActionsCompleted = report.DateActionsCompleted,
                    
                    Status = report.Status,
                    CreatedAt = report.CreatedAt,
                    AssignedHSE = report.AssignedHSE?.FullName, // Return the user's full name instead of ID
                    InjuredPersonsCount = report.InjuredPersonsCount,
                    
                    // Relations
                    InjuredPersons = report.InjuredPersons.Select(ip => new InjuredPersonDto
                    {
                        Id = ip.Id,
                        Name = ip.Name,
                        Department = ip.Department,
                        ZoneOfPerson = ip.ZoneOfPerson,
                        Gender = ip.Gender,
                        SelectedBodyPart = ip.SelectedBodyPart,
                        InjuryType = ip.InjuryType,
                        Severity = ip.Severity,
                        InjuryDescription = ip.InjuryDescription,
                        Injuries = ip.Injuries.Select(i => new InjuryDto
                        {
                            Id = i.Id,
                            BodyPart = i.BodyPart?.Name ?? "",
                            InjuryType = i.FractureType?.Name ?? "",
                            Severity = i.Severity,
                            Description = i.Description,
                            CreatedAt = i.CreatedAt
                        }).ToList()
                    }).ToList(),
                    
                    Actions = report.Actions?.Select(a => new ActionSummaryDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Description = a.Description,
                        Status = a.Status,
                        DueDate = a.DueDate,
                        Priority = "Medium", // Default since not in Action model
                        Hierarchy = a.Hierarchy,
                        AssignedTo = a.AssignedTo?.FullName,
                        CreatedById = a.CreatedById,
                        CreatedByName = a.CreatedBy?.FullName,
                        SubActionsCount = a.SubActions.Count(sa => 
                            (sa.Action == null || sa.Action.Status != "Aborted") && 
                            (sa.CorrectiveAction == null || sa.CorrectiveAction.Status != "Aborted")),
                        ProgressPercentage = _progressService.CalculateSubActionsProgressPercentage(a.SubActions.ToList()),
                        CreatedAt = a.CreatedAt
                    }).ToList() ?? new List<ActionSummaryDto>(),
                    
                    CorrectiveActions = report.CorrectiveActions?.Select(ca => new CorrectiveActionSummaryDto
                    {
                        Id = ca.Id,
                        Title = ca.Title,
                        Description = ca.Description,
                        Status = ca.Status,
                        DueDate = ca.DueDate,
                        Priority = ca.Priority,
                        Hierarchy = ca.Hierarchy,
                        AssignedTo = ca.AssignedToProfile?.FullName,
                        CreatedByHSEId = ca.CreatedByHSEId,
                        CreatedByName = ca.CreatedByHSE?.FullName,
                        SubActionsCount = ca.SubActions.Count(sa => 
                            (sa.Action == null || sa.Action.Status != "Aborted") && 
                            (sa.CorrectiveAction == null || sa.CorrectiveAction.Status != "Aborted")),
                        ProgressPercentage = ca.Status == "Aborted" ? 0 : _progressService.CalculateSubActionsProgressPercentage(ca.SubActions.ToList()),
                        CreatedAt = ca.CreatedAt
                    }).ToList() ?? new List<CorrectiveActionSummaryDto>(),
                    
                    Comments = report.Comments?.Select(c => new CommentDto
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UserName = c.User?.FullName ?? "Unknown",
                        Avatar = c.User?.Avatar
                    }).ToList() ?? new List<CommentDto>(),
                    
                    Attachments = report.Attachments?.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileSize = a.FileSize,
                        FileType = a.FileType,
                        UploadedAt = a.UploadedAt,
                        DownloadUrl = $"/api/reports/{report.Id}/attachments/{a.Id}"
                    }).ToList() ?? new List<AttachmentDto>()
                };

                return Ok(reportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching report {id}");
                return StatusCode(500, new { message = "Error fetching report details" });
            }
        }

        // 🔒 HSE/Admin: Mettre à jour le statut d'un rapport
        [HttpPut("{id}/status")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            try
            {
                var success = await _reportService.UpdateReportStatusAsync(id, statusDto.Status);
                if (!success)
                    return NotFound(new { message = "Report not found" });

                return Ok(new { message = "Report status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating report {id} status");
                return StatusCode(500, new { message = "Error updating report status" });
            }
        }

        // 🔒 Admin: Update assigned HSE agent
        [HttpPut("{id}/assign-hse")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAssignedHSE(int id, [FromBody] UpdateAssignedHSEDto dto)
        {
            _logger.LogInformation($"🔧 UpdateAssignedHSE called for report {id} with HSE ID: {dto?.AssignedHSEId}");
            
            if (dto == null)
            {
                _logger.LogError("❌ DTO is null");
                return BadRequest(new { message = "Invalid request data" });
            }
            
            try
            {
                var report = await _context.Reports.FindAsync(id);
                if (report == null)
                    return NotFound(new { message = "Report not found" });

                // Validate the HSE user exists and has HSE role
                if (!string.IsNullOrEmpty(dto.AssignedHSEId))
                {
                    var hseUser = await _context.Users
                        .Where(u => u.Id == dto.AssignedHSEId)
                        .FirstOrDefaultAsync();
                    
                    if (hseUser == null)
                        return BadRequest(new { message = "User not found" });
                    
                    var userRoles = await _userManager.GetRolesAsync(hseUser);
                    if (!userRoles.Contains("HSE"))
                        return BadRequest(new { message = "User is not an HSE agent" });
                }

                var previousHSEId = report.AssignedHSEId;
                report.AssignedHSEId = dto.AssignedHSEId;
                report.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification to newly assigned HSE user
                if (!string.IsNullOrEmpty(dto.AssignedHSEId) && dto.AssignedHSEId != previousHSEId)
                {
                    var currentUserId = User.FindFirst("UserId")?.Value;
                    try
                    {
                        await _notificationService.NotifyHSEOnReportAssignmentAsync(id, dto.AssignedHSEId, currentUserId ?? "system");
                        _logger.LogInformation($"✅ Sent report assignment notification to HSE user {dto.AssignedHSEId} for report {id}");
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, $"❌ Failed to send report assignment notification for report {id}");
                    }
                }

                return Ok(new { message = "HSE agent assignment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating HSE assignment for report {id}");
                _logger.LogError($"❌ Exception details: {ex.Message}");
                _logger.LogError($"❌ Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Error updating HSE assignment: {ex.Message}" });
            }
        }

        // 🔒 HSE/Admin: Ajouter un commentaire interne
        [HttpPost("{id}/comments")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentDto commentDto)
        {
            try
            {
                var report = await _context.Reports.FindAsync(id);
                if (report == null)
                    return NotFound(new { message = "Report not found" });

                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var comment = new Comment
                {
                    Content = commentDto.Content,
                    ReportId = id,
                    UserId = userId,
                    IsInternal = true // Toujours interne pour HSE/Admin
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Send notification to assigned HSE user if report is assigned and commenter is not the assigned HSE
                if (!string.IsNullOrEmpty(report.AssignedHSEId) && report.AssignedHSEId != userId)
                {
                    try
                    {
                        await _notificationService.NotifyHSEOnNewCommentAsync(id, userId, report.AssignedHSEId);
                        _logger.LogInformation($"✅ Sent comment notification to HSE user {report.AssignedHSEId} for report {id}");
                    }
                    catch (Exception notificationEx)
                    {
                        _logger.LogError(notificationEx, $"❌ Failed to send comment notification for report {id}");
                    }
                }

                return Ok(new { message = "Comment added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment to report {id}");
                return StatusCode(500, new { message = "Error adding comment" });
            }
        }

        // 🔒 Admin: Test endpoint to verify new routes work
        [HttpGet("test-new-endpoint")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestNewEndpoint()
        {
            return Ok(new { message = "New endpoint is working!", timestamp = DateTime.UtcNow });
        }

        [HttpGet("debug-hse-data")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> DebugHSEData()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                
                // Get current user info
                var user = await _context.Users.FindAsync(userId);
                
                // Get all reports
                var allReports = await _context.Reports
                    .Select(r => new { 
                        r.Id, 
                        r.Title, 
                        r.AssignedHSEId, 
                        r.Status, 
                        r.Zone,
                        r.ZoneId,
                        AssignedHSEEmail = r.AssignedHSE != null ? r.AssignedHSE.Email : null
                    })
                    .ToListAsync();

                // Get HSE zone responsibilities for this user
                var zoneResponsibilities = await _context.HSEZoneResponsibilities
                    .Where(hzr => hzr.HSEUserId == userId && hzr.IsActive)
                    .Select(hzr => new { hzr.ZoneId, ZoneName = hzr.Zone.Name })
                    .ToListAsync();

                return Ok(new {
                    CurrentUser = new {
                        user?.Id,
                        user?.Email,
                        user?.Zone,
                        user?.ZoneId
                    },
                    AllReports = allReports,
                    UserZoneResponsibilities = zoneResponsibilities,
                    TotalReports = allReports.Count,
                    ReportsAssignedToUser = allReports.Where(r => r.AssignedHSEId == userId).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // 🔒 Admin: Get all HSE users for assignment dropdown
        [HttpGet("hse-users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetHSEUsers()
        {
            _logger.LogInformation("🔧 GetHSEUsers called");
            try
            {
                // Get all users with HSE role using UserManager
                var allUsers = await _context.Users.ToListAsync();
                var hseUsers = new List<object>();
                
                foreach (var user in allUsers)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("HSE"))
                    {
                        hseUsers.Add(new {
                            id = user.Id,
                            name = user.FullName ?? $"{user.FirstName} {user.LastName}".Trim(),
                            email = user.Email
                        });
                    }
                }

                return Ok(hseUsers.OrderBy(u => ((dynamic)u).name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching HSE users");
                return StatusCode(500, new { message = "Error fetching HSE users" });
            }
        }

        // 🔒 HSE/Admin: Rapports récents pour dashboard
        [HttpGet("recent")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetRecentReports([FromQuery] int limit = 10)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                var recentReports = await _reportService.GetRecentReportsAsync(userId, limit);

                return Ok(recentReports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent reports");
                return StatusCode(500, new { message = "Error fetching recent reports" });
            }
        }

        /// <summary>
        /// Get reports by company ID for follow-up
        /// </summary>
        [HttpGet("by-company/{companyId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReportsByCompanyId(string companyId)
        {
            try
            {
                _logger.LogInformation($"Getting reports for company ID: {companyId}");
                
                var reports = await _context.Reports
                    .Where(r => r.ReporterCompanyId == companyId)
                    .OrderByDescending(r => r.CreatedAt) // Newest first
                    .Select(r => new
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Type = r.Type,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        TrackingNumber = r.TrackingNumber,
                        Zone = r.Zone
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {reports.Count} reports for company {companyId}");
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reports for company {companyId}");
                return StatusCode(500, new { message = "Error retrieving reports" });
            }
        }

        /// <summary>
        /// Get report details for follow-up (public, excludes sensitive data like comments)
        /// </summary>
        [HttpGet("follow-up/tracking/{trackingNumber}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReportForFollowUp(string trackingNumber)
        {
            try
            {
                _logger.LogInformation($"Getting report for follow-up by tracking number: {trackingNumber}");
                
                var report = await _reportService.GetReportByTrackingNumberAsync(trackingNumber);
                if (report == null)
                    return NotFound(new { message = "Report not found" });

                var firstInjuredPerson = report.InjuredPersons.FirstOrDefault();
                
                // Return limited data excluding sensitive information like comments
                var reportDto = new
                {
                    Id = report.Id,
                    TrackingNumber = report.TrackingNumber,
                    Title = report.Title,
                    Type = report.Type,
                    Zone = report.Zone,
                    ReporterId = report.ReporterCompanyId,
                    WorkShift = report.WorkShift,
                    IncidentDateTime = report.IncidentDateTime,
                    ReportDateTime = report.ReportDateTime,
                    Description = report.Description,
                    
                    // Injured person details from first injured person
                    InjuredPersonName = firstInjuredPerson?.Name,
                    InjuredPersonDepartment = firstInjuredPerson?.Department,
                    InjuredPersonZone = firstInjuredPerson?.ZoneOfPerson,
                    InjuryType = firstInjuredPerson?.InjuryType,
                    InjurySeverity = firstInjuredPerson?.Severity,
                    
                    // Actions taken by reporter
                    ImmediateActionsTaken = report.ImmediateActionsTaken,
                    ActionStatus = report.ActionStatus,
                    PersonInChargeOfActions = report.PersonInChargeOfActions,
                    DateActionsCompleted = report.DateActionsCompleted,
                    
                    Status = report.Status,
                    CreatedAt = report.CreatedAt,
                    InjuredPersonsCount = report.InjuredPersonsCount,
                    
                    // Note: In this system there are no separate HSE Actions - only corrective actions
                    
                    // Read-only Corrective Actions with Sub-Actions (summary only, no sensitive details)
                    CorrectiveActions = report.CorrectiveActions?.Select(ca => new
                    {
                        Id = ca.Id,
                        Title = ca.Title,
                        Description = ca.Description,
                        Status = ca.Status,
                        DueDate = ca.DueDate,
                        Priority = ca.Priority,
                        AssignedTo = ca.AssignedToProfile?.FullName,
                        SubActionsCount = ca.SubActions.Count(sa => sa.Status != "Canceled"),
                        ProgressPercentage = ca.Status == "Aborted" ? 0 : _progressService.CalculateSubActionsProgressPercentage(ca.SubActions.ToList()),
                        CreatedAt = ca.CreatedAt,
                        // Include sub-actions for better tracking visibility
                        SubActions = ca.SubActions?.Select(sa => new
                        {
                            Id = sa.Id,
                            Title = sa.Title,
                            Description = sa.Description,
                            Status = sa.Status,
                            DueDate = sa.DueDate,
                            AssignedTo = sa.AssignedTo?.FullName,
                            CreatedAt = sa.CreatedAt
                        }).ToList()
                    }).ToList(),
                    
                    // Include attachments for follow-up tracking (without sensitive content)
                    Attachments = report.Attachments?.Select(att => new
                    {
                        Id = att.Id,
                        FileName = att.FileName,
                        FileSize = att.FileSize,
                        UploadedAt = att.UploadedAt
                        // Note: FilePath is excluded for security
                    }).ToList()
                    
                    // Intentionally exclude Comments for public access
                };

                return Ok(reportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching report for follow-up by tracking number {trackingNumber}");
                return StatusCode(500, new { message = "Error fetching report details" });
            }
        }

        /// <summary>
        /// Validate if a company ID exists in the system
        /// </summary>
        [HttpGet("validate-company/{companyId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateCompanyId(string companyId)
        {
            try
            {
                _logger.LogInformation($"Validating company ID: {companyId}");
                
                // Check if there's any user with this company ID
                var user = await _context.Users
                    .Where(u => u.CompanyId == companyId)
                    .Select(u => new 
                    {
                        u.Id,
                        u.CompanyId,
                        u.FirstName,
                        u.LastName,
                        u.Department,
                        u.Position,
                        u.Email
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogInformation($"Company ID not found: {companyId}");
                    return Ok(new 
                    { 
                        isValid = false, 
                        message = $"Company ID \"{companyId}\" does not exist in the system." 
                    });
                }

                _logger.LogInformation($"Company ID found: {companyId} for user {user.Email}");
                return Ok(new 
                { 
                    isValid = true,
                    userId = user.Id,
                    companyId = user.CompanyId,
                    reporterName = $"{user.FirstName} {user.LastName}".Trim(),
                    department = user.Department,
                    position = user.Position,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating company ID: {companyId}");
                return StatusCode(500, new { message = "Error validating company ID" });
            }
        }

        /// <summary>
        /// Download attachment file
        /// </summary>
        [HttpGet("{reportId}/attachments/{attachmentId}")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> DownloadAttachment(int reportId, int attachmentId)
        {
            try
            {
                var attachment = await _context.ReportAttachments
                    .Where(a => a.ReportId == reportId && a.Id == attachmentId)
                    .FirstOrDefaultAsync();

                if (attachment == null)
                    return NotFound(new { message = "Attachment not found" });

                if (!System.IO.File.Exists(attachment.FilePath))
                    return NotFound(new { message = "File not found on disk" });

                var fileBytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath);
                return File(fileBytes, attachment.FileType ?? "application/octet-stream", attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {AttachmentId} for report {ReportId}", attachmentId, reportId);
                return StatusCode(500, new { message = "Error downloading attachment" });
            }
        }

        /// <summary>
        /// Download attachment file for follow-up (anonymous access for report tracking)
        /// </summary>
        [HttpGet("follow-up/{reportId}/attachments/{attachmentId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFollowUpAttachment(int reportId, int attachmentId)
        {
            try
            {
                // Verify the attachment belongs to a report that exists
                var attachment = await _context.ReportAttachments
                    .Include(a => a.Report)
                    .Where(a => a.ReportId == reportId && a.Id == attachmentId)
                    .FirstOrDefaultAsync();

                if (attachment == null || attachment.Report == null)
                    return NotFound(new { message = "Attachment not found" });

                if (!System.IO.File.Exists(attachment.FilePath))
                    return NotFound(new { message = "File not found on disk" });

                var fileBytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath);
                return File(fileBytes, attachment.FileType ?? "application/octet-stream", attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading follow-up attachment {AttachmentId} for report {ReportId}", attachmentId, reportId);
                return StatusCode(500, new { message = "Error downloading attachment" });
            }
        }
    }
}