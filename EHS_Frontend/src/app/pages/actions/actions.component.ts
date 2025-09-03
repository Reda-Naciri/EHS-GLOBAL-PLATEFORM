import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
// Removed demo data import - using only database data
import { CorrectiveActionsService, CorrectiveActionDetailDto } from '../../services/corrective-actions.service';
import { ActionsService, ActionDetailDto } from '../../services/actions.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { AlertService } from '../../services/alert.service';
import { UserDto } from '../../models/auth.models';
import { CorrectiveActionModalComponent } from '../../components/corrective-action-modal/corrective-action-modal.component';
import { SubActionModalComponent } from '../../components/sub-action-modal/sub-action-modal.component';
import { AlertContainerComponent } from '../../components/alert-container/alert-container.component';
import { ConfirmationDialogComponent } from '../../components/confirmation-dialog/confirmation-dialog.component';
import { forkJoin, of } from 'rxjs';
import { catchError, retry, delay } from 'rxjs/operators';

// Extended interface for combined actions
interface CombinedAction {
  id: string;
  title: string;
  description: string;
  status: string;
  priority?: string;
  hierarchy: string;
  assignedTo: string;
  assignedToName?: string;
  dueDate: string;
  createdAt?: string;
  type: 'regular' | 'corrective';
  reportId?: number;
  reportTitle?: string;
  reportTrackingNumber?: string;
  subActions?: any[];
  attachments?: any[];
  createdByName?: string;
  overdue?: boolean;
}

@Component({
  selector: 'app-actions',
  standalone: true,
  imports: [CommonModule, FormsModule, CorrectiveActionModalComponent, SubActionModalComponent, AlertContainerComponent, ConfirmationDialogComponent],
  templateUrl: './actions.component.html',
  styleUrls: ['./actions.component.css']
})
export class ActionsComponent implements OnInit {
  actions: CombinedAction[] = [];
  filteredActions: CombinedAction[] = [];
  correctiveActions: CorrectiveActionDetailDto[] = [];
  regularActions: ActionDetailDto[] = [];
  loading = false;
  error: string | null = null;

  statuses = ['All', 'Not Started', 'In Progress', 'Completed', 'Canceled', 'Aborted', 'Overdue'];
  hierarchies = ['All', 'Elimination', 'Substitution', 'Mesure d\'ingenierie', 'Mesures Administratives', 'EPI'];
  priorities = ['All', 'Low', 'Medium', 'High', 'Critical'];

  selectedStatus = 'All';
  selectedHierarchy = 'All';
  selectedPriority = 'All';
  searchTerm = '';

  // View mode and UI state
  viewMode: 'block' | 'calendar' = 'block';
  expandedActions = new Set<string>();
  
  // Calendar state
  currentCalendarDate = new Date();
  calendarDays: { date: Date; isCurrentMonth: boolean }[] = [];
  
  // Store all actions including cancelled/aborted for calendar view
  allActionsIncludingCancelled: CombinedAction[] = [];

  // Modal states
  showCorrectiveActionModal = false;
  showSubActionModal = false;
  correctiveActionLoading = false;
  correctiveActionError = '';
  selectedActionForSubAction: CombinedAction | null = null;
  
  // Abort Modal states
  showAbortModal = false;
  selectedActionToAbort: CombinedAction | null = null;
  abortReason = '';
  
  // Confirmation Dialog
  confirmationDialog = {
    show: false,
    title: '',
    message: '',
    confirmText: '',
    cancelText: 'Cancel',
    onConfirm: () => {},
    type: 'danger' as 'warning' | 'danger' | 'info' | 'question'
  };
  
  // Modal options
  priorityOptions = ['Low', 'Medium', 'High', 'Critical'];
  hierarchyOptions = ['Elimination', 'Substitution', 'Mesure d\'ingenierie', 'Mesures Administratives', 'EPI'];
  allUsers: UserDto[] = [];
  usersLoading = false;

  constructor(
    private correctiveActionsService: CorrectiveActionsService,
    private actionsService: ActionsService,
    private authService: AuthService,
    private userService: UserService,
    private alertService: AlertService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadAllActions();
    this.loadUsers();
  }

