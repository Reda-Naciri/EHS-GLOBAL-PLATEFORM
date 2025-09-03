using HSEBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace HSEBackend.Services
{
    public interface IHSEAccessControlService
    {
        Task<bool> CanCreateCorrectiveActionAsync(string userId, int reportId);
        Task<bool> CanAccessZoneAsync(string userId, int zoneId);
        Task<List<int>> GetUserResponsibleZoneIdsAsync(string userId);
        Task<bool> IsAdminAsync(string userId);
        Task<bool> IsHSEAsync(string userId);
        Task<bool> CanViewReportAsync(string userId, int reportId);
        Task<bool> CanInteractWithReportAsync(string userId, int reportId);
        Task<bool> CanOpenReportAsync(string userId, int reportId);
        Task<bool> CanManageCorrectiveActionAsync(string userId, int correctiveActionId);
        Task<bool> CanManageActionAsync(string userId, int actionId);
        Task<bool> IsCurrentReportAssigneeAsync(string userId, int reportId);
        Task<bool> CanCreateActionAsync(string userId, int reportId);
        Task<bool> IsActionAuthorAsync(string userId, int actionId);
        Task<bool> CanAbortActionAsync(string userId, int actionId);
        Task<bool> CanAbortCorrectiveActionAsync(string userId, int correctiveActionId);
        Task<bool> CanManageActionOutsideReportAsync(string userId, int actionId);
        Task<bool> CanManageActionInReportContextAsync(string userId, int actionId);
        Task<bool> CanCreateSubActionForActionAsync(string userId, int actionId);
        Task<bool> CanCreateSubActionForCorrectiveActionAsync(string userId, int correctiveActionId);
        Task<bool> CanCancelSubActionAsync(string userId, int subActionId);
    }

    public class HSEAccessControlService : IHSEAccessControlService
    {
        private readonly AppDbContext _context;

        public HSEAccessControlService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanCreateCorrectiveActionAsync(string userId, int reportId)
        {
            // ADMIN can create corrective actions for ALL reports
            if (await IsAdminAsync(userId))
                return true;

            // HSE users can create corrective actions for ALL reports
            if (await IsHSEAsync(userId))
                return true;

            return false;
        }

        public async Task<bool> CanAccessZoneAsync(string userId, int zoneId)
        {
            // Admin can access all zones
            if (await IsAdminAsync(userId))
                return true;

            // HSE can only access their responsible zones
            if (await IsHSEAsync(userId))
            {
                return await _context.HSEZoneResponsibilities
                    .AnyAsync(hzr => hzr.HSEUserId == userId && hzr.ZoneId == zoneId && hzr.IsActive);
            }

            return false;
        }

        public async Task<List<int>> GetUserResponsibleZoneIdsAsync(string userId)
        {
            // Admin has access to all zones
            if (await IsAdminAsync(userId))
            {
                return await _context.Zones
                    .Where(z => z.IsActive)
                    .Select(z => z.Id)
                    .ToListAsync();
            }

            // HSE users have access to their assigned zones + delegated zones
            if (await IsHSEAsync(userId))
            {
                var responsibleZoneIds = new List<int>();

                // Get permanent zone responsibilities
                var permanentZones = await _context.HSEZoneResponsibilities
                    .Where(hzr => hzr.HSEUserId == userId && hzr.IsActive)
                    .Select(hzr => hzr.ZoneId)
                    .ToListAsync();

                responsibleZoneIds.AddRange(permanentZones);

                // Get temporary delegated zones (active delegations)
                var currentDate = DateTime.UtcNow;
                var delegatedZones = await _context.HSEZoneDelegations
                    .Where(hzd => hzd.ToHSEUserId == userId && 
                                 hzd.IsActive &&
                                 hzd.StartDate <= currentDate && 
                                 hzd.EndDate >= currentDate)
                    .Select(hzd => hzd.ZoneId)
                    .ToListAsync();

                responsibleZoneIds.AddRange(delegatedZones);

                // Remove duplicates and return
                return responsibleZoneIds.Distinct().ToList();
            }

            return new List<int>();
        }

        public async Task<bool> IsAdminAsync(string userId)
        {
            return await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(x => x.UserId == userId && x.Name == "Admin");
        }

        public async Task<bool> IsHSEAsync(string userId)
        {
            return await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(x => x.UserId == userId && x.Name == "HSE");
        }

        /// <summary>
        /// Determines if a user can VIEW a report
        /// Admin: Can see all reports
        /// HSE: Can see assigned reports + open team reports
        /// </summary>
        public async Task<bool> CanViewReportAsync(string userId, int reportId)
        {
            // ADMIN can see ALL reports
            if (await IsAdminAsync(userId))
                return true;

            // HSE users can see assigned reports and open team reports
            if (await IsHSEAsync(userId))
            {
                var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
                if (report == null) return false;

                // Can see reports assigned to them
                if (report.AssignedHSEId == userId)
                    return true;

                // Can see open status team reports (reports opened by other HSE users)
                if (report.Status == "Opened")
                    return true;

                return false;
            }

            return false;
        }

        /// <summary>
        /// Determines if a user can INTERACT (comment, create actions) with a report
        /// </summary>
        public async Task<bool> CanInteractWithReportAsync(string userId, int reportId)
        {
            // ADMIN can interact with ALL reports
            if (await IsAdminAsync(userId))
                return true;

            // HSE users can interact with ALL reports
            if (await IsHSEAsync(userId))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if a user can OPEN (change status from Unopened to Opened) a report
        /// </summary>
        public async Task<bool> CanOpenReportAsync(string userId, int reportId)
        {
            // ADMIN can open ALL reports
            if (await IsAdminAsync(userId))
                return true;

            // HSE users can open ALL reports
            if (await IsHSEAsync(userId))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if a user can manage a corrective action based on current report assignment
        /// Priority: Current report assignee > Admin > Original creator (if still assigned)
        /// </summary>
        public async Task<bool> CanManageCorrectiveActionAsync(string userId, int correctiveActionId)
        {
            // ADMIN can manage ALL corrective actions
            if (await IsAdminAsync(userId))
                return true;

            // HSE users can manage ALL corrective actions
            if (await IsHSEAsync(userId))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if a user can manage a regular action
        /// Only action author can manage their action, except Admin can abort any action
        /// </summary>
        public async Task<bool> CanManageActionAsync(string userId, int actionId)
        {
            var action = await _context.Actions
                .Include(a => a.Report)
                .FirstOrDefaultAsync(a => a.Id == actionId);

            if (action == null) return false;

            // Action author can manage their action if still assigned to the report OR if accessing outside report context
            if (action.CreatedById == userId)
            {
                // If still assigned to report, can manage
                if (action.Report.AssignedHSEId == userId)
                    return true;
                
                // If not assigned to report anymore, can only manage outside report context
                // This will be handled by the calling context
                return false;
            }

            return false;
        }

        /// <summary>
        /// Checks if a user is currently assigned to a specific report
        /// </summary>
        public async Task<bool> IsCurrentReportAssigneeAsync(string userId, int reportId)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == reportId);

            return report?.AssignedHSEId == userId;
        }

        /// <summary>
        /// Determines if a user can create actions/subactions for a report
        /// Only Admin and assigned HSE can create actions
        /// </summary>
        public async Task<bool> CanCreateActionAsync(string userId, int reportId)
        {
            // ADMIN can create actions for ALL reports (respecting report status)
            if (await IsAdminAsync(userId))
            {
                var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
                if (report == null) return false;
                
                // Cannot create actions on closed reports
                return report.Status != "Closed";
            }

            // HSE users can only create actions for reports assigned to them
            if (await IsHSEAsync(userId))
            {
                var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
                if (report == null) return false;
                
                // Must be assigned to the report and report not closed
                return report.AssignedHSEId == userId && report.Status != "Closed";
            }

            return false;
        }

        /// <summary>
        /// Determines if a user is the author of an action and can fully manage it
        /// </summary>
        public async Task<bool> IsActionAuthorAsync(string userId, int actionId)
        {
            var action = await _context.Actions
                .Include(a => a.Report)
                .FirstOrDefaultAsync(a => a.Id == actionId);

            if (action == null)
                return false;

            // Must be the author AND still assigned to the report
            return action.CreatedById == userId && action.Report.AssignedHSEId == userId;
        }

        /// <summary>
        /// Determines if a user can abort an action (Admin can abort any action, HSE can abort actions in their assigned reports)
        /// </summary>
        public async Task<bool> CanAbortActionAsync(string userId, int actionId)
        {
            var action = await _context.Actions
                .Include(a => a.Report)
                .FirstOrDefaultAsync(a => a.Id == actionId);

            if (action == null)
                return false;

            // ADMIN can abort any action
            if (await IsAdminAsync(userId))
                return true;

            // HSE users can abort actions (including admin actions) in reports assigned to them
            if (await IsHSEAsync(userId))
            {
                // Get the user's information for comprehensive matching
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return false;

                // Check multiple assignment matching methods (same as frontend logic)
                // 1. Direct ID matching
                if (action.Report.AssignedHSEId == userId)
                    return true;

                // 2. Name-based matching (check if AssignedHSEId contains the user's full name)
                var userFullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.IsNullOrEmpty(userFullName) && action.Report.AssignedHSEId == userFullName)
                    return true;

                // 3. Company ID matching (for backwards compatibility)
                if (!string.IsNullOrEmpty(user.CompanyId) && action.Report.AssignedHSEId == user.CompanyId)
                    return true;

                return false;
            }

            return false;
        }

        /// <summary>
        /// Determines if a user can abort a corrective action (Admin can abort any corrective action, HSE can abort corrective actions in their assigned reports)
        /// </summary>
        public async Task<bool> CanAbortCorrectiveActionAsync(string userId, int correctiveActionId)
        {
            var correctiveAction = await _context.CorrectiveActions
                .Include(ca => ca.Report)
                .FirstOrDefaultAsync(ca => ca.Id == correctiveActionId);

            if (correctiveAction == null)
                return false;

            // ADMIN can abort any corrective action
            if (await IsAdminAsync(userId))
                return true;

            // HSE users can abort corrective actions (including admin corrective actions) in reports assigned to them
            if (await IsHSEAsync(userId))
            {
                // For standalone corrective actions (no report), HSE users can abort their own actions
                if (correctiveAction.Report == null)
                {
                    // Check if the HSE user is the creator of this standalone corrective action
                    return correctiveAction.CreatedByHSEId == userId;
                }

                // Get the user's information for comprehensive matching
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return false;

                // Check multiple assignment matching methods (same as frontend logic)
                // 1. Direct ID matching
                if (correctiveAction.Report.AssignedHSEId == userId)
                    return true;

                // 2. Name-based matching (check if AssignedHSEId contains the user's full name)
                var userFullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.IsNullOrEmpty(userFullName) && correctiveAction.Report.AssignedHSEId == userFullName)
                    return true;

                // 3. Company ID matching (for backwards compatibility)
                if (!string.IsNullOrEmpty(user.CompanyId) && correctiveAction.Report.AssignedHSEId == user.CompanyId)
                    return true;

                return false;
            }

            return false;
        }

        /// <summary>
        /// Determines if a user can manage an action outside of report context (e.g., in action management page)
        /// Action authors can always manage their actions outside report context
        /// </summary>
        public async Task<bool> CanManageActionOutsideReportAsync(string userId, int actionId)
        {
            var action = await _context.Actions
                .FirstOrDefaultAsync(a => a.Id == actionId);

            if (action == null) return false;

            // Action author can always manage their action outside report context
            return action.CreatedById == userId;
        }

        /// <summary>
        /// Determines if a user can manage an action within report context
        /// Action authors can only manage if still assigned to the report
        /// </summary>
        public async Task<bool> CanManageActionInReportContextAsync(string userId, int actionId)
        {
            var action = await _context.Actions
                .Include(a => a.Report)
                .FirstOrDefaultAsync(a => a.Id == actionId);

            if (action == null) return false;

            // Admin can abort any action in report context
            if (await IsAdminAsync(userId) && action.CreatedById != userId)
                return true; // But only for aborting, not full management

            // Action author can manage if still assigned to report
            if (action.CreatedById == userId && action.Report.AssignedHSEId == userId)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if a user can create sub-actions for a regular action
        /// Only action authors can create sub-actions for their own actions
        /// </summary>
        public async Task<bool> CanCreateSubActionForActionAsync(string userId, int actionId)
        {
            var action = await _context.Actions
                .FirstOrDefaultAsync(a => a.Id == actionId);

            if (action == null) return false;

            // Only action author can create sub-actions for their action
            return action.CreatedById == userId;
        }

        /// <summary>
        /// Determines if a user can create sub-actions for a corrective action
        /// Only corrective action authors can create sub-actions for their own corrective actions
        /// </summary>
        public async Task<bool> CanCreateSubActionForCorrectiveActionAsync(string userId, int correctiveActionId)
        {
            var correctiveAction = await _context.CorrectiveActions
                .FirstOrDefaultAsync(ca => ca.Id == correctiveActionId);

            if (correctiveAction == null) return false;

            // Only corrective action author can create sub-actions for their corrective action
            return correctiveAction.CreatedByHSEId == userId;
        }

        /// <summary>
        /// Determines if a user can cancel a sub-action
        /// Admin can cancel any sub-action, users can only cancel sub-actions of their own actions/corrective actions
        /// </summary>
        public async Task<bool> CanCancelSubActionAsync(string userId, int subActionId)
        {
            // ADMIN can cancel any sub-action
            if (await IsAdminAsync(userId))
                return true;

            var subAction = await _context.SubActions
                .Include(sa => sa.Action)
                .Include(sa => sa.CorrectiveAction)
                .FirstOrDefaultAsync(sa => sa.Id == subActionId);

            if (subAction == null) return false;

            // Check if user owns the parent action or corrective action
            if (subAction.Action != null)
            {
                return subAction.Action.CreatedById == userId;
            }
            
            if (subAction.CorrectiveAction != null)
            {
                return subAction.CorrectiveAction.CreatedByHSEId == userId;
            }

            return false;
        }
    }
}