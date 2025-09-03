import { Component, OnInit, inject, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { UserService } from '../../services/user.service';
import { ReportSummaryDto, RecentReportDto } from '../../models/report.models';
import { UserDto } from '../../models/auth.models';
import { Subscription, forkJoin, of } from 'rxjs';
import { catchError, finalize, distinctUntilChanged, filter } from 'rxjs/operators';
import { AlertService } from '../../services/alert.service';

interface ExtendedReportSummary extends ReportSummaryDto {
  icon: string;
  author: string;
  expanded: boolean;
  reporterName?: string;
  originalAssignedHSEId?: string; // Store original HSE user ID for filtering
}

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reports-list.component.html',
  styleUrls: ['./reports-list.component.css']
})
export class ReportsListComponent implements OnInit, OnDestroy {
  private reportService = inject(ReportService);
  private authService = inject(AuthService);
  private dashboardService = inject(DashboardService);
  private userService = inject(UserService);
  private alertService = inject(AlertService);
  private subscriptions: Subscription[] = [];
  
  // Dashboard stats for accurate status counts
  dashboardStats: any = null;
  
  // HSE assignment functionality
  hseUsers: any[] = [];
  showingAssignmentDropdown: number | null = null;
  
  // Debug counter to track repeated loads
  private loadReportsCallCount = 0;
  reports: ExtendedReportSummary[] = [];
  allReports: ExtendedReportSummary[] = [];
  assignedReports: ExtendedReportSummary[] = [];
  currentUser: UserDto | null = null;
  loading = true;
  error: string | null = null;
  private isLoadingReports = false;
  
  // Toggle for HSE users to switch between assigned and all reports
  showAssignedOnly = true; // Default to showing assigned reports for HSE users
  
  searchQuery: string = '';
  selectedType: string | null = null;
  selectedStatus: string | null = null;
  selectedHSE: string | null = null;
  
  // HSE agent list for admin filtering
  hseAgents: string[] = [];
  
  // Available statuses in the new system
  availableStatuses = ['Unopened', 'Opened', 'Closed'];
  availableTypes: string[] = [
    'Hasard',
    'Nearhit', 
    'Enviroment-aspect',
    'Improvement-Idea',
    'Incident-Management'
  ]; // Default types, will be updated from database data

