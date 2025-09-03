-- Script to delete all corrective actions and related data
-- Run this in your SQL Server Management Studio or database tool

-- Delete in order to respect foreign key constraints

-- 1. Delete sub-actions first (they reference actions and corrective actions)
DELETE FROM SubActions WHERE CorrectiveActionId IS NOT NULL;

-- 2. Delete corrective action attachments
DELETE FROM CorrectiveActionAttachments;

-- 3. Delete corrective actions
DELETE FROM CorrectiveActions;

-- 4. Reset identity if needed (optional - only if you want to start IDs from 1 again)
-- DBCC CHECKIDENT ('CorrectiveActions', RESEED, 0);
-- DBCC CHECKIDENT ('SubActions', RESEED, 0);

-- Verify the deletion
SELECT COUNT(*) AS RemainingCorrectiveActions FROM CorrectiveActions;
SELECT COUNT(*) AS RemainingSubActions FROM SubActions WHERE CorrectiveActionId IS NOT NULL;
SELECT COUNT(*) AS RemainingAttachments FROM CorrectiveActionAttachments;

PRINT 'All corrective actions have been deleted successfully.';