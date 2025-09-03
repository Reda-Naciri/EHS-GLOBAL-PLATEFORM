import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { CorrectiveActionsService, CreateCorrectiveActionDto, UpdateCorrectiveActionDto } from '../../services/corrective-actions.service';
import { ActionsService } from '../../services/actions.service';
import { SubActionsService } from '../../services/sub-actions.service';
import { AlertService } from '../../services/alert.service';
import { UserDto } from '../../models/auth.models';
import { ReportDetailDto } from '../../models/report.models';
import { BodyMapComponent } from '../../components/body-map/body-map.component';
import { CorrectiveActionModalComponent } from '../../components/corrective-action-modal/corrective-action-modal.component';
import { SubActionModalComponent } from '../../components/sub-action-modal/sub-action-modal.component';
import { ConfirmationDialogComponent } from '../../components/confirmation-dialog/confirmation-dialog.component';
import { AlertContainerComponent } from '../../components/alert-container/alert-container.component';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-report-details',
  standalone: true,
  imports: [CommonModule, FormsModule, BodyMapComponent, CorrectiveActionModalComponent, SubActionModalComponent, ConfirmationDialogComponent, AlertContainerComponent],
  templateUrl: './report-details.component.html',
  styleUrls: ['./report-details.component.css']
})
export class ReportDetailsComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private reportService = inject(ReportService);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private correctiveActionsService = inject(CorrectiveActionsService);
  private actionsService = inject(ActionsService);
  private subActionsService = inject(SubActionsService);
  private alertService = inject(AlertService);

  // Component state
  loading = true;
  error: string | null = null;
  isDescriptionExpanded = false;

  // Data
  reportId: string | null = null;
  report: ReportDetailDto | null = null;
  currentUser: UserDto | null = null;
  isTrackingNumber = false;
  reporterName: string | null = null;
  reporterUser: UserDto | null = null;
  hseAssignedUser: UserDto | null = null;
  allUsers: UserDto[] = [];

  // Comment form
  newComment = {
    content: '',
    isInternal: true
  };

  // Attachments carousel
  currentAttachmentIndex = 0;
  attachmentsPerView = 3; // Number of attachments visible at once

  // Corrective Actions Modal
  showCorrectiveActionModal = false;
  correctiveActionLoading = false;
  correctiveActionError: string | null = null;

  // SubActions Modal
  showSubActionModal = false;
  subActionModalMode: 'view' | 'create' = 'create';
  selectedActionForSubActions: any = null; // Can be ActionSummaryDto or CorrectiveActionSummaryDto
  existingSubActions: any[] = []; // Will hold SubActionDetailDto[]

  // Abort Action Modal
  showAbortModal = false;
  selectedActionToAbort: any = null;
  abortReason = '';
  subActionLoading = false;
  subActionError: string | null = null;
  subActionStatusOptions: string[] = [];
  
  // SubActions Dropdown
  expandedActionSubActions = new Set<number>(); // Track which actions have expanded sub-actions
  actionSubActions = new Map<number, any[]>(); // Store sub-actions for each action

  // Confirmation Dialogs
  confirmationDialog = {
    isOpen: false,
    title: '',
    message: '',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    confirmButtonClass: 'bg-red-600 hover:bg-red-700 text-white',
    cancelButtonClass: 'bg-gray-300 hover:bg-gray-400 text-gray-800',
    icon: 'warning' as 'warning' | 'danger' | 'info' | 'question',
    pendingAction: null as (() => void) | null
  };

  // Dropdown options
  priorityOptions: string[] = [];
  hierarchyOptions: string[] = [];
  statusOptions: string[] = [];

  // Subscriptions
  private subscriptions: Subscription[] = [];

  // Overdue helper methods
  isActionOverdue(action: any): boolean {
    if (!action.dueDate || action.status === 'Completed' || action.status === 'Canceled' || action.status === 'Aborted') {
      return false;
    }
    const today = new Date();
    const dueDate = new Date(action.dueDate);
    return dueDate < today;
  }

  isSubActionOverdue(subAction: any): boolean {
    if (!subAction.dueDate || subAction.status === 'Completed' || subAction.status === 'Canceled') {
      return false;
    }
    const today = new Date();
    const dueDate = new Date(subAction.dueDate);
    return dueDate < today;
  }

  hasOverdueSubActions(actionId: number): boolean {
    const subActions = this.getSubActionsForAction(actionId);
    return subActions.some(subAction => this.isSubActionOverdue(subAction));
  }

  ngOnInit() {
    console.log('📋 ReportDetails: Component initialized');
    
    // Get current user
    this.currentUser = this.authService.getCurrentUser();
    
    // Initialize dropdown options
    this.priorityOptions = this.correctiveActionsService.getPriorityOptions();
    this.statusOptions = this.correctiveActionsService.getStatusOptions();
    this.subActionStatusOptions = this.subActionsService.getStatusOptions();
    
    // Load hierarchy options from database with fallback
    this.loadHierarchyOptions();
    
    // Get report ID from route
    this.reportId = this.route.snapshot.paramMap.get('id');
    
    if (this.reportId) {
      console.log('📋 ReportDetails: Loading report with ID:', this.reportId);
      // Check if the ID looks like a tracking number (contains letters)
      this.isTrackingNumber = /[A-Za-z]/.test(this.reportId);
      this.loadReport();
    } else {
      this.error = 'Report ID not provided';
      this.loading = false;
    }
  }

  ngOnDestroy() {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  downloadAttachment(attachment: any): void {
    console.log('🔗 Downloading attachment:', attachment.fileName);
    
    const downloadSub = this.reportService.downloadAttachment(this.report!.id, attachment.id).subscribe({
      next: (blob: Blob) => {
        // Create a download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = attachment.fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        
        console.log('✅ Attachment downloaded:', attachment.fileName);
      },
      error: (error: any) => {
        console.error('❌ Error downloading attachment:', error);
        this.alertService.showError(`Failed to download attachment: ${error.error?.message || 'Unknown error'}`);
      }
    });
    
    this.subscriptions.push(downloadSub);
  }

  private loadReport() {
    if (!this.reportId) return;

    let reportObservable;
    
    if (this.isTrackingNumber) {
      console.log('📋 ReportDetails: Loading report by tracking number:', this.reportId);
      reportObservable = this.reportService.getReportByTrackingNumber(this.reportId);
    } else {
      console.log('📋 ReportDetails: Loading report by ID:', this.reportId);
      reportObservable = this.reportService.getReportById(parseInt(this.reportId));
    }

    const reportSubscription = reportObservable.subscribe({
      next: (report) => {
        console.log('📋 ReportDetails: Report loaded successfully:', report);
        this.report = report;
        this.loading = false;
        
        // Load sub-actions for all actions so status calculation works properly
        // Use refresh instead of preload to ensure we get fresh data
        this.refreshAllSubActions();
        
        // Load reporter and HSE user details after report is loaded
        console.log('🔍 Report data loaded:', report);
        console.log('🔍 Reporter ID type:', typeof report.reporterId, 'Value:', report.reporterId);
        console.log('🔍 Assigned HSE type:', typeof report.assignedHSE, 'Value:', report.assignedHSE);
        console.log('🔍 Report object keys:', Object.keys(report));
        console.log('🔍 Attachments data:', report.attachments);
        console.log('🔍 Attachments count:', report.attachments?.length || 0);
        console.log('🔍 Corrective Actions data:', report.correctiveActions);
        console.log('🔍 Corrective Actions count:', report.correctiveActions?.length || 0);
        if (report.correctiveActions?.length > 0) {
          console.log('🔍 First corrective action structure:', report.correctiveActions[0]);
          console.log('🔍 First action keys:', Object.keys(report.correctiveActions[0]));
          console.log('🔍 CreatedByHSEId values in actions:', report.correctiveActions.map(a => ({ 
            id: a.id, 
            createdByHSEId: a.createdByHSEId, 
            createdByName: a.createdByName,
            createdByType: typeof a.createdByHSEId,
            createdByExists: 'createdByHSEId' in a,
            allKeys: Object.keys(a)
          })));
        }
        console.log('🔍 Report does not have reporterName field - will look up user');
        console.log('🔍 Will search for user with reporterId:', report.reporterId);
        
        // Load all users to find reporter and HSE users
        this.loadUsersAndFindReporterAndHSE(report);
      },
      error: (error) => {
        console.error('📋 ReportDetails: Error loading report:', error);
        this.error = error.error?.message || 'Failed to load report';
        this.loading = false;
        this.alertService.showError(`Failed to load report: ${error.error?.message || 'Unknown error'}`);
      }
    });

    this.subscriptions.push(reportSubscription);
  }

  private loadReporterDetails(reporterId: string): void {
    console.log('👤 Loading reporter details for ID:', reporterId);
    
    // Try to get user by ID first
    const userSubscription = this.userService.getUserById(reporterId).subscribe({
      next: (user: UserDto) => {
        this.reporterUser = user;
        this.reporterName = user.fullName || null;
        console.log('👤 Reporter user loaded via getId:', this.reporterUser);
        console.log('👤 Reporter avatar:', user.avatar);
        console.log('👤 Avatar URL generated:', this.getAvatarUrl(user));
      },
      error: (error: any) => {
        console.log('👤 getUserById failed, trying reporter validation:', error);
        // Fallback to reporter validation
        const reporterSubscription = this.reportService.validateReporter(reporterId).subscribe({
          next: (response) => {
            if (response.isValid && response.reporterName) {
              this.reporterName = response.reporterName;
              console.log('👤 Reporter name loaded via validation:', this.reporterName);
            } else {
              this.reporterName = reporterId; // Fallback to ID
              console.log('👤 Using reporter ID as fallback:', reporterId);
            }
          },
          error: (error: any) => {
            console.error('👤 Error loading reporter name:', error);
            this.reporterName = reporterId; // Fallback to ID
          }
        });
        this.subscriptions.push(reporterSubscription);
      }
    });

    this.subscriptions.push(userSubscription);
  }

  private loadHSEUserDetails(userId: string): void {
    console.log('👤 [Fallback] Loading HSE user details individually for ID:', userId);
    console.log('👤 [Fallback] UserID type:', typeof userId, 'Value:', userId);
    
    // Try to get user by ID
    const hseSubscription = this.userService.getUserById(userId).subscribe({
      next: (user: UserDto) => {
        this.hseAssignedUser = user;
        console.log('👤 [Fallback] ✅ HSE user loaded via individual lookup:', this.hseAssignedUser);
        console.log('👤 [Fallback] ✅ HSE ID:', user.id);
        console.log('👤 [Fallback] ✅ HSE CompanyID:', user.companyId);
        console.log('👤 [Fallback] ✅ HSE avatar field:', user.avatar);
        console.log('👤 [Fallback] ✅ HSE Avatar URL generated:', this.getAvatarUrl(user));
        console.log('👤 [Fallback] ✅ Avatar URL is null?', this.getAvatarUrl(user) === null);
      },
      error: (error: any) => {
        console.error('👤 [Fallback] ❌ Error loading HSE user individually:', error);
        console.error('👤 [Fallback] ❌ HSE individual lookup failed for ID:', userId);
        console.error('👤 [Fallback] ❌ Error status:', error.status);
        console.error('👤 [Fallback] ❌ Error message:', error.message);
        console.log('👤 [Fallback] ❌ Will display using raw assignedHSE value:', userId);
      }
    });

    this.subscriptions.push(hseSubscription);
  }

  private loadUsersAndFindReporterAndHSE(report: ReportDetailDto): void {
    console.log('👥 Loading all users to find reporter and HSE users');
    
    // ReportDetailDto doesn't have reporterName field, so we need to look it up
    
    // Load all users to find the specific ones we need
    const usersSubscription = this.userService.getUsers(1, 1000).subscribe({
      next: (response) => {
        this.allUsers = response.users;
        console.log('👥 All users loaded:', this.allUsers.length, 'users');
        
        // Find reporter user
        if (report.reporterId) {
          this.reporterUser = this.findUserByIdOrCompanyId(report.reporterId);
          if (this.reporterUser) {
            console.log('👤 Found reporter user:', this.reporterUser);
            // Set reporter name from user data
            this.reporterName = this.reporterUser.fullName || null;
            console.log('👤 Reporter avatar:', this.reporterUser.avatar);
            console.log('👤 Reporter Avatar URL:', this.getAvatarUrl(this.reporterUser));
          } else {
            console.log('👤 Reporter user not found in users list for ID:', report.reporterId);
            // Fallback to reporter validation if user not found
            this.loadReporterDetails(report.reporterId);
          }
        }
        
        // Find HSE user
        if (report.assignedHSE) {
          console.log('🔍 HSE assignedHSE value:', report.assignedHSE);
          console.log('🔍 HSE assignedHSE type:', typeof report.assignedHSE);
          console.log('🔍 Available users for HSE search:', this.allUsers.map(u => ({id: u.id, companyId: u.companyId, email: u.email, fullName: u.fullName, role: u.role, avatar: u.avatar})));
          
          // Check for exact ID match first
          const exactMatch = this.allUsers.find(u => u.id === report.assignedHSE);
          console.log('🔍 Exact ID match result:', exactMatch);
          
          // Check for company ID match
          const companyIdMatch = this.allUsers.find(u => u.companyId === report.assignedHSE);
          console.log('🔍 Company ID match result:', companyIdMatch);
          
          this.hseAssignedUser = this.findUserByIdOrCompanyId(report.assignedHSE);
          if (this.hseAssignedUser) {
            console.log('👤 ✅ Found HSE user:', this.hseAssignedUser);
            console.log('👤 ✅ HSE avatar field:', this.hseAssignedUser.avatar);
            console.log('👤 ✅ HSE Avatar URL generated:', this.getAvatarUrl(this.hseAssignedUser));
            console.log('👤 ✅ HSE getAvatarUrl returns null?', this.getAvatarUrl(this.hseAssignedUser) === null);
          } else {
            console.log('👤 ❌ HSE user not found in users list for ID:', report.assignedHSE);
            console.log('👤 ❌ HSE users in list:', this.allUsers.filter(u => u.role === 'HSE').map(u => ({id: u.id, companyId: u.companyId, fullName: u.fullName})));
            console.log('👤 ❌ Trying individual lookup as fallback...');
            // Immediately load HSE user details as fallback
            this.loadHSEUserDetails(report.assignedHSE);
          }
        } else {
          console.log('🔍 No HSE assigned to this report');
        }
        
        // Call comprehensive status check
        setTimeout(() => this.checkHSEAvatarStatus(), 100);
      },
      error: (error: any) => {
        console.error('👥 Error loading users:', error);
        // Fallback to individual user loading
        if (report.reporterId) {
          this.loadReporterDetails(report.reporterId);
        }
        if (report.assignedHSE) {
          this.loadHSEUserDetails(report.assignedHSE);
        }
      }
    });
    
    this.subscriptions.push(usersSubscription);
  }

  private findUserByIdOrCompanyId(searchId: string): UserDto | null {
    if (!this.allUsers || !searchId) {
      console.log('🔍 findUserByIdOrCompanyId: Missing users or searchId', {allUsers: this.allUsers?.length, searchId});
      return null;
    }
    
    console.log('🔍 Searching for user with ID:', searchId);
    console.log('🔍 Total users to search:', this.allUsers.length);
    
    // First try to find by internal ID
    let user = this.allUsers.find(u => u.id === searchId);
    if (user) {
      console.log('👤 ✅ Found user by internal ID:', searchId, '→', user.fullName);
      return user;
    } else {
      console.log('👤 ❌ No match by internal ID for:', searchId);
    }
    
    // Then try to find by company ID
    user = this.allUsers.find(u => u.companyId === searchId);
    if (user) {
      console.log('👤 ✅ Found user by company ID:', searchId, '→', user.fullName);
      return user;
    } else {
      console.log('👤 ❌ No match by company ID for:', searchId);
    }
    
    // Try to find by email (in case the ID is an email)
    user = this.allUsers.find(u => u.email === searchId);
    if (user) {
      console.log('👤 ✅ Found user by email:', searchId, '→', user.fullName);
      return user;
    } else {
      console.log('👤 ❌ No match by email for:', searchId);
    }
    
    // Try case-insensitive email search
    user = this.allUsers.find(u => u.email?.toLowerCase() === searchId.toLowerCase());
    if (user) {
      console.log('👤 ✅ Found user by case-insensitive email:', searchId, '→', user.fullName);
      return user;
    }
    
    console.log('👤 ❌ User not found for:', searchId);
    console.log('👤 Available user IDs:', this.allUsers.map(u => u.id));
    console.log('👤 Available company IDs:', this.allUsers.map(u => u.companyId).filter(Boolean));
    console.log('👤 Available emails:', this.allUsers.map(u => u.email));
    return null;
  }

  onAddComment() {
    if (!this.newComment.content.trim() || !this.report || !this.currentUser) return;

    const commentData = {
      reportId: this.report!.id,
      content: this.newComment.content.trim(),
      author: this.currentUser!.id,
      isInternal: this.newComment.isInternal
    };

    console.log('💬 Adding comment:', commentData);

    const commentSubscription = this.reportService.addComment(commentData.reportId, commentData.content, commentData.author).subscribe({
      next: (comment) => {
        console.log('💬 Comment added successfully:', comment);
        // Reload the report to get updated comments
        this.loadReport();
        // Reset the comment form
        this.newComment.content = '';
      },
      error: (error) => {
        console.error('💬 Error adding comment:', error);
        this.error = error.error?.message || 'Failed to add comment';
      }
    });

    this.subscriptions.push(commentSubscription);
  }

  onEndReport() {
    if (!this.report || !this.currentUser) return;

    console.log('🔚 Ending report - changing status to Closed');

    const updateSubscription = this.reportService.updateReportStatus(this.report!.id, 'Closed').subscribe({
      next: () => {
        console.log('🔚 Report ended successfully');
        // Reload the report to get updated status
        this.loadReport();
      },
      error: (error: any) => {
        console.error('🔚 Error ending report:', error);
        this.error = error.error?.message || 'Failed to end report';
      }
    });

    this.subscriptions.push(updateSubscription);
  }

  onOpenReport() {
    if (!this.report || !this.currentUser) return;

    console.log('🔓 Opening report - changing status to Opened');

    const openSubscription = this.reportService.updateReportStatus(this.report!.id, 'Opened').subscribe({
      next: () => {
        console.log('🔓 Report opened successfully');
        // Reload the report to get updated status
        this.loadReport();
      },
      error: (error: any) => {
        console.error('🔓 Error opening report:', error);
        this.error = error.error?.message || 'Failed to open report';
      }
    });

    this.subscriptions.push(openSubscription);
  }

  toggleDescription() {
    this.isDescriptionExpanded = !this.isDescriptionExpanded;
  }

  goBack() {
    this.router.navigate(['/reports']);
  }

  // Corrective Actions Management
  onCreateCorrectiveAction(): void {
    console.log('🔧 Opening corrective action creation modal');
    this.correctiveActionError = null;
    this.showCorrectiveActionModal = true;
  }

  onAbortAction(action: any): void {
    this.selectedActionToAbort = action;
    this.abortReason = '';
    this.showAbortModal = true;
  }

  onCloseAbortModal(): void {
    this.showAbortModal = false;
    this.selectedActionToAbort = null;
    this.abortReason = '';
  }

  onConfirmAbort(): void {
    if (!this.selectedActionToAbort) return;
    
    if (!this.abortReason.trim()) {
      this.alertService.showError('A reason is required to abort an action');
      return;
    }

    this.executeAbortAction(this.selectedActionToAbort, this.abortReason.trim());
    this.onCloseAbortModal();
  }

  private executeAbortAction(action: any, reason: string): void {
    console.log('⚠️ Aborting action:', action.id, 'Reason:', reason);
    
    // Use the correct service based on action type
    let abortObservable;
    if ('createdByHSEId' in action) {
      // CorrectiveAction - use the new abort endpoint that tracks details
      abortObservable = this.correctiveActionsService.abortCorrectiveAction(action.id, reason);
    } else {
      // Regular Action - use the new abort endpoint that tracks details
      abortObservable = this.actionsService.abortAction(action.id, reason);
    }
    
    const abortSubscription = abortObservable.subscribe({
      next: () => {
        console.log('⚠️ Action aborted successfully with tracking details');
        this.loadReport(); // Reload to get updated data
        const actionType = 'createdByHSEId' in action ? 'Corrective action' : 'Action';
        this.alertService.showSuccess(`${actionType} "${action.title}" has been aborted`);
      },
      error: (error: any) => {
        console.error('⚠️ Error aborting action:', error);
        const actionType = 'createdByHSEId' in action ? 'corrective action' : 'action';
        this.alertService.showError(`Failed to abort ${actionType}: ${error.error?.message || 'Unknown error'}`);
      }
    });
    
    this.subscriptions.push(abortSubscription);
  }

  // Utility methods
  isAdmin(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'admin';
  }

  isHSE(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'hse' || 
           this.currentUser?.role?.toLowerCase() === 'hse user';
  }

  canOpenReport(): boolean {
    // Only assigned HSE users can open unopened reports (Admin cannot open reports)
    if (this.isHSE()) {
      // Primary matching: Check if assignedHSE matches user's full name
      const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
      const isAssignedByName = this.report?.assignedHSE === userFullName;
      
      // Fallback matching: Check ID and company ID (for backwards compatibility)
      const isAssignedById = this.report?.assignedHSE === this.currentUser?.id;
      const isAssignedByCompanyId = this.report?.assignedHSE === this.currentUser?.companyId;
      
      const isAssignedToUser = isAssignedByName || isAssignedById || isAssignedByCompanyId;
      
      console.log('🔓 canOpenReport: HSE user check');
      console.log('🔓 Current user ID:', this.currentUser?.id);
      console.log('🔓 Current user companyId:', this.currentUser?.companyId);
      console.log('🔓 Current user fullName:', userFullName);
      console.log('🔓 Report assignedHSE:', this.report?.assignedHSE);
      console.log('🔓 Assignment check:', { isAssignedByName, isAssignedById, isAssignedByCompanyId, isAssignedToUser });
      console.log('🔓 Report status:', this.report?.status);
      return isAssignedToUser && this.report?.status === 'Unopened';
    }
    
    return false;
  }

  canEndReport(): boolean {
    // Only HSE users assigned to this report can end it (not even admin)
    if (this.isHSE()) {
      // Primary matching: Check if assignedHSE matches user's full name
      const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
      const isAssignedByName = this.report?.assignedHSE === userFullName;
      
      // Fallback matching: Check ID and company ID (for backwards compatibility)
      const isAssignedById = this.report?.assignedHSE === this.currentUser?.id;
      const isAssignedByCompanyId = this.report?.assignedHSE === this.currentUser?.companyId;
      
      const isAssignedToUser = isAssignedByName || isAssignedById || isAssignedByCompanyId;
      
      console.log('🔚 canEndReport: HSE user check');
      console.log('🔚 Current user ID:', this.currentUser?.id);
      console.log('🔚 Current user companyId:', this.currentUser?.companyId);
      console.log('🔚 Current user fullName:', userFullName);
      console.log('🔚 Report assignedHSE:', this.report?.assignedHSE);
      console.log('🔚 Assignment check:', { isAssignedByName, isAssignedById, isAssignedByCompanyId, isAssignedToUser });
      console.log('🔚 Report status:', this.report?.status);
      return isAssignedToUser && this.report?.status === 'Opened';
    }
    
    // Admin and other users cannot end reports
    return false;
  }

  isReportClosed(): boolean {
    return this.report?.status === 'Closed';
  }

  canAddComments(): boolean {
    return this.isAdmin() || this.isHSE();
  }

  canViewCorrectiveActions(): boolean {
    // All users can view corrective actions section when report is opened or has existing actions
    return this.report?.status === 'Opened' || (this.report?.correctiveActions?.length || 0) > 0;
  }

  canManageCorrectiveActions(): boolean {
    // Admin can create actions on any report (respecting report status)
    if (this.isAdmin()) {
      return this.report?.status !== 'Closed';
    }
    
    // HSE users can only create actions for reports assigned to them (respecting report status)
    if (this.isHSE()) {
      // Primary matching: Check if assignedHSE matches user's full name
      const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
      const isAssignedByName = this.report?.assignedHSE === userFullName;
      
      // Fallback matching: Check ID and company ID (for backwards compatibility)
      const isAssignedById = this.report?.assignedHSE === this.currentUser?.id;
      const isAssignedByCompanyId = this.report?.assignedHSE === this.currentUser?.companyId;
      
      const isAssignedToUser = isAssignedByName || isAssignedById || isAssignedByCompanyId;
      
      console.log('🔧 canManageCorrectiveActions: HSE user check');
      console.log('🔧 Current user fullName:', userFullName);
      console.log('🔧 Report assignedHSE:', this.report?.assignedHSE);
      console.log('🔧 Assignment check:', { isAssignedByName, isAssignedById, isAssignedByCompanyId, isAssignedToUser });
      console.log('🔧 Report status:', this.report?.status);
      return isAssignedToUser && this.report?.status !== 'Closed';
    }
    
    // Other users cannot manage corrective actions
    return false;
  }

  isActionAuthor(action: any): boolean {
    if (!this.currentUser) {
      return false;
    }
    
    // Get the appropriate creator ID (CorrectiveActions use createdByHSEId, Actions use createdById)
    const creatorId = action.createdByHSEId || action.createdById;
    
    // Check if current user is the creator
    return creatorId === this.currentUser.id;
  }

  canCreateSubActionForAction(action: any): boolean {
    // Only action author can create sub-actions for their action (if still assigned to report)
    // or Admin can abort any action
    if (this.isAdmin() && this.isActionAuthor(action)) {
      return this.getCalculatedActionStatus(action) !== 'Completed' && 
             this.getCalculatedActionStatus(action) !== 'Aborted';
    }
    
    return this.isActionAuthorAndStillAssigned(action) && 
           this.getCalculatedActionStatus(action) !== 'Completed' && 
           this.getCalculatedActionStatus(action) !== 'Aborted';
  }

  isCurrentReportAssignee(): boolean {
    if (!this.currentUser || !this.report) {
      return false;
    }
    
    // Primary matching: Check if assignedHSE matches user's full name
    const userFullName = `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`.trim();
    const isAssignedByName = this.report.assignedHSE === userFullName;
    
    // Fallback matching: Check ID and company ID (for backwards compatibility)
    const isAssignedById = this.report.assignedHSE === this.currentUser.id;
    const isAssignedByCompanyId = this.report.assignedHSE === this.currentUser.companyId;
    
    return isAssignedByName || isAssignedById || isAssignedByCompanyId;
  }

  isActionAuthorAndStillAssigned(action: any): boolean {
    // Action author can only manage if they're still the current assignee
    return this.isActionAuthor(action) && this.isCurrentReportAssignee();
  }

  canManageAction(action: any): boolean {
    // Only action author can manage their action
    // Admin can only abort actions (not fully manage them unless they're the author)
    if (this.isAdmin()) {
      return this.isActionAuthor(action); // Admin can only manage their own actions
    }
    
    // HSE users can only manage their own actions if still assigned to the report
    return this.isActionAuthorAndStillAssigned(action);
  }

  canAbortAction(action: any): boolean {
    // Admin can abort any action (including their own) for traceability
    if (this.isAdmin()) {
      return true; // Admin can abort any action
    }
    
    // HSE users can abort any action in reports assigned to them (including admin actions)
    if (this.isHSE()) {
      const isStillAssigned = this.isCurrentReportAssignee();
      const isNotAborted = this.getCalculatedActionStatus(action) !== 'Aborted';
      const isNotCompleted = this.getCalculatedActionStatus(action) !== 'Completed';
      
      console.log('🚫 canAbortAction: HSE user check', {
        isStillAssigned,
        isNotAborted,
        isNotCompleted,
        currentUser: this.currentUser?.id,
        actionCreator: action.createdByHSEId || action.createdById,
        reportAssignedTo: this.report?.assignedHSE
      });
      
      return isStillAssigned && isNotAborted && isNotCompleted;
    }
    
    return false;
  }

  getCurrentManagerName(): string {
    // For now, this should be populated with the HSE name from backend
    // The frontend might need to look up user names or get them from the report data
    return this.report?.assignedHSE || 'Unassigned';
  }

  showReassignmentIndicator(action: any): boolean {
    // Show reassignment indicator if:
    // 1. User is the original action creator
    // 2. But is no longer assigned to this report
    // 3. And someone else is now assigned
    return this.isActionAuthor(action) && 
           !this.isCurrentReportAssignee() && 
           this.report?.assignedHSE !== null;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'status-badge status-completed';
      case 'In Progress':
        return 'status-badge status-in-progress';
      case 'Not Started':
        return 'status-badge status-not-started';
      default:
        return 'status-badge';
    }
  }

  getReportProgress(report: ReportDetailDto): number {
    // Calculate overall progress of all corrective actions in the report
    if (!report?.correctiveActions || report.correctiveActions.length === 0) {
      return 0;
    }

    let totalProgress = 0;
    let totalActions = report.correctiveActions.length;

    for (const action of report.correctiveActions) {
      // If action is aborted, count it as 0% progress
      if (action.status === 'Aborted') {
        totalProgress += 0;
      } else {
        // Use individual action progress percentage
        totalProgress += (action.progressPercentage || 0);
      }
    }

    return totalActions > 0 ? Math.round(totalProgress / totalActions) : 0;
  }

  getProgressBadgeClass(report: ReportDetailDto): string {
    const progress = this.getReportProgress(report);
    if (progress === 100) return 'status-completed';
    if (progress >= 50) return 'status-in-progress';
    return 'status-not-started';
  }

  getProgressBarColorClass(report: ReportDetailDto): string {
    const progress = this.getReportProgress(report);
    if (progress === 100) return 'bg-green-500';
    if (progress > 50) return 'bg-blue-400';
    if (progress > 25) return 'bg-yellow-400';
    return 'bg-red-500';
  }

  getProgress(status: 'Completed' | 'In Progress' | 'Not Started' | 'Canceled'): number {
    switch (status) {
      case 'Completed':
        return 100;
      case 'In Progress':
        return 60;
      case 'Not Started':
        return 0;
      default:
        return 0;
    }
  }

  getUserName(userId: string): string {
    // TODO: Implement proper user lookup from backend
    return 'Unknown User';
  }

  getUserAvatar(author: string): string {
    // Use the same default avatar pattern with user initials
    return `https://ui-avatars.com/api/?name=${author}&background=0ea5e9&color=fff&size=40`;
  }

  getCommentUserAvatar(comment: any): string | null {
    if (!comment?.avatar) return null;
    
    // If it's already a full URL, return as is
    if (comment.avatar.startsWith('http://') || comment.avatar.startsWith('https://')) {
      return comment.avatar;
    }
    
    // Get the base URL from environment (remove /api suffix)
    const baseUrl = environment.apiUrl.replace('/api', '');
    
    // If it's a relative URL, prepend the backend server URL
    if (comment.avatar.startsWith('/')) {
      return `${baseUrl}${comment.avatar}`;
    }
    // If it doesn't start with /, assume it's a filename and construct the full path
    return `${baseUrl}/uploads/avatars/${comment.avatar}`;
  }

  getCommentUserInitials(userName: string): string {
    if (!userName) return 'U';
    
    // If username has spaces, take first letter of each word
    if (userName.includes(' ')) {
      return userName.split(' ')
        .map(word => word.trim().charAt(0))
        .filter(char => char)
        .slice(0, 2)
        .join('')
        .toUpperCase();
    }
    
    // Otherwise, take first 2 characters
    return userName.slice(0, 2).toUpperCase();
  }

  getCurrentUserAvatar(): string {
    return this.getAvatarUrl(this.currentUser) || this.getDefaultAvatar();
  }

  getReporterName(reporterId?: string): string {
    if (!reporterId) return 'Unknown Reporter';
    
    // Return the loaded reporter name with proper fallback chain
    if (this.reporterUser) {
      return this.reporterUser.fullName || 
             `${this.reporterUser.firstName} ${this.reporterUser.lastName}`.trim() || 
             this.reporterUser.email || 
             reporterId;
    }
    
    // Return the loaded reporter name or fallback to ID
    return this.reporterName || reporterId;
  }

  getReporterAvatar(reporterId?: string): string {
    if (this.reporterUser) {
      return this.getAvatarUrl(this.reporterUser) || this.getUserInitialsAvatar(this.reporterUser);
    }
    return this.getDefaultAvatar();
  }

  getHSEAssignedAvatar(): string {
    if (this.hseAssignedUser) {
      return this.getAvatarUrl(this.hseAssignedUser) || this.getUserInitialsAvatar(this.hseAssignedUser);
    }
    return this.getDefaultAvatar();
  }

  getHSEAssignedName(assignedHSE?: string): string {
    if (!assignedHSE) return 'Not Assigned';
    
    // Return the full user name with proper fallback chain
    if (this.hseAssignedUser) {
      return this.hseAssignedUser.fullName || 
             `${this.hseAssignedUser.firstName} ${this.hseAssignedUser.lastName}`.trim() || 
             this.hseAssignedUser.email || 
             assignedHSE;
    }
    
    // Return the raw assigned HSE value as fallback
    return assignedHSE;
  }

  getHSEInitialsFromString(assignedHSE?: string): string {
    if (!assignedHSE) return 'HS';
    
    // If it looks like a name (has spaces), use first letters of words
    if (assignedHSE.includes(' ')) {
      return assignedHSE.split(' ')
        .map(word => word.trim().charAt(0))
        .filter(char => char)
        .slice(0, 2)
        .join('')
        .toUpperCase();
    }
    
    // Otherwise, take first 2 characters
    return assignedHSE.slice(0, 2).toUpperCase();
  }

  private getDefaultAvatar(): string {
    // Use the same avatar pattern as in user management
    return 'https://ui-avatars.com/api/?name=User&background=0ea5e9&color=fff&size=40';
  }

  getActionProgress(): number {
    // Calculate overall progress of all corrective actions in the report
    if (!this.report?.correctiveActions || this.report.correctiveActions.length === 0) {
      return 0;
    }

    let totalProgress = 0;
    let totalActions = this.report.correctiveActions.length;

    for (const action of this.report.correctiveActions) {
      // If action is aborted, count it as 0% progress
      if (action.status === 'Aborted') {
        totalProgress += 0;
      } else {
        // Calculate individual action progress and add to total
        totalProgress += this.getIndividualActionProgress(action);
      }
    }

    return totalActions > 0 ? Math.round(totalProgress / totalActions) : 0;
  }

  getIndividualActionProgress(action: any): number {
    // Use the individual corrective action progress calculated by the backend
    return action?.progressPercentage || 0;
  }

  getProgressBarColor(): string {
    const progress = this.getActionProgress();
    if (progress === 100) return 'bg-green-500';
    if (progress > 50) return 'bg-blue-400';
    if (progress > 25) return 'bg-yellow-400';
    return 'bg-red-500';
  }

  getAvatarUrl(user: UserDto | null): string | null {
    if (!user?.avatar) return null;
    
    // If it's already a full URL, return as is
    if (user.avatar.startsWith('http://') || user.avatar.startsWith('https://')) {
      return user.avatar;
    }
    
    // Get the base URL from environment (remove /api suffix)
    const baseUrl = environment.apiUrl.replace('/api', '');
    
    // If it's a relative URL, prepend the backend server URL
    if (user.avatar.startsWith('/')) {
      return `${baseUrl}${user.avatar}`;
    }
    // If it doesn't start with /, assume it's a filename and construct the full path
    return `${baseUrl}/uploads/avatars/${user.avatar}`;
  }

  getUserInitials(user: UserDto): string {
    // Try fullName first
    if (user.fullName) {
      const names = user.fullName.split(' ');
      if (names.length >= 2) {
        return (names[0][0] + names[names.length - 1][0]).toUpperCase();
      }
      return user.fullName[0].toUpperCase();
    }
    
    // Fallback to firstName + lastName
    if (user.firstName || user.lastName) {
      const first = user.firstName?.[0] || '';
      const last = user.lastName?.[0] || '';
      return (first + last).toUpperCase() || 'U';
    }
    
    // Final fallback to email
    if (user.email) {
      return user.email[0].toUpperCase();
    }
    
    return 'U';
  }

  getUserInitialsAvatar(user: UserDto): string {
    const initials = this.getUserInitials(user);
    return `https://ui-avatars.com/api/?name=${initials}&background=0ea5e9&color=fff&size=40`;
  }

  getCurrentUserInitials(): string {
    if (!this.currentUser) return 'U';
    return this.getUserInitials(this.currentUser);
  }

  // Debug method for template - returns empty string to not display anything
  debugHSEAvatar(): string {
    console.log('🖼️ Template debugging HSE avatar:');
    console.log('🖼️ hseAssignedUser:', this.hseAssignedUser);
    console.log('🖼️ hseAssignedUser?.avatar:', this.hseAssignedUser?.avatar);
    console.log('🖼️ getAvatarUrl(hseAssignedUser):', this.getAvatarUrl(this.hseAssignedUser));
    console.log('🖼️ Should show real avatar?', !!this.getAvatarUrl(this.hseAssignedUser));
    return ''; // Return empty string so nothing displays in template
  }

  onHSEAvatarError(event: any): void {
    console.log('🖼️ HSE Avatar image failed to load:', event);
    console.log('🖼️ Failed image src:', event.target?.src);
  }

  // Simple method to check HSE avatar status
  checkHSEAvatarStatus(): void {
    console.log('🚨 HSE Avatar Status Check:');
    console.log('🚨 report.assignedHSE:', this.report?.assignedHSE);
    console.log('🚨 hseAssignedUser exists:', !!this.hseAssignedUser);
    console.log('🚨 hseAssignedUser:', this.hseAssignedUser);
    console.log('🚨 HSE user avatar field:', this.hseAssignedUser?.avatar);
    console.log('🚨 getAvatarUrl result:', this.getAvatarUrl(this.hseAssignedUser));
    console.log('🚨 Template condition (getAvatarUrl(hseAssignedUser)):', !!this.getAvatarUrl(this.hseAssignedUser));
    
    // Also check if any users have avatars at all
    const usersWithAvatars = this.allUsers.filter(u => u.avatar);
    console.log('🚨 Total users with avatars:', usersWithAvatars.length);
    console.log('🚨 Users with avatars:', usersWithAvatars.map(u => ({
      id: u.id, 
      companyId: u.companyId, 
      fullName: u.fullName, 
      avatar: u.avatar
    })));
  }

  // Attachments carousel methods
  nextAttachment(): void {
    if (!this.report?.attachments) return;
    
    const maxIndex = Math.max(0, (this.report?.attachments?.length || 0) - this.attachmentsPerView);
    this.currentAttachmentIndex = Math.min(this.currentAttachmentIndex + 1, maxIndex);
  }

  prevAttachment(): void {
    this.currentAttachmentIndex = Math.max(this.currentAttachmentIndex - 1, 0);
  }

  canNavigateNext(): boolean {
    if (!this.report?.attachments) return false;
    return this.currentAttachmentIndex < (this.report?.attachments?.length || 0) - this.attachmentsPerView;
  }

  canNavigatePrev(): boolean {
    return this.currentAttachmentIndex > 0;
  }


  showCarousel(): boolean {
    return (this.report?.attachments?.length || 0) > this.attachmentsPerView;
  }

  getSlideIndicators(): any[] {
    if (!this.report?.attachments) return [];
    const totalSlides = Math.max(0, (this.report?.attachments?.length || 0) - this.attachmentsPerView + 1);
    return new Array(totalSlides);
  }

  goToSlide(slideIndex: number): void {
    if (!this.report?.attachments) return;
    const maxIndex = Math.max(0, (this.report?.attachments?.length || 0) - this.attachmentsPerView);
    this.currentAttachmentIndex = Math.min(slideIndex, maxIndex);
  }

  // Corrective Action Methods

  onSubmitCorrectiveAction(formData: any): void {
    // Validate form data
    const validationErrors = this.validateCorrectiveActionForm(formData);
    if (validationErrors.length > 0) {
      validationErrors.forEach(error => {
        this.alertService.showError(error);
      });
      return;
    }
    
    if (!this.report || !this.currentUser) {
      this.alertService.showError('Unable to create action: Missing report or user information');
      return;
    }

    // Show confirmation dialog
    this.showConfirmation(
      'Create Corrective Action',
      `Are you sure you want to create this corrective action: "${formData.title}"?`,
      () => this.executeCreateCorrectiveAction(formData),
      {
        confirmText: 'Create Action',
        confirmButtonClass: 'bg-green-600 hover:bg-green-700 text-white',
        icon: 'question'
      }
    );
  }

  private executeCreateCorrectiveAction(formData: any): void {
    this.correctiveActionLoading = true;
    this.correctiveActionError = null;

    const createData: CreateCorrectiveActionDto = {
      reportId: this.report!.id,
      title: formData.title.trim(),
      description: formData.description.trim(),
      priority: formData.priority.trim(),
      hierarchy: formData.hierarchy.trim(),
      dueDate: formData.dueDate ? new Date(formData.dueDate) : undefined,
      createdByHSEId: this.currentUser!.id // Use the actual user internal ID to match database
    };

    console.log('📝 Creating corrective action:', {
      createData,
      currentUser: this.currentUser,
      allUsersCount: this.allUsers?.length,
      createdByValue: createData.createdByHSEId,
      currentUserIdType: typeof this.currentUser!.id,
      currentUserIdValue: this.currentUser!.id,
      currentUserCompanyId: this.currentUser!.companyId,
      currentUserFullName: this.currentUser!.fullName,
      currentUserRole: this.currentUser!.role
    });

    const createSubscription = this.correctiveActionsService.createCorrectiveAction(createData).subscribe({
      next: (newAction) => {
        console.log('📝 Corrective action created successfully:', newAction);
        this.correctiveActionLoading = false;
        this.onCloseCorrectiveActionModal();
        
        // Show success alert immediately
        this.alertService.showSuccess(`Corrective action "${createData.title}" created successfully`);
        
        // Then reload to get updated data
        this.loadReport();
      },
      error: (error: any) => {
        console.error('📝 Error creating corrective action:', error);
        this.correctiveActionLoading = false;
        this.correctiveActionError = error.error?.message || 'Failed to create corrective action';
        this.alertService.showError(`Failed to create corrective action: ${error.error?.message || 'Unknown error'}`);
      }
    });

    this.subscriptions.push(createSubscription);
  }

  onCloseCorrectiveActionModal(): void {
    this.showCorrectiveActionModal = false;
    this.correctiveActionError = null;
  }


  getActionStatusClass(status: string): string {
    switch (status) {
      case 'Not Started': return 'bg-gray-100 text-gray-800';
      case 'In Progress': return 'bg-blue-100 text-blue-800';
      case 'Completed': return 'bg-green-100 text-green-800';
      case 'Aborted': return 'bg-red-100 text-red-800';
      case 'Canceled': return 'bg-orange-100 text-orange-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  getActionBorderClass(status: string): string {
    switch (status) {
      case 'Not Started': return 'border-l-gray-400';
      case 'In Progress': return 'border-l-blue-500';
      case 'Completed': return 'border-l-green-500';
      case 'Aborted': return 'border-l-red-500';
      case 'Canceled': return 'border-l-orange-500';
      default: return 'border-l-gray-400';
    }
  }

  getActionStatusIcon(status: string): string {
    switch (status) {
      case 'Not Started': return 'fas fa-clock';
      case 'In Progress': return 'fas fa-spinner';
      case 'Completed': return 'fas fa-check-circle';
      case 'Aborted': return 'fas fa-times-circle';
      case 'Canceled': return 'fas fa-ban';
      default: return 'fas fa-clock';
    }
  }

  /**
   * Calculate the actual status of an action based on its sub-actions
   * Following the logic: Not Started -> In Progress -> Completed
   */
  getCalculatedActionStatus(action: any): string {
    // If action is aborted, always return Aborted (overrides sub-action logic)
    if (action.status === 'Aborted') {
      return 'Aborted';
    }

    // Get sub-actions for this action
    const subActions = this.getSubActionsForAction(action.id);
    
    console.log(`🔍 Status calc for action ${action.id} (${action.title}):`, {
      subActions: subActions,
      subActionsLength: subActions?.length,
      actionSubActionsCount: action.subActionsCount,
      actionDbStatus: action.status,
      cacheHasData: this.actionSubActions.has(action.id),
      cacheSize: this.actionSubActions.size
    });
    
    // If action has sub-actions but we haven't loaded them yet, return a temporary status
    if (action.subActionsCount > 0 && (!subActions || subActions.length === 0)) {
      console.log(`⚠️ Action ${action.id} has ${action.subActionsCount} sub-actions but none loaded yet, using DB status temporarily`);
      return action.status || 'Not Started';
    }
    
    // If action truly has no sub-actions, use the database status
    if ((!subActions || subActions.length === 0) && action.subActionsCount === 0) {
      console.log(`✅ Action ${action.id} has no sub-actions, using DB status: ${action.status}`);
      return action.status || 'Not Started';
    }

    // Apply the finalized status calculation rules
    
    // Count different status types
    const notStartedCount = subActions.filter(sa => sa.status === 'Not Started').length;
    const inProgressCount = subActions.filter(sa => sa.status === 'In Progress').length;
    const completedCount = subActions.filter(sa => sa.status === 'Completed').length;
    const cancelledCount = subActions.filter(sa => sa.status === 'Canceled').length;
    
    console.log(`📊 Action ${action.id} sub-action status breakdown:`, {
      notStarted: notStartedCount,
      inProgress: inProgressCount,
      completed: completedCount,
      cancelled: cancelledCount,
      total: subActions.length
    });

    // Rule 1: If all SubActions are either NotStarted or Cancelled, the Action status is NotStarted
    if (inProgressCount === 0 && completedCount === 0) {
      return 'Not Started';
    }

    // Rule 2: If at least one SubAction is InProgress, the Action status becomes InProgress
    if (inProgressCount > 0) {
      return 'In Progress';
    }

    // Rule 3: If at least one SubAction is NotStarted or InProgress, and the rest are Completed or Cancelled, the Action remains InProgress
    if ((notStartedCount > 0 || inProgressCount > 0) && (completedCount > 0 || cancelledCount > 0)) {
      return 'In Progress';
    }

    // Rule 4: If all SubActions are either Completed or Cancelled and at least one is Completed, then the Action is Completed
    if (notStartedCount === 0 && inProgressCount === 0 && completedCount > 0) {
      return 'Completed';
    }

    // Rule 5: If all SubActions are Cancelled, the Action remains NotStarted (already covered by Rule 1)
    
    // Fallback (should not reach here with the rules above)
    return 'Not Started';
  }

  getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  getHierarchyClass(hierarchy: string): string {
    switch (hierarchy) {
      case 'Elimination': return 'text-green-700 bg-green-100';
      case 'Substitution': return 'text-blue-700 bg-blue-100';
      case 'Mesure d\'ingenierie': return 'text-purple-700 bg-purple-100';
      case 'Mesures Administratives': return 'text-orange-700 bg-orange-100';
      case 'EPI': return 'text-red-700 bg-red-100';
      default: return 'text-gray-700 bg-gray-100';
    }
  }

  getHierarchyIcon(hierarchy: string): string {
    switch (hierarchy) {
      case 'Elimination': return 'fas fa-ban';
      case 'Substitution': return 'fas fa-exchange-alt';
      case 'Mesure d\'ingenierie': return 'fas fa-cogs';
      case 'Mesures Administratives': return 'fas fa-clipboard-list';
      case 'EPI': return 'fas fa-hard-hat';
      default: return 'fas fa-shield-alt';
    }
  }

  loadHierarchyOptions(): void {
    const hierarchySubscription = this.correctiveActionsService.getHierarchyOptions().subscribe({
      next: (hierarchies) => {
        console.log('📊 Loaded hierarchy options from database:', hierarchies);
        this.hierarchyOptions = hierarchies;
      },
      error: (error) => {
        console.warn('⚠️ Failed to load hierarchies from database, using fallback:', error);
        // Use static options as fallback
        this.hierarchyOptions = this.correctiveActionsService.getStaticHierarchyOptions();
        console.log('📊 Using static hierarchy options:', this.hierarchyOptions);
      }
    });
    
    this.subscriptions.push(hierarchySubscription);
  }

  getActionCreatorName(action: any): string {
    // Get the appropriate creator ID (CorrectiveActions use createdByHSEId, Actions use createdById)
    const creatorId = action.createdByHSEId || action.createdById;

    // If we have the populated name from backend, use it (this is the preferred method)
    if (action.createdByName) {
      return action.createdByName;
    }
    
    // If no backend name but we have an ID and it's the current user, show "You"
    if (creatorId === this.currentUser?.id) {
      return 'You';
    }
    
    // Check if creatorId matches current user's company ID
    if (creatorId === this.currentUser?.['companyId']) {
      return 'You';
    }
    
    // Try to find user by either internal ID or company ID
    if (this.allUsers && this.allUsers.length > 0 && creatorId) {
      const creator = this.findUserByIdOrCompanyId(creatorId);
      if (creator) {
        return creator['userName'] || creator['fullName'] || 'Unknown User';
      }
    }
    
    // If creator ID is completely missing, show "Unknown"
    if (!creatorId || creatorId === null || creatorId === undefined) {
      return 'Unknown User';
    }
    
    // Final fallback
    return 'Unknown User';
  }

  getActionCreatorRole(action: any): string {
    // Get the appropriate creator ID (CorrectiveActions use createdByHSEId, Actions use createdById)
    const creatorId = action.createdByHSEId || action.createdById;
    
    // If the creator is the current user
    if (this.currentUser && creatorId === this.currentUser.id) {
      return this.currentUser.role || 'User';
    }
    
    // Look up the user in our loaded users list
    if (this.allUsers && this.allUsers.length > 0 && creatorId) {
      const creator = this.findUserByIdOrCompanyId(creatorId);
      if (creator) {
        return creator.role || 'User';
      }
    }
    
    // If we have HSE assigned user loaded and they're the creator
    if (this.hseAssignedUser && creatorId === this.hseAssignedUser.id) {
      return this.hseAssignedUser.role || 'HSE';
    }
    
    // If creator ID is completely missing, show "Unknown"
    if (!creatorId || creatorId === null || creatorId === undefined) {
      return 'Unknown';
    }
    
    return 'User';
  }


  // ===== SUB-ACTION MODAL METHODS =====

  onToggleSubActions(action: any): void {
    console.log('📋 Toggling sub-actions for action:', action);
    
    if (this.expandedActionSubActions.has(action.id)) {
      // Collapse - remove from expanded set
      this.expandedActionSubActions.delete(action.id);
    } else {
      // Expand - add to expanded set and load sub-actions
      this.expandedActionSubActions.add(action.id);
      this.loadSubActionsForDropdown(action);
    }
  }

  onAddSubAction(action: any): void {
    console.log('📋 Adding sub-action for action:', action);
    this.selectedActionForSubActions = action;
    this.subActionModalMode = 'create';
    this.showSubActionModal = true;
  }

  onCloseSubActionModal(): void {
    console.log('📋 Closing sub-actions modal');
    this.showSubActionModal = false;
    this.selectedActionForSubActions = null;
    this.subActionError = null;
    this.existingSubActions = [];
  }

  private loadSubActionsForDropdown(action: any): void {
    // Check if already loaded
    if (this.actionSubActions.has(action.id)) {
      return;
    }

    let getSubActionsObs;
    // Determine if this is a CorrectiveAction or regular Action
    if ('createdByHSEId' in action) {
      // CorrectiveAction
      getSubActionsObs = this.subActionsService.getCorrectiveActionSubActions(action.id);
    } else {
      // Regular Action
      getSubActionsObs = this.subActionsService.getActionSubActions(action.id);
    }

    const subscription = getSubActionsObs.subscribe({
      next: (subActions: any[]) => {
        console.log('📋 Loaded sub-actions for dropdown:', subActions);
        this.actionSubActions.set(action.id, subActions);
      },
      error: (error: any) => {
        console.error('📋 Error loading sub-actions:', error);
        // Remove from expanded set on error
        this.expandedActionSubActions.delete(action.id);
      }
    });

    this.subscriptions.push(subscription);
  }

  private loadSubActionsForAction(action: any): void {
    this.subActionLoading = true;
    this.subActionError = null;

    let getSubActionsObs;
    // Determine if this is a CorrectiveAction or regular Action
    if ('createdByHSEId' in action) {
      // CorrectiveAction
      getSubActionsObs = this.subActionsService.getCorrectiveActionSubActions(action.id);
    } else {
      // Regular Action
      getSubActionsObs = this.subActionsService.getActionSubActions(action.id);
    }

    const subscription = getSubActionsObs.subscribe({
      next: (subActions: any[]) => {
        console.log('📋 Loaded sub-actions:', subActions);
        this.existingSubActions = subActions;
        this.subActionLoading = false;
      },
      error: (error: any) => {
        console.error('📋 Error loading sub-actions:', error);
        this.subActionError = error.error?.message || 'Failed to load sub-actions';
        this.subActionLoading = false;
      }
    });

    this.subscriptions.push(subscription);
  }

  onCreateSubAction(subActionData: any): void {
    console.log('📋 Creating sub-action:', subActionData);
    
    // Validate form data
    const validationErrors = this.validateSubActionForm(subActionData);
    if (validationErrors.length > 0) {
      validationErrors.forEach(error => {
        this.alertService.showError(error);
      });
      return;
    }
    
    if (!this.selectedActionForSubActions) {
      this.alertService.showError('No action selected for sub-action creation');
      return;
    }

    // Show confirmation dialog
    this.showConfirmation(
      'Create Sub-Action',
      `Are you sure you want to create this sub-action: "${subActionData.title}"?`,
      () => this.executeCreateSubAction(subActionData),
      {
        confirmText: 'Create Sub-Action',
        confirmButtonClass: 'bg-blue-600 hover:bg-blue-700 text-white',
        icon: 'question'
      }
    );
  }

  private executeCreateSubAction(subActionData: any): void {
    this.subActionLoading = true;
    this.subActionError = null;

    const createDto = {
      title: subActionData.title,
      description: subActionData.description,
      dueDate: subActionData.dueDate || undefined,
      assignedToId: subActionData.assignedToId || undefined
    };

    let createObs;
    // Determine if this is a CorrectiveAction or regular Action
    if ('createdByHSEId' in this.selectedActionForSubActions) {
      // CorrectiveAction
      createObs = this.subActionsService.createCorrectiveActionSubAction(this.selectedActionForSubActions.id, createDto);
    } else {
      // Regular Action
      createObs = this.subActionsService.createActionSubAction(this.selectedActionForSubActions.id, createDto);
    }

    const subscription = createObs.subscribe({
      next: () => {
        console.log('📋 Sub-action created successfully');
        this.subActionLoading = false;
        this.onCloseSubActionModal();
        
        // Show success alert immediately
        this.alertService.showSuccess(`Sub-action "${subActionData.title}" created successfully`);
        
        // Reload sub-actions for the specific parent action
        if (this.selectedActionForSubActions) {
          this.actionSubActions.delete(this.selectedActionForSubActions.id);
          this.loadSubActionsForDropdown(this.selectedActionForSubActions);
        }
        
        // Then reload to get updated data (parent status, progress, etc.)
        this.loadReport();
      },
      error: (error: any) => {
        console.error('📋 Error creating sub-action:', error);
        this.subActionLoading = false;
        this.subActionError = error.error?.message || 'Failed to create sub-action';
        this.alertService.showError(`Failed to create sub-action: ${error.error?.message || 'Unknown error'}`);
      }
    });

    this.subscriptions.push(subscription);
  }

  // SubAction Management Methods
  onCancelSubAction(subAction: any): void {
    this.showConfirmation(
      'Cancel Sub-Action',
      `Are you sure you want to cancel the sub-action "${subAction.title}"? This action cannot be undone.`,
      () => this.executeCancelSubAction(subAction),
      {
        confirmText: 'Cancel Sub-Action',
        cancelText: 'Keep Sub-Action',
        confirmButtonClass: 'bg-red-600 hover:bg-red-700 text-white',
        icon: 'warning'
      }
    );
  }

  private findParentActionForSubAction(subAction: any): any {
    // Search through all actions (both regular actions and corrective actions) to find the parent
    const allActions = [
      ...(this.report?.actions || []),
      ...(this.report?.correctiveActions || [])
    ];
    
    // Find the action that has this sub-action in its expanded dropdown
    for (const action of allActions) {
      const actionSubActions = this.actionSubActions.get(action.id) || [];
      if (actionSubActions.some(sa => sa.id === subAction.id)) {
        return action;
      }
    }
    
    // If not found in cache, we can try to find by looking at which dropdowns are expanded
    for (const actionId of this.expandedActionSubActions) {
      const action = allActions.find(a => a.id === actionId);
      if (action) {
        return action; // Return the first expanded action as a fallback
      }
    }
    
    return null;
  }

  private executeCancelSubAction(subAction: any): void {
    const cancelSubscription = this.subActionsService.updateSubActionStatus(subAction.id, 'Canceled').subscribe({
      next: () => {
        console.log('📋 Sub-action cancelled successfully:', subAction.id);
        
        // Show success alert immediately
        this.alertService.showSuccess(`Sub-action "${subAction.title}" has been cancelled`);
        
        // Find the parent action for this sub-action and reload its sub-actions specifically
        const parentAction = this.findParentActionForSubAction(subAction);
        if (parentAction) {
          // Clear only this specific action's sub-actions cache
          this.actionSubActions.delete(parentAction.id);
          // Reload the sub-actions for this specific action
          this.loadSubActionsForDropdown(parentAction);
        }
        
        // Then reload the full report data to update parent statuses and progress
        this.loadReport();
      },
      error: (error: any) => {
        console.error('📋 Error cancelling sub-action:', error);
        this.alertService.showError(`Failed to cancel sub-action: ${error.error?.message || 'Unknown error'}`);
      }
    });
    
    this.subscriptions.push(cancelSubscription);
  }

  // Confirmation Dialog Methods
  showConfirmation(
    title: string, 
    message: string, 
    action: () => void, 
    options?: {
      confirmText?: string;
      cancelText?: string;
      confirmButtonClass?: string;
      icon?: 'warning' | 'danger' | 'info' | 'question';
    }
  ): void {
    this.confirmationDialog = {
      isOpen: true,
      title,
      message,
      confirmText: options?.confirmText || 'Confirm',
      cancelText: options?.cancelText || 'Cancel',
      confirmButtonClass: options?.confirmButtonClass || 'bg-red-600 hover:bg-red-700 text-white',
      cancelButtonClass: 'bg-gray-300 hover:bg-gray-400 text-gray-800',
      icon: (options?.icon || 'warning') as 'warning' | 'danger' | 'info' | 'question',
      pendingAction: action
    };
  }

  onConfirmAction(): void {
    if (this.confirmationDialog.pendingAction) {
      this.confirmationDialog.pendingAction();
    }
    this.closeConfirmation();
  }

  onCancelAction(): void {
    this.closeConfirmation();
  }

  closeConfirmation(): void {
    this.confirmationDialog.isOpen = false;
    this.confirmationDialog.pendingAction = null;
  }

  /**
   * Preload sub-actions for all actions on page load so status calculation works properly
   */
  private preloadAllSubActions(): void {
    if (!this.report) return;

    // Get all actions (both regular actions and corrective actions)
    const allActions = [
      ...(this.report.actions || []),
      ...(this.report.correctiveActions || [])
    ];

    console.log('🔄 Preloading sub-actions for all actions:', allActions.map(a => ({ id: a.id, title: a.title, subActionsCount: a.subActionsCount })));

    // Load sub-actions for each action
    allActions.forEach(action => {
      if (action.subActionsCount > 0) {
        console.log(`🔄 Loading sub-actions for action ${action.id} (${action.title}) - count: ${action.subActionsCount}`);
        this.loadSubActionsForDropdown(action);
      }
    });
  }

  /**
   * Force refresh all sub-actions by clearing cache and reloading
   */
  private refreshAllSubActions(): void {
    if (!this.report) return;

    console.log('🔄 Force refreshing all sub-actions...');
    
    // Clear all cached sub-actions
    this.actionSubActions.clear();
    
    // Reload all sub-actions
    this.preloadAllSubActions();
  }

  // SubActions Dropdown Helper Methods
  isSubActionsExpanded(actionId: number): boolean {
    return this.expandedActionSubActions.has(actionId);
  }

  getSubActionsForAction(actionId: number): any[] {
    return this.actionSubActions.get(actionId) || [];
  }

  getSubActionStatusClass(status: string): string {
    switch (status) {
      case 'Not Started': return 'bg-gray-100 text-gray-800';
      case 'In Progress': return 'bg-blue-100 text-blue-800';
      case 'Completed': return 'bg-green-100 text-green-800';
      case 'Aborted': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  getSubActionAssignedUser(assignedToId: string | null | undefined): string {
    if (!assignedToId) return 'Unassigned';
    const user = this.allUsers.find(u => u.id === assignedToId);
    return user ? `${user.fullName} (${user.email})` : 'Unknown User';
  }

  // Form Validation Methods
  validateCorrectiveActionForm(formData: any): string[] {
    const errors: string[] = [];
    
    if (!formData.title?.trim()) {
      errors.push('Action title is required');
    }
    
    if (!formData.description?.trim()) {
      errors.push('Action description is required');
    }
    
    if (!formData.priority?.trim()) {
      errors.push('Priority selection is required');
    }
    
    if (!formData.hierarchy?.trim()) {
      errors.push('Safety hierarchy selection is required');
    }
    
    return errors;
  }

  validateSubActionForm(formData: any): string[] {
    const errors: string[] = [];
    
    if (!formData.title?.trim()) {
      errors.push('Sub-action title is required');
    }
    
    if (!formData.description?.trim()) {
      errors.push('Sub-action description is required');
    }
    
    return errors;
  }

  onSubActionUpdated(): void {
    console.log('📋 Sub-action updated, refreshing report');
    // Reload the report to update action counts
    this.loadReport();
  }

  goHome(): void {
    console.log('🔧 ReportDetailsComponent: Going back to home page');
    this.router.navigate(['/']);
  }

}
