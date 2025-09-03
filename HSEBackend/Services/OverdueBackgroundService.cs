using HSEBackend.Services;

namespace HSEBackend.Services
{
    public class OverdueBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(1); // Check every hour

        public OverdueBackgroundService(IServiceProvider serviceProvider, ILogger<OverdueBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var overdueService = scope.ServiceProvider.GetRequiredService<IOverdueService>();
                        await overdueService.UpdateOverdueStatusAsync();
                    }
                    
                    _logger.LogInformation("Overdue status update completed at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating overdue status");
                }

                try
                {
                    await Task.Delay(_period, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // This is expected when the service is stopping
                    break;
                }
            }

            _logger.LogInformation("Overdue Background Service stopped.");
        }
    }
}