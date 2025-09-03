import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserDto } from '../../models/auth.models';
import { SubActionDetailDto, ActionSummaryDto, CorrectiveActionSummaryDto } from '../../models/report.models';

@Component({
  selector: 'app-sub-action-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sub-action-modal.component.html',
  styleUrls: ['./sub-action-modal.component.css']
})
export class SubActionModalComponent implements OnInit {
  @Input() show: boolean = false;
  @Input() mode: 'view' | 'create' = 'create'; // New input for modal mode
  @Input() action: ActionSummaryDto | CorrectiveActionSummaryDto | null = null; // Parent action
  @Input() subActions: SubActionDetailDto[] = []; // Existing sub-actions for view mode
  @Input() isLoading: boolean = false;
  @Input() error: string | null = null;
  @Input() statusOptions: string[] = [];
  @Input() allUsers: UserDto[] = [];

  @Output() closeModal = new EventEmitter<void>();
  @Output() submitForm = new EventEmitter<any>();

  subActionData = {
    title: '',
    description: '',
    dueDate: '',
    assignedToId: ''
  };

  // View mode controls
  showCreateForm = false;

  // Validation tracking
  showValidationErrors = false;
  validationErrors: string[] = [];

  // User search functionality
  userSearchTerm = '';
  filteredUsers: UserDto[] = [];
  showUserDropdown = false;
  selectedUser: UserDto | null = null;

  ngOnInit() {
    this.resetForm();
  }

  resetForm() {
    this.subActionData = {
      title: '',
      description: '',
      dueDate: '',
      assignedToId: ''
    };
    this.showValidationErrors = false;
    this.validationErrors = [];
    this.clearSelectedUser();
  }

  onClose() {
    this.resetForm();
    this.closeModal.emit();
  }

  onSubmit() {
    this.showValidationErrors = true;
    this.validateForm();
    
    if (this.isFormValid()) {
      this.submitForm.emit(this.subActionData);
    }
  }

  isFormValid(): boolean {
    return this.subActionData.title.trim().length > 0 && 
           this.subActionData.description.trim().length > 0 &&
           this.subActionData.assignedToId.trim().length > 0;
  }

  validateForm(): void {
    this.validationErrors = [];
    
    if (!this.subActionData.title.trim()) {
      this.validationErrors.push('Sub-action title is required');
    }
    
    if (!this.subActionData.description.trim()) {
      this.validationErrors.push('Sub-action description is required');
    }
    
    if (!this.subActionData.assignedToId.trim()) {
      this.validationErrors.push('Assigned user is required');
    }

    // Validate due date doesn't exceed parent action's due date
    if (this.subActionData.dueDate && this.action?.dueDate) {
      const subActionDueDate = new Date(this.subActionData.dueDate);
      const parentActionDueDate = new Date(this.action.dueDate);
      
      if (subActionDueDate > parentActionDueDate) {
        this.validationErrors.push('Sub-action due date cannot exceed parent action deadline');
      }
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    if (!this.showValidationErrors) return false;
    
    switch (fieldName) {
      case 'title':
        return !this.subActionData.title.trim();
      case 'description':
        return !this.subActionData.description.trim();
      case 'assignedTo':
        return !this.subActionData.assignedToId.trim();
      case 'dueDate':
        if (this.subActionData.dueDate && this.action?.dueDate) {
          const subActionDueDate = new Date(this.subActionData.dueDate);
          const parentActionDueDate = new Date(this.action.dueDate);
          return subActionDueDate > parentActionDueDate;
        }
        return false;
      default:
        return false;
    }
  }

  getFieldErrorClass(fieldName: string): string {
    return this.isFieldInvalid(fieldName) ? 'border-red-500 focus:border-red-500 focus:ring-red-200' : '';
  }

  getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  getMaxDueDate(): string | null {
    if (!this.action?.dueDate) return null;
    
    // Convert the parent action's due date to YYYY-MM-DD format for HTML date input
    const dueDate = new Date(this.action.dueDate);
    return dueDate.toISOString().split('T')[0];
  }

  // User search methods
  onUserSearch(event: any): void {
    const searchTerm = event.target.value.toLowerCase();
    this.userSearchTerm = searchTerm;
    
    if (searchTerm.length >= 2) {
      this.filteredUsers = this.allUsers
        .filter(user => 
          (user.fullName && user.fullName.toLowerCase().includes(searchTerm)) ||
          (user.email && user.email.toLowerCase().includes(searchTerm)) ||
          (user.role && user.role.toLowerCase().includes(searchTerm)) ||
          (user.companyId && user.companyId.toLowerCase().includes(searchTerm))
        )
        .slice(0, 10); // Limit to 10 results for performance
      this.showUserDropdown = true;
    } else {
      this.filteredUsers = [];
      this.showUserDropdown = false;
    }
  }

  selectUser(user: UserDto): void {
    this.selectedUser = user;
    this.subActionData.assignedToId = user.id;
    this.userSearchTerm = '';
    this.showUserDropdown = false;
    this.filteredUsers = [];
  }

  clearSelectedUser(): void {
    this.selectedUser = null;
    this.subActionData.assignedToId = '';
    this.userSearchTerm = '';
    this.showUserDropdown = false;
    this.filteredUsers = [];
  }

  onUserInputFocus(): void {
    if (this.selectedUser) {
      // If user is already selected and they click the input, clear it to allow new selection
      this.clearSelectedUser();
    } else if (this.userSearchTerm.length >= 2) {
      this.showUserDropdown = true;
    }
  }

  onUserInputBlur(): void {
    // Delay hiding dropdown to allow for click events
    setTimeout(() => {
      this.showUserDropdown = false;
      // If no user is selected and there's text, clear it to prevent random text
      if (!this.selectedUser && this.userSearchTerm) {
        this.userSearchTerm = '';
      }
    }, 200);
  }

  onUserInputKeydown(event: KeyboardEvent): void {
    // Prevent typing if a user is already selected
    if (this.selectedUser && event.key !== 'Backspace' && event.key !== 'Delete') {
      event.preventDefault();
    }
    
    // Clear selection on backspace/delete
    if (this.selectedUser && (event.key === 'Backspace' || event.key === 'Delete')) {
      this.clearSelectedUser();
      event.preventDefault();
    }
  }

  // View mode methods
  toggleCreateForm(): void {
    this.showCreateForm = !this.showCreateForm;
    if (this.showCreateForm) {
      this.resetForm();
    }
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Not Started': return 'bg-gray-100 text-gray-800';
      case 'In Progress': return 'bg-blue-100 text-blue-800';
      case 'Completed': return 'bg-green-100 text-green-800';
      case 'Aborted': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  getUserDisplayName(assignedToId: string | null | undefined): string {
    if (!assignedToId) return 'Unassigned';
    const user = this.allUsers.find(u => u.id === assignedToId);
    return user ? `${user.fullName} (${user.email})` : 'Unknown User';
  }
}