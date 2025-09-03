-- HSE Database Investigation for Corrective Actions
-- Focus: Find who authored each CorrectiveAction, specifically Yahya's actions

.headers on
.mode column

.print "=== HSE Database Investigation: Corrective Actions ==="
.print "Looking for Yahya's authored actions vs system showing only 1"
.print "Expected: Yahya authored 2 actions, System shows: 1 action"
.print ""

-- 1. All Users in the system
.print "=== ALL USERS IN DATABASE ==="
SELECT 
    Id, 
    FirstName, 
    LastName, 
    Email, 
    CompanyId,
    Department,
    Position
FROM AspNetUsers 
ORDER BY LastName, FirstName;

.print ""

-- 2. Find users with 'Yahya' in name or email
.print "=== USERS MATCHING 'YAHYA' ==="
SELECT 
    Id, 
    FirstName, 
    LastName, 
    Email, 
    CompanyId,
    Department,
    Position
FROM AspNetUsers 
WHERE FirstName LIKE '%Yahya%' OR LastName LIKE '%Yahya%' OR Email LIKE '%yahya%' OR Email LIKE '%Yahya%';

.print ""

-- 3. All CorrectiveActions with creator information
.print "=== ALL CORRECTIVE ACTIONS WITH CREATORS ==="
SELECT 
    ca.Id as ActionId,
    ca.Title,
    ca.Status,
    ca.Priority,
    ca.CreatedByHSEId,
    u.FirstName || ' ' || u.LastName as CreatedByName,
    u.Email as CreatedByEmail,
    ca.CreatedAt,
    ca.ReportId,
    ca.DueDate
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
ORDER BY ca.Id;

.print ""

-- 4. Focus on specific IDs mentioned in logs [42, 43, 45, 46]
.print "=== SPECIFIC ACTIONS FROM LOGS [42, 43, 45, 46] ==="
SELECT 
    ca.Id as ActionId,
    ca.Title,
    ca.Status,
    ca.CreatedByHSEId,
    u.FirstName || ' ' || u.LastName as CreatedByName,
    u.Email as CreatedByEmail,
    ca.CreatedAt,
    ca.ReportId
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
WHERE ca.Id IN (42, 43, 45, 46)
ORDER BY ca.Id;

.print ""

-- 5. Actions created by anyone with 'Yahya' in their name or email
.print "=== ACTIONS CREATED BY YAHYA ==="
SELECT 
    ca.Id as ActionId,
    ca.Title,
    ca.Status,
    ca.Priority,
    ca.CreatedByHSEId,
    u.FirstName || ' ' || u.LastName as CreatedByName,
    u.Email as CreatedByEmail,
    ca.CreatedAt,
    ca.ReportId,
    ca.DueDate,
    ca.AssignedToProfileId
FROM CorrectiveActions ca
INNER JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
WHERE u.FirstName LIKE '%Yahya%' OR u.LastName LIKE '%Yahya%' OR u.Email LIKE '%yahya%' OR u.Email LIKE '%Yahya%'
ORDER BY ca.Id;

.print ""

-- 6. Count actions by creator (to see if there's a mismatch)
.print "=== ACTION COUNT BY CREATOR ==="
SELECT 
    ca.CreatedByHSEId,
    u.FirstName || ' ' || u.LastName as CreatorName,
    u.Email,
    COUNT(ca.Id) as ActionCount
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
GROUP BY ca.CreatedByHSEId, u.FirstName, u.LastName, u.Email
HAVING ActionCount > 0
ORDER BY ActionCount DESC, CreatorName;

.print ""

-- 7. Check for any orphaned actions (created by non-existent users)
.print "=== ORPHANED ACTIONS (NO CREATOR FOUND) ==="
SELECT 
    ca.Id as ActionId,
    ca.Title,
    ca.Status,
    ca.CreatedByHSEId,
    ca.CreatedAt,
    'NO CREATOR FOUND' as Issue
FROM CorrectiveActions ca
LEFT JOIN AspNetUsers u ON ca.CreatedByHSEId = u.Id
WHERE u.Id IS NULL
ORDER BY ca.Id;

.print ""

-- 8. Check user roles for creators
.print "=== USER ROLES FOR ACTION CREATORS ==="
SELECT DISTINCT
    u.Id,
    u.FirstName || ' ' || u.LastName as UserName,
    u.Email,
    r.Name as RoleName,
    COUNT(ca.Id) as ActionsCreated
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN CorrectiveActions ca ON u.Id = ca.CreatedByHSEId
GROUP BY u.Id, u.FirstName, u.LastName, u.Email, r.Name
HAVING ActionsCreated > 0
ORDER BY ActionsCreated DESC, UserName;

.print ""
.print "=== INVESTIGATION COMPLETE ==="
.print "If Yahya appears with 2 actions but system shows 1, check:"
.print "1. Email notification filtering logic"
.print "2. User role/permission filtering"
.print "3. Frontend display logic"
.print "4. Database query filters in the application"