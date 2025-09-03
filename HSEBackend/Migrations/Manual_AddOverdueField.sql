-- Manual Migration: Add Overdue Field to Actions, CorrectiveActions, and SubActions tables
-- Run this SQL script in SQL Server Management Studio

USE [HSEDatabase];
GO

-- Add Overdue column to Actions table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Actions') AND name = 'Overdue')
BEGIN
    ALTER TABLE Actions ADD Overdue BIT NOT NULL DEFAULT 0;
    PRINT 'Added Overdue column to Actions table';
END
ELSE
BEGIN
    PRINT 'Overdue column already exists in Actions table';
END
GO

-- Add Overdue column to CorrectiveActions table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'CorrectiveActions') AND name = 'Overdue')
BEGIN
    ALTER TABLE CorrectiveActions ADD Overdue BIT NOT NULL DEFAULT 0;
    PRINT 'Added Overdue column to CorrectiveActions table';
END
ELSE
BEGIN
    PRINT 'Overdue column already exists in CorrectiveActions table';
END
GO

-- Add Overdue column to SubActions table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'SubActions') AND name = 'Overdue')
BEGIN
    ALTER TABLE SubActions ADD Overdue BIT NOT NULL DEFAULT 0;
    PRINT 'Added Overdue column to SubActions table';
END
ELSE
BEGIN
    PRINT 'Overdue column already exists in SubActions table';
END
GO

-- Update existing records to set Overdue = 1 where appropriate
-- Actions: Set Overdue = 1 where DueDate < GETDATE() and Status not in ('Completed', 'Canceled')
UPDATE Actions 
SET Overdue = 1 
WHERE DueDate IS NOT NULL 
  AND DueDate < GETDATE() 
  AND Status NOT IN ('Completed', 'Canceled');

PRINT 'Updated existing Actions with overdue status';

-- CorrectiveActions: Set Overdue = 1 where DueDate < GETDATE() and Status not in ('Completed', 'Canceled', 'Aborted')
UPDATE CorrectiveActions 
SET Overdue = 1 
WHERE DueDate < GETDATE() 
  AND Status NOT IN ('Completed', 'Canceled', 'Aborted');

PRINT 'Updated existing CorrectiveActions with overdue status';

-- SubActions: Set Overdue = 1 where DueDate < GETDATE() and Status not in ('Completed', 'Canceled')
UPDATE SubActions 
SET Overdue = 1 
WHERE DueDate IS NOT NULL 
  AND DueDate < GETDATE() 
  AND Status NOT IN ('Completed', 'Canceled');

PRINT 'Updated existing SubActions with overdue status';

PRINT 'Migration completed successfully!';