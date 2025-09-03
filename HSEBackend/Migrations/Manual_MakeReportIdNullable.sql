-- Migration to make ReportId nullable in CorrectiveActions table
-- This allows for standalone corrective actions not tied to specific reports

PRINT 'Starting migration to make ReportId nullable in CorrectiveActions table...'

-- Check if column exists and is not nullable
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CorrectiveActions' 
    AND COLUMN_NAME = 'ReportId' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT 'Making ReportId column nullable in CorrectiveActions table...'
    
    -- First, drop the foreign key constraint if it exists
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CorrectiveActions_Reports_ReportId')
    BEGIN
        PRINT 'Dropping foreign key constraint FK_CorrectiveActions_Reports_ReportId...'
        ALTER TABLE [CorrectiveActions] DROP CONSTRAINT [FK_CorrectiveActions_Reports_ReportId]
    END
    
    -- Make the ReportId column nullable
    ALTER TABLE [CorrectiveActions] ALTER COLUMN [ReportId] int NULL
    
    -- Recreate the foreign key constraint as nullable
    PRINT 'Recreating foreign key constraint as nullable...'
    ALTER TABLE [CorrectiveActions] ADD CONSTRAINT [FK_CorrectiveActions_Reports_ReportId] 
        FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([Id]) ON DELETE SET NULL
    
    PRINT 'ReportId column is now nullable in CorrectiveActions table.'
END
ELSE
BEGIN
    PRINT 'ReportId column is already nullable or does not exist.'
END

PRINT 'Migration completed successfully.'