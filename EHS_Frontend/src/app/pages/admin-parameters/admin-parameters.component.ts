import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { EmailTimerComponent } from '../../components/email-timer/email-timer.component';
import { 
  AdminParametersService, 
  Zone, 
  Department, 
  InjuryType, 
  Shift,
  CreateZoneDto,
  CreateDepartmentDto,
  CreateInjuryTypeDto,
  CreateShiftDto,
  UpdateZoneDto,
  UpdateDepartmentDto,
  UpdateInjuryTypeDto,
  UpdateShiftDto
} from '../../services/admin-parameters.service';
import { forkJoin } from 'rxjs';
import { ConfirmationDialogComponent } from '../../components/confirmation-dialog/confirmation-dialog.component';
import { ApplicationsService, Application, CreateApplicationDto, UpdateApplicationDto } from '../../services/applications.service';
import { ApplicationModalComponent } from '../../components/application-modal/application-modal.component';
import { EmailConfigurationService } from '../../services/email-configuration.service';
import { UserService } from '../../services/user.service';

type TabType = 'zones' | 'injuries' | 'departments' | 'shifts' | 'applications' | 'email';

interface Parameter {
  id?: number;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  code?: string;
  category?: string;
  startTime?: string;
  endTime?: string;
}

@Component({
  selector: 'app-admin-parameters',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmationDialogComponent, ApplicationModalComponent, EmailTimerComponent],
  templateUrl: './admin-parameters.component.html',
  styleUrls: ['./admin-parameters.component.css'],
})
export class AdminParametersComponent implements OnInit {
  loading = false;
  activeTab: TabType = 'zones';
  emailLoading = false;
  
  // Data arrays
  zones: Parameter[] = [];
  injuryTypes: Parameter[] = [];
  departments: Parameter[] = [];
  shifts: Parameter[] = [];
  applications: Application[] = [];
  
  // Email configuration
  emailConfig = {
    isEmailingEnabled: true,
    sendProfileAssignmentEmails: true,
    sendHSEUpdateEmails: true,
    hseUpdateIntervalMinutes: 360, // 6 hours in minutes
    sendHSEInstantReportEmails: true,
    sendAdminOverviewEmails: true,
    adminOverviewIntervalMinutes: 360, // 6 hours in minutes
    superAdminUserIds: ''
  };
  
  // Admin users for super admin selection
  adminUsers: any[] = [];
  selectedSuperAdminIds: string[] = [];
  
  // Modal states
  showModal = false;
  modalMode: 'create' | 'edit' = 'create';
  currentParameter: Parameter = { name: '', isActive: true };
  modalTitle = '';
  
  // Application Modal states
  showApplicationModal = false;
  applicationModalMode: 'create' | 'edit' = 'create';
  selectedApplication: Application | null = null;
  applicationModalLoading = false;
  
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
  
  // Form validation
  errors: { [key: string]: string } = {};
  
  // Access denial state
  accessDenied: boolean = false;
  accessDeniedMessage: string = '';

  constructor(
    private alertService: AlertService,
    private authService: AuthService,
    private router: Router,
    private adminParametersService: AdminParametersService,
    private applicationsService: ApplicationsService,
    private emailConfigurationService: EmailConfigurationService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    // Check access first, and only proceed if access is granted
    if (!this.checkAdminAccess()) {
      return; // Stop here if access is denied
    }
    
    this.loadAllParameters();
    
    // Test email configuration API connectivity on startup
    this.testEmailConfigConnectivity();
  }

