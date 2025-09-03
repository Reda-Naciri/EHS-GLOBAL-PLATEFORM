-- SQL Server Migration Script
-- Update report statuses from "Open" to "Unopened"

USE HSE_DB;

-- Check current status counts
SELECT 'BEFORE UPDATE' as Phase, Status, COUNT(*) as Count
FROM Reports
GROUP BY Status
ORDER BY Status;

-- Update "Open" to "Unopened"
UPDATE Reports 
SET Status = 'Unopened', 
    UpdatedAt = GETUTCDATE()
WHERE Status = 'Open';

PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' reports from "Open" to "Unopened"';

-- Check new status counts  
SELECT 'AFTER UPDATE' as Phase, Status, COUNT(*) as Count
FROM Reports
GROUP BY Status
ORDER BY Status;

-- Show total counts for verification
SELECT 
    COUNT(*) as TotalReports,
    SUM(CASE WHEN Status = 'Unopened' THEN 1 ELSE 0 END) as UnopenedReports,
    SUM(CASE WHEN Status = 'Opened' THEN 1 ELSE 0 END) as OpenedReports,
    SUM(CASE WHEN Status = 'Closed' THEN 1 ELSE 0 END) as ClosedReports
FROM Reports;