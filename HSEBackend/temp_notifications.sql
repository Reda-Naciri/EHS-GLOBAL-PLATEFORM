-- Script to create test notifications for HSE system testing
-- This will create sample notifications for the seeded users (admin@te.com and hse@te.com)

-- Get the user IDs for admin and HSE users
DECLARE @AdminUserId NVARCHAR(450) = 'admin-default-id';
DECLARE @HSEUserId NVARCHAR(450) = 'hse-default-id';

-- Create test notifications for Admin user
INSERT INTO Notifications (Title, Message, Type, UserId, TriggeredByUserId, IsRead, CreatedAt, IsEmailSent)
VALUES 
    ('Daily HSE System Update', 
     'Last 24h: 3 reports completed, 2 new actions created, 1 actions completed.', 
     'DailyUpdate', 
     @AdminUserId, 
     NULL, 
     0, 
     DATEADD(MINUTE, -2 hours, GETUTCDATE()), 
     0),
    
    ('New Registration Request', 
     'New user registration request from John Smith (john.smith@te.com) for Engineering department.', 
     'RegistrationRequest', 
     @AdminUserId, 
     NULL, 
     0, 
     DATEADD(MINUTE, -1 hour, GETUTCDATE()), 
     0),
     
    ('Overdue Items Alert', 
     'System has 2 overdue actions and 1 overdue sub-actions requiring attention.', 
     'OverdueAlert', 
     @AdminUserId, 
     NULL, 
     0, 
     DATEADD(MINUTE, -30 minutes, GETUTCDATE()), 
     0),
     
    ('Action Cancelled by HSE', 
     'HSE user HSE Manager cancelled action ''Install Safety Guard'' in report ''Machinery Incident Report''.', 
     'ActionCancelled', 
     @AdminUserId, 
     @HSEUserId, 
     1, 
     DATEADD(MINUTE, -3 hours, GETUTCDATE()), 
     1);

-- Create test notifications for HSE user
INSERT INTO Notifications (Title, Message, Type, UserId, TriggeredByUserId, RelatedReportId, IsRead, CreatedAt, IsEmailSent)
VALUES 
    ('New Report Submitted', 
     'A new Safety report ''Slip and Fall Incident'' has been submitted in Production Area A by Reporter ID: TE001234.', 
     'ReportSubmitted', 
     @HSEUserId, 
     @AdminUserId, 
     NULL, 
     0, 
     DATEADD(MINUTE, -45 minutes, GETUTCDATE()), 
     0),
     
    ('Report Assigned to You', 
     'Report ''Chemical Spill Investigation'' has been assigned to you by an administrator.', 
     'ReportAssigned', 
     @HSEUserId, 
     @AdminUserId, 
     NULL, 
     0, 
     DATEADD(MINUTE, -1.5 hours, GETUTCDATE()), 
     0),
     
    ('New Comment on Assigned Report', 
     'System Administrator added a comment to report ''Equipment Malfunction Report'' assigned to you.', 
     'CommentAdded', 
     @HSEUserId, 
     @AdminUserId, 
     NULL, 
     1, 
     DATEADD(MINUTE, -20 minutes, GETUTCDATE()), 
     0),
     
    ('Action Deadline Approaching', 
     'Action ''Conduct Safety Training'' is due in 2 day(s) (08/05/2025).', 
     'DeadlineApproaching', 
     @HSEUserId, 
     NULL, 
     NULL, 
     0, 
     DATEADD(MINUTE, -10 minutes, GETUTCDATE()), 
     0),
     
    ('New Action Added to Your Report', 
     'System Administrator added a new action ''Update Safety Protocols'' to report ''Safety Audit Report'' assigned to you.', 
     'ActionAdded', 
     @HSEUserId, 
     @AdminUserId, 
     NULL, 
     0, 
     DATEADD(MINUTE, -5 minutes, GETUTCDATE()), 
     0);

-- Query to verify the notifications were created
SELECT 
    Id,
    Title,
    Message,
    Type,
    UserId,
    TriggeredByUserId,
    IsRead,
    CreatedAt,
    IsEmailSent
FROM Notifications 
ORDER BY CreatedAt DESC;

-- Query to count notifications by user
SELECT 
    u.Email,
    COUNT(n.Id) as TotalNotifications,
    SUM(CASE WHEN n.IsRead = 0 THEN 1 ELSE 0 END) as UnreadNotifications
FROM AspNetUsers u
LEFT JOIN Notifications n ON u.Id = n.UserId
WHERE u.Email IN ('admin@te.com', 'hse@te.com')
GROUP BY u.Id, u.Email;