  ngOnInit(): void {
    console.log('🔧 ReportsListComponent: ngOnInit called');
    
    // Subscribe to current user to get role and zone information (only load once when user first becomes available)
    const userSubscription = this.authService.currentUser$.pipe(
      distinctUntilChanged((prev, curr) => {
        // Only emit when user actually changes (by ID)
        return prev?.id === curr?.id;
      }),
      filter(user => user !== null) // Only proceed when we have a valid user
    ).subscribe(user => {
      console.log('🔧 ReportsListComponent: User changed, loading reports for:', user?.email);
      
      if (user && (!this.currentUser || this.currentUser.id !== user.id)) {
        console.log('🔧 ReportsListComponent: New user detected, loading reports');
        this.currentUser = user;
        this.loadReports();
      }
    });
    this.subscriptions.push(userSubscription);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  private loadReports(): void {
    this.loadReportsCallCount++;
    console.log(`🚀 LOADING REPORTS (Call #${this.loadReportsCallCount}) for user: ${this.currentUser?.email}`);
    
    if (!this.currentUser) {
      console.log('❌ No current user, aborting');
      return;
    }

    if (this.isLoadingReports) {
      console.log('⏭️ Already loading reports, skipping');
      return;
    }

    this.isLoadingReports = true;
    this.loading = true;
    this.error = null;

    console.log('🔧 ReportsListComponent: Loading data for user:', this.currentUser);
    console.log('🔧 ReportsListComponent: User role:', this.currentUser.role);
    console.log('🔧 ReportsListComponent: User zone:', this.currentUser.zone);

    // Load reports based on user role and zone (same pattern as dashboard)
    this.loadRecentReports();
    
    // Also load dashboard stats for accurate status counts
    this.loadDashboardStats();
    
    // Load HSE users for admin assignment functionality
    if (this.isAdmin()) {
      console.log('🔧 User is admin, loading HSE users...');
      this.loadHSEUsers();
    } else {
      console.log('🔧 User is not admin, skipping HSE users load');
    }
  }

  private loadDashboardStats(): void {
    const userZone = this.isHSEUser() ? this.currentUser?.zone : undefined;
    
    this.dashboardService.getDashboardStats(userZone).subscribe({
      next: (stats) => {
        this.dashboardStats = stats;
        console.log('📊 ReportsListComponent: Dashboard stats loaded for status counts:', stats);
      },
      error: (error) => {
        console.error('📊 ReportsListComponent: Failed to load dashboard stats:', error);
      }
    });
  }

  private loadRecentReports(): void {
    const limit = 1000; // Higher limit for reports list to show all reports
    
    // Always load all reports, then separate them based on assignment
    this.loadAllReports(limit);
  }

  private loadReportsByZone(zone: string, limit: number): void {
    console.log('🔧 ReportsListComponent: Loading reports for zone:', zone);
    
    this.reportService.getReports(undefined, undefined, zone).subscribe({
      next: (reports) => {
        // Additional filter by zone if needed (server should handle this)
        const filteredReports = reports.filter(report => 
          report.zone === zone || report.zone === this.currentUser?.zone
        );
        
        this.processReports(filteredReports);
        console.log('🔧 ReportsListComponent: Filtered reports for HSE user:', this.allReports);
        this.loading = false;
      },
      error: (error) => {
        console.error('🔧 ReportsListComponent: Failed to load reports:', error);
        this.handleReportsError();
      }
    });
  }

  private loadAllReports(limit: number): void {
    console.log('🔧 ReportsListComponent: Loading all reports');
    
    this.reportService.getReports().pipe(
      finalize(() => {
        // Always reset loading flags regardless of success or error
        this.loading = false;
        this.isLoadingReports = false;
      })
    ).subscribe({
      next: (reports) => {
        console.log('🔧 ReportsListComponent: Reports loaded successfully, count:', reports.length);
        this.processReports(reports);
      },
      error: (error) => {
        console.error('🔧 ReportsListComponent: Failed to load reports:', error);
        this.handleReportsError();
      }
    });
  }

  private processReports(reports: ReportSummaryDto[]): void {
    console.log('✅ ReportsListComponent: Processing reports:', reports.length);
    console.log('🔍 ReportsListComponent: Raw reports data:', reports);
    
    // Debug: Check status distribution
    const statusCounts = reports.reduce((acc, report) => {
      acc[report.status] = (acc[report.status] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
    console.log('🔍 ReportsListComponent: Status distribution:', statusCounts);
    
    // Filter out Incident-Management reports
    const filteredReports = reports.filter(report => report.type !== 'Incident-Management');
    console.log('🔧 ReportsListComponent: Filtered out incidents, remaining:', filteredReports.length);
    
    // Convert ReportSummaryDto to ExtendedReportSummary format
    this.allReports = filteredReports.map(report => ({
      ...report,
      icon: this.getIcon(report.type),
      author: report.reporterId, // Use reporterId as author
      expanded: false,
      reporterName: undefined, // Will be loaded separately
      originalAssignedHSEId: report.assignedHSE // Store original HSE ID before name replacement
    }));
    
    // Process reports for HSE users with correct filtering logic
    if (this.isHSEUser()) {
      console.log('🔧 ReportsListComponent: Processing reports for HSE user');
      console.log('🔧 ReportsListComponent: Current user ID:', this.currentUser?.id);
      console.log('🔧 ReportsListComponent: Current user zone:', this.currentUser?.zone);
      
      // ASSIGNED REPORTS: All reports assigned to this HSE user (any status)
      console.log('🔧 DEBUG: All reports with HSE assignments:');
      this.allReports.forEach(report => {
        console.log(`🔧   Report ${report.id} (${report.title}): originalAssignedHSEId="${report.originalAssignedHSEId}" (type: ${typeof report.originalAssignedHSEId})`);
      });
      console.log(`🔧 DEBUG: Current user ID: "${this.currentUser?.id}" (type: ${typeof this.currentUser?.id})`);
      console.log(`🔧 DEBUG: Current user details:`, this.currentUser);
      
      this.assignedReports = this.allReports.filter(report => {
        // Primary matching: Check if assignedHSE matches user's full name
        const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
        const isAssignedByName = report.originalAssignedHSEId === userFullName;
        
        // Fallback matching: Check ID and company ID (for backwards compatibility)
        const isAssignedById = report.originalAssignedHSEId === this.currentUser?.id;
        const isAssignedByCompanyId = report.originalAssignedHSEId === this.currentUser?.companyId;
        
        const isAssigned = isAssignedByName || isAssignedById || isAssignedByCompanyId;
        
        console.log(`🔧 Assigned Check - Report ${report.id} (${report.title}):`, {
          originalAssignedHSEId: report.originalAssignedHSEId,
          userFullName: userFullName,
          userID: this.currentUser?.id,
          companyID: this.currentUser?.companyId,
          isAssignedByName,
          isAssignedById,
          isAssignedByCompanyId,
          finalMatch: isAssigned
        });
        
        return isAssigned;
      });
      
      console.log('🔧 ReportsListComponent: Assigned reports count:', this.assignedReports.length);
      console.log('🔧 ReportsListComponent: Assigned reports:', this.assignedReports.map(r => `${r.id}:${r.title}:${r.status}`));
      
      // Set initial reports based on toggle state
      this.reports = this.getFilteredReports();
      console.log('🔧 ReportsListComponent: Final reports array length:', this.reports.length);
    } else {
      // Admin sees all reports
      this.reports = this.getFilteredReports();
    }
    
    this.extractReportTypes();
    // Note: extractHSEAgents() will be called after HSE user names are loaded
    
    // Debug: Check HSE assignments
    console.log('🔍 ReportsListComponent: HSE assignments:', this.allReports.map(r => ({ id: r.id, title: r.title, assignedHSE: r.assignedHSE })));
    
    // Load reporter names from database (same as dashboard)
    this.loadReporterNamesFromDatabase(filteredReports);
    
    // Load HSE user names from database
    this.loadHSEUserNamesFromDatabase(filteredReports);
  }

  private handleReportsError(): void {
    this.error = 'Failed to load reports';
    this.loading = false;
    this.isLoadingReports = false;
    // Initialize with empty arrays to show default indicators
    this.allReports = [];
    this.reports = [];
    this.assignedReports = [];
    this.extractReportTypes(); // This will show default types
  }

  private loadReporterNamesFromDatabase(reports: ReportSummaryDto[]): void {
    // Get unique reporter IDs - since ReportSummaryDto only has reporterId, we'll look up all of them
    const reporterIdsToLookup = reports
      .filter(report => report.reporterId)
      .map(report => report.reporterId)
      .filter((id, index, self) => self.indexOf(id) === index); // Remove duplicates

    if (reporterIdsToLookup.length === 0) {
      console.log('🔧 ReportsListComponent: No reporter IDs need lookup');
      return; // No lookups needed
    }

    console.log('🔧 ReportsListComponent: Looking up names for', reporterIdsToLookup.length, 'unique reporters');

    // Use the same validation endpoint that works in report page
    const reporterLookups = reporterIdsToLookup.map(reporterId => {
      return this.reportService.validateCompanyId(reporterId).pipe(
        catchError(error => {
          console.error('🔧 ReportsListComponent: Failed to validate reporter:', reporterId);
          return of(null); // Return null if validation fails
        })
      );
    });

    // Execute all validations in parallel
    forkJoin(reporterLookups).pipe(
      catchError(error => {
        console.error('🔧 ReportsListComponent: Error in reporter validations:', error);
        return of([]); // Return empty array if all validations fail
      })
    ).subscribe(validationResults => {
      console.log('🔧 ReportsListComponent: Reporter validation results:', validationResults);
      
      // Map the results back to reports and update allReports
      let updatedCount = 0;
      reporterIdsToLookup.forEach((reporterId, index) => {
        const validationResult = validationResults[index];
        
        if (validationResult && validationResult.isValid && validationResult.reporterName) {
          const reporterName = validationResult.reporterName;
          
          // Update all reports with this reporter ID
          this.allReports.forEach(report => {
            if (report.reporterId === reporterId) {
              report.author = reporterName;
              report.reporterName = reporterName;
              updatedCount++;
            }
          });
        }
      });
      
      console.log('🔧 ReportsListComponent: Updated', updatedCount, 'reports with reporter names');
      
      // Update filtered reports once after all reporter names are loaded
      this.reports = this.getFilteredReports();
      console.log('🔧 ReportsListComponent: Final reports with names:', this.allReports);
    });
  }

  private loadHSEUserNamesFromDatabase(reports: ReportSummaryDto[]): void {
    // Get unique HSE user IDs that need lookup
    const hseUserIdsToLookup = reports
      .filter(report => report.assignedHSE && report.assignedHSE.trim() !== '')
      .map(report => report.assignedHSE!)
      .filter((id, index, self) => self.indexOf(id) === index); // Remove duplicates

    if (hseUserIdsToLookup.length === 0) {
      console.log('🔧 ReportsListComponent: No HSE user IDs need lookup');
      // Still extract HSE agents for any existing assignments
      this.extractHSEAgents();
      return;
    }

    console.log('🔧 ReportsListComponent: Looking up names for', hseUserIdsToLookup.length, 'unique HSE users');

    // Look up each HSE user by ID
    const hseUserLookups = hseUserIdsToLookup.map(userId => {
      return this.userService.getUserById(userId).pipe(
        catchError(error => {
          console.error('🔧 ReportsListComponent: Failed to lookup HSE user:', userId);
          return of(null); // Return null if lookup fails
        })
      );
    });

    // Execute all lookups in parallel
    forkJoin(hseUserLookups).pipe(
      catchError(error => {
        console.error('🔧 ReportsListComponent: Error in HSE user lookups:', error);
        return of([]);
      })
    ).subscribe(hseUserResults => {
      console.log('🔧 ReportsListComponent: HSE user lookup results:', hseUserResults);
      
      hseUserResults.forEach((userResult, index) => {
        const userId = hseUserIdsToLookup[index];
        if (userResult && userResult.id) {
          const userName = userResult.fullName || `${userResult.firstName} ${userResult.lastName}`.trim() || userResult.email || userId;
          
          // Update all reports with this HSE user ID
          this.allReports.forEach(report => {
            if (report.originalAssignedHSEId === userId) {
              report.assignedHSE = userName;
              console.log('🔧 ReportsListComponent: ✅ Set HSE name for', userId, '→', userName);
            }
          });
          
          // Also update assigned reports if user is HSE
          if (this.isHSEUser()) {
            this.assignedReports.forEach(report => {
              if (report.originalAssignedHSEId === userId) {
                report.assignedHSE = userName;
              }
            });
          }
        } else {
          console.warn('🔧 ReportsListComponent: ❌ No valid HSE user data for', userId);
        }
      });
      
      // For HSE users, also re-filter the assigned reports after name loading
      if (this.isHSEUser()) {
        // Re-filter assigned reports to make sure they still match after name updates
        this.assignedReports = this.allReports.filter(report => {
          // Use same assignment logic as above
          const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
          const isAssignedByName = report.originalAssignedHSEId === userFullName;
          const isAssignedById = report.originalAssignedHSEId === this.currentUser?.id;
          const isAssignedByCompanyId = report.originalAssignedHSEId === this.currentUser?.companyId;
          return isAssignedByName || isAssignedById || isAssignedByCompanyId;
        });
        console.log('🔧 ReportsListComponent: Re-filtered assigned reports after HSE name loading:', this.assignedReports.length);
      }
      
      // Single update after all HSE user name processing is complete
      this.reports = this.getFilteredReports();
      
      // Now extract HSE agents with their proper names
      this.extractHSEAgents();
      
      console.log('🔧 ReportsListComponent: Final reports with HSE names:', this.allReports);
    });
  }

  private extractReportTypes(): void {
    const typeSet = new Set<string>(this.availableTypes); // Start with default types
    this.allReports.forEach(report => {
      if (report.type) {
        typeSet.add(report.type);
      }
    });
    this.availableTypes = Array.from(typeSet).sort();
    console.log('🔧 ReportsListComponent: Report types (defaults + database):', this.availableTypes);
  }

  private extractHSEAgents(): void {
    const hseSet = new Set<string>();
    this.allReports.forEach(report => {
      if (report.assignedHSE) {
        hseSet.add(report.assignedHSE);
      }
    });
    this.hseAgents = Array.from(hseSet).sort();
    console.log('🔧 ReportsListComponent: HSE agents extracted:', this.hseAgents);
  }

  private loadReporterNames(): void {
    const reporterIds = [...new Set(this.allReports.map(r => r.reporterId))];
    console.log('🔧 ReportsListComponent: Loading reporter names for IDs:', reporterIds);
    
    // Create an array of validation requests
    const validationRequests = reporterIds.map(reporterId => {
      console.log('🔍 ReportsListComponent: Attempting to validate reporter ID:', reporterId);
      console.log('🔍 ReportsListComponent: Reporter ID type:', typeof reporterId, 'Length:', reporterId.length);
      
      return this.reportService.validateCompanyId(reporterId).pipe(
        catchError((error) => {
          console.error(`❌ Error validating reporter ${reporterId}:`, error);
          console.log('🔍 ReportsListComponent: Validation failed for:', reporterId, 'Error details:', error);
          return of({ isValid: false, reporterName: reporterId }); // Fallback to ID
        })
      );
    });
    
    if (validationRequests.length > 0) {
      const namesSub = forkJoin(validationRequests).subscribe({
        next: (results) => {
          console.log('✅ ReportsListComponent: Validation results:', results);
          
          // Create a map of reporter ID to name
          const reporterNameMap = new Map<string, string>();
          reporterIds.forEach((reporterId, index) => {
            const result = results[index];
            const name = result.isValid && result.reporterName ? result.reporterName : reporterId;
            reporterNameMap.set(reporterId, name);
          });
          
          // Update all reports with reporter names
          this.allReports = this.allReports.map(report => ({
            ...report,
            author: reporterNameMap.get(report.reporterId) || report.reporterId,
            reporterName: reporterNameMap.get(report.reporterId)
          }));
          
          // Update filtered reports respecting current toggle
          this.reports = this.getFilteredReports();
          console.log('✅ ReportsListComponent: Reporter names updated');
        },
        error: (error) => {
          console.error('❌ ReportsListComponent: Error loading reporter names:', error);
          // Keep using reporter IDs as fallback
        }
      });
      
      this.subscriptions.push(namesSub);
    }
  }

  toggle(report: ExtendedReportSummary): void {
    report.expanded = !report.expanded;
  }

  toggleTypeFilter(type: string): void {
    this.selectedType = this.selectedType === type ? null : type;
    this.reports = this.getFilteredReports();
  }

  toggleStatusFilter(status: string): void {
    this.selectedStatus = this.selectedStatus === status ? null : status;
    this.reports = this.getFilteredReports();
  }

  toggleHSEFilter(hse: string): void {
    this.selectedHSE = this.selectedHSE === hse ? null : hse;
    this.reports = this.getFilteredReports();
  }

  onSearchChange(): void {
    this.reports = this.getFilteredReports();
  }

  clearFilters(): void {
    this.selectedType = null;
    this.selectedStatus = null;
    this.selectedHSE = null;
    this.searchQuery = '';
    this.reports = this.getFilteredReports();
  }

  private getFilteredReports(): ExtendedReportSummary[] {
    // Use the correct base reports array based on HSE toggle
    const baseReports = this.getBaseReportsForFiltering();
    // console.log('🔧 getFilteredReports: Base reports count:', baseReports.length);
    
    const filteredReports = baseReports.filter(report => {
      // For HSE users, base reports are already properly filtered, so less restrictive filtering needed
      // For admin users, exclude aborted or canceled reports  
      const isNotAbortedOrCanceled = this.isAdmin() ? 
        (report.status !== 'Aborted' && report.status !== 'Canceled') : 
        true; // HSE base reports are already filtered appropriately
      
      // Search filter
      const matchesSearch = !this.searchQuery || 
        report.title.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.zone.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.type.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.status.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        (report.author && report.author.toLowerCase().includes(this.searchQuery.toLowerCase())) ||
        (report.assignedHSE && report.assignedHSE.toLowerCase().includes(this.searchQuery.toLowerCase())) ||
        (!report.assignedHSE && 'unassigned'.includes(this.searchQuery.toLowerCase()));

      // Type filter
      const matchesType = !this.selectedType || report.type === this.selectedType;
      
      // Status filter - use same mapping logic as getStatusCount()
      const matchesStatus = !this.selectedStatus || (() => {
        let reportStatus = report.status;
        if (report.status === 'Open') reportStatus = 'Opened'; // Map "Open" to "Opened"
        if (report.status === 'In Progress') reportStatus = 'Opened';
        return reportStatus === this.selectedStatus;
      })();
      
      // HSE filter (only for admins)
      const matchesHSE = !this.selectedHSE || report.assignedHSE === this.selectedHSE;
      
      // Role-based filtering
      const userCanSeeReport = this.canUserSeeReport(report);

      return isNotAbortedOrCanceled && matchesSearch && matchesType && matchesStatus && matchesHSE && userCanSeeReport;
    });
    
    console.log('🔧 getFilteredReports: Filtered reports count:', filteredReports.length);
    console.log('🔧 getFilteredReports: Final report IDs:', filteredReports.map(r => `${r.id}:${r.title}`));
    
    return filteredReports;
  }

  // Helper method to get the correct base reports array for filtering
  private getBaseReportsForFiltering(): ExtendedReportSummary[] {
    if (this.isHSEUser()) {
      if (this.showAssignedOnly) {
        // ASSIGNED REPORTS: All reports assigned to this HSE user (any status)
        console.log('🔧 getBaseReportsForFiltering (HSE): Returning ASSIGNED reports');
        console.log('🔧 getBaseReportsForFiltering (HSE): assigned reports =', this.assignedReports.length);
        console.log('🔧 getBaseReportsForFiltering (HSE): assigned report IDs:', this.assignedReports.map(r => `${r.id}:${r.title}:${r.status}`));
        return this.assignedReports;
      } else {
        // TEAM REPORTS: All reports with status "Opened" (excluding assigned reports)
        const teamReports = this.allReports.filter(report => {
          const isOpened = report.status === 'Opened';
          
          // Check if report is NOT assigned to current user using same logic as assignment check
          const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
          const isAssignedByName = report.originalAssignedHSEId === userFullName;
          const isAssignedById = report.originalAssignedHSEId === this.currentUser?.id;
          const isAssignedByCompanyId = report.originalAssignedHSEId === this.currentUser?.companyId;
          const isAssignedToMe = isAssignedByName || isAssignedById || isAssignedByCompanyId;
          
          const isNotAssignedToMe = !isAssignedToMe;
          const isTeamReport = isOpened && isNotAssignedToMe;
          
          if (isOpened) {
            console.log(`🔧 Team Report Check - Report ${report.id} (${report.title}): status=${report.status}, assignedTo=${report.originalAssignedHSEId}, currentUser=${this.currentUser?.id}, userFullName=${userFullName}, isAssignedToMe=${isAssignedToMe}, isTeamReport=${isTeamReport}`);
          }
          
          return isTeamReport;
        });
        
        console.log('🔧 getBaseReportsForFiltering (HSE): Returning TEAM reports');
        console.log('🔧 getBaseReportsForFiltering (HSE): team reports =', teamReports.length);
        console.log('🔧 getBaseReportsForFiltering (HSE): team report IDs:', teamReports.map(r => `${r.id}:${r.title}:${r.status}`));
        return teamReports;
      }
    } else {
      // For admin users, always use all reports
      console.log('🔧 getBaseReportsForFiltering (Admin): returning all reports =', this.allReports.length);
      return this.allReports;
    }
  }

  private canUserSeeReport(report: ReportSummaryDto): boolean {
    if (!this.currentUser) {
      return true; // Allow access if user not loaded yet
    }
    
    // Admin can see all reports
    if (this.currentUser.role === 'Admin') {
      return true;
    }
    
    // HSE users: for the filtering system, we need to allow them to see reports
    // The actual filtering by assignment is handled in the base array selection
    if (this.currentUser.role === 'HSE') {
      return true; // Allow all reports to be seen by HSE for filtering purposes
    }
    
    // Regular users can see all reports (but not manage them)
    return true;
  }

  get filteredReports(): ExtendedReportSummary[] {
    return this.reports;
  }

  getIcon(type: string): string {
    switch (type) {
      case 'Hasard': return '📄';
      case 'Nearhit': return '🚨';
      case 'Incident-Management': return '⚠️';
      case 'Enviroment-aspect': return '🏭';
      case 'Improvement-Idea': return '🛠️';
      // Handle any other types from database
      case 'Safety': return '🛡️';
      case 'Equipment': return '⚙️';
      case 'Training': return '📚';
      case 'Maintenance': return '🔧';
      case 'Quality': return '✅';
      case 'Process': return '🔄';
      default: return '📝'; // Default icon for unknown types
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Unopened': return 'not-started';
      case 'Opened': return 'in-progress';
      case 'Closed': return 'completed';
      default: return 'unknown';
    }
  }

  getTypeCount(type: string): number {
    // Use current base reports array based on HSE toggle
    const baseReports = this.getBaseReportsForCounting();
    return baseReports.filter(report => 
      report.type === type && 
      this.canUserSeeReport(report) && 
      report.status !== 'Aborted' && 
      report.status !== 'Canceled'
    ).length;
  }

  getTypePercentage(type: string): number {
    const baseReports = this.getBaseReportsForCounting();
    const total = baseReports.filter(report => 
      this.canUserSeeReport(report) && 
      report.status !== 'Aborted' && 
      report.status !== 'Canceled'
    ).length;
    const count = this.getTypeCount(type);
    return total > 0 ? (count / total) * 100 : 0;
  }

  getStatusCount(status: string): number {
    // Use current base reports array based on HSE toggle
    const baseReports = this.getBaseReportsForCounting();
    return baseReports.filter(report => {
      if (!this.canUserSeeReport(report)) return false;
      
      // Exclude aborted or canceled reports
      if (report.status === 'Aborted' || report.status === 'Canceled') return false;
      
      // Map old status names to new ones for counting
      let reportStatus = report.status;
      if (report.status === 'Open') reportStatus = 'Opened'; // Map "Open" to "Opened"
      if (report.status === 'In Progress') reportStatus = 'Opened';
      
      return reportStatus === status;
    }).length;
  }

  // Helper method to get the correct base reports array for counting
  private getBaseReportsForCounting(): ExtendedReportSummary[] {
    if (this.isHSEUser()) {
      // For HSE users, use the same logic as filtering
      if (this.showAssignedOnly) {
        // Count from assigned reports
        return this.assignedReports;
      } else {
        // Count from team reports (opened reports not assigned to current user)
        return this.allReports.filter(report => {
          const isOpened = report.status === 'Opened';
          
          // Check if report is NOT assigned to current user using same logic as assignment check
          const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
          const isAssignedByName = report.originalAssignedHSEId === userFullName;
          const isAssignedById = report.originalAssignedHSEId === this.currentUser?.id;
          const isAssignedByCompanyId = report.originalAssignedHSEId === this.currentUser?.companyId;
          const isAssignedToMe = isAssignedByName || isAssignedById || isAssignedByCompanyId;
          
          const isNotAssignedToMe = !isAssignedToMe;
          return isOpened && isNotAssignedToMe;
        });
      }
    } else {
      // For admin users, always use all reports
      return this.allReports;
    }
  }

  getHSEReportCount(hse: string): number {
    // HSE report count always uses all reports regardless of toggle
    return this.allReports.filter(report => 
      report.assignedHSE === hse && 
      this.canUserSeeReport(report) && 
      report.status !== 'Aborted' && 
      report.status !== 'Canceled'
    ).length;
  }
  
  // Toggle between assigned and all reports for HSE users
  toggleReportsView(): void {
    if (!this.isHSEUser()) return;
    
    console.log('🔧 toggleReportsView: Before toggle - showAssignedOnly:', this.showAssignedOnly);
    console.log('🔧 toggleReportsView: assignedReports length:', this.assignedReports.length);
    console.log('🔧 toggleReportsView: allReports length:', this.allReports.length);
    
    this.showAssignedOnly = !this.showAssignedOnly;
    
    console.log('🔧 toggleReportsView: After toggle - showAssignedOnly:', this.showAssignedOnly);
    
    // Refresh the reports list based on new toggle state
    this.reports = this.getFilteredReports();
    
    console.log('🔧 ReportsListComponent: Toggled view to:', this.showAssignedOnly ? 'My Assigned Reports' : 'Team Opened Reports');
    console.log('🔧 ReportsListComponent: Now showing', this.reports.length, 'reports');
  }

  // Helper method to filter reports from a specific base array
  private getFilteredReportsFromBase(baseReports: ExtendedReportSummary[]): ExtendedReportSummary[] {
    return baseReports.filter(report => {
      // Exclude aborted or canceled reports from the corrective actions section display
      const isNotAbortedOrCanceled = report.status !== 'Aborted' && report.status !== 'Canceled';
      
      // Search filter
      const matchesSearch = !this.searchQuery || 
        report.title.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.zone.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.type.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.status.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        (report.reporterName && report.reporterName.toLowerCase().includes(this.searchQuery.toLowerCase())) ||
        report.reporterId.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        (report.assignedHSE && report.assignedHSE.toLowerCase().includes(this.searchQuery.toLowerCase()));

      // Type filter
      const matchesType = !this.selectedType || report.type === this.selectedType;
      
      // Status filter
      const matchesStatus = !this.selectedStatus || (() => {
        let reportStatus = report.status;
        if (report.status === 'Open') reportStatus = 'Opened';
        return reportStatus === this.selectedStatus;
      })();
      
      // HSE filter (only for admins)
      const matchesHSE = !this.selectedHSE || report.assignedHSE === this.selectedHSE;

      return isNotAbortedOrCanceled && matchesSearch && matchesType && matchesStatus && matchesHSE;
    });
  }

  // Helper methods for UI  
  isAdmin(): boolean {
    return this.currentUser?.role === 'Admin';
  }

  isHSEUser(): boolean {
    return this.currentUser?.role === 'HSE';
  }

  refreshReports(): void {
    this.loadReports();
  }

  getTypeClass(type: string): string {
    switch (type) {
      case 'Hasard': return 'type-hazard';
      case 'Nearhit': return 'type-nearhit';
      case 'Enviroment-aspect': return 'type-environment';
      case 'Improvement-Idea': return 'type-improvement';
      default: return '';
    }
  }

  // ===== HSE ASSIGNMENT METHODS (ADMIN ONLY) =====
  private loadHSEUsers(): void {
    console.log('🔧 Loading HSE users for admin assignment...');
    this.reportService.getHSEUsers().subscribe({
      next: (users) => {
        this.hseUsers = users;
        console.log('🔧 HSE users loaded for assignment:', users);
      },
      error: (error) => {
        console.error('❌ Error loading HSE users:', error);
        console.error('❌ HSE users error details:', JSON.stringify(error, null, 2));
      }
    });
  }

  toggleAssignmentDropdown(reportId: number): void {
    this.showingAssignmentDropdown = this.showingAssignmentDropdown === reportId ? null : reportId;
  }

  assignHSEAgent(reportId: number, hseUserId: string | null): void {
    if (!this.isAdmin()) {
      console.error('❌ User is not admin, cannot assign HSE agent');
      alert('You do not have permission to assign HSE agents');
      return;
    }

    // Clear any existing alert messages
    this.alertService.clearAll();

    console.log('🔧 Assigning HSE agent:', { reportId, hseUserId, currentUser: this.currentUser });
    this.reportService.updateAssignedHSE(reportId, hseUserId).subscribe({
      next: (response) => {
        console.log('✅ HSE agent assignment updated:', response);
        
        // Update the local report data
        const report = this.allReports.find(r => r.id === reportId);
        const hseUser = hseUserId ? this.hseUsers.find(u => u.id === hseUserId) : null;
        const hseUserName = hseUser ? hseUser.name : null;
        
        if (report) {
          report.assignedHSE = hseUserName;
        }
        
        // Refresh the filtered reports
        this.reports = this.getFilteredReports();
        
        // Close the dropdown
        this.showingAssignmentDropdown = null;
        
        // Show success confirmation with AlertService after a delay to ensure component updates are complete
        setTimeout(() => {
          if (hseUserName) {
            this.alertService.showSuccess(`✅ Report successfully assigned to ${hseUserName}`, {
              autoHide: true,
              autoHideDuration: 4000,
              position: 'top-right'
            });
          } else {
            this.alertService.showSuccess('✅ Report assignment removed successfully', {
              autoHide: true,
              autoHideDuration: 4000,
              position: 'top-right'
            });
          }
        }, 300); // Small delay to ensure DOM updates are complete
      },
      error: (error) => {
        console.error('❌ Error updating HSE assignment:', error);
        console.error('❌ Error details:', JSON.stringify(error, null, 2));
        
        // Show error with AlertService
        if (error.error && error.error.message) {
          this.alertService.showError(`Error updating HSE assignment: ${error.error.message}`);
        } else {
          this.alertService.showError('Error updating HSE assignment. Please try again.');
        }
      }
    });
  }

  getAssignedHSEName(report: ExtendedReportSummary): string {
    return report.assignedHSE || 'Unassigned';
  }


  // Close assignment dropdown when clicking outside

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.relative.inline-block')) {
      this.showingAssignmentDropdown = null;
    }
  }

}
