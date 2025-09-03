using HSEBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Services
{
    public interface IOverdueService
    {
        Task UpdateOverdueStatusAsync();
        Task<bool> IsActionOverdueAsync(int actionId);
        Task<bool> IsCorrectiveActionOverdueAsync(int correctiveActionId);
        Task<bool> IsSubActionOverdueAsync(int subActionId);
    }

    public class OverdueService : IOverdueService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OverdueService> _logger;

        public OverdueService(AppDbContext context, ILogger<OverdueService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Updates overdue status for all actions, corrective actions, and sub-actions
        /// This method should be called periodically (e.g., daily via a background service)
        /// </summary>
        public async Task UpdateOverdueStatusAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Update Actions
                var overdueActions = await _context.Actions
                    .Where(a => a.DueDate.HasValue && 
                               a.DueDate < now && 
                               !new[] { "Completed", "Canceled" }.Contains(a.Status) &&
                               !a.Overdue)
                    .ToListAsync();

                foreach (var action in overdueActions)
                {
                    action.Overdue = true;
                    action.UpdatedAt = now;
                }

                // Update CorrectiveActions
                var overdueCorrectiveActions = await _context.CorrectiveActions
                    .Where(ca => ca.DueDate < now && 
                                !new[] { "Completed", "Canceled", "Aborted" }.Contains(ca.Status) &&
                                !ca.Overdue)
                    .ToListAsync();

                foreach (var correctiveAction in overdueCorrectiveActions)
                {
                    correctiveAction.Overdue = true;
                    correctiveAction.UpdatedAt = now;
                }

                // Update SubActions
                var overdueSubActions = await _context.SubActions
                    .Where(sa => sa.DueDate.HasValue && 
                                sa.DueDate < now && 
                                !new[] { "Completed", "Canceled" }.Contains(sa.Status) &&
                                !sa.Overdue)
                    .ToListAsync();

                foreach (var subAction in overdueSubActions)
                {
                    subAction.Overdue = true;
                    subAction.UpdatedAt = now;
                }

                // Reset overdue status for items that are no longer overdue (status changed to completed/canceled)
                var noLongerOverdueActions = await _context.Actions
                    .Where(a => a.Overdue && 
                               new[] { "Completed", "Canceled" }.Contains(a.Status))
                    .ToListAsync();

                foreach (var action in noLongerOverdueActions)
                {
                    action.Overdue = false;
                    action.UpdatedAt = now;
                }

                var noLongerOverdueCorrectiveActions = await _context.CorrectiveActions
                    .Where(ca => ca.Overdue && 
                                new[] { "Completed", "Canceled", "Aborted" }.Contains(ca.Status))
                    .ToListAsync();

                foreach (var correctiveAction in noLongerOverdueCorrectiveActions)
                {
                    correctiveAction.Overdue = false;
                    correctiveAction.UpdatedAt = now;
                }

                var noLongerOverdueSubActions = await _context.SubActions
                    .Where(sa => sa.Overdue && 
                                new[] { "Completed", "Canceled" }.Contains(sa.Status))
                    .ToListAsync();

                foreach (var subAction in noLongerOverdueSubActions)
                {
                    subAction.Overdue = false;
                    subAction.UpdatedAt = now;
                }

                // Save all changes
                var changes = await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Updated overdue status. Changes made: {changes}");
                _logger.LogInformation($"New overdue items: {overdueActions.Count} actions, {overdueCorrectiveActions.Count} corrective actions, {overdueSubActions.Count} sub-actions");
                _logger.LogInformation($"Reset overdue status: {noLongerOverdueActions.Count} actions, {noLongerOverdueCorrectiveActions.Count} corrective actions, {noLongerOverdueSubActions.Count} sub-actions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overdue status");
                throw;
            }
        }

        public async Task<bool> IsActionOverdueAsync(int actionId)
        {
            var action = await _context.Actions
                .FirstOrDefaultAsync(a => a.Id == actionId);

            if (action == null || !action.DueDate.HasValue)
                return false;

            return action.DueDate < DateTime.UtcNow && 
                   !new[] { "Completed", "Canceled" }.Contains(action.Status);
        }

        public async Task<bool> IsCorrectiveActionOverdueAsync(int correctiveActionId)
        {
            var correctiveAction = await _context.CorrectiveActions
                .FirstOrDefaultAsync(ca => ca.Id == correctiveActionId);

            if (correctiveAction == null)
                return false;

            return correctiveAction.DueDate < DateTime.UtcNow && 
                   !new[] { "Completed", "Canceled", "Aborted" }.Contains(correctiveAction.Status);
        }

        public async Task<bool> IsSubActionOverdueAsync(int subActionId)
        {
            var subAction = await _context.SubActions
                .FirstOrDefaultAsync(sa => sa.Id == subActionId);

            if (subAction == null || !subAction.DueDate.HasValue)
                return false;

            return subAction.DueDate < DateTime.UtcNow && 
                   !new[] { "Completed", "Canceled" }.Contains(subAction.Status);
        }
    }
}