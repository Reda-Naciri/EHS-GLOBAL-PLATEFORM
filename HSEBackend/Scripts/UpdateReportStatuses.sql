-- Update Report Status Names Migration Script
-- This script updates existing report statuses to match the new status workflow:
-- "Open" -> "Unopened"
-- "In Progress" -> "Opened" 
-- "Closed" remains "Closed"

-- Enable NOCOUNT to suppress the number of rows affected messages
SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY
    -- Update existing reports with old status names to new ones
    PRINT 'Starting report status migration...';
    
    -- Count reports before update
    DECLARE @OpenCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 'Open');
    DECLARE @InProgressCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 'In Progress');
    DECLARE @ClosedCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 'Closed');
    
    PRINT 'Current status counts:';
    PRINT '  Open: ' + CAST(@OpenCount AS VARCHAR(10));
    PRINT '  In Progress: ' + CAST(@InProgressCount AS VARCHAR(10));
    PRINT '  Closed: ' + CAST(@ClosedCount AS VARCHAR(10));
    
    -- Update "Open" to "Unopened"
    UPDATE Reports 
    SET Status = 'Unopened', 
        UpdatedAt = GETUTCDATE()
    WHERE Status = 'Open';
    
    PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' reports from "Open" to "Unopened"';
    
    -- Update "In Progress" to "Opened"
    UPDATE Reports 
    SET Status = 'Opened', 
        UpdatedAt = GETUTCDATE()
    WHERE Status = 'In Progress';
    
    PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' reports from "In Progress" to "Opened"';
    
    -- Verify the updates
    DECLARE @NewUnopenedCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 'Unopened');
    DECLARE @NewOpenedCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 'Opened');
    DECLARE @NewClosedCount INT = (SELECT COUNT(*) FROM Reports WHERE Status = 'Closed');
    
    PRINT 'New status counts:';
    PRINT '  Unopened: ' + CAST(@NewUnopenedCount AS VARCHAR(10));
    PRINT '  Opened: ' + CAST(@NewOpenedCount AS VARCHAR(10));
    PRINT '  Closed: ' + CAST(@NewClosedCount AS VARCHAR(10));
    
    -- Sanity check: total count should remain the same
    DECLARE @OldTotal INT = @OpenCount + @InProgressCount + @ClosedCount;
    DECLARE @NewTotal INT = @NewUnopenedCount + @NewOpenedCount + @NewClosedCount;
    
    IF @OldTotal = @NewTotal
    BEGIN
        PRINT 'Verification successful: Total report count remains ' + CAST(@NewTotal AS VARCHAR(10));
        COMMIT TRANSACTION;
        PRINT 'Report status migration completed successfully!';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Total count mismatch! Old: ' + CAST(@OldTotal AS VARCHAR(10)) + ', New: ' + CAST(@NewTotal AS VARCHAR(10));
        ROLLBACK TRANSACTION;
        PRINT 'Migration rolled back due to count mismatch.';
    END
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    PRINT 'ERROR: Migration failed with error:';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Migration has been rolled back.';
END CATCH;

SET NOCOUNT OFF;