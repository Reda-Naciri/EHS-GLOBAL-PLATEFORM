using HSEBackend.Data;
using HSEBackend.DTOs;
using HSEBackend.Models;
using HSEBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/register-request")]
    public class RegistrationRequestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IEnhancedEmailService _enhancedEmailService;

        public RegistrationRequestController(AppDbContext context, IEmailService emailService, UserManager<ApplicationUser> userManager, INotificationService notificationService, IEnhancedEmailService enhancedEmailService)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _notificationService = notificationService;
            _enhancedEmailService = enhancedEmailService;
        }

        // 🔓 Public: Submit a new Profil request
        [HttpPost]
        public async Task<IActionResult> SubmitRequest([FromBody] RegistrationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email || u.UserName == request.CompanyId);

            var pendingUserExists = await _context.PendingUsers
                .AnyAsync(p => p.Email == request.Email || p.CompanyId == request.CompanyId);

            if (userExists || pendingUserExists)
                return BadRequest(new { message = "An account with this email or ID already exists." });

            var exists = await _context.RegistrationRequests
                .AnyAsync(r =>
                    (r.Email == request.Email || r.CompanyId == request.CompanyId) &&
                    r.Status == "Pending");

            if (exists)
                return BadRequest(new { message = "A pending request with this email or company ID already exists." });

            request.Status = "Pending";
            request.SubmittedAt = DateTime.UtcNow;

            _context.RegistrationRequests.Add(request);
            await _context.SaveChangesAsync();

            // ✅ Email au demandeur
            await _emailService.SendGenericEmail(
                request.Email,
                "Registration Request Received",
                $"Hello {request.FullName},<br><br>Your registration request has been received and is currently under review by our HSE team.<br><br>" +
                $"<b>Request Details:</b><br>" +
                $"Company ID: {request.CompanyId}<br>" +
                $"Position: {request.Position}<br>" +
                $"Department: {request.Department}<br><br>" +
                $"You will receive another email once it's approved or rejected.<br><br>Thank you.<br>HSE Team"
            );

            // ✅ Send email notifications to admin users (using same logic as admin overview emails)
            try
            {
                var config = await _enhancedEmailService.GetEmailConfigurationAsync();
                
                // Get admin users to notify (using same method as admin overview emails)
                var usersToNotify = await GetAdminUsersForRegistrationNotification(config);
                
                foreach (var user in usersToNotify)
                {
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendGenericEmail(
                            user.Email,
                            $"[HSE Platform] New Registration Request: {request.FullName}",
                            $"A new registration request has been submitted:<br><br>" +
                            $"<b>Name:</b> {request.FullName}<br>" +
                            $"<b>ID:</b> {request.CompanyId}<br>" +
                            $"<b>Email:</b> {request.Email}<br>" +
                            $"<b>Department:</b> {request.Department}<br>" +
                            $"<b>Position:</b> {request.Position}<br>" +
                            $"<b>Submitted At:</b> {request.SubmittedAt}<br><br>" +
                            $"Please review it in the HSE admin panel."
                        );
                    }
                }
                
                Console.WriteLine($"✅ Sent email notifications to {usersToNotify.Count} admin/HSE users for registration request {request.Id}");
            }
            catch (Exception emailEx)
            {
                Console.WriteLine($"❌ Failed to send email notifications for registration request {request.Id}: {emailEx.Message}");
            }

            // Send in-app notification to all HSE users about new registration request
            try
            {
                await _notificationService.NotifyHSEOnNewRegistrationRequestAsync(request.Id, request.FullName, request.CompanyId);
                Console.WriteLine($"✅ Sent in-app notification for registration request {request.Id}");
            }
            catch (Exception notificationEx)
            {
                // Log error but don't fail the request submission
                Console.WriteLine($"❌ Failed to send in-app notification for registration request {request.Id}: {notificationEx.Message}");
            }

            return Ok(new { message = "Registration request submitted successfully." });
        }

        // 🔒 Admin/HSE: View all requests
        [HttpGet]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetAllRequests()
        {
            // Get all requests and sort them properly
            var allRequests = await _context.RegistrationRequests.ToListAsync();
            
            // Separate pending and non-pending requests
            var pendingRequests = allRequests
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();
                
            var processedRequests = allRequests
                .Where(r => r.Status != "Pending")
                .OrderByDescending(r => r.SubmittedAt)
                .ToList();
            
            // Combine: pending first, then processed
            var sortedRequests = pendingRequests.Concat(processedRequests).ToList();
            
            return Ok(sortedRequests);
        }

        // 🔒 Admin/HSE: Get count of pending requests
        [HttpGet("count")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> GetPendingCount()
        {
            var count = await _context.RegistrationRequests
                .Where(r => r.Status == "Pending")
                .CountAsync();
            return Ok(new { count = count });
        }

        // 🔒 Admin/HSE: Approve request and create ApplicationUser
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            Console.WriteLine($"🔍 ApproveRequest called with ID: {id}");
            
            var request = await _context.RegistrationRequests.FindAsync(id);
            if (request == null) 
            {
                Console.WriteLine($"❌ Request with ID {id} not found");
                return NotFound();
            }
            
            Console.WriteLine($"📋 Found request: {request.FullName} ({request.Email}) - Status: {request.Status}");

            // Check if request is not pending
            if (request.Status != "Pending")
            {
                Console.WriteLine($"⚠️ Request status is '{request.Status}', not 'Pending'. Cannot approve.");
                return BadRequest(new { 
                    message = $"Cannot approve request. Current status is '{request.Status}'. Only pending requests can be approved.",
                    currentStatus = request.Status,
                    allowedStatus = "Pending"
                });
            }
            
            Console.WriteLine($"✅ Request is pending, proceeding with approval...");

            // Check if user already exists
            Console.WriteLine($"🔍 Checking if user exists with email: {request.Email}");
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                Console.WriteLine($"❌ User already exists with email: {request.Email}");
                return BadRequest(new { message = "User with this email already exists." });
            }
            
            Console.WriteLine($"✅ No existing user found, creating new user...");

            // Create ApplicationUser (Profile user with NO password - cannot login)
            var nameParts = (request.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // CRITICAL: Explicitly create DateTime to avoid null issues
            var birthDate = new DateTime(1990, 1, 1);
            Console.WriteLine($"🗓️ Created DateOfBirth: {birthDate}");
            
            var user = new ApplicationUser();
            
            // Set all properties individually to ensure they're not null
            user.UserName = request.CompanyId;
            user.Email = request.Email;
            user.FirstName = nameParts.Length > 0 ? nameParts[0] : "";
            user.LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";
            user.CompanyId = request.CompanyId;
            user.Department = request.Department ?? "";
            user.Zone = "Unknown";
            user.Position = request.Position ?? "";
            user.LocalJobTitle = "";
            user.LaborIndicator = "";
            user.DateOfBirth = birthDate; // CRITICAL: Set separately
            user.AccountCreatedAt = DateTime.UtcNow;
            user.IsOnline = false;
            user.IsActive = true;
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = false;
            user.TwoFactorEnabled = false;
            user.LockoutEnabled = false;
            user.AccessFailedCount = 0;
            user.PasswordHash = null;

            Console.WriteLine($"🔨 Creating user: {user.UserName} ({user.Email})");
            Console.WriteLine($"📅 DateOfBirth value: {user.DateOfBirth}");
            Console.WriteLine($"🏢 CompanyId: {user.CompanyId}");
            Console.WriteLine($"📧 EmailConfirmed: {user.EmailConfirmed}");
            Console.WriteLine($"🔢 IsActive: {user.IsActive}, IsOnline: {user.IsOnline}");
            
            // Additional safety check - ensure DateOfBirth is definitely set
            if (user.DateOfBirth == DateTime.MinValue || user.DateOfBirth == default(DateTime))
            {
                Console.WriteLine($"⚠️ DateOfBirth is default value, manually setting it");
                user.DateOfBirth = new DateTime(1990, 1, 1);
            }
            
            // Generate a unique ID for the user
            user.Id = Guid.NewGuid().ToString();
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            
            // Try direct Entity Framework approach instead of UserManager
            try 
            {
                Console.WriteLine($"🔨 Adding user directly to context...");
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ User created successfully with ID: {user.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Direct EF user creation failed: {ex.Message}");
                return BadRequest(new { message = $"User creation failed: {ex.Message}" });
            }

            // Assign Profile role using UserManager
            Console.WriteLine($"🎭 Assigning 'Profil' role to user...");
            var roleResult = await _userManager.AddToRoleAsync(user, "Profil");
            if (!roleResult.Succeeded)
            {
                var roleErrorMessages = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                Console.WriteLine($"❌ Role assignment failed: {roleErrorMessages}");
                Console.WriteLine($"🗑️ Cleaning up user...");
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return BadRequest(new { message = $"Role assignment failed: {roleErrorMessages}" });
            }
            
            Console.WriteLine($"✅ Role 'Profil' assigned successfully");

            // Update request status
            request.Status = "Approved";

            // Also create PendingUser for backward compatibility (optional)
            var pendingUser = new PendingUser
            {
                FullName = request.FullName ?? "",
                CompanyId = request.CompanyId ?? "",
                Email = request.Email ?? "",
                Department = request.Department ?? "",
                Position = request.Position ?? "",
                Role = "Profil",
                CreatedAt = DateTime.UtcNow
            };

            _context.PendingUsers.Add(pendingUser);
            _context.RegistrationRequests.Update(request);
            
            Console.WriteLine($"💾 Saving changes to database...");
            try 
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Database changes saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database save failed: {ex.Message}");
                Console.WriteLine($"🗑️ Cleaning up user...");
                await _userManager.DeleteAsync(user);
                throw;
            }

            Console.WriteLine($"📧 Sending approval email to: {request.Email}");
            try 
            {
                await _emailService.SendGenericEmail(
                    request.Email,
                    "Registration Approved",
                    $"Hello {request.FullName},<br><br>Your registration request has been approved!<br><br>" +
                    $"<b>Your Account Details:</b><br>" +
                    $"TE ID: {request.CompanyId}<br>" +
                    $"Position: {request.Position}<br>" +
                    $"Department: {request.Department}<br><br>" +
                    $"You can now submit safety reports using your TE ID: {request.CompanyId}.<br><br>" +
                    $"Note: You can submit reports without logging in to the platform.<br><br>" +
                    $"Thank you,<br>HSE Team"
                );
                Console.WriteLine($"✅ Email sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Email sending failed: {ex.Message} (continuing anyway)");
            }

            Console.WriteLine($"🎉 Approval process completed successfully for request ID: {id}");
            return Ok(new { 
                message = "Request approved, user created, and email sent.",
                user = new {
                    id = user.Id,
                    email = user.Email,
                    companyId = user.UserName,
                    role = "Profil",
                    canLogin = false
                }
            });
        }

        // 🔒 Admin/HSE: Reject request
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "HSE,Admin")]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await _context.RegistrationRequests.FindAsync(id);
            if (request == null) return NotFound();

            // Check if request is not pending
            if (request.Status != "Pending")
            {
                return BadRequest(new { 
                    message = $"Cannot reject request. Current status is '{request.Status}'. Only pending requests can be rejected.",
                    currentStatus = request.Status,
                    allowedStatus = "Pending"
                });
            }

            request.Status = "Rejected";
            await _context.SaveChangesAsync();

            await _emailService.SendGenericEmail(
                request.Email,
                "Registration Rejected",
                $"Hello {request.FullName},<br><br>We regret to inform you that your registration request has been rejected.<br><br>" +
                $"<b>Request Details:</b><br>" +
                $"TE ID: {request.CompanyId}<br>" +
                $"Position: {request.Position}<br>" +
                $"Department: {request.Department}<br><br>" +
                $"If you have any questions, please contact the HSE team.<br><br>" +
                $"Thank you.<br>HSE Team"
            );

            return Ok(new { message = "Request rejected and email sent." });
        }

        // 🔒 Admin: Convert old approved requests to ApplicationUsers
        [HttpPost("migrate-approved")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MigrateApprovedRequests()
        {
            var approvedRequests = await _context.RegistrationRequests
                .Where(r => r.Status == "Approved")
                .ToListAsync();

            var createdUsers = new List<object>();
            var errors = new List<string>();

            foreach (var request in approvedRequests)
            {
                try
                {
                    // Check if user already exists
                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        errors.Add($"User {request.Email} already exists");
                        continue;
                    }

                    // Create ApplicationUser
                    var user = new ApplicationUser
                    {
                        UserName = request.CompanyId,
                        Email = request.Email,
                        FirstName = request.FullName.Split(' ').FirstOrDefault() ?? "",
                        LastName = string.Join(" ", request.FullName.Split(' ').Skip(1)) ?? "",
                        Department = request.Department,
                        Zone = "Unknown",
                        Position = request.Position,
                        LocalJobTitle = "",
                        LaborIndicator = "",
                        EmailConfirmed = true,
                        PasswordHash = null // No password
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Profil");
                        createdUsers.Add(new { 
                            email = user.Email, 
                            companyId = user.UserName,
                            name = $"{user.FirstName} {user.LastName}".Trim()
                        });
                    }
                    else
                    {
                        errors.Add($"Failed to create user {request.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing {request.Email}: {ex.Message}");
                }
            }

            return Ok(new { 
                message = $"Migration completed. Created {createdUsers.Count} users.",
                createdUsers = createdUsers,
                errors = errors
            });
        }

        // 🔒 Admin: Reset request status to Pending (allows re-processing)
        [HttpPut("{id}/reset")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetRequestStatus(int id)
        {
            var request = await _context.RegistrationRequests.FindAsync(id);
            if (request == null) return NotFound();

            var oldStatus = request.Status;
            request.Status = "Pending";
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Request status changed from {oldStatus} to Pending. Can now be re-processed.",
                requestId = id,
                oldStatus = oldStatus,
                newStatus = "Pending"
            });
        }

        // 🔒 Admin: Delete a registration request and associated user (if exists)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.RegistrationRequests.FindAsync(id);
            if (request == null) return NotFound();

            var messages = new List<string>();

            // If request was approved, try to delete the associated user
            if (request.Status == "Approved")
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user != null)
                {
                    var userDeletionResult = await _userManager.DeleteAsync(user);
                    if (userDeletionResult.Succeeded)
                    {
                        messages.Add($"Associated user account ({user.Email}) deleted successfully.");
                    }
                    else
                    {
                        messages.Add($"Warning: Could not delete user account: {string.Join(", ", userDeletionResult.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Delete from PendingUsers if exists
            var pendingUser = await _context.PendingUsers.FirstOrDefaultAsync(p => p.Email == request.Email);
            if (pendingUser != null)
            {
                _context.PendingUsers.Remove(pendingUser);
                messages.Add("Removed from pending users.");
            }

            // Delete the registration request
            _context.RegistrationRequests.Remove(request);
            await _context.SaveChangesAsync();

            messages.Add("Registration request deleted successfully.");

            return Ok(new { 
                message = "Deletion completed.",
                details = messages,
                deletedRequest = new {
                    id = request.Id,
                    fullName = request.FullName,
                    email = request.Email,
                    status = request.Status
                }
            });
        }

        // 🔒 Admin: Update request status manually
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRequestStatus(int id, [FromBody] UpdateRegistrationStatusDto statusDto)
        {
            var request = await _context.RegistrationRequests.FindAsync(id);
            if (request == null) return NotFound();

            var oldStatus = request.Status;
            request.Status = statusDto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Request status updated from {oldStatus} to {statusDto.Status}.",
                requestId = id,
                oldStatus = oldStatus,
                newStatus = statusDto.Status
            });
        }

        private async Task<List<ApplicationUser>> GetAdminUsersForRegistrationNotification(EmailConfiguration config)
        {
            var adminUsers = new List<ApplicationUser>();

            // Get super admin users if specified (same logic as admin overview emails)
            if (!string.IsNullOrEmpty(config.SuperAdminUserIds))
            {
                var superAdminIds = config.SuperAdminUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var userId in superAdminIds)
                {
                    var user = await _userManager.FindByIdAsync(userId.Trim());
                    if (user != null && user.IsActive && !string.IsNullOrEmpty(user.Email))
                    {
                        adminUsers.Add(user);
                    }
                }
            }

            // If no super admin users specified, get all admin users
            if (!adminUsers.Any())
            {
                var allAdminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                adminUsers = allAdminUsers.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email)).ToList();
            }

            return adminUsers;
        }
    }
}
