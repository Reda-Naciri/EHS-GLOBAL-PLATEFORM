-- Create test notification with redirectUrl to test redirection functionality

-- Get the first user ID from the database
WITH FirstUser AS (
    SELECT Id, Email FROM AspNetUsers LIMIT 1
)
INSERT INTO Notifications (
    Id,
    Title, 
    Message, 
    Type, 
    UserId, 
    TriggeredByUserId, 
    RelatedReportId,
    RelatedActionId,
    RedirectUrl,
    IsRead, 
    CreatedAt, 
    IsEmailSent
)
SELECT 
    LOWER(HEX(RANDOMBLOB(16))),
    'Test Action Update',
    'This is a test notification with redirect URL to verify redirection functionality.',
    'SubActionUpdatedByProfile',
    Id,
    NULL,
    1,
    1,
    '/reports/1#action-1',
    0,
    datetime('now'),
    0
FROM FirstUser;

-- Verify the notification was created
SELECT 
    Id,
    Title,
    Message,
    Type,
    UserId,
    RedirectUrl,
    CreatedAt
FROM Notifications 
WHERE Title = 'Test Action Update'
ORDER BY CreatedAt DESC;