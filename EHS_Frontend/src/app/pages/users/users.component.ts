import { Component, HostListener, Renderer2, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { RegistrationService } from '../../services/registration.service';
import { AlertService } from '../../services/alert.service';
import { UserDto, CreateUserDto } from '../../models/auth.models';
import { Subscription } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { UserModalComponent } from '../../components/user-modal/user-modal.component';
import { ConfirmationDialogComponent } from '../../components/confirmation-dialog/confirmation-dialog.component';
import { EditUserModalComponent, EditUserFormData } from '../../components/edit-user-modal/edit-user-modal.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, UserModalComponent, ConfirmationDialogComponent, EditUserModalComponent],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class UsersComponent implements OnInit, OnDestroy {
  searchQuery: string = '';
  selectedRole: string = '';
  selectedDepartment: string = '';
  users: UserDto[] = [];
  allUsers: UserDto[] = [];
  roles: string[] = [];
  departments: string[] = [];
  dropdownOpen = false;
  sidebarOpen = false;
  loading = true;
  error: string | null = null;
  
  // Add User Modal properties
  showAddUserModal = false;
  addUserLoading = false;
  addUserError: string | null = null;
  
  // Edit User Modal properties
  showEditUserModal = false;
  editUserLoading = false;
  editUserError: string | null = null;
  userToEdit: UserDto | null = null;
  
  // Delete User Confirmation Dialog properties
  showDeleteConfirmation = false;
  userToDelete: UserDto | null = null;
  
  // Role Change Confirmation Dialog properties
  showRoleChangeConfirmation = false;
  userForRoleChange: UserDto | null = null;
  originalRole: string = '';
  newRole: string = '';
  
  // User Status Management properties
  showDeactivateConfirmation = false;
  userToDeactivate: UserDto | null = null;
  togglingUsers: Set<string> = new Set(); // Track users being toggled
  
  // Pagination properties
  currentPage: number = 1;
  pageSize: number = 10;
  totalPages: number = 1;
  totalCount: number = 0;

  // Current user for role checking
  currentUser: any = null;
  private subscriptions: Subscription[] = [];
  
  // Access denial state
  accessDenied: boolean = false;
  accessDeniedMessage: string = '';

  // Tab management
  activeTab: string = 'users';

  // User requests properties
  userRequests: any[] = [];
  allUserRequests: any[] = [];
  requestsLoading = false;
  requestsError: string | null = null;
  pendingRequestsCount = 0;
  
  // User requests pagination
  requestsCurrentPage: number = 1;
  requestsPageSize: number = 10;
  requestsTotalPages: number = 1;
  requestsTotalCount: number = 0;

  constructor(
    private renderer: Renderer2, 
    private el: ElementRef,
    private router: Router,
    private authService: AuthService,
    private userService: UserService,
    private registrationService: RegistrationService,
    private http: HttpClient,
    private alertService: AlertService
  ) { }

  ngOnInit(): void {
    // Check access first, and only proceed if access is granted
    if (!this.checkHSEAccess()) {
      return; // Stop here if access is denied
    }

    // Subscribe to current user for role-based features
    const userSubscription = this.authService.currentUser$.subscribe(user => {
      const previousUser = this.currentUser;
      this.currentUser = user;
      
      // If user changed (account switch), refresh the user list immediately
      if (previousUser && user && previousUser.id !== user.id) {
        console.log('🔄 User account switched, refreshing user list immediately');
        this.refreshUserStatus();
      }
    });
    this.subscriptions.push(userSubscription);
    
    this.loadInitialData();
    
    // Set up periodic refresh for user status updates (every 30 seconds)
    const refreshInterval = setInterval(() => {
      this.refreshUserStatus();
    }, 30000);
    
    // Refresh when page becomes visible (user switches back to tab)
    const visibilityHandler = () => {
      if (!document.hidden) {
        console.log('🔄 Page became visible, refreshing user status');
        this.refreshUserStatus();
      }
    };
    document.addEventListener('visibilitychange', visibilityHandler);
    
    // Clean up interval and event listener on destroy
    this.subscriptions.push({
      unsubscribe: () => {
        clearInterval(refreshInterval);
        document.removeEventListener('visibilitychange', visibilityHandler);
      }
    } as any);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  checkHSEAccess(): boolean {
    const currentUser = this.authService.getCurrentUser();
    console.log('🔐 Checking user page access...');
    console.log('🔐 Current user:', currentUser);
    console.log('🔐 User role:', currentUser?.role);
    
    // Block HSE users from accessing the users management page
    if (currentUser?.role === 'HSE') {
      console.error('❌ HSE user attempted to access users page');
      this.accessDenied = true;
      this.accessDeniedMessage = 'Access denied. HSE users cannot access the users management page.';
      // Use setTimeout to ensure the component renders first, then show message and redirect
      setTimeout(() => {
        console.log('🚨 Displaying access denied message for HSE user');
        // After a longer delay, redirect to dashboard
        setTimeout(() => {
          console.log('🔄 Redirecting HSE user to dashboard');
          this.router.navigate(['/dashboard']);
        }, 4000); // 4 second delay to read the message
      }, 100); // Small delay to ensure component renders
      return false; // Access denied
    }
    
    console.log('✅ Users page access granted');
    return true; // Access granted
  }

  private loadInitialData(): void {
    this.loading = true;
    this.error = null;

    // Load metadata (roles)
    this.loadMetadata();
    
    // Load users data
    this.loadUsers();
    
    // Load pending requests count immediately
    this.loadPendingRequestsCount();
  }

  private loadMetadata(): void {
    // Load roles
    const rolesSubscription = this.userService.getRoles().subscribe({
      next: (roles) => {
        this.roles = roles;
      },
      error: (error) => {
        console.error('Failed to load roles:', error);
        // Use default roles as fallback
        this.roles = ['Admin', 'HSE', 'Profil'];
      }
    });

    // Load departments
    const departmentsSubscription = this.userService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
      },
      error: (error) => {
        console.error('Failed to load departments:', error);
        this.departments = [];
      }
    });

    this.subscriptions.push(rolesSubscription);
    this.subscriptions.push(departmentsSubscription);
  }

  private loadUsers(): void {
    const usersSubscription = this.userService.getUsers(
      this.currentPage,
      this.pageSize,
      this.searchQuery || undefined,
      this.selectedRole || undefined,
      this.selectedDepartment || undefined
    ).subscribe({
      next: (response) => {
        this.users = response.users;
        this.allUsers = response.users;
        this.totalPages = response.pagination.totalPages;
        this.totalCount = response.pagination.totalCount;
        this.loading = false;
        console.log('Users loaded:', response);
        if (response.users[0]) {
          console.log('First user:', response.users[0]);
          console.log('First user companyId:', response.users[0].companyId);
        }
      },
      error: (error) => {
        console.error('Failed to load users:', error);
        this.error = 'Failed to load users';
        this.loading = false;
        this.users = [];
        this.allUsers = [];
      }
    });

    this.subscriptions.push(usersSubscription);
  }

  get paginatedUsers(): UserDto[] {
    // Backend now handles sorting, so return users directly
    return this.users;
  }

  isCurrentUser(user: UserDto): boolean {
    return user.id === this.currentUser?.id;
  }

  refreshUsers(): void {
    this.loadUsers();
  }


  getDisplayId(user: UserDto): string {
    return user.companyId || 'Not Assigned';
  }

  getUserInitials(user: UserDto): string {
    const firstName = user.firstName || '';
    const lastName = user.lastName || '';
    const initials = (firstName.charAt(0) + lastName.charAt(0)).toUpperCase();
    return initials || '??';
  }

  getAvatarUrl(user: UserDto): string | null {
    if (!user.avatar) return null;
    
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

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadUsers();
    }
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadUsers();
    }
  }

  openUserModal() {
    console.log("Opening add user modal");
    this.showAddUserModal = true;
    this.addUserError = null;
    this.addUserLoading = false;
  }

  closeAddUserModal() {
    this.showAddUserModal = false;
    this.addUserError = null;
    this.addUserLoading = false;
  }

  submitAddUser(formData: any) {
    // Validate all required fields
    if (!formData.email || !formData.fullName || !formData.role || !formData.companyId || !formData.department || !formData.position) {
      this.addUserError = 'Please fill in all required fields (Email, Full Name, Role, Company ID, Department, Position)';
      return;
    }

    this.addUserLoading = true;
    this.addUserError = null;

    const createUserDto: CreateUserDto = {
      email: formData.email,
      fullName: formData.fullName,
      role: formData.role,
      companyId: formData.companyId,
      department: formData.department,
      position: formData.position
    };

    console.log('🔍 Frontend: Form data received:', formData);
    console.log('📤 Frontend: Sending CreateUserDto:', createUserDto);
    console.log('📤 Frontend: JSON payload:', JSON.stringify(createUserDto, null, 2));

    const createSubscription = this.userService.createUser(createUserDto).subscribe({
      next: (response) => {
        console.log('User created successfully:', response);
        this.addUserLoading = false;
        this.closeAddUserModal();
        // Refresh users list
        this.loadUsers();
        // Show success message
        this.alertService.showSuccess(`User created successfully! ${formData.role === 'Profil' ? 'Profile user can now submit reports.' : 'Login credentials sent via email.'}`);
      },
      error: (error) => {
        console.error('❌ Frontend: Failed to create user - Full error:', error);
        console.error('❌ Frontend: Error status:', error.status);
        console.error('❌ Frontend: Error statusText:', error.statusText);
        console.error('❌ Frontend: Error body:', error.error);
        
        if (error.status === 400) {
          console.error('❌ Frontend: 400 Bad Request details:', error.error);
          if (error.error.errors) {
            console.error('❌ Frontend: Validation errors:', error.error.errors);
            Object.keys(error.error.errors).forEach(field => {
              console.error(`❌ Frontend: Field '${field}':`, error.error.errors[field]);
            });
          }
        }
        
        this.addUserError = error.error?.message || `Failed to create user (${error.status}: ${error.statusText})`;
        this.addUserLoading = false;
      }
    });

    this.subscriptions.push(createSubscription);
  }

  editUser(user: UserDto) {
    console.log("Edit user:", user);
    console.log("User ID:", user.id);
    console.log("User object keys:", Object.keys(user));
    this.userToEdit = user;
    this.showEditUserModal = true;
    this.editUserError = null;
    this.editUserLoading = false;
  }

  closeEditUserModal() {
    this.showEditUserModal = false;
    this.editUserError = null;
    this.editUserLoading = false;
    this.userToEdit = null;
  }

  submitEditUser(formData: EditUserFormData) {
    if (!this.userToEdit) return;
    
    console.log('Edit user form data:', formData);
    console.log('User to edit:', this.userToEdit);
    console.log('User to edit ID:', this.userToEdit.id);
    
    // Check if user has Company ID
    if (!this.userToEdit.companyId) {
      this.editUserError = 'Cannot edit this user - missing Company ID.';
      return;
    }
    
    console.log('Company ID being used for API call:', this.userToEdit.companyId);
    console.log('User object companyId field:', this.userToEdit.companyId);
    console.log('All user object properties:', Object.keys(this.userToEdit));
    
    // Test the routing first
    console.log('Testing Company ID routing...');
    this.userService.testCompanyIdRouting(this.userToEdit.companyId!).subscribe({
      next: (response) => {
        console.log('✅ Company ID routing test successful:', response);
      },
      error: (error) => {
        console.error('❌ Company ID routing test failed:', error);
      }
    });
    
    this.editUserLoading = true;
    this.editUserError = null;
    
    // Prepare FormData for API call
    const apiFormData = new FormData();
    
    if (formData.fullName) {
      apiFormData.append('FullName', formData.fullName);
    }
    
    if (formData.dateOfBirth) {
      apiFormData.append('DateOfBirth', formData.dateOfBirth);
    }
    
    // Always send position and department, even if empty
    apiFormData.append('Position', formData.position || '');
    apiFormData.append('Department', formData.department || '');
    
    if (formData.avatar) {
      apiFormData.append('Avatar', formData.avatar);
    }
    
    // Make profile update API call using Company ID
    const profileUpdateSubscription = this.userService.updateUserProfile(this.userToEdit.companyId!, apiFormData).subscribe({
      next: (response) => {
        console.log('User profile updated successfully:', response);
        this.editUserLoading = false;
        
        // If password change was requested, use admin reset (since only admins change passwords)
        if (formData.newPassword) {
          this.adminResetPassword(formData.newPassword);
        } else {
          this.closeEditUserModal();
          this.alertService.showSuccess('User profile updated successfully!');
          this.loadUsers(); // Refresh users list
        }
      },
      error: (error) => {
        console.error('Failed to update user profile:', error);
        this.editUserError = error.error?.message || 'Failed to update user profile';
        this.editUserLoading = false;
      }
    });
    
    this.subscriptions.push(profileUpdateSubscription);
  }
  

  private adminResetPassword(newPassword: string) {
    if (!this.userToEdit) return;
    
    const resetData = {
      newPassword: newPassword
    };
    
    console.log('🔑 Frontend: Admin reset password data:', {
      companyId: this.userToEdit.companyId,
      hasNewPassword: !!newPassword,
      newPasswordLength: newPassword?.length
    });
    
    const resetSubscription = this.userService.adminResetPassword(this.userToEdit.companyId!, resetData).subscribe({
      next: (response) => {
        console.log('Password reset successfully by admin:', response);
        this.editUserLoading = false;
        this.closeEditUserModal();
        this.alertService.showSuccess('Password reset successfully by admin!');
        this.loadUsers(); // Refresh users list
      },
      error: (error) => {
        console.error('Failed to reset password:', error);
        console.error('Admin reset password error details:', {
          status: error.status,
          statusText: error.statusText,
          message: error.error?.message,
          errors: error.error?.errors
        });
        
        // Show specific error message
        if (error.error?.errors && Array.isArray(error.error.errors)) {
          this.editUserError = `Password reset failed: ${error.error.errors.join(', ')}`;
        } else if (error.error?.message) {
          this.editUserError = `Password reset failed: ${error.error.message}`;
        } else {
          this.editUserError = 'Profile updated but failed to reset password. Please try again.';
        }
        
        this.editUserLoading = false;
      }
    });
    
    this.subscriptions.push(resetSubscription);
  }

  deleteUser(companyId: string) {
    // Find the user to store for deletion
    const user = this.users.find(u => u.companyId === companyId);
    if (user) {
      this.userToDelete = user;
      this.showDeleteConfirmation = true;
    }
  }

  confirmDeleteUser() {
    if (!this.userToDelete) return;
    
    const user = this.userToDelete;
    const userName = user.fullName || `${user.firstName} ${user.lastName}`.trim();
    
    const deleteSubscription = this.userService.deleteUser(user.companyId!).subscribe({
      next: (response) => {
        console.log("User deleted successfully:", response);
        this.alertService.showSuccess(`${userName} has been successfully deleted from the system.`);
        // Reload users after deletion
        this.loadUsers();
        // Reset delete state
        this.userToDelete = null;
        this.showDeleteConfirmation = false;
      },
      error: (error) => {
        console.error("Failed to delete user:", error);
        this.alertService.showError(`Failed to delete ${userName}. ${error.error?.message || 'Please try again or contact support.'}`);
        this.error = 'Failed to delete user';
        // Reset delete state
        this.userToDelete = null;
        this.showDeleteConfirmation = false;
      }
    });
    this.subscriptions.push(deleteSubscription);
  }

  cancelDeleteUser() {
    this.userToDelete = null;
    this.showDeleteConfirmation = false;
  }

  onRoleSelectionChange(user: UserDto, event: any) {
    const newRole = event.target.value;
    const originalRole = this.allUsers.find(u => u.id === user.id)?.role || user.role || '';
    
    // If the role hasn't actually changed, don't show confirmation
    if (originalRole === newRole) {
      return;
    }
    
    // Store the user and role information for confirmation
    this.userForRoleChange = { ...user };
    this.originalRole = originalRole;
    this.newRole = newRole;
    this.showRoleChangeConfirmation = true;
    
    // Reset the dropdown to original role in case user cancels
    // We need to reset the model after Angular processes the change
    setTimeout(() => {
      user.role = originalRole;
    }, 0);
  }

  updateUserRole(user: UserDto) {
    // This method is kept for compatibility but now we use onRoleSelectionChange
    console.log('updateUserRole called - this should not happen with new implementation');
  }

  confirmRoleChange() {
    if (!this.userForRoleChange) return;
    
    const user = this.userForRoleChange;
    const oldRole = this.originalRole || '';
    const newRole = this.newRole || '';
    const userName = user.fullName || `${user.firstName || ''} ${user.lastName || ''}`.trim();
    
    console.log(`Updating role for ${userName} from ${oldRole} to ${newRole}`);
    
    const updateSubscription = this.userService.updateUserRole(user.id, { role: newRole }).subscribe({
      next: (response) => {
        console.log(`Role updated successfully for ${userName}:`, response);
        
        // Update the role in the local users array immediately
        const userIndex = this.users.findIndex(u => u.id === user.id);
        if (userIndex !== -1) {
          this.users[userIndex].role = newRole;
        }
        const allUserIndex = this.allUsers.findIndex(u => u.id === user.id);
        if (allUserIndex !== -1) {
          this.allUsers[allUserIndex].role = newRole;
        }
        
        // Show appropriate message based on role change
        if (newRole === 'Profil' && (oldRole === 'HSE' || oldRole === 'Admin')) {
          this.alertService.showWarning(`${userName}'s role changed to Profile. They can no longer login but can still submit reports.`);
        } else if ((newRole === 'HSE' || newRole === 'Admin') && oldRole === 'Profil') {
          this.alertService.showSuccess(`${userName}'s role upgraded to ${newRole}. New login credentials sent via email.`);
        } else {
          this.alertService.showSuccess(`${userName}'s role changed to ${newRole}. Updated credentials sent via email.`);
        }
        
        // Reset to page 1 after role change to ensure user can see the updated user
        // Since we have client-side sorting combined with server-side pagination,
        // a role change might move the user to a different position in the sorted list
        console.log(`Role changed from ${oldRole} to ${newRole}, resetting to page 1`);
        this.currentPage = 1;
        
        // Reload users to get fresh data and ensure proper sorting display
        this.loadUsers();
        
        // Reset role change state
        this.resetRoleChangeState();
      },
      error: (error) => {
        console.error("Failed to update user role:", error);
        this.alertService.showError(`Failed to change ${userName}'s role. ${error.error?.message || 'Please try again or contact support.'}`);
        this.error = error.error?.message || 'Failed to update user role';
        // Reset role change state
        this.resetRoleChangeState();
      }
    });
    this.subscriptions.push(updateSubscription);
  }

  cancelRoleChange() {
    // Simply reset the state - the UI should already show the correct original role
    this.resetRoleChangeState();
  }

  private resetRoleChangeState() {
    this.userForRoleChange = null;
    this.originalRole = '';
    this.newRole = '';
    this.showRoleChangeConfirmation = false;
  }

  getRoleChangeWarningMessage(): string {
    if (!this.userForRoleChange || !this.originalRole || !this.newRole) {
      return '';
    }

    const newRole = this.newRole || '';
    const originalRole = this.originalRole || '';

    // Downgrade warnings
    if (newRole === 'Profil' && (originalRole === 'HSE' || originalRole === 'Admin')) {
      return 'This will remove their login access and they will only be able to submit reports.';
    }
    
    // Upgrade notifications
    if ((newRole === 'HSE' || newRole === 'Admin') && originalRole === 'Profil') {
      return 'New login credentials will be automatically generated and sent via email.';
    }
    
    // Role change between login roles
    if ((newRole === 'HSE' || newRole === 'Admin') && (originalRole === 'HSE' || originalRole === 'Admin')) {
      return 'New credentials will be generated and sent via email due to permission changes.';
    }

    return 'This will update their system permissions accordingly.';
  }

  getRoleChangeConfirmationMessage(): string {
    if (!this.userForRoleChange) {
      return 'Are you sure you want to change this user role?';
    }

    const user = this.userForRoleChange;
    const userName = user.fullName || `${user.firstName || ''} ${user.lastName || ''}`.trim() || 'this user';
    const originalRole = this.originalRole || '';
    const newRole = this.newRole || '';
    const warningMessage = this.getRoleChangeWarningMessage();

    return `Are you sure you want to change ${userName}'s role from ${originalRole} to ${newRole}? ${warningMessage}`;
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  toggleSidebar() {
    this.sidebarOpen = !this.sidebarOpen;
    const sidebar = this.el.nativeElement.querySelector('.sidebar');
    if (this.sidebarOpen) {
      this.renderer.addClass(sidebar, 'open');
    } else {
      this.renderer.removeClass(sidebar, 'open');
    }
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

  logout() {
    console.log("Logging out...");
  }

  // Tab switching methods
  switchToUsersTable() {
    this.activeTab = 'users';
  }

  switchToRequestsTable() {
    this.activeTab = 'requests';
    this.loadUserRequests();
  }

  // Tab state helpers
  isUsersTab(): boolean {
    return this.activeTab === 'users';
  }

  isRequestsTab(): boolean {
    return this.activeTab === 'requests';
  }

  // User requests methods
  private loadUserRequests() {
    this.requestsLoading = true;
    this.requestsError = null;
    
    console.log('Loading user requests...');

    const requestsSubscription = this.registrationService.getPendingRequests().subscribe({
      next: (requests: any[]) => {
        console.log('Requests loaded from database:', requests);
        this.allUserRequests = requests || [];
        this.requestsTotalCount = this.allUserRequests.length;
        this.requestsTotalPages = Math.ceil(this.requestsTotalCount / this.requestsPageSize);
        
        // Apply pagination
        this.updateRequestsPagination();
        
        // Count only pending requests from the received data
        this.pendingRequestsCount = this.allUserRequests.filter((r: any) => r.status === 'Pending').length;
        this.requestsLoading = false;
      },
      error: (error: any) => {
        console.error('Failed to load requests from database:', error);
        this.requestsError = `Failed to load user requests: ${error.message || error.toString()}`;
        this.requestsLoading = false;
        this.userRequests = [];
        this.allUserRequests = [];
        this.pendingRequestsCount = 0;
        this.requestsTotalCount = 0;
        this.requestsTotalPages = 1;
      }
    });

    this.subscriptions.push(requestsSubscription);
  }

  private loadPendingRequestsCount() {
    const countSubscription = this.registrationService.getPendingRequestsCount().subscribe({
      next: (response: {count: number}) => {
        this.pendingRequestsCount = response.count;
        console.log('Pending requests count:', this.pendingRequestsCount);
      },
      error: (error: any) => {
        console.error('Failed to load pending requests count:', error);
        this.pendingRequestsCount = 0;
      }
    });

    this.subscriptions.push(countSubscription);
  }

  refreshRequests() {
    this.requestsCurrentPage = 1; // Reset to first page
    this.loadUserRequests();
    this.loadPendingRequestsCount();
  }

  approveRequest(requestId: string) {
    console.log(`🔍 Frontend: Attempting to approve request ID: ${requestId}`);
    
    // Find and mark request as processing
    const request = this.userRequests.find(r => r.id === requestId);
    if (request) {
      console.log(`📋 Frontend: Found request in list: ${request.fullName} (${request.email}) - Status: ${request.status}`);
      request.processing = true;
    } else {
      console.error(`❌ Frontend: Request with ID ${requestId} not found in userRequests list`);
    }

    console.log(`📡 Frontend: Sending approve request to backend...`);
    const approveSubscription = this.registrationService.approveRequest(requestId).subscribe({
      next: (response: any) => {
        console.log('✅ Frontend: Request approved successfully:', response);
        // Refresh all data: user requests, count, and main users table
        console.log('🔄 Frontend: Refreshing user requests, count, and users table...');
        this.requestsCurrentPage = 1; // Reset pagination after approval
        this.loadUserRequests();
        this.loadPendingRequestsCount();
        this.loadUsers(); // Refresh main users table
      },
      error: (error: any) => {
        console.error('❌ Frontend: Failed to approve request:', error);
        console.log('📊 Frontend: Full error object:', JSON.stringify(error, null, 2));
        
        if (request) {
          request.processing = false;
        }
        // Show specific error message if available
        const errorMessage = error.error?.message || 'Failed to approve request';
        console.log(`💬 Frontend: Error message to display: ${errorMessage}`);
        this.requestsError = errorMessage;
        
        // Refresh all data to ensure we have the latest status
        console.log('🔄 Frontend: Refreshing user requests, count, and users table after error...');
        this.requestsCurrentPage = 1; // Reset pagination after error
        this.loadUserRequests();
        this.loadPendingRequestsCount();
        this.loadUsers(); // Refresh main users table
      }
    });

    this.subscriptions.push(approveSubscription);
  }

  rejectRequest(requestId: string) {
    // Find and mark request as processing
    const request = this.userRequests.find(r => r.id === requestId);
    if (request) {
      request.processing = true;
    }

    const rejectSubscription = this.registrationService.rejectRequest(requestId).subscribe({
      next: (response: any) => {
        console.log('Request rejected:', response);
        // Refresh all data: user requests, count, and main users table
        this.requestsCurrentPage = 1; // Reset pagination after rejection
        this.loadUserRequests();
        this.loadPendingRequestsCount();
        this.loadUsers(); // Refresh main users table
      },
      error: (error: any) => {
        console.error('Failed to reject request:', error);
        if (request) {
          request.processing = false;
        }
        // Show specific error message if available
        const errorMessage = error.error?.message || 'Failed to reject request';
        this.requestsError = errorMessage;
        
        // Refresh all data to ensure we have the latest status
        this.requestsCurrentPage = 1; // Reset pagination after rejection error
        this.loadUserRequests();
        this.loadPendingRequestsCount();
        this.loadUsers(); // Refresh main users table
      }
    });

    this.subscriptions.push(rejectSubscription);
  }

  // Filter methods
  onSearchChange(): void {
    // Reset to first page when searching
    this.currentPage = 1;
    this.loadUsers();
  }

  applyFilters(): void {
    // Reset to first page when applying filters
    this.currentPage = 1;
    this.loadUsers();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedRole = '';
    this.selectedDepartment = '';
    this.currentPage = 1;
    this.loadUsers();
  }

  // Refresh user status without showing loading indicator
  refreshUserStatus(): void {
    const usersSubscription = this.userService.getUsers(
      this.currentPage,
      this.pageSize,
      this.searchQuery || undefined,
      this.selectedRole || undefined,
      this.selectedDepartment || undefined
    ).subscribe({
      next: (response) => {
        // Only update the users array, don't change loading state
        this.users = response.users;
        this.allUsers = response.users;
        this.totalPages = response.pagination.totalPages;
        this.totalCount = response.pagination.totalCount;
        console.log('User status refreshed silently');
      },
      error: (error) => {
        console.error('Failed to refresh user status:', error);
        // Don't show error to user for background refresh
      }
    });
    this.subscriptions.push(usersSubscription);
  }

  // User requests pagination methods
  private updateRequestsPagination(): void {
    const startIndex = (this.requestsCurrentPage - 1) * this.requestsPageSize;
    const endIndex = startIndex + this.requestsPageSize;
    this.userRequests = this.allUserRequests.slice(startIndex, endIndex);
  }

  requestsPrevPage(): void {
    if (this.requestsCurrentPage > 1) {
      this.requestsCurrentPage--;
      this.updateRequestsPagination();
    }
  }

  requestsNextPage(): void {
    if (this.requestsCurrentPage < this.requestsTotalPages) {
      this.requestsCurrentPage++;
      this.updateRequestsPagination();
    }
  }

  get paginatedUserRequests(): any[] {
    return this.userRequests;
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'Approved':
        return 'bg-green-100 text-green-800';
      case 'Rejected':
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  // User Status Management Methods
  toggleUserStatus(user: UserDto, event: any): void {
    // Prevent toggle if already processing
    if (this.togglingUsers.has(user.id)) {
      event.preventDefault();
      return;
    }

    const isActivating = event.target.checked;
    
    // Reset the checkbox to its original state until operation completes
    event.target.checked = user.isActive;
    
    if (isActivating) {
      this.activateUser(user);
    } else {
      this.deactivateUser(user);
    }
  }

  isUserToggling(user: UserDto): boolean {
    return this.togglingUsers.has(user.id);
  }

  activateUser(user: UserDto): void {
    if (!user.id) return;
    
    const userName = user.fullName || `${user.firstName} ${user.lastName}`.trim();
    
    // Add to toggling set
    this.togglingUsers.add(user.id);
    
    const activateSubscription = this.userService.activateUser(user.id).subscribe({
      next: (response) => {
        console.log('User activated successfully:', response);
        this.alertService.showSuccess(`${userName} has been activated successfully.`);
        
        // Update local user object
        user.isActive = true;
        user.deactivatedAt = undefined;
        
        // Remove from toggling set
        this.togglingUsers.delete(user.id);
        
        // Refresh the users list to get fresh data
        this.loadUsers();
      },
      error: (error) => {
        console.error('Failed to activate user:', error);
        this.alertService.showError(`Failed to activate ${userName}. ${error.error?.message || 'Please try again.'}`);
        
        // Remove from toggling set on error
        this.togglingUsers.delete(user.id);
      }
    });
    
    this.subscriptions.push(activateSubscription);
  }

  deactivateUser(user: UserDto): void {
    // Add to toggling set to show loading state
    this.togglingUsers.add(user.id);
    
    this.userToDeactivate = user;
    this.showDeactivateConfirmation = true;
  }

  confirmDeactivateUser(): void {
    if (!this.userToDeactivate || !this.userToDeactivate.id) return;
    
    const user = this.userToDeactivate;
    const userName = user.fullName || `${user.firstName} ${user.lastName}`.trim();
    
    const deactivateSubscription = this.userService.deactivateUser(user.id).subscribe({
      next: (response) => {
        console.log('User deactivated successfully:', response);
        this.alertService.showSuccess(`${userName} has been deactivated successfully.`);
        
        // Update local user object
        user.isActive = false;
        user.deactivatedAt = new Date().toISOString();
        
        // Remove from toggling set
        this.togglingUsers.delete(user.id);
        
        // Reset deactivation state
        this.resetDeactivationState();
        
        // Refresh the users list to get fresh data
        this.loadUsers();
      },
      error: (error) => {
        console.error('Failed to deactivate user:', error);
        this.alertService.showError(`Failed to deactivate ${userName}. ${error.error?.message || 'Please try again.'}`);
        
        // Remove from toggling set on error
        this.togglingUsers.delete(user.id);
        this.resetDeactivationState();
      }
    });
    
    this.subscriptions.push(deactivateSubscription);
  }

  cancelDeactivateUser(): void {
    // Remove from toggling set when cancelled
    if (this.userToDeactivate?.id) {
      this.togglingUsers.delete(this.userToDeactivate.id);
    }
    this.resetDeactivationState();
  }

  private resetDeactivationState(): void {
    this.userToDeactivate = null;
    this.showDeactivateConfirmation = false;
  }

  getDeactivationConfirmationMessage(): string {
    if (!this.userToDeactivate) {
      return 'Are you sure you want to deactivate this user?';
    }

    const userName = this.userToDeactivate.fullName || 
                    `${this.userToDeactivate.firstName || ''} ${this.userToDeactivate.lastName || ''}`.trim() || 
                    'this user';

    return `Are you sure you want to deactivate ${userName}? They will not be able to login until reactivated by an administrator.`;
  }
}
