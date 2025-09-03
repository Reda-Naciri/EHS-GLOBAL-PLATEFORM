import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-corrective-action-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './corrective-action-modal.component.html',
  styleUrls: ['./corrective-action-modal.component.css']
})
export class CorrectiveActionModalComponent implements OnInit {
  @Input() show: boolean = false;
  @Input() isLoading: boolean = false;
  @Input() error: string | null = null;
  @Input() priorityOptions: string[] = [];
  @Input() hierarchyOptions: string[] = [];

  @Output() closeModal = new EventEmitter<void>();
  @Output() submitForm = new EventEmitter<any>();

  correctiveActionForm = {
    title: '',
    description: '',
    dueDate: '',
    priority: '',
    hierarchy: ''
  };

  // Validation tracking
  showValidationErrors = false;
  validationErrors: string[] = [];

  ngOnInit() {
    this.resetForm();
  }

  resetForm() {
    this.correctiveActionForm = {
      title: '',
      description: '',
      dueDate: '',
      priority: '',
      hierarchy: ''
    };
    this.showValidationErrors = false;
    this.validationErrors = [];
  }

  onClose() {
    this.resetForm();
    this.closeModal.emit();
  }

  onSubmit() {
    this.showValidationErrors = true;
    this.validateForm();
    
    if (this.isFormValid()) {
      this.submitForm.emit(this.correctiveActionForm);
    }
  }

  isFormValid(): boolean {
    return this.correctiveActionForm.title.trim().length > 0 && 
           this.correctiveActionForm.description.trim().length > 0 &&
           this.correctiveActionForm.priority.trim().length > 0 &&
           this.correctiveActionForm.hierarchy.trim().length > 0;
  }

  validateForm(): void {
    this.validationErrors = [];
    
    if (!this.correctiveActionForm.title.trim()) {
      this.validationErrors.push('Action title is required');
    }
    
    if (!this.correctiveActionForm.description.trim()) {
      this.validationErrors.push('Action description is required');
    }
    
    if (!this.correctiveActionForm.priority.trim()) {
      this.validationErrors.push('Priority selection is required');
    }
    
    if (!this.correctiveActionForm.hierarchy.trim()) {
      this.validationErrors.push('Safety hierarchy selection is required');
    }
  }

  isFieldInvalid(fieldName: string): boolean {
    if (!this.showValidationErrors) return false;
    
    switch (fieldName) {
      case 'title':
        return !this.correctiveActionForm.title.trim();
      case 'description':
        return !this.correctiveActionForm.description.trim();
      case 'priority':
        return !this.correctiveActionForm.priority.trim();
      case 'hierarchy':
        return !this.correctiveActionForm.hierarchy.trim();
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
}