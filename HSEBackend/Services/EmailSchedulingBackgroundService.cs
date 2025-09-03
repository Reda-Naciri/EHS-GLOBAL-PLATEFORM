using HSEBackend.Data;
using HSEBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Services
{
    public class EmailSchedulingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailSchedulingBackgroundService> _logger;
        private TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Default 1 minute, will be adjusted dynamically
        
        private DateTime _lastHSEEmailSent = DateTime.MinValue;
        private DateTime _lastAdminEmailSent = DateTime.MinValue;

        public EmailSchedulingBackgroundService(IServiceProvider serviceProvider, ILogger<EmailSchedulingBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void ResetTimers()
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation("🔄 Resetting email scheduling timers due to configuration change at {ResetTime}", now);
            _lastHSEEmailSent = now;
            _lastAdminEmailSent = now;
            _logger.LogInformation("📅 Email timers reset - next emails will be sent after their configured intervals from now");
        }

        public async void LogNextScheduledTimes()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var config = await context.EmailConfigurations.FirstOrDefaultAsync();
                    
                    if (config != null)
                    {
                        var now = DateTime.UtcNow;
                        
                        if (config.SendHSEUpdateEmails)
                        {
                            var nextHSE = _lastHSEEmailSent.AddMinutes(config.HSEUpdateIntervalMinutes);
                            var hseIntervalDisplay = config.HSEUpdateIntervalMinutes < 60 ? $"{config.HSEUpdateIntervalMinutes} minutes" : $"{config.HSEUpdateIntervalMinutes / 60.0:F1} hours";
                            _logger.LogInformation("📅 Next HSE email scheduled for {Time} (in {Minutes:F1} minutes) - Interval: {IntervalDisplay}", nextHSE, (nextHSE - now).TotalMinutes, hseIntervalDisplay);
                        }
                        
                        if (config.SendAdminOverviewEmails)
                        {
                            var nextAdmin = _lastAdminEmailSent.AddMinutes(config.AdminOverviewIntervalMinutes);
                            var adminIntervalDisplay = config.AdminOverviewIntervalMinutes < 60 ? $"{config.AdminOverviewIntervalMinutes} minutes" : $"{config.AdminOverviewIntervalMinutes / 60.0:F1} hours";
                            _logger.LogInformation("📅 Next Admin email scheduled for {Time} (in {Minutes:F1} minutes) - Interval: {IntervalDisplay}", nextAdmin, (nextAdmin - now).TotalMinutes, adminIntervalDisplay);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging next scheduled times");
            }
        }

        public async Task<NextScheduledEmailsDto> GetNextScheduledEmailsAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var config = await context.EmailConfigurations.FirstOrDefaultAsync();
                    
                    var result = new NextScheduledEmailsDto();
                    
                    if (config != null)
                    {
                        var now = DateTime.UtcNow;
                        
                        if (config.SendHSEUpdateEmails)
                        {
                            var nextHSE = _lastHSEEmailSent.AddMinutes(config.HSEUpdateIntervalMinutes);
                            var minutesUntilHSE = (nextHSE - now).TotalMinutes;
                            
                            result.HSEEmail = new EmailTimerInfo
                            {
                                IsEnabled = true,
                                NextScheduledTime = nextHSE,
                                MinutesUntilNext = Math.Max(0, minutesUntilHSE),
                                IntervalMinutes = config.HSEUpdateIntervalMinutes,
                                LastSentTime = _lastHSEEmailSent == DateTime.MinValue ? null : _lastHSEEmailSent
                            };
                        }
                        
                        if (config.SendAdminOverviewEmails)
                        {
                            var nextAdmin = _lastAdminEmailSent.AddMinutes(config.AdminOverviewIntervalMinutes);
                            var minutesUntilAdmin = (nextAdmin - now).TotalMinutes;
                            
                            result.AdminEmail = new EmailTimerInfo
                            {
                                IsEnabled = true,
                                NextScheduledTime = nextAdmin,
                                MinutesUntilNext = Math.Max(0, minutesUntilAdmin),
                                IntervalMinutes = config.AdminOverviewIntervalMinutes,
                                LastSentTime = _lastAdminEmailSent == DateTime.MinValue ? null : _lastAdminEmailSent
                            };
                        }
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next scheduled emails");
                return new NextScheduledEmailsDto();
            }
        }

        private TimeSpan CalculateOptimalCheckInterval(HSEBackend.Models.EmailConfiguration config)
        {
            // Find the smallest interval to ensure responsive checking
            var intervals = new List<int>();
            
            if (config.SendHSEUpdateEmails)
                intervals.Add(config.HSEUpdateIntervalMinutes);
            
            if (config.SendAdminOverviewEmails)
                intervals.Add(config.AdminOverviewIntervalMinutes);
            
            if (!intervals.Any())
                return TimeSpan.FromMinutes(10); // Default if no emails enabled
            
            var minInterval = intervals.Min();
            
            // Check interval should be at most 1/4 of the smallest email interval, but at least 1 minute
            var checkMinutes = Math.Max(1, Math.Min(minInterval / 4, 5));
            
            _logger.LogInformation("📊 Calculated check interval: {CheckMinutes} minutes (based on min email interval: {MinInterval} minutes)", 
                checkMinutes, minInterval);
            
            return TimeSpan.FromMinutes(checkMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📧 Email Scheduling Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEnhancedEmailService>();
                        
                        // Get current email configuration
                        var config = await context.EmailConfigurations.FirstOrDefaultAsync();
                        if (config == null)
                        {
                            _logger.LogWarning("⚠️ No email configuration found. Creating default configuration.");
                            config = new HSEBackend.Models.EmailConfiguration();
                            context.EmailConfigurations.Add(config);
                            await context.SaveChangesAsync();
                        }

                        // Update check interval based on current configuration
                        var optimalInterval = CalculateOptimalCheckInterval(config);
                        if (_checkInterval != optimalInterval)
                        {
                            _checkInterval = optimalInterval;
                            _logger.LogInformation("🔄 Updated check interval to {Minutes} minutes", _checkInterval.TotalMinutes);
                        }

                        var now = DateTime.UtcNow;

                        // Check if emailing is enabled
                        if (!config.IsEmailingEnabled)
                        {
                            _logger.LogDebug("📧 Email sending is disabled in configuration.");
                        }
                        else
                        {
                            // Check HSE update emails
                            if (config.SendHSEUpdateEmails)
                            {
                                // Use minutes directly (simple and accurate)
                                var hseIntervalMinutes = config.HSEUpdateIntervalMinutes;
                                var hseEmailDue = _lastHSEEmailSent.AddMinutes(hseIntervalMinutes);
                                
                                if (now >= hseEmailDue)
                                {
                                    var intervalDisplay = hseIntervalMinutes < 60 ? $"{hseIntervalMinutes} minutes" : $"{hseIntervalMinutes / 60.0:F1} hours";
                                    _logger.LogInformation("📧 Sending HSE update emails... (Interval: {IntervalDisplay})", intervalDisplay);
                                    try
                                    {
                                        await emailService.SendHSEUpdateEmailsAsync();
                                        _lastHSEEmailSent = now;
                                        _logger.LogInformation("✅ HSE update emails sent successfully at {Time}", now);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "❌ Failed to send HSE update emails");
                                    }
                                }
                                else
                                {
                                    var nextHSEEmail = hseEmailDue;
                                    var intervalDisplay = hseIntervalMinutes < 60 ? $"{hseIntervalMinutes} minutes" : $"{hseIntervalMinutes / 60.0:F1} hours";
                                    _logger.LogDebug("⏰ Next HSE email scheduled for {Time} (in {Minutes} minutes) - Using {IntervalDisplay} interval", nextHSEEmail, (nextHSEEmail - now).TotalMinutes, intervalDisplay);
                                }
                            }

                            // Check Admin overview emails
                            if (config.SendAdminOverviewEmails)
                            {
                                // Use minutes directly (simple and accurate)
                                var adminIntervalMinutes = config.AdminOverviewIntervalMinutes;
                                var adminEmailDue = _lastAdminEmailSent.AddMinutes(adminIntervalMinutes);
                                
                                if (now >= adminEmailDue)
                                {
                                    var intervalDisplay = adminIntervalMinutes < 60 ? $"{adminIntervalMinutes} minutes" : $"{adminIntervalMinutes / 60.0:F1} hours";
                                    _logger.LogInformation("📧 Sending Admin overview emails... (Interval: {IntervalDisplay})", intervalDisplay);
                                    try
                                    {
                                        await emailService.SendAdminOverviewEmailsAsync();
                                        _lastAdminEmailSent = now;
                                        _logger.LogInformation("✅ Admin overview emails sent successfully at {Time}", now);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "❌ Failed to send Admin overview emails");
                                    }
                                }
                                else
                                {
                                    var nextAdminEmail = adminEmailDue;
                                    var intervalDisplay = adminIntervalMinutes < 60 ? $"{adminIntervalMinutes} minutes" : $"{adminIntervalMinutes / 60.0:F1} hours";
                                    _logger.LogDebug("⏰ Next Admin email scheduled for {Time} (in {Minutes} minutes) - Using {IntervalDisplay} interval", nextAdminEmail, (nextAdminEmail - now).TotalMinutes, intervalDisplay);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error occurred in Email Scheduling Background Service");
                }

                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // This is expected when the service is stopping
                    break;
                }
            }

            _logger.LogInformation("📧 Email Scheduling Background Service stopped.");
        }
    }

    // DTOs for email timer information
    public class NextScheduledEmailsDto
    {
        public EmailTimerInfo? HSEEmail { get; set; }
        public EmailTimerInfo? AdminEmail { get; set; }
    }

    public class EmailTimerInfo
    {
        public bool IsEnabled { get; set; }
        public DateTime NextScheduledTime { get; set; }
        public double MinutesUntilNext { get; set; }
        public int IntervalMinutes { get; set; }
        public DateTime? LastSentTime { get; set; }
    }
}