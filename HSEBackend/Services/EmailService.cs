using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HSEBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendGenericEmail(string toEmail, string subject, string body)
        {
            try
            {
                // Lecture des paramètres SMTP de façon sûre
                var host = _config["SmtpSettings:Host"] ?? throw new Exception("SMTP Host not configured");
                var port = int.Parse(_config["SmtpSettings:Port"] ?? throw new Exception("SMTP Port not configured"));
                var user = _config["SmtpSettings:User"] ?? throw new Exception("SMTP User not configured");
                var pass = _config["SmtpSettings:Password"] ?? throw new Exception("SMTP Password not configured");
                var enableSsl = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");

                var smtpClient = new SmtpClient(host)
                {
                    Port = port,
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = enableSsl,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(user),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"✅ Email successfully sent to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send email to {toEmail}");
                throw;
            }
        }

        public async Task SendUserCreationEmailAsync(string email, string firstName, string lastName, string role, string? password = null, string? companyId = null, string? department = null, string? position = null)
        {
            try
            {
                string subject;
                string body;

                if (role == "Profil")
                {
                    // Profile user email
                    subject = "HSE System - Profile Created";
                    body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #2c5aa0; text-align: center;'>HSE Safety Management System</h2>
                            
                            <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                            
                            <p>Your profile has been created in the HSE system.</p>
                            
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #2c5aa0; margin: 20px 0;'>
                                <h3 style='margin-top: 0; color: #2c5aa0;'>Account Details:</h3>
                                <p><strong>Company ID:</strong> {companyId ?? "Not assigned"}</p>
                                <p><strong>Position:</strong> {position ?? "Not specified"}</p>
                                <p><strong>Department:</strong> {department ?? "Not specified"}</p>
                            </div>
                            
                            <p>You can now submit safety reports using your TE ID.</p>
                            
                            <div style='background-color: #fff3cd; padding: 15px; border: 1px solid #ffeaa7; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 0;'><strong>Note:</strong> This is a profile-only account. You cannot login to the system.</p>
                            </div>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='text-align: center; color: #666; font-size: 14px;'>
                                Thank you,<br>
                                <strong>HSE Team</strong><br>
                                TE Connectivity
                            </p>
                        </div>
                    </body>
                    </html>";
                }
                else
                {
                    // HSE/Admin user email
                    subject = $"HSE System - {role} Account Created";
                    body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #2c5aa0; text-align: center;'>HSE Safety Management System</h2>
                            
                            <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                            
                            <p>Your <strong>{role}</strong> account has been created in the HSE system.</p>
                            
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0;'>
                                <h3 style='margin-top: 0; color: #28a745;'>Login Credentials:</h3>
                                <p><strong>Email:</strong> {email}</p>
                                <p><strong>Password:</strong> <code style='background: #f1f1f1; padding: 2px 6px; border-radius: 3px;'>{password}</code></p>
                            </div>
                            
                            <div style='background-color: #f8d7da; padding: 15px; border: 1px solid #f5c6cb; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 0;'><strong>IMPORTANT:</strong> Please change your password after your first login for security reasons.</p>
                            </div>
                            
                            <p>You can now access the HSE management system with full {role.ToLower()} privileges.</p>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='http://192.168.0.245:4200/login' style='background-color: #2c5aa0; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Access HSE System</a>
                            </div>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='text-align: center; color: #666; font-size: 14px;'>
                                Thank you,<br>
                                <strong>HSE Team</strong><br>
                                TE Connectivity
                            </p>
                        </div>
                    </body>
                    </html>";
                }

                await SendGenericEmail(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send user creation email to {email}");
                throw;
            }
        }

        public async Task SendRoleChangeEmailAsync(string email, string firstName, string lastName, string oldRole, string newRole, string? newPassword = null, string? companyId = null)
        {
            try
            {
                string subject = $"HSE System - Role Updated to {newRole}";
                string body;

                if (newRole == "Profil")
                {
                    // Downgrade email
                    body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #2c5aa0; text-align: center;'>HSE Safety Management System</h2>
                            
                            <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                            
                            <p>Your account role has been changed from <strong>{oldRole}</strong> to <strong>Profile</strong>.</p>
                            
                            <div style='background-color: #fff3cd; padding: 15px; border: 1px solid #ffeaa7; border-radius: 5px; margin: 20px 0;'>
                                <h3 style='margin-top: 0; color: #856404;'>Important Changes:</h3>
                                <ul style='margin: 0; padding-left: 20px;'>
                                    <li>You can no longer login to the HSE management system</li>
                                    <li>You can still submit safety reports using your TE ID: <strong>{companyId}</strong></li>
                                </ul>
                            </div>
                            
                            <p>If you have any questions about this change, please contact the HSE team.</p>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='text-align: center; color: #666; font-size: 14px;'>
                                Thank you,<br>
                                <strong>HSE Team</strong><br>
                                TE Connectivity
                            </p>
                        </div>
                    </body>
                    </html>";
                }
                else
                {
                    // Upgrade/Change email
                    string changeType = oldRole == "Profil" ? "upgraded" : "changed";
                    body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                            <h2 style='color: #2c5aa0; text-align: center;'>HSE Safety Management System</h2>
                            
                            <p>Hello <strong>{firstName} {lastName}</strong>,</p>
                            
                            <p>Your account role has been <strong>{changeType}</strong> from <strong>{oldRole}</strong> to <strong>{newRole}</strong>.</p>
                            
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0;'>
                                <h3 style='margin-top: 0; color: #28a745;'>New Login Credentials:</h3>
                                <p><strong>Email:</strong> {email}</p>
                                <p><strong>Password:</strong> <code style='background: #f1f1f1; padding: 2px 6px; border-radius: 3px;'>{newPassword}</code></p>
                            </div>
                            
                            <div style='background-color: #f8d7da; padding: 15px; border: 1px solid #f5c6cb; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 0;'><strong>IMPORTANT:</strong> Please change your password after your first login for security reasons.</p>
                            </div>
                            
                            <p>You now have <strong>{newRole}</strong> access to the HSE management system.</p>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='http://192.168.0.245:4200/login' style='background-color: #2c5aa0; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Access HSE System</a>
                            </div>
                            
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='text-align: center; color: #666; font-size: 14px;'>
                                Thank you,<br>
                                <strong>HSE Team</strong><br>
                                TE Connectivity
                            </p>
                        </div>
                    </body>
                    </html>";
                }

                await SendGenericEmail(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send role change email to {email}");
                throw;
            }
        }
    }
}
