import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-user-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-modal.component.html',
  styleUrls: ['./user-modal.component.css']
})
export class UserModalComponent implements OnInit {
  @Input() show: boolean = false;
  @Input() title: string = 'Add User';
  @Input() isLoading: boolean = false;
  @Input() error: string | null = null;
  @Input() departments: string[] = [];
  @Input() submitButtonText: string = 'Create User';

  @Output() closeModal = new EventEmitter<void>();
  @Output() submitForm = new EventEmitter<any>();

  userForm = {
    email: '',
    fullName: '',
    role: 'Profil',
    companyId: '',
    department: '',
    position: ''
  };

  ngOnInit() {
    this.resetForm();
  }

  resetForm() {
    this.userForm = {
      email: '',
      fullName: '',
      role: 'Profil',
      companyId: '',
      department: '',
      position: ''
    };
  }

  onClose() {
    this.resetForm();
    this.closeModal.emit();
  }

  onSubmit() {
    if (!this.isFormValid()) {
      return;
    }
    this.submitForm.emit(this.userForm);
  }

  isFormValid(): boolean {
    return !!(this.userForm.email && 
             this.userForm.fullName && 
             this.userForm.role && 
             this.userForm.companyId && 
             this.userForm.department && 
             this.userForm.position);
  }

  getRoleDescription(): string {
    switch (this.userForm.role) {
      case 'Profil':
        return 'Profile users cannot login but can submit safety reports using their TE ID.';
      case 'HSE':
        return 'HSE users will receive login credentials via email with a generated password.';
      case 'Admin':
        return 'Admin users will receive login credentials via email with a generated password.';
      default:
        return '';
    }
  }

  getRequiredFieldsError(): string | null {
    const missing = [];
    if (!this.userForm.email) missing.push('Email');
    if (!this.userForm.fullName) missing.push('Full Name');
    if (!this.userForm.role) missing.push('Role');
    if (!this.userForm.companyId) missing.push('Company ID');
    if (!this.userForm.department) missing.push('Department');
    if (!this.userForm.position) missing.push('Position');
    
    if (missing.length > 0) {
      return `Please fill in all required fields: ${missing.join(', ')}`;
    }
    return null;
  }

  getRoleIcon(): string {
    switch (this.userForm.role) {
      case 'Profil':
        return 'fas fa-user';
      case 'HSE':
        return 'fas fa-hard-hat';
      case 'Admin':
        return 'fas fa-user-shield';
      default:
        return 'fas fa-user';
    }
  }
}