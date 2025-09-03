import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserDto } from '../../models/auth.models';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface EditUserFormData {
  fullName: string;
  dateOfBirth: string;
  position: string;
  department: string;
  avatar?: File;
  newPassword?: string;
  confirmPassword?: string;
}

export interface Zone {
  id: number;
  name: string;
  code: string;
  description: string;
}

export interface ZoneAssignment {
  zoneId: number;
  zoneName: string;
  zoneCode: string;
  assignedAt: string;
  isActive: boolean;
}

export interface HSEUser {
  id: string;
  email: string;
  fullName: string;
  department: string;
  companyId: string;
}

export interface ZoneDelegation {
  id: number;
  fromHSEUserName: string;
  fromHSEUserEmail: string;
  toHSEUserName: string;
  toHSEUserEmail: string;
  zoneName: string;
  zoneCode: string;
  startDate: string;
  endDate: string;
  reason: string;
  isActive: boolean;
  createdAt: string;
  createdByAdminName: string;
}

@Component({
  selector: 'app-edit-user-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './edit-user-modal.component.html',
  styleUrls: ['./edit-user-modal.component.css']
})
export class EditUserModalComponent implements OnInit, OnChanges {
  @Input() show = false;
  @Input() user: UserDto | null = null;
  @Input() isLoading = false;
  @Input() error: string | null = null;
  @Input() title = 'Edit User';

  @Output() closeModal = new EventEmitter<void>();
  @Output() submitForm = new EventEmitter<EditUserFormData>();

  // Form data
  fullName = '';
  dateOfBirth = '';
  position = '';
  department = '';
  selectedAvatar: File | null = null;
  newPassword = '';
  confirmPassword = '';
  
  // UI state
  showPasswordSection = false;
  avatarPreview: string | null = null;
  
  // Validation
  formErrors: { [key: string]: string } = {};

  // Zone management (only for Admin users editing HSE users)
  showZoneSection = false;
  availableZones: Zone[] = [];
  userZones: ZoneAssignment[] = [];
  selectedZoneId: number | null = null;
  hseUsers: HSEUser[] = [];
  zoneDelegations: ZoneDelegation[] = [];
  showCreateDelegation = false;
  currentUser: UserDto | null = null;
  
  // Custom alert and confirmation system
  showAlert = false;
  showConfirm = false;
  alertConfig = {
    title: '',
    message: '',
    type: 'info' as 'info' | 'success' | 'warning' | 'error'
  };
  confirmConfig = {
    title: '',
    message: '',
    confirmText: 'Confirm',
    cancelText: 'Cancel',
    onConfirm: () => {},
    onCancel: () => {}
  };
  
  // Delegation form
  delegationForm = {
    toHSEUserId: '',
    zoneId: null as number | null,
    startDate: '',
    endDate: '',
    reason: ''
  };

  constructor(
    private authService: AuthService,
    private http: HttpClient
  ) {}


