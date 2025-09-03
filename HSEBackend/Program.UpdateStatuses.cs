using HSEBackend.Data;
using HSEBackend.Services;
using Microsoft.EntityFrameworkCore;

// Quick utility to update all corrective action statuses
public class StatusUpdater
{
    public static async Task UpdateAllStatusesAsync(AppDbContext context, ProgressCalculationService progressService)
    {
        Console.WriteLine("🔄 Starting corrective action status update...");
        
        try
        {
            // Get all corrective actions with their sub-actions (excluding aborted ones)
            var correctiveActions = await context.CorrectiveActions
                .Include(ca => ca.SubActions)
                .Where(ca => ca.Status != "Aborted")
                .ToListAsync();

            var updateCount = 0;
            var totalCount = correctiveActions.Count;

            Console.WriteLine($"📊 Found {totalCount} corrective actions to process");

            foreach (var correctiveAction in correctiveActions)
            {
                var oldStatus = correctiveAction.Status;
                var newStatus = progressService.CalculateParentStatus(correctiveAction.SubActions.ToList());

                Console.WriteLine($"📋 CorrectiveAction {correctiveAction.Id} ({correctiveAction.Title}):");
                Console.WriteLine($"    Sub-actions: {correctiveAction.SubActions.Count}");
                Console.WriteLine($"    Current Status: '{oldStatus}'");
                Console.WriteLine($"    Calculated Status: '{newStatus}'");

                if (oldStatus != newStatus)
                {
                    correctiveAction.Status = newStatus;
                    correctiveAction.IsCompleted = newStatus == "Completed";
                    correctiveAction.UpdatedAt = DateTime.UtcNow;
                    updateCount++;

                    Console.WriteLine($"    ✅ Status updated: '{oldStatus}' → '{newStatus}'");
                }
                else
                {
                    Console.WriteLine($"    ✓ Status already correct");
                }
                Console.WriteLine();
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Status update completed: {updateCount}/{totalCount} records updated");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating statuses: {ex.Message}");
            throw;
        }
    }
}