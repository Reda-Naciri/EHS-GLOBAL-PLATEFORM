import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Application, CreateApplicationDto, UpdateApplicationDto } from '../../services/applications.service';

@Component({
  selector: 'app-application-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './application-modal.component.html',
  styleUrls: ['./application-modal.component.css']
})
export class ApplicationModalComponent implements OnChanges {
  @Input() show = false;
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() application: Application | null = null;
  @Input() isLoading = false;
  @Input() error: string | null = null;

  @Output() closeModal = new EventEmitter<void>();
  @Output() submitForm = new EventEmitter<CreateApplicationDto | UpdateApplicationDto>();

  formData: CreateApplicationDto = {
    title: '',
    icon: '',
    redirectUrl: '',
    isActive: true,
    order: 1
  };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['show'] && this.show) {
      this.resetForm();
    }
    
    if (changes['application'] && this.application) {
      this.populateForm();
    }
  }

  private resetForm(): void {
    if (this.mode === 'create') {
      this.formData = {
        title: '',
        icon: '',
        redirectUrl: '',
        isActive: true,
        order: 1
      };
    } else if (this.application) {
      this.populateForm();
    }
  }

  private populateForm(): void {
    if (this.application) {
      this.formData = {
        title: this.application.title,
        icon: this.application.icon,
        redirectUrl: this.application.redirectUrl,
        isActive: this.application.isActive,
        order: this.application.order
      };
    }
  }

  getIconPreview(): string {
    if (!this.formData.icon) return '❓';
    
    // Check if it's a Font Awesome class
    if (this.formData.icon.includes('fa-')) {
      return `<i class="${this.formData.icon}"></i>`;
    }
    
    // Otherwise treat as emoji
    return this.formData.icon;
  }

  onSubmit(form: NgForm): void {
    if (form.valid && !this.isLoading) {
      this.submitForm.emit(this.formData);
    }
  }

  onCloseModal(): void {
    this.closeModal.emit();
  }
}