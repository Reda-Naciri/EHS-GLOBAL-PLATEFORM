using HSEBackend.Models;

namespace HSEBackend.Services
{
    public class ProgressCalculationService
    {
        /// <summary>
        /// Calculate progress value for a single sub-action
        /// Not Started = 0, In Progress = 0.5, Completed = 1, Cancelled = 0
        /// </summary>
        public double GetSubActionProgressValue(string status)
        {
            return status switch
            {
                "Completed" => 1.0,
                "In Progress" => 0.5,
                "Not Started" => 0.0,
                "Cancelled" => 0.0,
                _ => 0.0
            };
        }

        /// <summary>
        /// Calculate progress value for a corrective action based on its status
        /// Not Started = 0, In Progress = 0.5, Completed = 1, Cancelled = 0, Aborted = 0
        /// </summary>
        public double GetCorrectiveActionProgressValue(string status)
        {
            return status switch
            {
                "Completed" => 1.0,
                "In Progress" => 0.5,
                "Open" => 0.0, // Open is equivalent to Not Started for corrective actions
                "Not Started" => 0.0,
                "Cancelled" => 0.0,
                "Aborted" => 0.0, // Aborted actions count as 0% progress
                _ => 0.0
            };
        }

        /// <summary>
        /// Calculate progress percentage for sub-actions of a single corrective action
        /// Used in: Corrective action section progress bar
        /// </summary>
        public int CalculateSubActionsProgressPercentage(List<SubAction> subActions)
        {
            if (subActions == null || !subActions.Any()) return 0;

            // remove cancelled from calculation
            var validSubActions = subActions.Where(sa => sa.Status != "Canceled").ToList();
            if (!validSubActions.Any()) return 0; // nothing to calculate

            var totalProgress = validSubActions.Sum(sa => GetSubActionProgressValue(sa.Status));
            var percentage = (totalProgress / validSubActions.Count) * 100;

            return (int)Math.Round(percentage);
        }


        /// <summary>
        /// Calculate overall progress percentage for all corrective actions in a report
        /// Used in: Report details section progress bar (global progress)
        /// </summary>
        public int CalculateOverallCorrectiveActionsProgress(List<CorrectiveAction> correctiveActions)
        {
            if (correctiveActions == null || !correctiveActions.Any()) return 0;

            var totalProgress = correctiveActions.Sum(ca => GetCorrectiveActionProgressValue(ca.Status));
            var percentage = (totalProgress / correctiveActions.Count) * 100;
            
            return (int)Math.Round(percentage);
        }

        /// <summary>
        /// Calculate parent status based on sub-action statuses  
        /// Rules (matching frontend logic exactly):
        /// 1. If all SubActions are either NotStarted or Cancelled, the Action status is NotStarted
        /// 2. If at least one SubAction is InProgress, the Action status becomes InProgress
        /// 3. If at least one SubAction is NotStarted or InProgress, and the rest are Completed or Cancelled, the Action remains InProgress
        /// 4. If all SubActions are either Completed or Cancelled and at least one is Completed, then the Action is Completed
        /// 5. If all SubActions are Cancelled, the Action remains NotStarted (covered by Rule 1)
        /// </summary>
        public string CalculateParentStatus(List<SubAction> subActions)
        {
            if (subActions == null || !subActions.Any()) return "Not Started";

            // Count different status types (matching frontend exactly)
            var notStartedCount = subActions.Count(sa => sa.Status == "Not Started");
            var inProgressCount = subActions.Count(sa => sa.Status == "In Progress");
            var completedCount = subActions.Count(sa => sa.Status == "Completed");
            var cancelledCount = subActions.Count(sa => sa.Status == "Cancelled");

            // Rule 1: If all SubActions are either NotStarted or Cancelled, the Action status is NotStarted
            if (inProgressCount == 0 && completedCount == 0)
            {
                return "Not Started";
            }

            // Rule 2: If at least one SubAction is InProgress, the Action status becomes InProgress
            if (inProgressCount > 0)
            {
                return "In Progress";
            }

            // Rule 3: If at least one SubAction is NotStarted or InProgress, and the rest are Completed or Cancelled, the Action remains InProgress
            if ((notStartedCount > 0 || inProgressCount > 0) && (completedCount > 0 || cancelledCount > 0))
            {
                return "In Progress";
            }

            // Rule 4: If all SubActions are either Completed or Cancelled and at least one is Completed, then the Action is Completed
            if (notStartedCount == 0 && inProgressCount == 0 && completedCount > 0)
            {
                return "Completed";
            }

            // Rule 5: If all SubActions are Cancelled, the Action remains NotStarted (already covered by Rule 1)
            
            // Fallback (should not reach here with the rules above)
            return "Not Started";
        }
    }
}