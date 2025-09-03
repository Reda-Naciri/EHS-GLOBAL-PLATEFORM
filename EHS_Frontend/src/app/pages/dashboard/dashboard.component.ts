import { Component, HostListener, Renderer2, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription, forkJoin, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { DashboardService } from '../../services/dashboard.service';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { CorrectiveActionsService, CorrectiveActionDetailDto } from '../../services/corrective-actions.service';
import { DashboardStatsDto, RecentActivityDto } from '../../models/dashboard.models';
import { RecentReportDto } from '../../models/report.models';
import { UserDto } from '../../models/auth.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  dropdownOpen = false;
  sidebarOpen = false;
  showAllReports = false;
  loading = true;
  error: string | null = null;

  dashboardStats: DashboardStatsDto | null = null;
  recentReports: RecentReportDto[] = [];
  recentActivity: RecentActivityDto[] = [];
  currentUser: UserDto | null = null;
  
  // Action data for real counts
  correctiveActions: CorrectiveActionDetailDto[] = [];
  
  
  private userSubscription?: Subscription;

  constructor(
    private renderer: Renderer2, 
    private el: ElementRef,
    private dashboardService: DashboardService,
    private reportService: ReportService,
    private authService: AuthService,
    private correctiveActionsService: CorrectiveActionsService
  ) { }

  ngOnInit(): void {
    // Subscribe to current user to get role and zone information
    this.userSubscription = this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        console.log('📊 Dashboard: Current user loaded:', user);
        this.loadDashboardData();
      }
    });
  }

  ngOnDestroy(): void {
    this.userSubscription?.unsubscribe();
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  toggleSidebar() {
    this.sidebarOpen = !this.sidebarOpen;
    const sidebar = this.el.nativeElement.querySelector('.sidebar');
    this.sidebarOpen
      ? this.renderer.addClass(sidebar, 'open')
      : this.renderer.removeClass(sidebar, 'open');
  }

  @HostListener('document:click', ['$event'])
  closeSidebar(event: Event) {
    if (this.sidebarOpen) {
      const sidebar = this.el.nativeElement.querySelector('.sidebar');
      const hamburger = this.el.nativeElement.querySelector('.hamburger');
      if (!sidebar.contains(event.target as Node) && !hamburger.contains(event.target as Node)) {
        this.renderer.removeClass(sidebar, 'open');
        this.sidebarOpen = false;
      }
    }
  }

  private loadDashboardData(): void {
    if (!this.currentUser) {
      return;
    }

    this.loading = true;
    this.error = null;

    console.log('📊 Dashboard: Loading data for user:', this.currentUser);
    console.log('📊 Dashboard: User role:', this.currentUser.role);
    console.log('📊 Dashboard: User zone:', this.currentUser.zone);

    // Load dashboard stats based on user role
    this.loadDashboardStats();
    
    // Load recent reports based on user role and zone
    this.loadRecentReports();
    
    // Load corrective actions for real counts
    this.loadCorrectiveActions();
  }

  private loadDashboardStats(): void {
    // For HSE users, stats should be based on their assigned reports only
    // For admins, show all stats
    console.log('📊 Dashboard: Loading stats for user role:', this.currentUser?.role);
    
    // Try to get stats from backend first
    this.dashboardService.getDashboardStats().subscribe({
      next: (stats) => {
        console.log('📊 Dashboard: Backend stats loaded:', stats);
        
        // For HSE users, filter stats to only show data for their assigned reports
        if (this.isHSEUser()) {
          this.dashboardStats = this.filterStatsForHSEUser(stats);
        } else {
          this.dashboardStats = stats;
        }
        
        console.log('📊 Dashboard: Final filtered stats:', this.dashboardStats);
      },
      error: (error) => {
        console.error('📊 Dashboard: Failed to load stats from backend:', error);
        console.log('📊 Dashboard: Using mock data with role-based filtering');
        // Use mock data with role-based filtering
        this.dashboardStats = this.getMockDashboardStats();
      }
    });
  }

  private loadRecentReports(): void {
    // For HSE users, load ALL assigned reports (no limit)
    // For admins, limit to recent 10 reports
    if (this.isHSEUser()) {
      console.log('📊 Dashboard: Loading assigned reports for HSE user');
      // Load ALL reports assigned to this HSE user (no limit)
      this.loadAssignedReports(10); // Start with 10 for display, load all for counting
    } else {
      console.log('📊 Dashboard: Loading all reports for admin');
      // Load recent 10 reports for admin
      this.loadAllReports(10);
    }
  }

  private loadAssignedReports(limit: number): void {
    console.log('📊 Dashboard: Loading reports assigned to HSE user:', this.currentUser?.id, this.currentUser?.companyId);
    console.log('📊 Dashboard: Current user details:', this.currentUser);
    
    // Load ALL reports to get accurate counts for HSE user (not just recent ones)
    this.reportService.getRecentReports(1000).subscribe({
      next: (allReports) => {
        console.log('📊 Dashboard: Raw reports received:', allReports.length);
        console.log('📊 Dashboard: First few reports:', allReports.slice(0, 3));
        
        console.log('📊 Dashboard: Filtering reports for HSE user assignment');
        
        // Filter reports assigned to this HSE user
        const assignedReports = allReports.filter(report => {
          // Primary matching: Check if assignedHSE matches user's full name
          const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
          const isAssignedByName = report.assignedHSE === userFullName;
          
          // Fallback matching: Check ID and company ID (for backwards compatibility)
          const isAssignedById = report.assignedHSE === this.currentUser?.id;
          const isAssignedByCompanyId = report.assignedHSE === this.currentUser?.companyId;
          
          // Also check string versions in case of type mismatches
          const isAssignedByIdString = String(report.assignedHSE) === String(this.currentUser?.id);
          const isAssignedByCompanyIdString = String(report.assignedHSE) === String(this.currentUser?.companyId);
          
          const isMatch = isAssignedByName || isAssignedById || isAssignedByCompanyId || isAssignedByIdString || isAssignedByCompanyIdString;
          
          console.log(`📊 Dashboard: Report ${report.id}:`, {
            assignedHSE: report.assignedHSE,
            userFullName: userFullName,
            userID: this.currentUser?.id,
            companyID: this.currentUser?.companyId,
            isAssignedByName,
            isAssignedById,
            isAssignedByCompanyId,
            isAssignedByIdString,
            isAssignedByCompanyIdString,
            finalMatch: isMatch
          });
          
          return isMatch;
        });
        
        console.log('📊 Dashboard: Filtered assigned reports:', assignedReports.length);
        console.log('📊 Dashboard: Assigned reports details:', assignedReports);
        
        // Store assigned reports for display and indicator calculations
        this.recentReports = assignedReports;
        
        if (assignedReports.length === 0) {
          console.log('📊 Dashboard: No assigned reports found for HSE user');
          console.log('📊 Dashboard: This means either:');
          console.log('1. No reports exist in database');
          console.log('2. No reports are assigned to this HSE user');
          console.log('3. The assignedHSE field format doesn\'t match user name/ID/companyId');
          console.log('📊 Dashboard: Current user for reference:', {
            id: this.currentUser?.id,
            companyId: this.currentUser?.companyId,
            firstName: this.currentUser?.firstName,
            lastName: this.currentUser?.lastName,
            fullName: `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim(),
            role: this.currentUser?.role,
            email: this.currentUser?.email
          });
        } else {
          console.log('📊 Dashboard: ✅ Found assigned reports! Using filtered results');
        }
        
        // For display in recent reports section, limit to recent ones
        const recentAssignedReports = assignedReports
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
          .slice(0, Math.min(limit, assignedReports.length));
        
        // Load real user names from database for display
        this.loadReporterNamesFromDatabase(recentAssignedReports);
        
        console.log('📊 Dashboard: Total assigned reports:', assignedReports.length);
        console.log('📊 Dashboard: Recent assigned reports for display:', recentAssignedReports.length);
        console.log('📊 Dashboard: Assigned reports breakdown:', {
          total: assignedReports.length,
          safety: assignedReports.filter(r => r.type !== 'Incident-Management').length,
          incidents: assignedReports.filter(r => r.type === 'Incident-Management').length,
          unopened: assignedReports.filter(r => r.status === 'Unopened').length,
          opened: assignedReports.filter(r => r.status === 'Opened').length,
          closed: assignedReports.filter(r => r.status === 'Closed').length
        });
        
        this.loading = false;
      },
      error: (error) => {
        console.error('📊 Dashboard: Failed to load reports:', error);
        this.handleReportsError();
      }
    });
  }

  private loadAllReports(limit: number): void {
    console.log('📊 Dashboard: Loading all reports for admin');
    
    this.reportService.getRecentReports(limit).subscribe({
      next: (reports) => {
        // Load real user names from database
        this.loadReporterNamesFromDatabase(reports);
        this.recentReports = reports;
        console.log('📊 Dashboard: All reports loaded for admin:', reports);
        this.loading = false;
      },
      error: (error) => {
        console.error('📊 Dashboard: Failed to load reports:', error);
        this.handleReportsError();
      }
    });
  }

  private handleReportsError(): void {
    this.error = 'Failed to load reports';
    this.loading = false;
    
    // No mock data - use empty array when backend fails
    this.recentReports = [];
  }

  // Calculate dashboard stats from actual assigned reports for HSE users
  private calculateDashboardStatsFromReports(reports: any[]): void {
    console.log('📊 Dashboard: Calculating stats from', reports.length, 'assigned reports');
    
    // Count reports by status
    const statusCounts = reports.reduce((acc, report) => {
      const status = report.status;
      acc[status] = (acc[status] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
    
    // Count reports by type
    const typeCounts = reports.reduce((acc, report) => {
      const type = report.type;
      acc[type] = (acc[type] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
    
    // Count reports by zone
    const zoneCounts = reports.reduce((acc, report) => {
      const zone = report.zone;
      acc[zone] = (acc[zone] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
    
    // Create dashboard stats based on actual data
    this.dashboardStats = {
      reports: {
        totalReports: reports.length,
        openReports: statusCounts['Unopened'] || 0,
        inProgressReports: statusCounts['Opened'] || 0,
        closedReports: statusCounts['Closed'] || 0,
        reportsThisMonth: reports.length, // Simplified for now
        reportsLastMonth: 0, // Would need date filtering
        monthlyGrowthRate: 0,
        reportsByType: typeCounts,
        reportsByZone: zoneCounts,
        reportsByStatus: statusCounts
      },
      actions: {
        totalActions: 0, // Would need to sum from reports
        completedActions: 0,
        inProgressActions: 0,
        notStartedActions: 0,
        overdueActions: 0,
        completionRate: 0,
        actionsByHierarchy: {},
        actionsByStatus: {}
      },
      users: {
        totalUsers: 0,
        activeUsers: 0,
        pendingUsers: 0,
        usersByRole: {},
        usersByDepartment: {}
      },
      trends: {
        reportTrends: [],
        actionTrends: [],
        recentActivity: []
      }
    };
    
    console.log('📊 Dashboard: Calculated stats:', this.dashboardStats);
  }

  private filterStatsForHSEUser(stats: DashboardStatsDto): DashboardStatsDto {
    // This method is now only used as fallback when backend stats are available
    // but we want to override with real calculated stats from assigned reports
    return this.dashboardStats || stats;
  }

  private getMockDashboardStats(): DashboardStatsDto {
    const isAdmin = this.isAdmin();
    const userZone = this.currentUser?.zone || 'Zone A';

    return {
      reports: {
        totalReports: isAdmin ? 1 : 1, // Updated to match actual database
        openReports: isAdmin ? 1 : 1, // This represents "Unopened" reports - updated to match database
        inProgressReports: isAdmin ? 0 : 0, // This represents "Opened" reports - none in database
        closedReports: isAdmin ? 0 : 0, // This represents "Closed" reports - none in database
        reportsThisMonth: isAdmin ? 15 : 5,
        reportsLastMonth: isAdmin ? 10 : 3,
        monthlyGrowthRate: isAdmin ? 50 : 67,
        reportsByType: isAdmin ? {
          'Hasard': 8,
          'Incident-Management': 6,
          'Nearhit': 5,
          'Enviroment-aspect': 4,
          'Improvement-Idea': 2
        } : {
          'Hasard': 3,
          'Incident-Management': 2,
          'Nearhit': 2,
          'Enviroment-aspect': 1,
          'Improvement-Idea': 0
        },
        reportsByZone: isAdmin ? {
          'Zone A': 8,
          'Zone B': 6,
          'Zone C': 5,
          'Zone D': 6
        } : {
          [userZone]: 8
        },
        reportsByStatus: isAdmin ? {
          'Unopened': 8,
          'Opened': 12,
          'Closed': 5
        } : {
          'Unopened': 3,
          'Opened': 4,
          'Closed': 1
        }
      },
      actions: {
        totalActions: isAdmin ? 45 : 12,
        completedActions: isAdmin ? 30 : 8,
        inProgressActions: isAdmin ? 10 : 3,
        notStartedActions: isAdmin ? 5 : 1,
        overdueActions: isAdmin ? 3 : 1,
        completionRate: isAdmin ? 67 : 75,
        actionsByHierarchy: {},
        actionsByStatus: {}
      },
      users: {
        totalUsers: isAdmin ? 150 : 0, // HSE users don't see user stats
        activeUsers: isAdmin ? 120 : 0,
        pendingUsers: isAdmin ? 8 : 0,
        usersByRole: isAdmin ? { 'Admin': 5, 'HSE': 25, 'User': 120 } : {},
        usersByDepartment: {}
      },
      trends: {
        reportTrends: [],
        actionTrends: [],
        recentActivity: []
      }
    };
  }

  get totalReports(): number {
    if (this.isHSEUser()) {
      // For HSE users, show only reports assigned to them
      const count = this.getAssignedReportsCount();
      console.log('📊 Dashboard: HSE totalReports count:', count);
      return count;
    }
    return this.dashboardStats?.reports?.totalReports || 0;
  }

  get totalNonIncidentReports(): number {
    if (this.isHSEUser()) {
      // For HSE users, count assigned safety reports (excluding incidents)
      return this.getAssignedSafetyReportsCount();
    }
    
    const stats = this.dashboardStats?.reports;
    if (!stats) return 0;
    
    const total = stats.totalReports || 0;
    const incidents = stats.reportsByType?.['Incident-Management'] || 0;
    return total - incidents;
  }

  get incidentManagementCount(): number {
    if (this.isHSEUser()) {
      // For HSE users, count assigned incident reports only
      return this.getAssignedIncidentReportsCount();
    }
    return this.dashboardStats?.reports?.reportsByType?.['Incident-Management'] || 0;
  }

  get safetyReportsPercentage(): number {
    const total = this.totalReports;
    if (total === 0) return 0;
    return Math.round((this.totalNonIncidentReports / total) * 100);
  }

  get incidentReportsPercentage(): number {
    const total = this.totalReports;
    if (total === 0) return 0;
    return Math.round((this.incidentManagementCount / total) * 100);
  }

  get openReports(): number {
    if (this.isHSEUser()) {
      // For HSE users, show only assigned reports that are unopened
      return this.getAssignedReportsByStatus('Unopened');
    }
    return this.dashboardStats?.reports?.openReports || 0;
  }

  get closedReports(): number {
    if (this.isHSEUser()) {
      // For HSE users, show only assigned reports that are closed
      return this.getAssignedReportsByStatus('Closed');
    }
    return this.dashboardStats?.reports?.closedReports || 0;
  }

  get totalActions(): number {
    return this.getRealActionCount();
  }

  get totalActionsExcludingAborted(): number {
    const total = this.getRealActionCount();
    const aborted = this.getRealActionCount('Aborted');
    return Math.max(0, total - aborted);
  }

  get inProgressActions(): number {
    return this.getRealActionCount('In Progress');
  }

  get notStartedActions(): number {
    return this.getRealActionCount('Not Started');
  }

  get overdueActions(): number {
    return this.getRealOverdueActionCount();
  }

  get completionRate(): string {
    const total = this.getRealActionCount();
    const completed = this.getRealActionCount('Completed');
    
    if (total === 0) return '0%';
    return `${Math.round((completed / total) * 100)}%`;
  }

  get completedActions(): number {
    return this.getRealActionCount('Completed');
  }

  // Load corrective actions with same access control as actions page
  private loadCorrectiveActions(): void {
    console.log('📊 Dashboard: Loading corrective actions for real counts');
    
    this.correctiveActionsService.getAllCorrectiveActions().subscribe({
      next: (actions) => {
        console.log('📊 Dashboard: Loaded corrective actions:', actions.length);
        this.correctiveActions = actions;
        
        // Log action breakdown for debugging
        console.log('📊 Dashboard: Action breakdown:', {
          total: actions.length,
          completed: actions.filter(a => a.status === 'Completed').length,
          inProgress: actions.filter(a => a.status === 'In Progress').length,
          notStarted: actions.filter(a => a.status === 'Not Started').length,
          overdue: actions.filter(a => a.overdue).length,
          aborted: actions.filter(a => a.status === 'Aborted').length
        });
      },
      error: (error) => {
        console.error('📊 Dashboard: Failed to load corrective actions:', error);
        this.correctiveActions = [];
      }
    });
  }

  // Get real action counts (same logic as actions page)
  private getRealActionCount(status?: string): number {
    if (!this.correctiveActions.length) return 0;
    
    if (!status) {
      // Total actions
      return this.correctiveActions.length;
    }
    
    // Count by status
    return this.correctiveActions.filter(action => action.status === status).length;
  }

  private getRealOverdueActionCount(): number {
    if (!this.correctiveActions.length) return 0;
    
    const today = new Date();
    return this.correctiveActions.filter(action => {
      const dueDate = new Date(action.dueDate);
      return dueDate < today && action.status !== 'Completed' && action.status !== 'Canceled' && action.status !== 'Aborted';
    }).length;
  }

  getReporterName(report: RecentReportDto): string {
    // Use only data from database - if reporterName exists use it, otherwise show the ID
    return report.reporterName || report.reporterId || 'Unknown Reporter';
  }

  getHSEAgent(report: RecentReportDto): string {
    // Only show HSE agent if it exists in database
    return report.assignedHSE || 'Unassigned';
  }

  // Load real reporter names using the same validation endpoint as report page
  private loadReporterNamesFromDatabase(reports: RecentReportDto[]): void {
    // Get unique reporter IDs that don't already have names
    const reporterIdsToLookup = reports
      .filter(report => !report.reporterName && report.reporterId)
      .map(report => report.reporterId)
      .filter((id, index, self) => self.indexOf(id) === index); // Remove duplicates

    console.log('📊 Dashboard: Reports to process:', reports.length);
    console.log('📊 Dashboard: Reporter IDs needing lookup:', reporterIdsToLookup);

    if (reporterIdsToLookup.length === 0) {
      console.log('📊 Dashboard: No reporter IDs need lookup');
      return; // No lookups needed
    }

    // Use the same validation endpoint that works in report page
    const reporterLookups = reporterIdsToLookup.map(reporterId => {
      console.log('📊 Dashboard: Validating reporter ID using existing endpoint:', reporterId);
      return this.reportService.validateCompanyId(reporterId).pipe(
        catchError(error => {
          console.error('📊 Dashboard: Failed to validate reporter:', reporterId, 'Error:', error);
          return of(null); // Return null if validation fails
        })
      );
    });

    // Execute all validations in parallel
    forkJoin(reporterLookups).pipe(
      catchError(error => {
        console.error('📊 Dashboard: Error in reporter validations:', error);
        return of([]); // Return empty array if all validations fail
      })
    ).subscribe(validationResults => {
      console.log('📊 Dashboard: Reporter validation results:', validationResults);
      
      // Map the results back to reports
      reporterIdsToLookup.forEach((reporterId, index) => {
        const validationResult = validationResults[index];
        console.log('📊 Dashboard: Processing validation result for', reporterId, ':', validationResult);
        
        if (validationResult && validationResult.isValid && validationResult.reporterName) {
          const reporterName = validationResult.reporterName;
          
          // Update all reports with this reporter ID
          reports.forEach(report => {
            if (report.reporterId === reporterId) {
              report.reporterName = reporterName;
              console.log('📊 Dashboard: ✅ Set real name for', reporterId, '→', reporterName);
            }
          });
        } else {
          console.warn('📊 Dashboard: ❌ No valid reporter data for', reporterId, 'Validation result:', validationResult);
        }
      });
      
      console.log('📊 Dashboard: Final reports with names:', reports);
    });
  }

  getIcon(type: string): string {
    switch (type) {
      case 'Hasard': return '📄';
      case 'Nearhit': return '🚨';
      case 'Incident-Management': return '⚠️';
      case 'Enviroment-aspect': return '🏭';
      case 'Improvement-Idea': return '🛠️';
      default: return '📝';
    }
  }

  get displayedReports() {
    const limit = this.showAllReports ? 10 : 5;
    
    if (this.isHSEUser()) {
      // For HSE users, show recent assigned reports sorted by date
      const sortedReports = this.recentReports
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
      return sortedReports.slice(0, limit);
    }
    
    return this.recentReports.slice(0, limit);
  }

  toggleShowMore() {
    this.showAllReports = !this.showAllReports;
  }

  logout() {
    this.authService.logout();
  }

  // Role checking helper methods
  isAdmin(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'admin';
  }

  isHSEUser(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'hse' || 
           this.currentUser?.role?.toLowerCase() === 'hse user';
  }

  getCurrentUserZone(): string {
    return this.currentUser?.zone || 'Unknown Zone';
  }

  // HSE User filtering helper methods
  private getAssignedReports(): any[] {
    if (!this.isHSEUser() || !this.recentReports) {
      console.log('📊 Dashboard: getAssignedReports - not HSE user or no recent reports', {
        isHSE: this.isHSEUser(),
        hasReports: !!this.recentReports,
        reportsLength: this.recentReports?.length
      });
      return [];
    }
    
    // For HSE users, this.recentReports already contains only assigned reports
    // No need to filter again since we filtered when loading
    console.log('📊 Dashboard: getAssignedReports - returning stored assigned reports:', this.recentReports.length);
    return this.recentReports;
  }

  private getAssignedReportsCount(): number {
    const count = this.getAssignedReports().length;
    console.log('📊 Dashboard: getAssignedReportsCount:', count);
    return count;
  }

  private getAssignedSafetyReportsCount(): number {
    const assignedReports = this.getAssignedReports();
    return assignedReports.filter(report => report.type !== 'Incident-Management').length;
  }

  private getAssignedIncidentReportsCount(): number {
    const assignedReports = this.getAssignedReports();
    return assignedReports.filter(report => report.type === 'Incident-Management').length;
  }

  private getAssignedReportsByStatus(status: string): number {
    const assignedReports = this.getAssignedReports();
    return assignedReports.filter(report => report.status === status).length;
  }

  // Dashboard display helper methods
  getWelcomeMessage(): string {
    if (!this.currentUser) return 'Welcome to HSE Dashboard';
    
    const firstName = this.currentUser.firstName || 'User';
    if (this.isAdmin()) {
      return `Welcome Admin ${firstName}`;
    } else if (this.isHSEUser()) {
      return `Welcome HSE ${firstName} - ${this.getCurrentUserZone()}`;
    } else {
      return `Welcome ${firstName}`;
    }
  }

  getDashboardScope(): string {
    if (this.isAdmin()) {
      return 'All Reports & Incidents';
    } else if (this.isHSEUser()) {
      return `Reports for ${this.getCurrentUserZone()}`;
    } else {
      return 'Your Reports';
    }
  }
}
