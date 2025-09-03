-- HSEBackend Database Verification Script
-- Run this script to verify database structure and data

USE HSE_DB;
GO

-- Display database information
PRINT '=== HSE Database Verification ==='
PRINT 'Database: ' + DB_NAME()
PRINT 'Date: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT ''

-- Check if main tables exist
PRINT '=== Table Structure Verification ==='
SELECT 
    TABLE_NAME,
    TABLE_TYPE,
    CASE 
        WHEN TABLE_NAME IN ('Reports', 'RegistrationRequests', 'PendingUsers', 'AspNetUsers') THEN 'Core Table'
        WHEN TABLE_NAME LIKE 'AspNet%' THEN 'Identity Table'
        WHEN TABLE_NAME IN ('Comments', 'InjuredPersons', 'Injuries', 'Actions', 'CorrectiveActions') THEN 'Related Table'
        ELSE 'Other'
    END AS TableCategory
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TableCategory, TABLE_NAME;

-- Check table record counts
PRINT ''
PRINT '=== Table Record Counts ==='

DECLARE @sql NVARCHAR(MAX) = ''
SELECT @sql = @sql + 'SELECT ''' + TABLE_NAME + ''' AS TableName, COUNT(*) AS RecordCount FROM ' + TABLE_NAME + ' UNION ALL '
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND TABLE_NAME NOT LIKE '__EFMigrationsHistory'

-- Remove the last UNION ALL
SET @sql = LEFT(@sql, LEN(@sql) - 10)
SET @sql = @sql + ' ORDER BY RecordCount DESC'

EXEC sp_executesql @sql

-- Check Reports table structure
PRINT ''
PRINT '=== Reports Table Structure ==='
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Reports'
ORDER BY ORDINAL_POSITION;

-- Sample data from Reports table
PRINT ''
PRINT '=== Sample Reports Data ==='
IF EXISTS (SELECT 1 FROM Reports)
BEGIN
    SELECT TOP 5
        Id,
        Title,
        Type,
        Status,
        Zone,
        ReporterId,
        CreatedAt,
        InjuredPersonsCount
    FROM Reports
    ORDER BY CreatedAt DESC;
END
ELSE
BEGIN
    PRINT 'No reports found in the database.'
END

-- Check RegistrationRequests table
PRINT ''
PRINT '=== Registration Requests Data ==='
IF EXISTS (SELECT 1 FROM RegistrationRequests)
BEGIN
    SELECT 
        Id,
        FullName,
        CompanyId,
        Email,
        Department,
        Status,
        SubmittedAt
    FROM RegistrationRequests
    ORDER BY SubmittedAt DESC;
END
ELSE
BEGIN
    PRINT 'No registration requests found in the database.'
END

-- Check PendingUsers table
PRINT ''
PRINT '=== Pending Users Data ==='
IF EXISTS (SELECT 1 FROM PendingUsers)
BEGIN
    SELECT 
        Id,
        FullName,
        CompanyId,
        Email,
        Department,
        Role,
        CreatedAt
    FROM PendingUsers
    ORDER BY CreatedAt DESC;
END
ELSE
BEGIN
    PRINT 'No pending users found in the database.'
END

-- Check AspNetUsers table
PRINT ''
PRINT '=== AspNet Users Data ==='
IF EXISTS (SELECT 1 FROM AspNetUsers)
BEGIN
    SELECT 
        Id,
        UserName,
        Email,
        EmailConfirmed,
        PhoneNumber,
        LockoutEnabled,
        AccessFailedCount
    FROM AspNetUsers;
END
ELSE
BEGIN
    PRINT 'No AspNet users found in the database.'
END

-- Check for foreign key relationships
PRINT ''
PRINT '=== Foreign Key Relationships ==='
SELECT 
    FK.name AS ForeignKeyName,
    TP.name AS ParentTable,
    CP.name AS ParentColumn,
    TR.name AS ReferencedTable,
    CR.name AS ReferencedColumn
FROM sys.foreign_keys FK
INNER JOIN sys.tables TP ON FK.parent_object_id = TP.object_id
INNER JOIN sys.tables TR ON FK.referenced_object_id = TR.object_id
INNER JOIN sys.foreign_key_columns FKC ON FK.object_id = FKC.constraint_object_id
INNER JOIN sys.columns CP ON FKC.parent_column_id = CP.column_id AND FKC.parent_object_id = CP.object_id
INNER JOIN sys.columns CR ON FKC.referenced_column_id = CR.column_id AND FKC.referenced_object_id = CR.object_id
ORDER BY TP.name, FK.name;

-- Check indexes
PRINT ''
PRINT '=== Index Information ==='
SELECT 
    T.name AS TableName,
    I.name AS IndexName,
    I.type_desc AS IndexType,
    I.is_unique AS IsUnique,
    I.is_primary_key AS IsPrimaryKey
FROM sys.indexes I
INNER JOIN sys.tables T ON I.object_id = T.object_id
WHERE T.name IN ('Reports', 'RegistrationRequests', 'PendingUsers', 'AspNetUsers', 'Comments', 'InjuredPersons')
  AND I.name IS NOT NULL
ORDER BY T.name, I.name;

-- Recent activity check
PRINT ''
PRINT '=== Recent Activity Check ==='
PRINT 'Recent Reports (last 7 days):'
SELECT COUNT(*) AS RecentReportsCount
FROM Reports
WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE());

PRINT 'Recent Registration Requests (last 7 days):'
SELECT COUNT(*) AS RecentRequestsCount
FROM RegistrationRequests
WHERE SubmittedAt >= DATEADD(DAY, -7, GETDATE());

-- Database size information
PRINT ''
PRINT '=== Database Size Information ==='
SELECT 
    name AS DatabaseName,
    size * 8 / 1024 AS SizeMB,
    fileproperty(name, 'SpaceUsed') * 8 / 1024 AS UsedSpaceMB
FROM sys.database_files;

-- Check for potential issues
PRINT ''
PRINT '=== Potential Issues Check ==='

-- Check for reports without injured persons when they should have them
PRINT 'Reports with InjuredPersonsCount > 0 but no InjuredPersons records:'
SELECT 
    R.Id,
    R.Title,
    R.Type,
    R.InjuredPersonsCount,
    COUNT(IP.Id) AS ActualInjuredPersonsCount
FROM Reports R
LEFT JOIN InjuredPersons IP ON R.Id = IP.ReportId
WHERE R.InjuredPersonsCount > 0
GROUP BY R.Id, R.Title, R.Type, R.InjuredPersonsCount
HAVING COUNT(IP.Id) = 0;

-- Check for orphaned records
PRINT 'Orphaned InjuredPersons records:'
SELECT IP.Id, IP.Name, IP.ReportId
FROM InjuredPersons IP
LEFT JOIN Reports R ON IP.ReportId = R.Id
WHERE R.Id IS NULL;

PRINT 'Orphaned Comments records:'
SELECT C.Id, C.Content, C.ReportId
FROM Comments C
LEFT JOIN Reports R ON C.ReportId = R.Id
WHERE R.Id IS NULL;

-- Performance analysis
PRINT ''
PRINT '=== Performance Analysis ==='
PRINT 'Average report processing time by status:'
SELECT 
    Status,
    COUNT(*) AS ReportCount,
    AVG(DATEDIFF(MINUTE, CreatedAt, GETDATE())) AS AvgMinutesSinceCreation
FROM Reports
GROUP BY Status
ORDER BY Status;

PRINT ''
PRINT '=== Database Verification Complete ==='
PRINT 'Review the results above for any issues or inconsistencies.'