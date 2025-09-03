-- Insert test notifications for HSE notification banner testing
-- Clear existing test notifications first
DELETE FROM Notifications WHERE Message LIKE '%test%' OR Message LIKE '%sample%' OR Type LIKE '%Test%';

-- Get user IDs
DECLARE @AdminUserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@te.com');
DECLARE @HSEUserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'hse@te.com');

-- Insert sample notifications for Admin user
INSERT INTO Notifications (Title, Message, Type, UserId, IsRead, IsEmailSent, CreatedAt, ReadAt) VALUES
('Daily HSE System Update', 'Last 24h: 3 reports completed, 2 new actions created, 1 action completed.', 'DailyUpdate', @AdminUserId, 0, 0, DATEADD(MINUTE, -30, GETUTCDATE()), NULL),
('New Registration Request', 'New user registration request from John Smith (john.smith@te.com) for Production department.', 'RegistrationRequest', @AdminUserId, 0, 0, DATEADD(HOUR, -2, GETUTCDATE()), NULL),
('Overdue Items Alert', 'System has 2 overdue actions and 1 overdue sub-actions requiring attention.', 'OverdueAlert', @AdminUserId, 0, 0, DATEADD(HOUR, -4, GETUTCDATE()), NULL),
('Action Cancelled by HSE', 'HSE user HSE Manager cancelled action ''Safety Equipment Check'' in report ''Equipment Maintenance''.', 'ActionCancelled', @AdminUserId, 1, 0, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(MINUTE, -10, GETUTCDATE()));

-- Insert sample notifications for HSE user
INSERT INTO Notifications (Title, Message, Type, UserId, IsRead, IsEmailSent, CreatedAt, ReadAt) VALUES
('New Report Submitted', 'A new Incident report ''Slip and Fall in Production Area A'' has been submitted in Production Area A by Reporter ID: TE001234.', 'ReportSubmitted', @HSEUserId, 0, 0, DATEADD(MINUTE, -15, GETUTCDATE()), NULL),
('Report Assigned to You', 'Report ''Chemical Spill in Laboratory'' has been assigned to you by an administrator.', 'ReportAssigned', @HSEUserId, 0, 0, DATEADD(MINUTE, -45, GETUTCDATE()), NULL),
('New Comment on Assigned Report', 'John Doe added a comment to report ''Equipment Malfunction'' assigned to you.', 'CommentAdded', @HSEUserId, 1, 0, DATEADD(HOUR, -1, GETUTCDATE()), DATEADD(MINUTE, -5, GETUTCDATE())),
('Action Deadline Approaching', 'Action ''Replace Safety Guards'' is due in 2 day(s) (08/05/2025).', 'DeadlineApproaching', @HSEUserId, 0, 0, DATEADD(HOUR, -3, GETUTCDATE()), NULL),
('New Action Added to Your Report', 'System Administrator added a new action ''Emergency Response Training'' to report ''Fire Safety Inspection'' assigned to you.', 'ActionAdded', @HSEUserId, 0, 0, DATEADD(HOUR, -6, GETUTCDATE()), NULL);

-- Verify the insertions
SELECT 
    N.Title,
    N.Message,
    N.Type,
    N.IsRead,
    N.CreatedAt,
    U.Email as UserEmail
FROM Notifications N
JOIN AspNetUsers U ON N.UserId = U.Id
WHERE N.CreatedAt >= DATEADD(HOUR, -12, GETUTCDATE())
ORDER BY N.CreatedAt DESC;

PRINT 'Test notifications inserted successfully!';