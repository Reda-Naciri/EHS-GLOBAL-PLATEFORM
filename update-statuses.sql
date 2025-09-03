-- Update all corrective action statuses based on sub-actions using new calculation logic
-- This script will be executed via a backend endpoint

-- First, let's see current corrective actions with their sub-actions
SELECT 
    ca.Id as CorrectiveActionId,
    ca.Title,
    ca.Status as CurrentStatus,
    COUNT(sa.Id) as TotalSubActions,
    COUNT(CASE WHEN sa.Status = 'Not Started' THEN 1 END) as NotStartedCount,
    COUNT(CASE WHEN sa.Status = 'In Progress' THEN 1 END) as InProgressCount,
    COUNT(CASE WHEN sa.Status = 'Completed' THEN 1 END) as CompletedCount,
    COUNT(CASE WHEN sa.Status = 'Cancelled' THEN 1 END) as CancelledCount
FROM CorrectiveActions ca
LEFT JOIN SubActions sa ON ca.Id = sa.CorrectiveActionId
WHERE ca.Status != 'Aborted'
GROUP BY ca.Id, ca.Title, ca.Status
ORDER BY ca.Id;