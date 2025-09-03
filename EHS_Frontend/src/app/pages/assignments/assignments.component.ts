import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AlertService } from '../../services/alert.service';
import { ActionsService, ActionDetailDto } from '../../services/actions.service';
import { SubActionsService } from '../../services/sub-actions.service';
import { ReportService } from '../../services/report.service';
import { forkJoin, Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

// Use backend DTOs - extend with frontend specific properties
interface AssignmentAction extends ActionDetailDto {
  companyId?: string; // For frontend display
  statusChanged?: boolean; // Track if status changed for backend update
  author?: string; // Frontend display field (mapped from createdByName)
  priority?: string; // Frontend display field (mapped from hierarchy)
  assignedTo?: any; // Frontend display field
}

@Component({
  selector: 'app-assignments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './assignments.component.html',
  styleUrls: ['./assignments.component.css']
})
export class AssignmentsComponent implements OnInit {
  loading = false;
  searchMode = true;
  companyId = '';
  actions: AssignmentAction[] = [];
  filteredActions: AssignmentAction[] = [];
  
  
  // Company user information from validation
  companyUser: any = null;
  
  // Filters - only status and priority for actions
  selectedStatus = 'All';
  selectedPriority = 'All';
  searchTerm = '';
  
  // Custom confirmation dialog
  showConfirmDialog = false;
  confirmMessage = '';
  confirmAction: (() => void) | null = null;
  
  // Statistics
  stats = {
    total: 0,
    notStarted: 0,
    inProgress: 0,
    completed: 0
  };

  // Filter options - simplified as requested
  statuses = ['All', 'Not Started', 'In Progress', 'Completed'];
  priorities = ['All', 'Low', 'Medium', 'High', 'Critical'];

  constructor(
    private alertService: AlertService,
    private router: Router,
    private reportService: ReportService,
    private actionsService: ActionsService,
    private subActionsService: SubActionsService
  ) {}

  ngOnInit(): void {
    // No authentication required - users can access assignments with just their Company ID
  }

  searchAssignments(): void {
    if (!this.companyId.trim()) {
      this.alertService.showError('Please enter a valid Company ID');
      return;
    }

    this.loading = true;
    
    // Validate company ID exists in the system
    this.validateCompanyExists(this.companyId);
  }

  processAssignedSubActions(assignedSubActions: any[]): void {
    console.log('=== PROCESSING ASSIGNED SUB-ACTIONS ===');
    console.log('Total sub-actions assigned to user:', assignedSubActions.length);

    if (assignedSubActions.length === 0) {
      // No sub-actions assigned to this user - show empty dashboard
      console.log('No sub-actions assigned to user - showing empty dashboard');
      this.actions = [];
      this.filteredActions = [];
      this.calculateStats();
      this.loading = false;
      this.searchMode = false;
      return;
    }

    // Group sub-actions by their parent corrective action
    const correctiveActionGroups = new Map<number, any[]>();
    
    assignedSubActions.forEach(subAction => {
      console.log('Processing sub-action:', {
        id: subAction.id,
        title: subAction.title,
        correctiveActionId: subAction.correctiveActionId,
        correctiveActionTitle: subAction.correctiveActionTitle,
        correctiveActionStatus: subAction.correctiveActionStatus,
        correctiveActionAuthor: subAction.correctiveActionAuthor,
        correctiveActionCreatedAt: subAction.correctiveActionCreatedAt
      });
      
      console.log('🔍 AUTHOR DEBUG - Raw value:', subAction.correctiveActionAuthor);
      console.log('🔍 DATE DEBUG - Raw value:', subAction.correctiveActionCreatedAt);
      console.log('🔍 DATE DEBUG - Type:', typeof subAction.correctiveActionCreatedAt);
      console.log('🔍 DATE DEBUG - Is null/undefined?', subAction.correctiveActionCreatedAt == null);

      if (subAction.correctiveActionId) {
        if (!correctiveActionGroups.has(subAction.correctiveActionId)) {
          correctiveActionGroups.set(subAction.correctiveActionId, []);
        }
        correctiveActionGroups.get(subAction.correctiveActionId)!.push(subAction);
      } else {
        console.warn('Sub-action has no corrective action ID:', subAction);
      }
    });

    console.log('Grouped by corrective actions:', correctiveActionGroups.size, 'groups');

    // Convert corrective action groups to assignment actions
    this.actions = Array.from(correctiveActionGroups.entries()).map(([correctiveActionId, subActions]) => {
      const firstSubAction = subActions[0]; // Get corrective action data from first sub-action
      
      console.log(`Creating assignment action for corrective action ${correctiveActionId}:`, {
        title: firstSubAction.correctiveActionTitle,
        description: firstSubAction.correctiveActionDescription,
        status: firstSubAction.correctiveActionStatus,
        priority: firstSubAction.correctiveActionPriority,
        author: firstSubAction.correctiveActionAuthor,
        dueDate: firstSubAction.correctiveActionDueDate,
        createdAt: firstSubAction.correctiveActionCreatedAt,
        subActionsCount: subActions.length
      });

      const action: AssignmentAction = {
        // Use corrective action ID as the action ID
        id: correctiveActionId,
        // Use REAL corrective action data
        title: firstSubAction.correctiveActionTitle || 'Corrective Action',
        description: firstSubAction.correctiveActionDescription || 'Corrective action with assigned sub-actions',
        status: firstSubAction.correctiveActionStatus || 'Not Started',
        dueDate: firstSubAction.correctiveActionDueDate,
        hierarchy: firstSubAction.correctiveActionHierarchy || 'Administrative Controls',
        // Frontend fields
        author: firstSubAction.correctiveActionAuthor || 'HSE Team', // Real corrective action author
        priority: firstSubAction.correctiveActionPriority || 'Medium',
        assignedTo: this.companyUser,
        companyId: this.companyId,
        // Map sub-actions
        subActions: subActions.map(subAction => ({
          ...subAction,
          assignedTo: this.companyUser
        })),
        // Required backend fields (placeholder values since this represents a corrective action)
        assignedToId: undefined,
        assignedToName: undefined,
        createdById: '',
        createdByName: firstSubAction.correctiveActionAuthor || 'HSE Team',
        createdAt: firstSubAction.correctiveActionCreatedAt || new Date(),
        updatedAt: new Date(),
        reportId: undefined,
        reportTitle: undefined,
        reportTrackingNumber: undefined,
        overdue: false,
        attachments: [],
        // Frontend specific
        statusChanged: false
      };

      return action;
    });

    // Sort actions by creation date (newest first) to ensure proper ordering
    this.actions.sort((a, b) => {
      const dateA = new Date(a.createdAt || 0);
      const dateB = new Date(b.createdAt || 0);
      return dateB.getTime() - dateA.getTime(); // Newest first
    });

    console.log('=== PROCESSING COMPLETE ===');
    console.log(`✓ Created ${this.actions.length} assignment actions from corrective actions`);
    console.log(`✓ User: ${this.companyUser?.fullName} (${this.companyUser?.userId})`);

    // Automatically update all action statuses based on their sub-actions
    this.actions.forEach(action => {
      this.updateActionStatusAutomatically(action);
    });

    // Initialize filtered actions and calculate stats
    this.filteredActions = [...this.actions];
    this.calculateStats();
    this.applyFilters();
    
    this.loading = false;
    this.searchMode = false;

    console.log('Assignment loading complete. Actions to display:', this.actions);
  }

  calculateStats(): void {
    // Calculate stats for sub-actions only (as requested)
    let allSubActions: Array<{status: string}> = [];
    this.actions.forEach(action => {
      allSubActions.push(...action.subActions);
    });

    this.stats.total = allSubActions.length;
    this.stats.notStarted = allSubActions.filter(item => item.status === 'Not Started').length;
    this.stats.inProgress = allSubActions.filter(item => item.status === 'In Progress').length;
    this.stats.completed = allSubActions.filter(item => item.status === 'Completed').length;
  }

  applyFilters(): void {
    this.filteredActions = this.actions.filter(action => {
      const matchesStatus = this.selectedStatus === 'All' || action.status === this.selectedStatus;
      const matchesPriority = this.selectedPriority === 'All' || action.priority === this.selectedPriority;
      
      const searchLower = this.searchTerm.toLowerCase();
      const matchesSearch = this.searchTerm === '' ||
        action.title.toLowerCase().includes(searchLower) ||
        action.description.toLowerCase().includes(searchLower) ||
        (action.author && action.author.toLowerCase().includes(searchLower)) ||
        action.subActions.some(sub => 
          sub.title.toLowerCase().includes(searchLower) ||
          sub.description.toLowerCase().includes(searchLower)
        );

      return matchesStatus && matchesPriority && matchesSearch;
    });
  }

  clearFilters(): void {
    this.selectedStatus = 'All';
    this.selectedPriority = 'All';
    this.searchTerm = '';
    this.applyFilters();
  }

  resetSearch(): void {
    this.searchMode = true;
    this.companyId = '';
    this.companyUser = null;
    this.actions = [];
    this.filteredActions = [];
    this.stats = { total: 0, notStarted: 0, inProgress: 0, completed: 0 };
  }

  // Direct sub-action status advancement with custom confirmation
  advanceSubActionStatus(action: AssignmentAction, subAction: any): void {
    const nextStatus = this.getNextStatus(subAction.status);
    if (!nextStatus) return;

    // Show custom confirmation dialog
    this.confirmMessage = `Are you sure you want to change "${subAction.title}" status from "${subAction.status}" to "${nextStatus}"?`;
    this.confirmAction = () => {
      // Update sub-action status via backend API - include user ID for profile user identification
      this.subActionsService.updateSubActionStatus(subAction.id, nextStatus, this.companyUser?.userId).subscribe({
        next: () => {
          console.log(`Sub-action ${subAction.id} status updated to ${nextStatus}`);
          
          // Update local sub-action status
          subAction.status = nextStatus;
          subAction.updatedAt = new Date().toISOString();

          // Automatically update parent action status based on sub-actions
          this.updateActionStatusAutomatically(action);

          // Update parent action status in backend if it changed
          this.updateActionStatusInBackend(action);

          // Recalculate stats and apply filters
          this.calculateStats();
          this.applyFilters();

          // Use our existing alert service for success message
          this.alertService.showSuccess(`Sub-action "${subAction.title}" status updated to ${nextStatus}!`);
        },
        error: (error) => {
          console.error('Error updating sub-action status:', error);
          this.alertService.showError('Failed to update sub-action status. Please try again.');
        }
      });
    };
    this.showConfirmDialog = true;
  }

  // Confirmation dialog methods
  confirmStatusChange(): void {
    if (this.confirmAction) {
      this.confirmAction();
    }
    this.closeConfirmDialog();
  }

  closeConfirmDialog(): void {
    this.showConfirmDialog = false;
    this.confirmMessage = '';
    this.confirmAction = null;
  }

  // Utility methods
  formatDate(date: string | Date | undefined): string {
    if (!date) return 'Not set';
    return new Date(date).toLocaleDateString();
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

  getStatusClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'bg-green-100 text-green-800 border-green-300';
      case 'In Progress':
        return 'bg-orange-100 text-orange-800 border-orange-300';
      case 'Not Started':
        return 'bg-gray-100 text-gray-800 border-gray-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  getOverdueClass(): string {
    return 'bg-red-100 text-red-800 border-red-300';
  }

  isOverdue(item: any): boolean {
    console.log('🔍 OVERDUE CHECK - Item:', item.title || item.id, 'overdue property:', item.overdue);
    
    // Use backend overdue property if available
    if (item && typeof item.overdue === 'boolean') {
      console.log('🔍 OVERDUE CHECK - Using backend overdue value:', item.overdue);
      return item.overdue;
    }
    
    console.log('🔍 OVERDUE CHECK - No overdue property, using date fallback');
    
    // Fallback to date comparison if overdue property not available
    if (!item.dueDate) return false;
    
    const due = new Date(item.dueDate);
    const today = new Date();
    
    if (isNaN(due.getTime())) return false;
    
    today.setHours(0, 0, 0, 0);
    due.setHours(0, 0, 0, 0);
    
    return due < today;
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'Completed':
        return 'fa-check-circle';
      case 'In Progress':
        return 'fa-clock';
      case 'Not Started':
        return 'fa-play-circle';
      default:
        return 'fa-question-circle';
    }
  }

  getPriorityClass(priority: string): string {
    switch (priority) {
      case 'Critical':
        return 'bg-red-100 text-red-800 border-red-300';
      case 'High':
        return 'bg-orange-100 text-orange-800 border-orange-300';
      case 'Medium':
        return 'bg-yellow-100 text-yellow-800 border-yellow-300';
      case 'Low':
        return 'bg-green-100 text-green-800 border-green-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  // Automatically update action status based on sub-actions completion
  updateActionStatusAutomatically(action: AssignmentAction): void {
    const subActions = action.subActions;
    if (subActions.length === 0) return;

    const previousStatus = action.status;
    const allCompleted = subActions.every(sub => sub.status === 'Completed');
    const anyInProgress = subActions.some(sub => sub.status === 'In Progress');
    const anyStarted = subActions.some(sub => sub.status !== 'Not Started');

    // Logic for automatic action status updates:
    if (allCompleted && action.status !== 'Completed') {
      action.status = 'Completed';
      console.log(`Action "${action.title}" automatically set to Completed - all sub-actions completed`);
    } else if (anyInProgress && action.status === 'Not Started') {
      action.status = 'In Progress';
      console.log(`Action "${action.title}" automatically set to In Progress - sub-actions started`);
    } else if (anyStarted && action.status === 'Not Started') {
      action.status = 'In Progress';
      console.log(`Action "${action.title}" automatically set to In Progress - work has begun`);
    }

    // Track if status changed for backend update
    action.statusChanged = action.status !== previousStatus;
  }

  // Update action status in backend when it changes automatically
  updateActionStatusInBackend(action: AssignmentAction): void {
    if (action.statusChanged) {
      this.actionsService.updateActionStatus(action.id, action.status).subscribe({
        next: () => {
          console.log(`Action ${action.id} status updated to ${action.status} in backend`);
          action.statusChanged = false; // Reset flag
        },
        error: (error) => {
          console.error('Error updating action status in backend:', error);
          // Don't show error to user as this is automatic background update
        }
      });
    }
  }

  // Map backend hierarchy field to frontend priority
  mapPriorityFromBackend(hierarchy: string | null | undefined): string {
    if (!hierarchy) return 'Medium';
    
    // Map hierarchy levels to priority levels
    const priorityMap: { [key: string]: string } = {
      'Elimination': 'Critical',
      'Substitution': 'High', 
      'Engineering Controls': 'High',
      'Administrative Controls': 'Medium',
      'Personal Protective Equipment (PPE)': 'Low'
    };
    
    return priorityMap[hierarchy] || 'Medium';
  }

  // Get next status in progression
  getNextStatus(currentStatus: string): 'In Progress' | 'Completed' | null {
    switch (currentStatus) {
      case 'Not Started':
        return 'In Progress';
      case 'In Progress':
        return 'Completed';
      case 'Completed':
        return null; // Cannot advance further
      default:
        return null;
    }
  }

  canAdvanceSubActionStatus(status: string): boolean {
    // Sub-actions can be advanced if they have a next status
    return this.getNextStatus(status) !== null;
  }

  trackByActionId(index: number, action: AssignmentAction): number {
    return action.id;
  }

  trackBySubActionId(index: number, subAction: any): number {
    return subAction.id;
  }

  goHome(): void {
    console.log('🔧 AssignmentsComponent: Going back to home page');
    this.router.navigate(['/']);
  }

  // Validate if company ID actually exists in the system
  validateCompanyExists(companyId: string): void {
    console.log('Validating company ID:', companyId);
    
    // Use the report service validation endpoint
    this.reportService.validateCompanyId(companyId).subscribe({
      next: (response: any) => {
        console.log('Company validation response:', response);
        
        if (!response.isValid) {
          console.log('Company ID does not exist:', companyId);
          this.alertService.showError(response.message || `Company ID "${companyId}" does not exist.`);
          this.loading = false;
          return;
        }
        
        console.log('Company ID exists, full backend response:', response);
        console.log('Backend fields - userId:', response.userId);
        console.log('Backend fields - reporterName:', response.reporterName);
        console.log('Backend fields - department:', response.department);
        console.log('Backend fields - position:', response.position);
        console.log('Backend fields - companyId:', response.companyId);
        
        // Store company user information for display (map backend fields to frontend)
        this.companyUser = {
          userId: response.userId, // Store user ID for sub-action filtering
          fullName: response.reporterName,
          companyId: response.companyId,
          department: response.department,
          position: response.position,
          email: null // Not provided by backend
        };
        
        console.log('Final companyUser object:', this.companyUser);
        
        // Company exists, proceed to load assignments
        this.loadAllActionsForAssignments();
      },
      error: (error) => {
        console.error('Error validating company:', error);
        if (error.status === 404) {
          this.alertService.showError(`Company ID "${companyId}" does not exist.`);
        } else {
          this.alertService.showError('Failed to validate company ID. Please try again.');
        }
        this.loading = false;
      }
    });
  }
  
  // Load sub-actions assigned to this user, then group by corrective actions
  loadAllActionsForAssignments(): void {
    console.log('Loading sub-actions assigned to user:', this.companyUser?.userId);
    
    if (!this.companyUser?.userId) {
      console.error('No user ID available for loading assignments');
      this.alertService.showError('User ID not found. Please try again.');
      this.loading = false;
      return;
    }

    console.log('🔍 Calling backend with userId:', this.companyUser.userId);
    console.log('🔍 CompanyUser object:', this.companyUser);
    
    this.subActionsService.getSubActionsByAssignedUser(this.companyUser.userId).subscribe({
      next: (assignedSubActions) => {
        console.log('✅ Backend response - Sub-actions assigned to user:', assignedSubActions);
        console.log('✅ Response length:', assignedSubActions.length);
        this.processAssignedSubActions(assignedSubActions);
      },
      error: (error) => {
        console.error('Error loading assigned sub-actions:', error);
        this.alertService.showError('Failed to load assignments. Please try again.');
        this.loading = false;
      }
    });
  }
}