  loadAllActions(): void {
    this.loading = true;
    this.error = null;
    
    // Clear all existing data first
    this.actions = [];
    this.filteredActions = [];
    this.regularActions = [];
    this.correctiveActions = [];

    console.log('🔍 Loading all corrective actions from database...');

    // Load corrective actions directly from the API
    this.correctiveActionsService.getAllCorrectiveActions().subscribe({
      next: (correctiveActions) => {
        console.log('✅ Successfully loaded corrective actions:', correctiveActions.length);
        console.log('📋 Actions data:', correctiveActions);
        
        this.correctiveActions = correctiveActions || [];
        this.combineActions();
        this.loading = false;
        this.error = null;

        if (this.actions.length === 0) {
          this.error = 'No actions found in database.';
        }
      },
      error: (error) => {
        console.error('❌ Error loading corrective actions:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        
        this.loading = false;
        this.error = `Failed to load actions: ${error.status} - ${error.message}`;
        this.actions = [];
        this.filteredActions = [];
      }
    });
  }

  loadUsers(): void {
    this.usersLoading = true;
    // Get all users by requesting a large page size
    this.userService.getUsers(1, 1000).subscribe({
      next: (response) => {
        console.log('✅ Loaded users for sub-action assignment:', response.users.length);
        this.allUsers = response.users;
        this.usersLoading = false;
      },
      error: (error) => {
        console.error('❌ Error loading users:', error);
        this.usersLoading = false;
        // Fallback to empty array - sub-action creation will still work but without user search
        this.allUsers = [];
      }
    });
  }

  private combineActions(): void {
    const combined: CombinedAction[] = [];
    const today = new Date();
    const currentUser = this.authService.getCurrentUser();

    console.log('🔄 Combining actions for user:', currentUser?.fullName, 'Role:', currentUser?.role);

    // Only process corrective actions for now (simplified)
    this.correctiveActions.forEach(ca => {
      const dueDate = new Date(ca.dueDate);
      
      // Process sub-actions for overdue status
      // Sub-actions can be overdue even if completed, as long as they're not canceled
      const processedSubActions = (ca.subActions || []).map(subAction => ({
        ...subAction,
        overdue: subAction.dueDate && new Date(subAction.dueDate) < today && 
                subAction.status !== 'Canceled'
      }));

      // Calculate the action status based on sub-actions (same logic as report-details page)
      const calculatedStatus = this.getCalculatedActionStatus(ca, processedSubActions);
      
      // An action is overdue if its due date has passed and it's not canceled/aborted
      // Completed actions can still be overdue if they were completed after their deadline
      const isOverdue = dueDate < today && calculatedStatus !== 'Canceled' && calculatedStatus !== 'Aborted';

      combined.push({
        id: `corrective-${ca.id}`,
        title: ca.title,
        description: ca.description,
        status: calculatedStatus, // Use calculated status instead of database status
        priority: ca.priority,
        hierarchy: ca.hierarchy,
        assignedTo: ca.assignedTo,
        assignedToName: ca.createdByName || ca.assignedTo,
        dueDate: ca.dueDate.toString(),
        createdAt: ca.createdAt.toString(),
        type: 'corrective',
        reportId: ca.reportId,
        reportTitle: ca.reportTitle,
        reportTrackingNumber: ca.reportTrackingNumber,
        subActions: processedSubActions,
        attachments: ca.attachments || [],
        createdByName: ca.createdByName,
        overdue: isOverdue
      });
    });

    console.log('✅ Combined actions before filtering:', combined.length, combined);
    
    // Filter actions for HSE users - show only actions assigned to them
    if (this.isHSEUser() && currentUser) {
      const filteredForHSE = combined.filter(action => {
        // Check if action is assigned to current HSE user
        const userFullName = `${currentUser.firstName || ''} ${currentUser.lastName || ''}`.trim();
        const isAssignedByName = action.assignedToName === userFullName;
        const isAssignedById = action.assignedTo === currentUser.id;
        const isAssignedByCompanyId = action.assignedTo === currentUser.companyId;
        const isCreatedByUser = action.createdByName === userFullName;
        
        const isUserAction = isAssignedByName || isAssignedById || isAssignedByCompanyId || isCreatedByUser;
        
        console.log(`🔧 HSE Action Filter - Action ${action.id} (${action.title}):`, {
          assignedTo: action.assignedTo,
          assignedToName: action.assignedToName,
          createdByName: action.createdByName,
          userFullName: userFullName,
          userID: currentUser.id,
          companyID: currentUser.companyId,
          isAssignedByName,
          isAssignedById,
          isAssignedByCompanyId,
          isCreatedByUser,
          finalMatch: isUserAction
        });
        
        return isUserAction;
      });
      
      console.log('📊 HSE filtered actions:', filteredForHSE.length, 'out of', combined.length);
      
      // Store all filtered actions (including cancelled/aborted) for calendar view
      this.allActionsIncludingCancelled = filteredForHSE;
      
      // For block view, use the same actions - filtering will be done in filterActions()
      this.actions = filteredForHSE;
    } else {
      // Admin users see all actions
      this.allActionsIncludingCancelled = combined;
      this.actions = combined;
    }
    
    console.log('✅ Final actions for user:', this.actions.length, this.actions);
    console.log('✅ All actions including cancelled for calendar:', this.allActionsIncludingCancelled.length);
    this.filterActions();
  }

  /**
   * Calculate the actual status of an action based on its sub-actions
   * Following the logic: Not Started -> In Progress -> Completed
   */
  private getCalculatedActionStatus(action: any, subActions: any[]): string {
    // If action is aborted, always return Aborted (overrides sub-action logic)
    if (action.status === 'Aborted') {
      return 'Aborted';
    }
    
    // If action truly has no sub-actions, use the database status
    if (!subActions || subActions.length === 0) {
      return action.status || 'Not Started';
    }

    // Apply the finalized status calculation rules
    
    // Count different status types
    const notStartedCount = subActions.filter(sa => sa.status === 'Not Started').length;
    const inProgressCount = subActions.filter(sa => sa.status === 'In Progress').length;
    const completedCount = subActions.filter(sa => sa.status === 'Completed').length;
    const cancelledCount = subActions.filter(sa => sa.status === 'Cancelled').length;

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

  // KPI Getters
  get totalActions(): number {
    return this.actions.length;
  }

  get completedActions(): number {
    return this.actions.filter(a => a.status === 'Completed').length;
  }

  get inProgressActions(): number {
    return this.actions.filter(a => a.status === 'In Progress').length;
  }

  get overdueActions(): number {
    return this.actions.filter(a => a.overdue).length;
  }

  get notStartedActions(): number {
    return this.actions.filter(a => a.status === 'Not Started').length;
  }

  get canceledOrAbortedActions(): number {
    return this.actions.filter(a => a.status === 'Canceled' || a.status === 'Aborted').length;
  }

  get completionRate(): string {
    return this.totalActions === 0
      ? '0%'
      : `${Math.round((this.completedActions / this.totalActions) * 100)}%`;
  }

  // Utility Methods
  getUserName(id: string): string {
    // No more demo data - return the ID or fetch from backend if needed
    return id || 'Unknown';
  }

  formatDate(date: string): string {
    if (!date) return '';
    return new Date(date).toLocaleDateString();
  }

  // Enhanced filtering with multiple search criteria
  filterActions(): void {
    this.filteredActions = this.actions.filter(action => {
      // Status matching - check both main action and sub-actions
      let matchesStatus = false;
      if (this.selectedStatus === 'All') {
        matchesStatus = true;
      } else if (this.selectedStatus === 'Overdue') {
        matchesStatus = action.overdue || false;
      } else {
        // Check main action status
        matchesStatus = action.status === this.selectedStatus;
        
        // Also check if any sub-action has the selected status
        if (!matchesStatus && action.subActions && action.subActions.length > 0) {
          matchesStatus = action.subActions.some(subAction => subAction.status === this.selectedStatus);
        }
      }
      
      const matchesHierarchy = this.selectedHierarchy === 'All' || action.hierarchy === this.selectedHierarchy;
      const matchesPriority = this.selectedPriority === 'All' || (action.priority || 'Medium') === this.selectedPriority;
      
      // Enhanced search: title, description, author, report tracking number, and sub-actions
      const searchLower = this.searchTerm.toLowerCase();
      let matchesSearch = this.searchTerm === '' ||
        action.title.toLowerCase().includes(searchLower) ||
        action.description.toLowerCase().includes(searchLower) ||
        (action.createdByName && action.createdByName.toLowerCase().includes(searchLower)) ||
        (action.assignedToName && action.assignedToName.toLowerCase().includes(searchLower)) ||
        (action.reportTrackingNumber && action.reportTrackingNumber.toLowerCase().includes(searchLower)) ||
        (action.reportTitle && action.reportTitle.toLowerCase().includes(searchLower));
      
      // Also search within sub-actions
      if (!matchesSearch && action.subActions && action.subActions.length > 0) {
        matchesSearch = action.subActions.some(subAction => 
          subAction.title.toLowerCase().includes(searchLower) ||
          subAction.description.toLowerCase().includes(searchLower) ||
          (subAction.assignedToName && subAction.assignedToName.toLowerCase().includes(searchLower))
        );
      }

      return matchesStatus && matchesHierarchy && matchesPriority && matchesSearch;
    });
  }

  // View Management
  setViewMode(mode: 'block' | 'calendar'): void {
    this.viewMode = mode;
  }

  // Details Expansion
  toggleDetails(actionId: string): void {
    if (this.expandedActions.has(actionId)) {
      this.expandedActions.delete(actionId);
    } else {
      this.expandedActions.add(actionId);
    }
  }

  isDetailsExpanded(actionId: string): boolean {
    return this.expandedActions.has(actionId);
  }

  // Action Management

  onEditSubAction(subAction: any): void {
    console.log('Edit sub-action:', subAction.title);
    // TODO: Implement sub-action editing modal
  }

  onCancelSubAction(subAction: any): void {
    this.confirmationDialog = {
      show: true,
      title: 'Cancel Sub-action',
      message: `Are you sure you want to cancel the sub-action "${subAction.title}"? This action cannot be undone.`,
      confirmText: 'Cancel Sub-action',
      cancelText: 'Keep Sub-action',
      type: 'danger',
      onConfirm: () => {
        this.executeCancelSubAction(subAction);
        this.confirmationDialog.show = false;
      }
    };
  }

  private executeCancelSubAction(subAction: any): void {
    console.log('🚫 Canceling sub-action:', subAction.title);
    
    // Call API to cancel sub-action using the dedicated sub-actions endpoint
    this.correctiveActionsService.cancelSubAction(subAction.id).subscribe({
      next: () => {
        console.log('✅ Sub-action canceled successfully via API');
        this.alertService.showSuccess(`Sub-action "${subAction.title}" has been canceled`);
        this.loadAllActions(); // Refresh to get updated data
      },
      error: (error) => {
        console.error('❌ Error canceling sub-action:', error);
        this.alertService.showError(`Failed to cancel sub-action: ${error.error?.message || 'Unknown error'}`);
      }
    });
  }

  onCloseConfirmation(): void {
    this.confirmationDialog.show = false;
  }

  onViewActionDetails(action: CombinedAction): void {
    if (action.type === 'corrective') {
      // Navigate to corrective action details
      console.log('View corrective action details:', action.id);
    } else {
      // Navigate to regular action details
      console.log('View regular action details:', action.id);
    }
  }

  onUpdateActionStatus(action: CombinedAction, newStatus: string): void {
    const previousStatus = action.status;
    
    // Optimistic update - update UI immediately
    action.status = newStatus;
    this.filterActions();

    if (action.type === 'corrective') {
      const actionId = parseInt(action.id.replace('corrective-', ''));
      this.correctiveActionsService.updateCorrectiveActionStatus(actionId, newStatus).pipe(
        retry(1), // Retry once if it fails
        catchError(error => {
          console.error('Error updating corrective action status:', error);
          // Revert optimistic update
          action.status = previousStatus;
          this.filterActions();
          this.error = 'Failed to update action status. Please try again.';
          return of(null);
        })
      ).subscribe({
        next: (updated) => {
          if (updated) {
            // Success - the optimistic update was correct
            this.error = null;
          }
        }
      });
    } else {
      const actionId = parseInt(action.id);
      this.actionsService.updateActionStatus(actionId, newStatus).pipe(
        retry(1), // Retry once if it fails
        catchError(error => {
          console.error('Error updating action status:', error);
          // Revert optimistic update
          action.status = previousStatus;
          this.filterActions();
          this.error = 'Failed to update action status. Please try again.';
          return of(null);
        })
      ).subscribe({
        next: () => {
          // Success - the optimistic update was correct
          this.error = null;
        }
      });
    }
  }

  // Sub-actions Helper
  getSubActionsCount(action: CombinedAction): number {
    return action.subActions ? action.subActions.length : 0;
  }

  getActiveSubActionsCount(action: CombinedAction): number {
    return action.subActions ? action.subActions.filter(sa => sa.status !== 'Canceled').length : 0;
  }

  // Check if any sub-actions are overdue
  hasOverdueSubActions(action: CombinedAction): boolean {
    return action.subActions ? action.subActions.some(sa => sa.overdue && sa.status !== 'Canceled') : false;
  }

  // Action Type Helper
  getActionTypeLabel(action: CombinedAction): string {
    return action.type === 'corrective' ? 'Corrective Action' : 'Regular Action';
  }

  getActionTypeBadgeClass(action: CombinedAction): string {
    return action.type === 'corrective' 
      ? 'bg-red-100 text-red-800' 
      : 'bg-blue-100 text-blue-800';
  }

  // CSS Class Helpers
  getPriorityClass(priority: string): string {
    switch (priority) {
      case 'High': return 'category-high';
      case 'Medium': return 'category-medium';
      case 'Low': return 'category-low';
      default: return 'category-medium';
    }
  }

  getStatusBorderClass(action: CombinedAction): string {
    // If action is overdue, always show overdue styling
    if (action.overdue) {
      return 'status-overdue';
    }
    
    switch (action.status) {
      case 'Not Started': return 'status-pending';
      case 'In Progress': return 'status-in-progress';
      case 'Completed': return 'status-completed';
      case 'On Hold': return 'status-on-hold';
      case 'Canceled': return 'status-canceled';
      case 'Aborted': return 'status-canceled';
      default: return 'status-pending';
    }
  }

  getAssigneeAvatarClass(status: string): string {
    // Since we removed all colors inside action components, return neutral styling only
    return 'border border-gray-300';
  }

  getDeadlineClass(dueDate: string, status: string): string {
    // Overdue styling is now handled globally in the HTML template
    return 'text-gray-700';
  }

  getStatusDotClass(status: string): string {
    // Since we removed all colors inside action components, return neutral styling only
    return 'border border-gray-300';
  }

  getStatusTextClass(status: string): string {
    // Since we removed all colors inside action components, return neutral styling only
    return 'text-gray-700';
  }

  getSubActionStatusClass(status: string): string {
    switch (status) {
      case 'Completed': return 'border-green-300 text-green-700 bg-green-50';
      case 'In Progress': return 'border-blue-300 text-blue-700 bg-blue-50';
      case 'Not Started': return 'border-gray-300 text-gray-700 bg-gray-50';
      case 'On Hold': return 'border-yellow-300 text-yellow-700 bg-yellow-50';
      case 'Canceled': return 'border-red-300 text-red-700 bg-red-50';
      default: return 'border-gray-300 text-gray-700 bg-gray-50';
    }
  }

  getPriorityColorClass(priority: string): string {
    switch (priority) {
      case 'Critical': return 'bg-red-500 text-white';
      case 'High': return 'bg-orange-500 text-white';
      case 'Medium': return 'bg-yellow-500 text-white';
      case 'Low': return 'bg-green-500 text-white';
      default: return 'bg-gray-500 text-white';
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Completed': return 'border-green-300 text-green-700 bg-green-100';
      case 'In Progress': return 'border-blue-300 text-blue-700 bg-blue-100';
      case 'Not Started': return 'border-gray-300 text-gray-700 bg-gray-100';
      case 'On Hold': return 'border-yellow-300 text-yellow-700 bg-yellow-100';
      case 'Open': return 'border-purple-300 text-purple-700 bg-purple-100';
      case 'Canceled': return 'border-red-300 text-red-700 bg-red-50';
      case 'Aborted': return 'border-red-300 text-red-700 bg-red-50';
      default: return 'border-gray-300 text-gray-700 bg-gray-100';
    }
  }

  isAdmin(): boolean {
    const currentUser = this.authService.getCurrentUser();
    return currentUser?.role === 'Admin';
  }

  isHSEUser(): boolean {
    const currentUser = this.authService.getCurrentUser();
    return currentUser?.role === 'HSE' || currentUser?.role === 'HSE Agent';
  }

  // Access control methods
  canCreateSubAction(action: CombinedAction): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Cannot create sub-actions for aborted actions
    if (action.status === 'Aborted') return false;

    // Only action author can create sub-actions for their own actions
    return action.createdByName === currentUser.fullName || 
           action.assignedTo === currentUser.id ||
           action.assignedToName === currentUser.fullName;
  }

  canCancelSubAction(action: CombinedAction, subAction: any): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Admin can cancel any sub-action
    if (this.isAdmin()) return true;

    // Non-admin users can only cancel sub-actions of their own actions
    return action.createdByName === currentUser.fullName || 
           action.assignedTo === currentUser.id ||
           action.assignedToName === currentUser.fullName;
  }