  ngOnInit() {
    this.resetForm();
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['user'] && this.user) {
      this.populateForm();
      this.checkZoneAccess();
    }
    if (changes['show'] && this.show) {
      this.resetForm();
      this.populateForm();
      this.checkZoneAccess();
    }
  }

  private populateForm() {
    if (this.user) {
      this.fullName = this.user.fullName || `${this.user.firstName || ''} ${this.user.lastName || ''}`.trim();
      this.dateOfBirth = this.user.dateOfBirth ? new Date(this.user.dateOfBirth).toISOString().split('T')[0] : '';
      this.position = this.user.position || '';
      this.department = this.user.department || '';
      this.avatarPreview = this.user ? this.getAvatarUrl(this.user) : null;
    }
  }

  private resetForm() {
    this.fullName = '';
    this.dateOfBirth = '';
    this.position = '';
    this.department = '';
    this.selectedAvatar = null;
    this.newPassword = '';
    this.confirmPassword = '';
    this.showPasswordSection = false;
    this.avatarPreview = null;
    this.formErrors = {};
  }

  onAvatarSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.formErrors['avatar'] = 'Please select a valid image file';
        return;
      }
      
      // Validate file size (2MB max)
      if (file.size > 2 * 1024 * 1024) {
        this.formErrors['avatar'] = 'Image size must be less than 2MB';
        return;
      }
      
      this.selectedAvatar = file;
      delete this.formErrors['avatar'];
      
      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.avatarPreview = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  removeAvatar() {
    this.selectedAvatar = null;
    this.avatarPreview = null;
    // Reset file input
    const fileInput = document.getElementById('avatar') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  togglePasswordSection() {
    this.showPasswordSection = !this.showPasswordSection;
    if (!this.showPasswordSection) {
      this.newPassword = '';
      this.confirmPassword = '';
      delete this.formErrors['newPassword'];
      delete this.formErrors['confirmPassword'];
    }
  }

  validateForm(): boolean {
    this.formErrors = {};
    let isValid = true;

    // Validate full name
    if (!this.fullName.trim()) {
      this.formErrors['fullName'] = 'Full name is required';
      isValid = false;
    }

    // Validate date of birth
    if (this.dateOfBirth) {
      const birthDate = new Date(this.dateOfBirth);
      const today = new Date();
      const age = today.getFullYear() - birthDate.getFullYear();
      
      if (age < 16 || age > 100) {
        this.formErrors['dateOfBirth'] = 'Please enter a valid birth date (age 16-100)';
        isValid = false;
      }
    }

    // Validate password section if shown
    if (this.showPasswordSection) {
      if (!this.newPassword) {
        this.formErrors['newPassword'] = 'New password is required';
        isValid = false;
      } else if (this.newPassword.length < 6) {
        this.formErrors['newPassword'] = 'Password must be at least 6 characters';
        isValid = false;
      }
      
      if (this.newPassword !== this.confirmPassword) {
        this.formErrors['confirmPassword'] = 'Passwords do not match';
        isValid = false;
      }
    }

    return isValid;
  }

  onSubmit() {
    if (!this.validateForm()) {
      return;
    }

    const formData: EditUserFormData = {
      fullName: this.fullName.trim(),
      dateOfBirth: this.dateOfBirth,
      position: this.position.trim(),
      department: this.department.trim()
    };

    // Add avatar if selected
    if (this.selectedAvatar) {
      formData.avatar = this.selectedAvatar;
    }

    // Add password data if changing password
    if (this.showPasswordSection && this.newPassword) {
      formData.newPassword = this.newPassword;
      formData.confirmPassword = this.confirmPassword;
    }

    this.submitForm.emit(formData);
  }

  onClose() {
    this.resetForm();
    this.closeModal.emit();
  }

  onBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  getUserInitials(): string {
    if (this.user) {
      const firstName = this.user.firstName || '';
      const lastName = this.user.lastName || '';
      return (firstName.charAt(0) + lastName.charAt(0)).toUpperCase() || '??';
    }
    return '??';
  }

  formatAccountCreated(): string {
    if (this.user?.accountCreatedAt) {
      return new Date(this.user.accountCreatedAt).toLocaleDateString();
    }
    return 'N/A';
  }

  getLastLoginFormatted(): string {
    if (this.user?.lastLoginAt) {
      return new Date(this.user.lastLoginAt).toLocaleDateString();
    }
    return 'Never';
  }

  // Zone Management Methods
  private checkZoneAccess() {
    // Only show zone section for Admin users editing HSE users
    console.log('🔍 Zone Access Check:', {
      currentUserRole: this.currentUser?.role,
      editingUserRole: this.user?.role,
      showZoneSection: this.currentUser?.role === 'Admin' && this.user?.role === 'HSE'
    });
    
    this.showZoneSection = this.currentUser?.role === 'Admin' && this.user?.role === 'HSE';
    
    if (this.showZoneSection && this.user) {
      console.log('🔍 Loading zone data for user:', this.user.email);
      this.loadZoneData();
    }
  }

  private async loadZoneData() {
    try {
      console.log('🔍 Starting to load zone data...');
      // Load available zones, user zones, HSE users, and delegations in parallel
      const [zones, userZones, hseUsers, delegations] = await Promise.all([
        this.loadAvailableZones(),
        this.loadUserZones(),
        this.loadHSEUsers(),
        this.loadZoneDelegations()
      ]);
      console.log('✅ All zone data loaded successfully');
      console.log('🔍 Final available zones:', this.availableZones);
      console.log('🔍 Final user zones:', this.userZones);
    } catch (error) {
      console.error('❌ Error loading zone data:', error);
    }
  }

  private loadAvailableZones(): Promise<void> {
    return new Promise((resolve) => {
      console.log('🔍 Loading available zones from:', `${environment.apiUrl}/users/available-zones`);
      this.http.get<Zone[]>(`${environment.apiUrl}/users/available-zones`).subscribe({
        next: (zones) => {
          console.log('✅ Available zones loaded:', zones);
          this.availableZones = zones;
          resolve();
        },
        error: (error) => {
          console.error('❌ Error loading available zones:', error);
          resolve();
        }
      });
    });
  }

  private loadUserZones(): Promise<void> {
    if (!this.user?.id) return Promise.resolve();
    
    return new Promise((resolve) => {
      this.http.get<ZoneAssignment[]>(`${environment.apiUrl}/users/${this.user!.id}/zones`).subscribe({
        next: (zones) => {
          this.userZones = zones;
          resolve();
        },
        error: (error) => {
          console.error('Error loading user zones:', error);
          resolve();
        }
      });
    });
  }

  private loadHSEUsers(): Promise<void> {
    return new Promise((resolve) => {
      this.http.get<HSEUser[]>(`${environment.apiUrl}/users/hse-users`).subscribe({
        next: (users) => {
          this.hseUsers = users;
          resolve();
        },
        error: (error) => {
          console.error('Error loading HSE users:', error);
          resolve();
        }
      });
    });
  }

  private loadZoneDelegations(): Promise<void> {
    if (!this.user?.id) return Promise.resolve();
    
    return new Promise((resolve) => {
      this.http.get<ZoneDelegation[]>(`${environment.apiUrl}/users/zone-delegations?fromUserId=${this.user!.id}`).subscribe({
        next: (delegations) => {
          this.zoneDelegations = delegations;
          resolve();
        },
        error: (error) => {
          console.error('Error loading zone delegations:', error);
          resolve();
        }
      });
    });
  }

  onAssignZone() {
    if (!this.selectedZoneId || !this.user?.id) return;

    const assignData = {
      userId: this.user.id,
      zoneId: this.selectedZoneId
    };

    this.http.post(`${environment.apiUrl}/users/${this.user.id}/zones/assign`, assignData).subscribe({
      next: () => {
        this.selectedZoneId = null;
        this.loadUserZones(); // Refresh the list
        this.showCustomAlert('Success', 'Zone assigned successfully', 'success');
      },
      error: (error) => {
        console.error('Error assigning zone:', error);
        const errorMessage = error.error?.message || 'Failed to assign zone';
        this.showCustomAlert('Assignment Error', errorMessage, 'error');
      }
    });
  }

  onRemoveZone(zoneId: number) {
    if (!this.user?.id) return;

    this.showCustomConfirm(
      'Remove Zone Assignment',
      'Are you sure you want to remove this zone assignment?',
      () => {
        this.http.delete(`${environment.apiUrl}/users/${this.user!.id}/zones/${zoneId}`).subscribe({
          next: () => {
            this.loadUserZones(); // Refresh the list
            this.loadZoneDelegations(); // Refresh delegations
            this.showCustomAlert('Success', 'Zone assignment removed successfully', 'success');
          },
          error: (error) => {
            console.error('Error removing zone:', error);
            const errorMessage = error.error?.message || 'Failed to remove zone assignment';
            this.showCustomAlert('Removal Error', errorMessage, 'error');
          }
        });
      },
      () => {}, // onCancel - do nothing
      'Remove',
      'Cancel'
    );
  }

  onCreateDelegation() {
    this.showCreateDelegation = true;
    // Set default dates
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    this.delegationForm.startDate = today.toISOString().split('T')[0];
    this.delegationForm.endDate = tomorrow.toISOString().split('T')[0];
  }

  onCancelDelegation() {
    this.showCreateDelegation = false;
    this.delegationForm = {
      toHSEUserId: '',
      zoneId: null,
      startDate: '',
      endDate: '',
      reason: ''
    };
  }

  onSubmitDelegation() {
    if (!this.user?.id || !this.delegationForm.toHSEUserId || !this.delegationForm.zoneId) {
      return;
    }

    const delegationData = {
      fromHSEUserId: this.user.id,
      toHSEUserId: this.delegationForm.toHSEUserId,
      zoneId: this.delegationForm.zoneId,
      startDate: this.delegationForm.startDate,
      endDate: this.delegationForm.endDate,
      reason: this.delegationForm.reason
    };

    this.http.post(`${environment.apiUrl}/users/zone-delegations`, delegationData).subscribe({
      next: () => {
        this.onCancelDelegation();
        this.loadZoneDelegations(); // Refresh delegations
        this.showCustomAlert('Success', 'Delegation created successfully', 'success');
      },
      error: (error) => {
        console.error('Error creating delegation:', error);
        const errorMessage = error.error?.message || 'Failed to create delegation';
        this.showCustomAlert('Delegation Error', errorMessage, 'error');
      }
    });
  }

  onEndDelegation(delegationId: number) {
    this.showCustomConfirm(
      'End Delegation',
      'Are you sure you want to end this delegation?',
      () => {
        this.http.delete(`${environment.apiUrl}/users/zone-delegations/${delegationId}`).subscribe({
          next: () => {
            this.loadZoneDelegations(); // Refresh delegations
            this.showCustomAlert('Success', 'Delegation ended successfully', 'success');
          },
          error: (error) => {
            console.error('Error ending delegation:', error);
            const errorMessage = error.error?.message || 'Failed to end delegation';
            this.showCustomAlert('Delegation Error', errorMessage, 'error');
          }
        });
      },
      () => {}, // onCancel - do nothing
      'End Delegation',
      'Cancel'
    );
  }

  getAvailableZonesForAssignment(): Zone[] {
    const assignedZoneIds = this.userZones.map(uz => uz.zoneId);
    const filteredZones = this.availableZones.filter(zone => !assignedZoneIds.includes(zone.id));
    
    console.log('🔍 getAvailableZonesForAssignment called:');
    console.log('  - All available zones:', this.availableZones);
    console.log('  - User assigned zone IDs:', assignedZoneIds);
    console.log('  - Filtered zones for dropdown:', filteredZones);
    
    return filteredZones;
  }

  getUserZonesForDelegation(): ZoneAssignment[] {
    return this.userZones.filter(zone => zone.isActive);
  }

  getHSEUsersForDelegation(): HSEUser[] {
    return this.hseUsers.filter(user => user.id !== this.user?.id);
  }

  private getAvatarUrl(user: UserDto): string | null {
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

  // Custom Alert and Confirmation Methods
  showCustomAlert(title: string, message: string, type: 'info' | 'success' | 'warning' | 'error' = 'info') {
    this.alertConfig = { title, message, type };
    this.showAlert = true;
  }

  closeAlert() {
    this.showAlert = false;
  }

  showCustomConfirm(
    title: string, 
    message: string, 
    onConfirm: () => void, 
    onCancel: () => void = () => {}, 
    confirmText: string = 'Confirm', 
    cancelText: string = 'Cancel'
  ) {
    this.confirmConfig = { title, message, onConfirm, onCancel, confirmText, cancelText };
    this.showConfirm = true;
  }

  closeConfirm() {
    this.showConfirm = false;
  }

  onConfirmAction() {
    this.confirmConfig.onConfirm();
    this.closeConfirm();
  }

  onCancelAction() {
    this.confirmConfig.onCancel();
    this.closeConfirm();
  }
}