  checkAdminAccess(): boolean {
    const currentUser = this.authService.getCurrentUser();
    console.log('🔐 Checking admin access...');
    console.log('🔐 Current user:', currentUser);
    console.log('🔐 User role:', currentUser?.role);
    
    // Block HSE users from accessing the admin parameters page
    if (currentUser?.role === 'HSE') {
      console.error('❌ HSE user attempted to access admin parameters');
      this.accessDenied = true;
      this.accessDeniedMessage = 'Access denied. HSE users cannot access the system parameters page.';
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
    
    if (!currentUser) {
      console.error('❌ No current user found');
      this.alertService.showError('Please log in to access admin features.');
      return false; // Access denied
    }
    
    if (currentUser.role !== 'Admin') {
      console.error('❌ User is not Admin, role:', currentUser.role);
      this.alertService.showError('Access denied. Admin privileges required.');
      return false; // Access denied
    }
    
    console.log('✅ Admin access granted');
    return true; // Access granted
  }

  loadAllParameters(): void {
    this.loading = true;
    
    console.log('🔄 Loading all parameters from backend...');
    
    // Load all parameter types from backend including applications
    forkJoin({
      zones: this.adminParametersService.getZones(),
      departments: this.adminParametersService.getDepartments(),
      injuryTypes: this.adminParametersService.getInjuryTypes(),
      shifts: this.adminParametersService.getShifts(),
      applications: this.applicationsService.getApplications()
    }).subscribe({
      next: (data) => {
        console.log('✅ Parameters loaded successfully:', data);
        console.log('Zones:', data.zones);
        console.log('Departments:', data.departments);
        console.log('Injury Types:', data.injuryTypes);
        console.log('Shifts:', data.shifts);
        console.log('Applications:', data.applications);
        
        this.zones = data.zones;
        this.departments = data.departments;
        this.injuryTypes = data.injuryTypes;
        this.shifts = data.shifts;
        this.applications = data.applications.sort((a, b) => a.order - b.order);
        this.loading = false;
      },
      error: (error) => {
        console.error('❌ Error loading parameters:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        console.error('Full error object:', error);
        
        this.alertService.showError('Failed to load parameters. Please try again.');
        this.loading = false;
      }
    });
  }


  // Tab management (legacy - superseded by new setActiveTab method)

  // Modal management
  openCreateModal(): void {
    if (this.activeTab === 'applications') {
      this.onCreateApplication();
    } else {
      this.modalMode = 'create';
      this.modalTitle = `Add New ${this.getCurrentTabName().slice(0, -1)}`;
      this.currentParameter = { 
        name: '', 
        description: '', 
        isActive: true,
        code: '',
        category: this.activeTab === 'injuries' ? '' : undefined,
        startTime: this.activeTab === 'shifts' ? '08:00' : undefined,
        endTime: this.activeTab === 'shifts' ? '16:00' : undefined
      };
      this.errors = {};
      this.showModal = true;
    }
  }

  openEditModal(parameter: Parameter): void {
    this.modalMode = 'edit';
    this.modalTitle = `Edit ${this.getCurrentTabName().slice(0, -1)}`;
    this.currentParameter = { ...parameter };
    this.errors = {};
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.currentParameter = { name: '', isActive: true };
    this.errors = {};
  }

  // CRUD operations
  saveParameter(): void {
    if (!this.validateParameter()) return;

    if (this.modalMode === 'create') {
      this.createParameter();
    } else {
      this.updateParameter();
    }
  }

  validateParameter(): boolean {
    this.errors = {};

    if (!this.currentParameter.name.trim()) {
      this.errors['name'] = 'Name is required';
    }

    // Validate code field for all parameter types
    if (!this.currentParameter.code?.trim()) {
      this.errors['code'] = 'Code is required';
    }

    // Validate category for injury types
    if (this.activeTab === 'injuries' && !this.currentParameter.category?.trim()) {
      this.errors['category'] = 'Category is required';
    }

    // Validate time fields for shifts
    if (this.activeTab === 'shifts') {
      if (!this.currentParameter.startTime?.trim()) {
        this.errors['startTime'] = 'Start time is required';
      }
      if (!this.currentParameter.endTime?.trim()) {
        this.errors['endTime'] = 'End time is required';
      }
      
      // Validate that start time is before end time
      if (this.currentParameter.startTime && this.currentParameter.endTime) {
        const startTime = new Date(`2000-01-01T${this.currentParameter.startTime}`);
        const endTime = new Date(`2000-01-01T${this.currentParameter.endTime}`);
        
        if (startTime >= endTime) {
          this.errors['endTime'] = 'End time must be after start time';
        }
      }
    }

    // Check for duplicates (excluding current item in edit mode)
    const currentData = this.getCurrentData();
    const duplicate = currentData.find(item => 
      item.name.toLowerCase() === this.currentParameter.name.toLowerCase() &&
      (this.modalMode === 'create' || item.id !== this.currentParameter.id)
    );

    if (duplicate) {
      this.errors['name'] = 'This name already exists';
    }

    // Check for duplicate codes
    if (this.currentParameter.code) {
      const duplicateCode = currentData.find(item => 
        (item as any).code?.toLowerCase() === this.currentParameter.code?.toLowerCase() &&
        (this.modalMode === 'create' || item.id !== this.currentParameter.id)
      );

      if (duplicateCode) {
        this.errors['code'] = 'This code already exists';
      }
    }

    return Object.keys(this.errors).length === 0;
  }

  createParameter(): void {
    switch (this.activeTab) {
      case 'zones':
        this.createZone();
        break;
      case 'departments':
        this.createDepartment();
        break;
      case 'injuries':
        this.createInjuryType();
        break;
      case 'shifts':
        this.createShift();
        break;
    }
  }

  private createZone(): void {
    const dto: CreateZoneDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      code: this.currentParameter.code || '',
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.createZone(dto).subscribe({
      next: (zone) => {
        this.zones.push(zone);
        this.alertService.showSuccess(`Zone "${zone.name}" created successfully! ✅`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error creating zone:', error);
        this.alertService.showError(error.error?.message || 'Error creating zone');
      }
    });
  }

  private createDepartment(): void {
    const dto: CreateDepartmentDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      code: this.currentParameter.code || '',
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.createDepartment(dto).subscribe({
      next: (department) => {
        this.departments.push(department);
        this.alertService.showSuccess(`Department "${department.name}" created successfully! ✅`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error creating department:', error);
        this.alertService.showError(error.error?.message || 'Error creating department');
      }
    });
  }

  private createInjuryType(): void {
    const dto: CreateInjuryTypeDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      code: this.currentParameter.code || '',
      category: this.currentParameter.category || 'General',
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.createInjuryType(dto).subscribe({
      next: (injuryType) => {
        this.injuryTypes.push(injuryType);
        this.alertService.showSuccess(`Injury type "${injuryType.name}" created successfully! ✅`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error creating injury type:', error);
        this.alertService.showError(error.error?.message || 'Error creating injury type');
      }
    });
  }

  private createShift(): void {
    const dto: CreateShiftDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      startTime: this.currentParameter.startTime || '08:00:00',
      endTime: this.currentParameter.endTime || '16:00:00',
      code: this.currentParameter.code || '',
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.createShift(dto).subscribe({
      next: (shift) => {
        this.shifts.push(shift);
        this.alertService.showSuccess(`Shift "${shift.name}" created successfully! ✅`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error creating shift:', error);
        this.alertService.showError(error.error?.message || 'Error creating shift');
      }
    });
  }

  updateParameter(): void {
    if (!this.currentParameter.id) return;

    switch (this.activeTab) {
      case 'zones':
        this.updateZone();
        break;
      case 'departments':
        this.updateDepartment();
        break;
      case 'injuries':
        this.updateInjuryType();
        break;
      case 'shifts':
        this.updateShift();
        break;
    }
  }

  private updateZone(): void {
    if (!this.currentParameter.id) return;

    const dto: UpdateZoneDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      code: this.currentParameter.code,
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.updateZone(this.currentParameter.id, dto).subscribe({
      next: () => {
        const index = this.zones.findIndex(z => z.id === this.currentParameter.id);
        if (index !== -1) {
          this.zones[index] = { ...this.currentParameter as Zone, updatedAt: new Date().toISOString() };
        }
        this.alertService.showSuccess(`Zone "${this.currentParameter.name}" updated successfully! 💾`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error updating zone:', error);
        this.alertService.showError(error.error?.message || 'Error updating zone');
      }
    });
  }

  private updateDepartment(): void {
    if (!this.currentParameter.id) return;

    const dto: UpdateDepartmentDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      code: this.currentParameter.code,
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.updateDepartment(this.currentParameter.id, dto).subscribe({
      next: () => {
        const index = this.departments.findIndex(d => d.id === this.currentParameter.id);
        if (index !== -1) {
          this.departments[index] = { ...this.currentParameter as Department, updatedAt: new Date().toISOString() };
        }
        this.alertService.showSuccess(`Department "${this.currentParameter.name}" updated successfully! 💾`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error updating department:', error);
        this.alertService.showError(error.error?.message || 'Error updating department');
      }
    });
  }

  private updateInjuryType(): void {
    if (!this.currentParameter.id) return;

    const dto: UpdateInjuryTypeDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      code: this.currentParameter.code,
      category: this.currentParameter.category,
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.updateInjuryType(this.currentParameter.id, dto).subscribe({
      next: () => {
        const index = this.injuryTypes.findIndex(i => i.id === this.currentParameter.id);
        if (index !== -1) {
          this.injuryTypes[index] = { ...this.currentParameter as InjuryType, updatedAt: new Date().toISOString() };
        }
        this.alertService.showSuccess(`Injury type "${this.currentParameter.name}" updated successfully! 💾`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error updating injury type:', error);
        this.alertService.showError(error.error?.message || 'Error updating injury type');
      }
    });
  }

  private updateShift(): void {
    if (!this.currentParameter.id) return;

    const dto: UpdateShiftDto = {
      name: this.currentParameter.name,
      description: this.currentParameter.description,
      startTime: this.currentParameter.startTime,
      endTime: this.currentParameter.endTime,
      code: this.currentParameter.code,
      isActive: this.currentParameter.isActive
    };

    this.adminParametersService.updateShift(this.currentParameter.id, dto).subscribe({
      next: () => {
        const index = this.shifts.findIndex(s => s.id === this.currentParameter.id);
        if (index !== -1) {
          this.shifts[index] = { ...this.currentParameter as Shift, updatedAt: new Date().toISOString() };
        }
        this.alertService.showSuccess(`Shift "${this.currentParameter.name}" updated successfully! 💾`);
        this.closeModal();
      },
      error: (error) => {
        console.error('Error updating shift:', error);
        this.alertService.showError(error.error?.message || 'Error updating shift');
      }
    });
  }

  deleteParameter(parameter: Parameter): void {
    if (!parameter.id) return;
    
    this.confirmationDialog = {
      show: true,
      title: 'Delete Parameter',
      message: `Are you sure you want to delete "${parameter.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      type: 'danger',
      onConfirm: () => {
        this.executeDeleteParameter(parameter);
        this.onCloseConfirmation();
      }
    };
  }

  private executeDeleteParameter(parameter: Parameter): void {
    if (!parameter.id) return;
    
    switch (this.activeTab) {
      case 'zones':
        this.deleteZone(parameter.id, parameter);
        break;
      case 'departments':
        this.deleteDepartment(parameter.id, parameter);
        break;
      case 'injuries':
        this.deleteInjuryType(parameter.id, parameter);
        break;
      case 'shifts':
        this.deleteShift(parameter.id, parameter);
        break;
    }
  }

  private deleteZone(id: number, parameter: Parameter): void {
    this.adminParametersService.deleteZone(id).subscribe({
      next: () => {
        this.zones = this.zones.filter(z => z.id !== id);
        this.alertService.showSuccess(`Zone "${parameter.name}" deleted successfully!`);
      },
      error: (error) => {
        console.error('Error deleting zone:', error);
        const errorMessage = error.error?.message || 'Error deleting zone';
        
        if (errorMessage.includes('being used')) {
          this.handleInUseError(parameter, 'zone', errorMessage);
        } else {
          this.alertService.showError(errorMessage);
        }
      }
    });
  }

  private deleteDepartment(id: number, parameter: Parameter): void {
    this.adminParametersService.deleteDepartment(id).subscribe({
      next: () => {
        this.departments = this.departments.filter(d => d.id !== id);
        this.alertService.showSuccess(`Department "${parameter.name}" deleted successfully!`);
      },
      error: (error) => {
        console.error('Error deleting department:', error);
        const errorMessage = error.error?.message || 'Error deleting department';
        
        if (errorMessage.includes('being used')) {
          this.handleInUseError(parameter, 'department', errorMessage);
        } else {
          this.alertService.showError(errorMessage);
        }
      }
    });
  }

  private deleteInjuryType(id: number, parameter: Parameter): void {
    this.adminParametersService.deleteInjuryType(id).subscribe({
      next: () => {
        this.injuryTypes = this.injuryTypes.filter(i => i.id !== id);
        this.alertService.showSuccess(`Injury type "${parameter.name}" deleted successfully!`);
      },
      error: (error) => {
        console.error('Error deleting injury type:', error);
        const errorMessage = error.error?.message || 'Error deleting injury type';
        
        if (errorMessage.includes('being used')) {
          this.handleInUseError(parameter, 'injury type', errorMessage);
        } else {
          this.alertService.showError(errorMessage);
        }
      }
    });
  }

  private deleteShift(id: number, parameter: Parameter): void {
    this.adminParametersService.deleteShift(id).subscribe({
      next: () => {
        this.shifts = this.shifts.filter(s => s.id !== id);
        this.alertService.showSuccess(`Shift "${parameter.name}" deleted successfully!`);
      },
      error: (error) => {
        console.error('Error deleting shift:', error);
        const errorMessage = error.error?.message || 'Error deleting shift';
        
        if (errorMessage.includes('being used')) {
          this.handleInUseError(parameter, 'shift', errorMessage);
        } else {
          this.alertService.showError(errorMessage);
        }
      }
    });
  }

  toggleStatus(parameter: Parameter): void {
    if (!parameter.id) return;
    
    const newStatus = !parameter.isActive;
    const dto = { isActive: newStatus };

    switch (this.activeTab) {
      case 'zones':
        this.adminParametersService.updateZone(parameter.id, dto).subscribe({
          next: () => {
            parameter.isActive = newStatus;
            parameter.updatedAt = new Date().toISOString();
            const status = newStatus ? 'activated' : 'deactivated';
            const statusIcon = newStatus ? '✅' : '🔒';
            this.alertService.showSuccess(`Zone "${parameter.name}" ${status} successfully! ${statusIcon}`);
          },
          error: (error) => {
            console.error('Error updating zone status:', error);
            this.alertService.showError(error.error?.message || 'Error updating zone status');
          }
        });
        break;
      case 'departments':
        this.adminParametersService.updateDepartment(parameter.id, dto).subscribe({
          next: () => {
            parameter.isActive = newStatus;
            parameter.updatedAt = new Date().toISOString();
            const status = newStatus ? 'activated' : 'deactivated';
            const statusIcon = newStatus ? '✅' : '🔒';
            this.alertService.showSuccess(`Department "${parameter.name}" ${status} successfully! ${statusIcon}`);
          },
          error: (error) => {
            console.error('Error updating department status:', error);
            this.alertService.showError(error.error?.message || 'Error updating department status');
          }
        });
        break;
      case 'injuries':
        this.adminParametersService.updateInjuryType(parameter.id, dto).subscribe({
          next: () => {
            parameter.isActive = newStatus;
            parameter.updatedAt = new Date().toISOString();
            const status = newStatus ? 'activated' : 'deactivated';
            const statusIcon = newStatus ? '✅' : '🔒';
            this.alertService.showSuccess(`Injury type "${parameter.name}" ${status} successfully! ${statusIcon}`);
          },
          error: (error) => {
            console.error('Error updating injury type status:', error);
            this.alertService.showError(error.error?.message || 'Error updating injury type status');
          }
        });
        break;
      case 'shifts':
        this.adminParametersService.updateShift(parameter.id, dto).subscribe({
          next: () => {
            parameter.isActive = newStatus;
            parameter.updatedAt = new Date().toISOString();
            const status = newStatus ? 'activated' : 'deactivated';
            const statusIcon = newStatus ? '✅' : '🔒';
            this.alertService.showSuccess(`Shift "${parameter.name}" ${status} successfully! ${statusIcon}`);
          },
          error: (error) => {
            console.error('Error updating shift status:', error);
            this.alertService.showError(error.error?.message || 'Error updating shift status');
          }
        });
        break;
    }
  }


  private handleInUseError(parameter: Parameter, itemType: string, errorMessage: string): void {
    // Show error message first
    this.alertService.showError(errorMessage);
    
    // If parameter is currently active, offer to deactivate instead
    if (parameter.isActive) {
      this.confirmationDialog = {
        show: true,
        title: 'Deactivate Instead?',
        message: `Would you like to deactivate this ${itemType} instead? This will make it unavailable for new assignments while preserving existing data.`,
        confirmText: 'Deactivate',
        cancelText: 'Cancel',
        type: 'warning',
        onConfirm: () => {
          parameter.isActive = false;
          this.toggleStatus(parameter);
          this.onCloseConfirmation();
        }
      };
    } else {
      // If already inactive, explain why it cannot be deleted
      this.alertService.showWarning(
        `This ${itemType} cannot be deleted because it's being used in the system. It has already been deactivated.`
      );
    }
  }

  // Application management methods
  onCreateApplication(): void {
    this.applicationModalMode = 'create';
    this.selectedApplication = null;
    this.showApplicationModal = true;
  }

  onEditApplication(application: Application): void {
    this.applicationModalMode = 'edit';
    this.selectedApplication = application;
    this.showApplicationModal = true;
  }

  onDeleteApplication(application: Application): void {
    this.confirmationDialog = {
      show: true,
      title: 'Delete Application',
      message: `Are you sure you want to delete "${application.title}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel',
      type: 'danger',
      onConfirm: () => {
        this.executeDeleteApplication(application);
        this.onCloseConfirmation();
      }
    };
  }

  executeDeleteApplication(application: Application): void {
    if (!application.id) return;
    
    this.applicationsService.deleteApplication(application.id).subscribe({
      next: () => {
        this.alertService.showSuccess(`Application "${application.title}" deleted successfully! 🗑️`);
        this.loadAllParameters();
      },
      error: (error) => {
        console.error('Error deleting application:', error);
        this.alertService.showError(error.error?.message || 'Failed to delete application');
      }
    });
  }

  onToggleApplicationStatus(application: Application): void {
    if (!application.id) return;
    
    this.applicationsService.toggleApplicationStatus(application.id).subscribe({
      next: (updatedApp) => {
        const statusText = updatedApp.isActive ? 'activated' : 'deactivated';
        this.alertService.showSuccess(`Application "${application.title}" ${statusText} successfully! 🔄`);
        this.loadAllParameters();
      },
      error: (error) => {
        console.error('Error toggling application status:', error);
        this.alertService.showError(error.error?.message || 'Failed to update application status');
      }
    });
  }

  onApplicationModalSubmit(formData: CreateApplicationDto | UpdateApplicationDto): void {
    this.applicationModalLoading = true;
    
    if (this.applicationModalMode === 'create') {
      this.applicationsService.createApplication(formData as CreateApplicationDto).subscribe({
        next: (newApp) => {
          this.alertService.showSuccess(`Application "${newApp.title}" created successfully! ✨`);
          this.showApplicationModal = false;
          this.applicationModalLoading = false;
          this.loadAllParameters();
        },
        error: (error) => {
          console.error('Error creating application:', error);
          this.alertService.showError(error.error?.message || 'Failed to create application');
          this.applicationModalLoading = false;
        }
      });
    } else if (this.selectedApplication?.id) {
      this.applicationsService.updateApplication(this.selectedApplication.id, formData as UpdateApplicationDto).subscribe({
        next: (updatedApp) => {
          this.alertService.showSuccess(`Application "${updatedApp.title}" updated successfully! 💾`);
          this.showApplicationModal = false;
          this.applicationModalLoading = false;
          this.loadAllParameters();
        },
        error: (error) => {
          console.error('Error updating application:', error);
          this.alertService.showError(error.error?.message || 'Failed to update application');
          this.applicationModalLoading = false;
        }
      });
    }
  }

  onCloseApplicationModal(): void {
    this.showApplicationModal = false;
    this.applicationModalLoading = false;
    this.selectedApplication = null;
  }

  // Tab Management Methods
  setActiveTab(tab: TabType): void {
    this.activeTab = tab;
    
    if (tab === 'email') {
      this.loadEmailConfiguration();
      this.loadAdminUsers();
    }
  }

  getCurrentTabName(): string {
    switch (this.activeTab) {
      case 'zones':
        return 'Zones';
      case 'injuries':
        return 'Injury Types';
      case 'departments':
        return 'Departments';
      case 'shifts':
        return 'Shifts';
      case 'applications':
        return 'Applications';
      case 'email':
        return 'Email Configuration';
      default:
        return '';
    }
  }

  getCurrentData(): Parameter[] {
    switch (this.activeTab) {
      case 'zones':
        return this.zones;
      case 'injuries':
        return this.injuryTypes;
      case 'departments':
        return this.departments;
      case 'shifts':
        return this.shifts;
      case 'applications':
        return this.applications as any; // Cast for compatibility
      case 'email':
        return []; // Email tab doesn't use parameter data
      default:
        return [];
    }
  }

  getTabIcon(): string {
    switch (this.activeTab) {
      case 'zones':
        return 'fa-map-marker-alt';
      case 'injuries':
        return 'fa-band-aid';
      case 'departments':
        return 'fa-building';
      case 'shifts':
        return 'fa-clock';
      case 'applications':
        return 'fa-mobile-alt';
      case 'email':
        return 'fa-envelope';
      default:
        return 'fa-cog';
    }
  }

  getModalSubtitle(): string {
    return `${this.modalMode === 'create' ? 'Add a new' : 'Edit'} ${this.getCurrentTabName().slice(0, -1).toLowerCase()}`;
  }

  // Email Configuration Methods
  loadEmailConfiguration(): void {
    console.log('🔄 Loading email configuration...');
    this.emailLoading = true;
    
    this.emailConfigurationService.getEmailConfiguration().subscribe({
      next: (config) => {
        console.log('✅ Email configuration loaded successfully:', config);
        this.emailConfig = {
          isEmailingEnabled: config.isEmailingEnabled,
          sendProfileAssignmentEmails: config.sendProfileAssignmentEmails,
          sendHSEUpdateEmails: config.sendHSEUpdateEmails,
          hseUpdateIntervalMinutes: config.hseUpdateIntervalMinutes || 360,
          sendHSEInstantReportEmails: config.sendHSEInstantReportEmails,
          sendAdminOverviewEmails: config.sendAdminOverviewEmails,
          adminOverviewIntervalMinutes: config.adminOverviewIntervalMinutes || 360,
          superAdminUserIds: config.superAdminUserIds || ''
        };
        
        // Parse the comma-separated IDs into selectedSuperAdminIds array
        if (config.superAdminUserIds) {
          this.selectedSuperAdminIds = config.superAdminUserIds
            .split(',')
            .map(id => id.trim())
            .filter(id => id.length > 0);
        } else {
          this.selectedSuperAdminIds = [];
        }
        
        console.log('📧 Email config loaded:', this.emailConfig);
        console.log('👥 Selected super admin IDs:', this.selectedSuperAdminIds);
        this.emailLoading = false;
      },
      error: (error) => {
        console.error('❌ Error loading email configuration:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        console.error('Full error object:', error);
        
        if (error.status === 401) {
          this.alertService.showError('Access denied. Admin privileges required for email configuration.');
        } else if (error.status === 404) {
          this.alertService.showError('Email configuration endpoint not found. Please contact system administrator.');
        } else if (error.status === 0) {
          this.alertService.showError('Cannot connect to server. Please check if the backend is running.');
        } else {
          this.alertService.showError(`Failed to load email configuration: ${error.error?.message || error.message || 'Unknown error'}`);
        }
        this.emailLoading = false;
      }
    });
  }

  loadAdminUsers(): void {
    console.log('🔄 Loading admin users...');
    // Get all users with Admin role
    this.userService.getUsers(1, 1000, '', 'Admin').subscribe({
      next: (response) => {
        console.log('✅ Admin users loaded successfully:', response);
        this.adminUsers = response.users.map(user => ({
          id: user.id,
          companyId: user.companyId,
          fullName: `${user.firstName} ${user.lastName}`,
          email: user.email,
          isActive: user.isActive
        }));
        console.log('👥 Mapped admin users:', this.adminUsers);
      },
      error: (error) => {
        console.error('❌ Error loading admin users:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        console.error('Full error object:', error);
        
        if (error.status === 401) {
          this.alertService.showError('Access denied. Admin privileges required to view admin users.');
        } else {
          this.alertService.showError(`Failed to load admin users: ${error.error?.message || error.message || 'Unknown error'}`);
        }
      }
    });
  }

  onIntervalChange(type: 'hse' | 'admin', event: any): void {
    const newInterval = parseInt(event.target.value);
    console.log('🔧 onIntervalChange:', { type, selectedValue: event.target.value, parsedValue: newInterval });
    console.log('🔧 emailConfig BEFORE update:', { hse: this.emailConfig.hseUpdateIntervalMinutes, admin: this.emailConfig.adminOverviewIntervalMinutes });
    
    if (type === 'hse') {
      this.emailConfig.hseUpdateIntervalMinutes = newInterval;
      console.log('🔧 HSE interval updated to:', this.emailConfig.hseUpdateIntervalMinutes);
    } else {
      this.emailConfig.adminOverviewIntervalMinutes = newInterval;
      console.log('🔧 Admin interval updated to:', this.emailConfig.adminOverviewIntervalMinutes);
    }
    
    console.log('🔧 emailConfig AFTER update:', { hse: this.emailConfig.hseUpdateIntervalMinutes, admin: this.emailConfig.adminOverviewIntervalMinutes });
    
    // Auto-save when interval changes to reset timer immediately
    this.saveEmailConfiguration();
  }

  saveEmailConfiguration(): void {
    this.emailLoading = true;

    // Convert selectedSuperAdminIds array back to comma-separated string and map property names
    const configToSave = {
      isEmailingEnabled: this.emailConfig.isEmailingEnabled,
      sendProfileAssignmentEmails: this.emailConfig.sendProfileAssignmentEmails,
      sendHSEUpdateEmails: this.emailConfig.sendHSEUpdateEmails,
      HSEUpdateIntervalMinutes: this.emailConfig.hseUpdateIntervalMinutes,
      sendHSEInstantReportEmails: this.emailConfig.sendHSEInstantReportEmails,
      sendAdminOverviewEmails: this.emailConfig.sendAdminOverviewEmails,
      AdminOverviewIntervalMinutes: this.emailConfig.adminOverviewIntervalMinutes,
      superAdminUserIds: this.selectedSuperAdminIds.join(',')
    };
    
    console.log('🚀 Sending email configuration to backend:', configToSave);
    console.log('🚀 HSE Interval being sent:', configToSave.HSEUpdateIntervalMinutes);
    console.log('🚀 Admin Interval being sent:', configToSave.AdminOverviewIntervalMinutes);

    this.emailConfigurationService.updateEmailConfiguration(configToSave).subscribe({
      next: () => {
        this.alertService.showSuccess('Email configuration saved successfully! Timer reset. 📧');
        this.emailLoading = false;
        
        // Call backend to reset email timers immediately
        this.resetEmailTimers();
      },
      error: (error) => {
        console.error('Error saving email configuration:', error);
        this.alertService.showError(error.error?.message || 'Failed to save email configuration');
        this.emailLoading = false;
      }
    });
  }

  private resetEmailTimers(): void {
    // Call backend endpoint to reset email scheduling timers
    this.emailConfigurationService.resetEmailTimers().subscribe({
      next: () => {
        console.log('✅ Email timers reset successfully');
      },
      error: (error) => {
        console.error('❌ Error resetting email timers:', error);
      }
    });
  }

  // Super Admin Selection Methods
  toggleSuperAdminSelection(adminId: string): void {
    const index = this.selectedSuperAdminIds.indexOf(adminId);
    if (index > -1) {
      this.selectedSuperAdminIds.splice(index, 1);
    } else {
      this.selectedSuperAdminIds.push(adminId);
    }
  }

  isSuperAdminSelected(adminId: string): boolean {
    return this.selectedSuperAdminIds.includes(adminId);
  }

  selectAllSuperAdmins(): void {
    this.selectedSuperAdminIds = this.adminUsers
      .filter(user => user.isActive)
      .map(user => user.companyId);
  }

  deselectAllSuperAdmins(): void {
    this.selectedSuperAdminIds = [];
  }

  testHSEEmails(): void {
    this.emailLoading = true;

    this.emailConfigurationService.testHSEUpdateEmails().subscribe({
      next: () => {
        this.alertService.showSuccess('HSE test emails sent successfully! 📬');
        this.emailLoading = false;
      },
      error: (error) => {
        console.error('Error sending HSE test emails:', error);
        this.alertService.showError(error.error?.message || 'Failed to send HSE test emails');
        this.emailLoading = false;
      }
    });
  }

  testAdminEmails(): void {
    this.emailLoading = true;

    this.emailConfigurationService.testAdminOverviewEmails().subscribe({
      next: () => {
        this.alertService.showSuccess('Admin test emails sent successfully! 📮');
        this.emailLoading = false;
      },
      error: (error) => {
        console.error('Error sending admin test emails:', error);
        this.alertService.showError(error.error?.message || 'Failed to send admin test emails');
        this.emailLoading = false;
      }
    });
  }

  // Tracking and Utility Methods
  trackById(index: number, item: Parameter): number {
    return item.id || index;
  }

  trackByAppId(index: number, item: Application): number {
    return item.id || index;
  }

  formatDate(dateString: string): string {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleDateString();
  }

  getStatusClass(isActive: boolean): string {
    return isActive 
      ? 'bg-green-100 text-green-800 border-green-200' 
      : 'bg-red-100 text-red-800 border-red-200';
  }

  getStatusIcon(isActive: boolean): string {
    return isActive ? 'fa-check-circle' : 'fa-times-circle';
  }

  // Utility methods
  getIconDisplay(icon: string): string {
    if (icon.includes('fa-')) {
      return `<i class="${icon}"></i>`;
    }
    return icon;
  }

  onCloseConfirmation(): void {
    this.confirmationDialog.show = false;
  }

  // Debugging and connectivity testing
  testEmailConfigConnectivity(): void {
    console.log('🔍 Testing email configuration API connectivity...');
    
    // Check JWT token status first
    this.checkJWTTokenStatus();
    
    // Test if we can reach the email config endpoint
    this.emailConfigurationService.getEmailConfiguration().subscribe({
      next: (config) => {
        console.log('✅ Email configuration API is working correctly');
        console.log('📧 Current config:', config);
      },
      error: (error) => {
        console.error('❌ Email configuration API test failed:', error);
        
        if (error.status === 0) {
          console.error('🔌 Network error - backend might be down or CORS issue');
        } else if (error.status === 401) {
          console.error('🔐 Authentication error - user might not be logged in as Admin');
          this.checkJWTTokenStatus();
        } else if (error.status === 404) {
          console.error('🔍 API endpoint not found - check backend routing');
        } else if (error.status === 500) {
          console.error('💥 Server error - check backend logs');
        }
      }
    });
  }

  checkJWTTokenStatus(): void {
    const token = localStorage.getItem('token');
    console.log('🎫 JWT Token check:');
    console.log('🎫 Token exists:', !!token);
    
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const expiry = payload.exp * 1000;
        const now = Date.now();
        const isExpired = now > expiry;
        
        console.log('🎫 Token payload:', payload);
        console.log('🎫 Token expires at:', new Date(expiry));
        console.log('🎫 Current time:', new Date(now));
        console.log('🎫 Token expired:', isExpired);
        console.log('🎫 User role in token:', payload.role);
        
        if (isExpired) {
          console.error('❌ JWT token has expired! Please log in again.');
          this.alertService.showError('Your session has expired. Please log in again.');
        } else {
          console.log('✅ JWT token is valid and not expired');
        }
      } catch (e) {
        console.error('❌ Invalid JWT token format:', e);
      }
    } else {
      console.error('❌ No JWT token found in localStorage');
      this.alertService.showError('No authentication token found. Please log in.');
    }
  }

  // Method to manually test API with current token
  testWithCurrentAuth(): void {
    console.log('🧪 Testing API call with current authentication...');
    const token = localStorage.getItem('token');
    
    if (!token) {
      console.error('❌ No token available for testing');
      this.alertService.showError('Please log in first');
      return;
    }

    // Make a direct HTTP call with the token
    const headers = { 'Authorization': `Bearer ${token}` };
    const testUrl = `${environment.apiUrl}/emailconfiguration`;
    
    console.log('🧪 Making test request to:', testUrl);
    console.log('🧪 With headers:', headers);

    fetch(testUrl, { 
      method: 'GET',
      headers: headers 
    })
    .then(async response => {
      console.log('🧪 Test response status:', response.status);
      console.log('🧪 Test response headers:', Object.fromEntries(response.headers.entries()));
      
      const responseText = await response.text();
      console.log('🧪 Response body:', responseText);
      
      if (response.status === 200) {
        const data = JSON.parse(responseText);
        console.log('✅ Test successful! Data:', data);
        this.alertService.showSuccess('Direct API test successful!');
      } else if (response.status === 500) {
        console.error('💥 Server error details:', responseText);
        this.alertService.showError(`Server error (500): Check backend logs for details`);
      } else {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
    })
    .catch(error => {
      console.error('❌ Test failed:', error);
      this.alertService.showError(`Direct API test failed: ${error.message}`);
    });
  }
}