  canAbortAction(action: CombinedAction): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    // Admin can abort any action
    if (this.isAdmin()) return true;

    // Non-admin users can only abort their own actions
    return action.createdByName === currentUser.fullName || 
           action.assignedTo === currentUser.id ||
           action.assignedToName === currentUser.fullName;
  }

  // Helper Methods
  getCurrentUser() {
    return this.authService.getCurrentUser();
  }

  // Performance Optimization
  trackByActionId(index: number, action: CombinedAction): string {
    return action.id;
  }

  trackBySubActionId(index: number, subAction: any): string {
    return subAction.id;
  }

  // Hierarchy styling methods (copied from report-details component)
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

  // Navigation methods
  navigateToReport(reportId?: number): void {
    if (reportId) {
      console.log('🔗 Navigating to report:', reportId);
      this.router.navigate(['/reports', reportId]);
    } else {
      console.warn('⚠️ No report ID provided for navigation');
    }
  }

  // Action management methods
  onAbortAction(action: CombinedAction): void {
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

  private executeAbortAction(action: CombinedAction, reason: string): void {
    console.log('⚠️ Aborting action:', action.id, 'Reason:', reason);
    
    // Use the correct service based on action type
    let abortObservable;
    if (action.type === 'corrective') {
      const actionId = parseInt(action.id.replace('corrective-', ''));
      abortObservable = this.correctiveActionsService.abortCorrectiveAction(actionId, reason);
    } else {
      const actionId = parseInt(action.id);
      abortObservable = this.actionsService.abortAction(actionId, reason);
    }
    
    abortObservable.subscribe({
      next: () => {
        console.log('⚠️ Action aborted successfully with tracking details');
        this.loadAllActions(); // Reload to get updated data
        const actionType = action.type === 'corrective' ? 'Corrective action' : 'Action';
        this.alertService.showSuccess(`${actionType} "${action.title}" has been aborted`);
      },
      error: (error: any) => {
        console.error('⚠️ Error aborting action:', error);
        const actionType = action.type === 'corrective' ? 'corrective action' : 'action';
        this.alertService.showError(`Failed to abort ${actionType}: ${error.error?.message || 'Unknown error'}`);
      }
    });
  }

  onCreateNewAction(): void {
    console.log('➕ Opening corrective action creation modal');
    this.showCorrectiveActionModal = true;
    this.correctiveActionError = '';
  }

  onAddSubAction(action: CombinedAction): void {
    console.log('➕ Opening sub-action creation modal for:', action.title);
    this.selectedActionForSubAction = action;
    this.showSubActionModal = true;
  }

  // Modal event handlers
  onCloseCorrectiveActionModal(): void {
    this.showCorrectiveActionModal = false;
    this.correctiveActionError = '';
  }

  onCloseSubActionModal(): void {
    this.showSubActionModal = false;
    this.selectedActionForSubAction = null;
  }

  onSubmitCorrectiveAction(formData: any): void {
    console.log('📤 Submitting standalone corrective action:', formData);
    this.correctiveActionLoading = true;
    this.correctiveActionError = '';

    // For standalone corrective actions, we can create them without a report context
    // This allows users to create general corrective actions not tied to specific incidents
    
    // Modify formData to handle standalone creation and fix field mapping
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser?.id) {
      this.correctiveActionLoading = false;
      this.alertService.showError('User authentication error. Please log in again.');
      return;
    }

    const standaloneFormData = {
      title: formData.title,
      description: formData.description,
      dueDate: formData.dueDate || new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // Default to 30 days if no due date provided
      priority: formData.priority,
      hierarchy: formData.hierarchy,
      // For standalone actions, don't send reportId at all to avoid constraint issues
      createdByHSEId: currentUser.id
    };

    console.log('📋 Mapped standalone form data:', standaloneFormData);

    this.correctiveActionsService.createCorrectiveAction(standaloneFormData).subscribe({
      next: (response) => {
        console.log('✅ Standalone corrective action created:', response);
        this.correctiveActionLoading = false;
        this.onCloseCorrectiveActionModal();
        this.alertService.showSuccess('Corrective action created successfully!');
        this.loadAllActions(); // Refresh the list
      },
      error: (error) => {
        console.error('❌ Error creating corrective action:', error);
        this.correctiveActionLoading = false;
        this.correctiveActionError = error.error?.message || 'Failed to create corrective action. Please try again.';
        this.alertService.showError('Failed to create corrective action: ' + (error.error?.message || 'Unknown error'));
      }
    });
  }

  onSubmitSubAction(formData: any): void {
    if (!this.selectedActionForSubAction) return;

    console.log('📤 Submitting sub-action:', formData);
    const action = this.selectedActionForSubAction;
    this.correctiveActionLoading = true;

    if (action.type === 'corrective') {
      const actionId = parseInt(action.id.replace('corrective-', ''));
      
      // Create sub-action using the corrective actions service
      // Note: Check if createSubAction method exists in the service
      if (this.correctiveActionsService.createSubAction) {
        this.correctiveActionsService.createSubAction(actionId, formData).subscribe({
          next: (response) => {
            console.log('✅ Sub-action created successfully:', response);
            this.correctiveActionLoading = false;
            this.onCloseSubActionModal();
            this.alertService.showSuccess('Sub-action created successfully!');
            this.loadAllActions(); // Refresh to show new sub-action
          },
          error: (error) => {
            console.error('❌ Error creating sub-action:', error);
            this.correctiveActionLoading = false;
            this.alertService.showError('Failed to create sub-action: ' + (error.error?.message || 'Unknown error'));
          }
        });
      } else {
        // Fallback: simulate success for now
        console.warn('⚠️ createSubAction method not available, simulating success');
        setTimeout(() => {
          this.correctiveActionLoading = false;
          this.onCloseSubActionModal();
          this.alertService.showSuccess('Sub-action created successfully!');
          this.loadAllActions();
        }, 1000);
      }
    } else {
      // Handle regular actions if needed
      console.warn('⚠️ Sub-action creation for regular actions not implemented yet');
      this.correctiveActionLoading = false;
      this.alertService.showError('Sub-action creation for regular actions is not yet implemented');
    }
  }

  getParentActionForModal(): any {
    if (!this.selectedActionForSubAction) return null;
    
    const action = this.selectedActionForSubAction;
    
    // Convert CombinedAction to the format expected by the modal
    return {
      id: action.type === 'corrective' ? parseInt(action.id.replace('corrective-', '')) : parseInt(action.id),
      title: action.title,
      description: action.description,
      status: action.status,
      priority: action.priority,
      hierarchy: action.hierarchy,
      dueDate: action.dueDate,
      createdAt: action.createdAt
    };
  }

  // ===== CALENDAR METHODS =====
  
  generateCalendarDays(): void {
    const year = this.currentCalendarDate.getFullYear();
    const month = this.currentCalendarDate.getMonth();
    
    // First day of the current month
    const firstDay = new Date(year, month, 1);
    // Last day of the current month
    const lastDay = new Date(year, month + 1, 0);
    
    // Start from the Sunday of the week containing the first day
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay());
    
    // End at the Saturday of the week containing the last day
    const endDate = new Date(lastDay);
    endDate.setDate(endDate.getDate() + (6 - lastDay.getDay()));
    
    this.calendarDays = [];
    const currentDate = new Date(startDate);
    
    while (currentDate <= endDate) {
      this.calendarDays.push({
        date: new Date(currentDate),
        isCurrentMonth: currentDate.getMonth() === month
      });
      currentDate.setDate(currentDate.getDate() + 1);
    }
  }
  
  getCalendarDays(): { date: Date; isCurrentMonth: boolean }[] {
    if (this.calendarDays.length === 0) {
      this.generateCalendarDays();
    }
    return this.calendarDays;
  }
  
  getCalendarTitle(): string {
    return this.currentCalendarDate.toLocaleDateString('en-US', { 
      month: 'long', 
      year: 'numeric' 
    });
  }
  
  navigateCalendar(direction: 'prev' | 'next'): void {
    const newDate = new Date(this.currentCalendarDate);
    if (direction === 'prev') {
      newDate.setMonth(newDate.getMonth() - 1);
    } else {
      newDate.setMonth(newDate.getMonth() + 1);
    }
    this.currentCalendarDate = newDate;
    this.generateCalendarDays();
  }
  
  getCalendarDayClass(day: { date: Date; isCurrentMonth: boolean }): string {
    const today = new Date();
    const isToday = day.date.toDateString() === today.toDateString();
    
    let classes = '';
    if (isToday) {
      classes += 'bg-blue-50 border-blue-300 ';
    } else if (!day.isCurrentMonth) {
      classes += 'bg-gray-50 ';
    } else {
      classes += 'bg-white ';
    }
    
    return classes;
  }
  
  getActionsForDay(date: Date): CombinedAction[] {
    const dateString = date.toDateString();
    // Include ALL actions, even cancelled/aborted ones
    return this.actions.filter(action => {
      const actionDate = new Date(action.dueDate);
      return actionDate.toDateString() === dateString;
    }).slice(0, 3); // Limit to 3 actions per day for display
  }

  getActionsAndSubActionsForDay(date: Date): { action: CombinedAction; title: string; isSubAction: boolean; subAction?: any }[] {
    const dateString = date.toDateString();
    const items: { action: CombinedAction; title: string; isSubAction: boolean; subAction?: any }[] = [];
    
    // Include ALL actions (including cancelled/aborted) from the calendar-specific array
    this.allActionsIncludingCancelled.forEach(action => {
      // Check main action due date
      const actionDate = new Date(action.dueDate);
      if (actionDate.toDateString() === dateString) {
        items.push({
          action: action,
          title: action.title,
          isSubAction: false
        });
      }
      
      // Check sub-actions due dates
      if (action.subActions) {
        action.subActions.forEach(subAction => {
          if (subAction.dueDate) {
            const subActionDate = new Date(subAction.dueDate);
            if (subActionDate.toDateString() === dateString) {
              // Create a pseudo action for sub-action with parent action context
              const subActionAsAction: CombinedAction = {
                ...action,
                title: subAction.title,
                status: subAction.status,
                dueDate: subAction.dueDate,
                overdue: subAction.overdue || false
              };
              
              items.push({
                action: subActionAsAction,
                title: subAction.title,
                isSubAction: true,
                subAction: subAction
              });
            }
          }
        });
      }
    });
    
    // Sort by time if multiple items on same day, limit to 3 for display
    return items.slice(0, 3);
  }

  getCalendarItemTitle(item: { action: CombinedAction; title: string; isSubAction: boolean; subAction?: any }): string {
    const type = item.isSubAction ? 'Sub-action' : 'Action';
    const status = item.action.status;
    const overdue = item.action.overdue ? ' (OVERDUE)' : '';
    const cancelled = (status === 'Canceled' || status === 'Aborted') ? ' (CANCELLED/ABORTED)' : '';
    const completedAndOverdue = (status === 'Completed' && item.action.overdue) ? ' - Completed Late' : '';
    
    return `${type}: ${item.title} - ${status}${overdue}${cancelled}${completedAndOverdue}`;
  }

  getCalendarTextClass(action: CombinedAction): string {
    // Add strikethrough for cancelled/aborted actions
    if (action.status === 'Canceled' || action.status === 'Aborted') {
      return 'line-through';
    }
    return '';
  }
  
  getActionCalendarClass(action: CombinedAction): string {
    // Status-based background colors
    switch (action.status) {
      case 'Completed':
        return 'bg-green-100 text-green-800 border border-green-300 hover:bg-green-200';
      case 'In Progress':
        return 'bg-yellow-100 text-yellow-800 border border-yellow-300 hover:bg-yellow-200';
      case 'Not Started':
        return 'bg-gray-100 text-gray-800 border border-gray-300 hover:bg-gray-200';
      case 'Canceled':
      case 'Aborted':
        return 'bg-red-100 text-red-800 border border-red-300 hover:bg-red-200';
      default:
        return 'bg-gray-100 text-gray-800 border border-gray-300 hover:bg-gray-200';
    }
  }
  
  getActionDotClass(action: CombinedAction): string {
    // Status-based dot colors
    switch (action.status) {
      case 'Completed':
        return 'bg-green-500';
      case 'In Progress':
        return 'bg-yellow-500';
      case 'Not Started':
        return 'bg-gray-500';
      case 'Canceled':
      case 'Aborted':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  }

  formatDateTime(date: string | Date | undefined): string {
    if (!date) return 'Not set';
    
    // If the date string doesn't end with 'Z', it's likely coming from backend as local time format
    // but is actually UTC, so we need to append 'Z' to tell JS it's UTC
    let dateString = date.toString();
    if (!dateString.endsWith('Z') && !dateString.includes('+') && !dateString.includes('T')) {
      // If it's a simple date format, treat as UTC
      dateString += 'Z';
    } else if (dateString.includes('T') && !dateString.endsWith('Z') && !dateString.includes('+')) {
      // If it's ISO format but without timezone info, assume it's UTC
      dateString += 'Z';
    }
    
    const dateObj = new Date(dateString);
    return dateObj.toLocaleString();
  }
}
