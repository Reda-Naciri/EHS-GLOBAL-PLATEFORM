import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { NotificationService } from '../../services/notification.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { AlertService } from '../../services/alert.service';
import { NotificationDto } from '../../models/notification.models';
import { UserDto } from '../../models/auth.models';
import { EditUserModalComponent, EditUserFormData } from '../edit-user-modal/edit-user-modal.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, EditUserModalComponent],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Input() toggleSidebar!: () => void;
  @Input() pageTitle: string = '';

  // Dropdown states
  userDropdownOpen = false;
  notificationDropdownOpen = false;

  // Notification data
  notifications: NotificationDto[] = [];
  unreadCount = 0;
  
  // User data
  currentUser: UserDto | null = null;
  cachedAvatarUrl: string | null = null;

  // Edit Profile Modal properties
  showEditProfileModal = false;
  editProfileLoading = false;
  editProfileError: string | null = null;

  // Subscriptions
  private notificationSubscription?: Subscription;
  private unreadCountSubscription?: Subscription;
  private userSubscription?: Subscription;

  constructor(
    private notificationService: NotificationService,
    private authService: AuthService,
    private userService: UserService,
    private alertService: AlertService,
    private router: Router
  ) {}

  ngOnInit() {
    // Subscribe to current user
    this.userSubscription = this.authService.currentUser$.subscribe(user => {
      console.log('🔍 NAVBAR: Current user updated:', user);
      console.log('🔍 NAVBAR: User avatar property:', user?.avatar);
      console.log('🔍 NAVBAR: User email:', user?.email);
      this.currentUser = user;
      this.cachedAvatarUrl = user ? this.getAvatarUrl(user) : null;
    });

    // Subscribe to notifications
    this.notificationSubscription = this.notificationService.notifications$.subscribe(notifications => {
      console.log('🔔 NAVBAR: Received notifications:', notifications);
      console.log('🔔 NAVBAR: Notifications count:', notifications.length);
      this.notifications = notifications.slice(0, 5); // Show only 5 most recent
      console.log('🔔 NAVBAR: Displaying notifications:', this.notifications);
    });

    // Subscribe to unread count
    this.unreadCountSubscription = this.notificationService.unreadCount$.subscribe(count => {
      console.log('🔔 NAVBAR: Unread count updated:', count);
      this.unreadCount = count;
    });
    
    // Force refresh user session to get updated avatar data from backend
    console.log('🔄 NAVBAR: Forcing session validation to get fresh user data including avatar');
    setTimeout(() => {
      this.authService.validateSession();
    }, 1000);
  }

  ngOnDestroy() {
    this.notificationSubscription?.unsubscribe();
    this.unreadCountSubscription?.unsubscribe();
    this.userSubscription?.unsubscribe();
  }

  // Toggle dropdowns
  toggleUserDropdown() {
    this.userDropdownOpen = !this.userDropdownOpen;
    if (this.userDropdownOpen) {
      this.notificationDropdownOpen = false;
    }
  }

  toggleNotificationDropdown() {
    this.notificationDropdownOpen = !this.notificationDropdownOpen;
    if (this.notificationDropdownOpen) {
      this.userDropdownOpen = false;
    }
  }

  // Close all dropdowns
  closeDropdowns() {
    this.userDropdownOpen = false;
    this.notificationDropdownOpen = false;
  }

  // Notification actions
  markNotificationAsRead(notification: NotificationDto) {
    // Mark as read first
    if (!notification.isRead) {
      this.notificationService.markAsReadLocal(notification.id);
    }
    
    // Handle redirection if redirectUrl is provided
    if (notification.redirectUrl) {
      this.router.navigateByUrl(notification.redirectUrl);
      this.notificationDropdownOpen = false; // Close dropdown
    }
  }

  markAllAsRead() {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notificationService.refreshNotifications();
      }
    });
  }

  // User actions
  logout() {
    this.authService.logout();
  }

  editProfile() {
    if (this.currentUser) {
      this.showEditProfileModal = true;
      this.editProfileError = null;
      this.editProfileLoading = false;
      this.closeDropdowns(); // Close the dropdown when opening modal
    }
  }

  closeEditProfileModal() {
    this.showEditProfileModal = false;
    this.editProfileError = null;
    this.editProfileLoading = false;
  }

  submitEditProfile(formData: EditUserFormData) {
    if (!this.currentUser || !this.currentUser.companyId) {
      this.editProfileError = 'Unable to update profile - user information missing.';
      return;
    }
    
    this.editProfileLoading = true;
    this.editProfileError = null;
    
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
    const profileUpdateSubscription = this.userService.updateUserProfile(this.currentUser.companyId, apiFormData).subscribe({
      next: (response) => {
        console.log('Profile updated successfully:', response);
        this.editProfileLoading = false;
        
        // If password change was requested, use admin reset (only for non-Profile users)
        if (formData.newPassword && this.currentUser?.role !== 'Profil') {
          this.adminResetPassword(formData.newPassword, formData);
        } else {
          this.closeEditProfileModal();
          this.alertService.showSuccess('Profile updated successfully!');
          
          // Immediately update current user object if form data contains changes
          if (this.currentUser && formData.fullName) {
            this.currentUser.fullName = formData.fullName;
            
            // Split fullName back to firstName and lastName for compatibility
            const nameParts = formData.fullName.trim().split(' ');
            if (nameParts.length > 0) {
              this.currentUser.firstName = nameParts[0];
              this.currentUser.lastName = nameParts.slice(1).join(' ');
            }
          }
          
          if (this.currentUser && formData.dateOfBirth) {
            this.currentUser.dateOfBirth = new Date(formData.dateOfBirth);
          }
          
          // Update position and department
          if (this.currentUser) {
            this.currentUser.position = formData.position || '';
            this.currentUser.department = formData.department || '';
          }
          
          // Refresh user data from server to get updated avatar URL
          this.authService.validateSession();
        }
      },
      error: (error) => {
        console.error('Failed to update profile:', error);
        this.editProfileError = error.error?.message || 'Failed to update profile';
        this.editProfileLoading = false;
      }
    });
  }

  private adminResetPassword(newPassword: string, formData?: EditUserFormData) {
    if (!this.currentUser || !this.currentUser.companyId) return;
    
    const resetData = {
      newPassword: newPassword
    };
    
    const resetSubscription = this.userService.adminResetPassword(this.currentUser.companyId, resetData).subscribe({
      next: (response) => {
        console.log('Password reset successfully:', response);
        this.editProfileLoading = false;
        this.closeEditProfileModal();
        this.alertService.showSuccess('Profile and password updated successfully!');
        
        // Immediately update current user object if form data contains changes
        if (this.currentUser && formData) {
          if (formData.fullName) {
            this.currentUser.fullName = formData.fullName;
            
            // Split fullName back to firstName and lastName for compatibility
            const nameParts = formData.fullName.trim().split(' ');
            if (nameParts.length > 0) {
              this.currentUser.firstName = nameParts[0];
              this.currentUser.lastName = nameParts.slice(1).join(' ');
            }
          }
          
          if (formData.dateOfBirth) {
            this.currentUser.dateOfBirth = new Date(formData.dateOfBirth);
          }
          
          // Update position and department
          this.currentUser.position = formData.position || '';
          this.currentUser.department = formData.department || '';
        }
        
        // Refresh user data from server to get updated avatar URL
        this.authService.validateSession();
      },
      error: (error) => {
        console.error('Failed to reset password:', error);
        
        // Show specific error message
        if (error.error?.errors && Array.isArray(error.error.errors)) {
          this.editProfileError = `Profile updated but password reset failed: ${error.error.errors.join(', ')}`;
        } else if (error.error?.message) {
          this.editProfileError = `Profile updated but password reset failed: ${error.error.message}`;
        } else {
          this.editProfileError = 'Profile updated but failed to reset password. Please try again.';
        }
        
        this.editProfileLoading = false;
      }
    });
  }

  // Helper methods
  getUserDisplayName(): string {
    if (!this.currentUser) return 'User';
    return `${this.currentUser.firstName} ${this.currentUser.lastName}`.trim() || this.currentUser.email;
  }

  getUserInitials(): string {
    if (!this.currentUser) return 'U';
    const firstName = this.currentUser.firstName || '';
    const lastName = this.currentUser.lastName || '';
    return (firstName.charAt(0) + lastName.charAt(0)).toUpperCase() || this.currentUser.email.charAt(0).toUpperCase();
  }

  getNotificationIcon(type: string): any {
    const icon = this.notificationService.getNotificationIcon(type);
    console.log(`🔔 NAVBAR: Icon for type "${type}":`, icon);
    return icon;
  }

  getRelativeTime(date: Date): string {
    return this.notificationService.getRelativeTime(date);
  }

  getTypeColor(type: string): any {
    const color = this.notificationService.getTypeColor(type);
    console.log(`🎨 NAVBAR: Color for type "${type}":`, color);
    return color;
  }

  getAvatarUrl(user: UserDto): string | null {
    console.log('🔍 NAVBAR getAvatarUrl called with:', user);
    console.log('🔍 NAVBAR avatar value:', user?.avatar);
    
    if (!user.avatar) {
      console.log('❌ NAVBAR: No avatar found, returning null');
      return null;
    }
    
    // Get the base URL from environment (remove /api suffix)
    const baseUrl = environment.apiUrl.replace('/api', '');
    let result: string;
    
    // If it's already a full URL, return as is
    if (user.avatar.startsWith('http://') || user.avatar.startsWith('https://')) {
      result = user.avatar;
    }
    // If it's a relative URL, prepend the backend server URL
    else if (user.avatar.startsWith('/')) {
      result = `${baseUrl}${user.avatar}`;
    }
    // If it doesn't start with /, assume it's a filename and construct the full path
    else {
      result = `${baseUrl}/uploads/avatars/${user.avatar}`;
    }
    
    console.log('✅ NAVBAR returning avatar URL:', result);
    return result;
  }
}
