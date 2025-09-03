-- Check all reports and their statuses
SELECT 
    COUNT(*) as TotalReports,
    Status,
    COUNT(*) as CountByStatus
FROM Reports 
GROUP BY Status
UNION ALL
SELECT 
    COUNT(*) as TotalReports,
    'TOTAL' as Status,
    COUNT(*) as CountByStatus
FROM Reports;

-- Show all individual reports
SELECT Id, Title, Status, ReporterId, Zone, CreatedAt 
FROM Reports 
ORDER BY CreatedAt DESC;