import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, NgForOf, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BodyMapComponent } from '../../components/body-map/body-map.component';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { CreateReportDto } from '../../models/report.models';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [CommonModule, FormsModule, NgIf, NgForOf, BodyMapComponent],
  templateUrl: './report.component.html',
  styleUrls: ['./report.component.css']
})
export class ReportComponent implements OnInit, OnDestroy {
  reportType: string | null = null;
  reportTitle: string = '';
  loading = false;
  
  
  // Debounce subject for ReporterId validation
  private reporterIdSubject = new Subject<string>();
  private hasUserStartedTyping = false;

  // Backend reference data
  departments: any[] = [];
  zones: any[] = [];
  shifts: any[] = [];
  fractureTypes: any[] = [];
  
  // Company ID validation
  companyIdValid = false;
  companyIdValidating = false;
  reporterInfo: any = null;

  // Form data
  reportForm = {
    reporterId: '',
    workShift: '',
    title: '',
    zone: '',
    incidentDateTime: this.formatDateForInput(new Date()), // String format for datetime-local input
    description: '',
    immediateActionsTaken: '',
    actionStatus: 'Open',
    personInChargeOfActions: '',
    dateActionsCompleted: null as Date | null,
    injuredPersonsCount: 0,
    attachments: [] as File[]
  };

  get injuredCount(): number {
    return this.reportForm.injuredPersonsCount;
  }

  set injuredCount(value: number) {
    this.reportForm.injuredPersonsCount = value;
  }

  onInjuredCountChange(count: number): void {
    console.log('🔧 Injured count changed to:', count);
    
    // Update the form count
    this.reportForm.injuredPersonsCount = count;
    
    // Adjust the injuredPersons array size
    while (this.injuredPersons.length < count) {
      this.injuredPersons.push({
        id: this.injuredPersons.length + 1,
        name: '',
        age: null,
        gender: '',
        department: '',
        zoneOfPerson: '',
        selectedBodyPart: '',
        injuryType: '',
        severity: '',
        description: '',
        injuries: []
      });
    }
    
    // Remove extra persons if count decreased
    while (this.injuredPersons.length > count) {
      this.injuredPersons.pop();
    }
    
    // Update validation array
    this.fieldValidation.injuredPersons = this.injuredPersons.map(() => ({
      name: true,
      department: true,
      zoneOfPerson: true,
      gender: true
    }));
    
    console.log('🔧 Updated injured persons array:', this.injuredPersons);
  }

  injuredPersons: {
    id: number;
    name: string;
    age: number | null;
    gender: string;
    department: string;
    zoneOfPerson: string;
    selectedBodyPart: string;
    injuryType: string;
    severity: string;
    description: string;
    injuries: { id: string, bodyPart: string, injuryType: string, severity: string, description: string }[];
  }[] = [];

  // Form validation state for visual feedback
  fieldValidation = {
    reporterId: true,
    workShift: true,
    title: true,
    zone: true,
    incidentDateTime: true,
    description: true,
    injuredPersons: [] as { name: boolean, department: boolean, zoneOfPerson: boolean, gender: boolean }[]
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private reportService: ReportService,
    private authService: AuthService,
    private alertService: AlertService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.reportType = params.get('type');
      this.setReportDetails(this.reportType);
    });
    
    // Load reference data from backend
    this.loadReferenceData();
    
    // Don't auto-fill reporter ID - always let user enter it manually
    this.reportForm.reporterId = '';
    console.log('🔧 ReportComponent: Reporter ID field left empty for manual entry');

    // Setup debounced validation for ReporterId
    this.reporterIdSubject.pipe(
      debounceTime(800), // Wait 800ms after user stops typing
      distinctUntilChanged() // Only proceed if value actually changed
    ).subscribe(reporterId => {
      if (reporterId.trim()) {
        this.validateCompanyId();
      } else {
        // Reset validation state if field is empty
        this.companyIdValid = false;
        this.companyIdValidating = false;
        this.reporterInfo = null;
      }
    });
  }

  ngOnDestroy(): void {
    this.reporterIdSubject.complete();
  }

  // Load all reference data from backend
  loadReferenceData(): void {
    // Load departments
    this.reportService.getDepartments().subscribe({
      next: (data) => {
        this.departments = data;
        console.log('Departments loaded:', data);
      },
      error: (error) => console.error('Error loading departments:', error)
    });

    // Load zones
    this.reportService.getZones().subscribe({
      next: (data) => {
        this.zones = data;
        console.log('Zones loaded:', data);
      },
      error: (error) => console.error('Error loading zones:', error)
    });

    // Load shifts
    this.reportService.getShifts().subscribe({
      next: (data) => {
        this.shifts = data;
        console.log('Shifts loaded:', data);
      },
      error: (error) => console.error('Error loading shifts:', error)
    });

    // Load fracture/injury types
    this.reportService.getFractureTypes().subscribe({
      next: (data) => {
        this.fractureTypes = data;
        console.log('Fracture types loaded:', data);
      },
      error: (error) => console.error('Error loading fracture types:', error)
    });
  }

  // Called when user types in ReporterId field
  onReporterIdInput(): void {
    this.hasUserStartedTyping = true;
    // Reset validation states when user is typing
    this.companyIdValid = false;
    this.companyIdValidating = false;
    this.reporterInfo = null;
    // Trigger debounced validation
    this.reporterIdSubject.next(this.reportForm.reporterId);
  }

  // Validate Company ID with debounce
  validateCompanyId(): void {
    if (!this.reportForm.reporterId.trim()) {
      this.companyIdValid = false;
      this.companyIdValidating = false;
      this.reporterInfo = null;
      return;
    }

    this.companyIdValidating = true;
    this.reportService.validateCompanyId(this.reportForm.reporterId).subscribe({
      next: (response) => {
        this.companyIdValidating = false;
        if (response.isValid) {
          this.companyIdValid = true;
          this.reporterInfo = response;
          console.log('Company ID validated:', response);
        } else {
          this.companyIdValid = false;
          this.reporterInfo = null;
          this.alertService.showError(response.message);
        }
      },
      error: (error) => {
        this.companyIdValidating = false;
        this.companyIdValid = false;
        this.reporterInfo = null;
        this.alertService.showError('Error validating Company ID');
        console.error('Validation error:', error);
      }
    });
  }

  // Check if validation message should be shown
  shouldShowValidationMessage(): boolean {
    return this.hasUserStartedTyping && this.reportForm.reporterId.length > 0;
  }

  setReportDetails(type: string | null): void {
    switch (type) {
      case 'Hasard':
        this.reportTitle = 'Hasard Situation Report';
        break;
      case 'Nearhit':
        this.reportTitle = 'Nearhit Report';
        break;
      case 'Enviroment-aspect':
        this.reportTitle = 'Environment Aspect Report';
        break;
      case 'Improvement-Idea':
        this.reportTitle = 'Improvement Idea Report';
        break;
      case 'Incident-Management':
        this.reportTitle = 'Incident Management Report';
        break;
      default:
        this.reportTitle = 'Unknown Report';
    }
  }


  addInjury(personIndex: number): void {
    const person = this.injuredPersons[personIndex];
    console.log('🔧 Adding injury - selectedBodyPart:', person.selectedBodyPart);
    console.log('🔧 Adding injury - injuryType:', person.injuryType);
    console.log('🔧 Adding injury - severity:', person.severity);
    
    if (!person.selectedBodyPart || !person.severity || !person.injuryType) {
      this.alertService.showWarning('Please fill body part, severity and injury type before adding injury');
      return;
    }

    // Check for duplicate body part
    const existingInjury = person.injuries.find(injury => 
      injury.bodyPart.toLowerCase() === person.selectedBodyPart.toLowerCase()
    );

    if (existingInjury) {
      // Show alert for duplicate body part
      this.alertService.showWarning(`Body part "${person.selectedBodyPart}" is already selected. Please choose a different body part.`);
      return; // Don't add duplicate
    }

    // Add new injury (no duplicate found) - convert SVG ID to DB name
    const dbBodyPartName = this.svgIdToDbName(person.selectedBodyPart);
    const newInjury = {
      id: Date.now().toString(),
      bodyPart: dbBodyPartName,  // Store DB name instead of SVG ID
      injuryType: person.injuryType,
      severity: person.severity,
      description: person.description
    };
    person.injuries.push(newInjury);

    // Clear fields
    person.selectedBodyPart = '';
    person.injuryType = '';
    person.severity = '';
    person.description = '';
  }

  removeInjury(personIndex: number, injuryId: string): void {
    this.injuredPersons[personIndex].injuries = this.injuredPersons[personIndex].injuries.filter(injury => injury.id !== injuryId);
  }

  onFileSelect(event: any): void {
    const files = event.target.files;
    if (files && files.length > 0) {
      // Add new files to existing attachments instead of replacing them
      const newFiles = Array.from(files) as File[];
      this.reportForm.attachments = [...this.reportForm.attachments, ...newFiles];
      console.log('📎 Files added:', newFiles.map(f => f.name));
      console.log('📎 Total attachments:', this.reportForm.attachments.length);
    }
    // Clear the input so the same file can be selected again if needed
    event.target.value = '';
  }

  removeAttachment(index: number): void {
    if (index >= 0 && index < this.reportForm.attachments.length) {
      const removedFile = this.reportForm.attachments[index];
      this.reportForm.attachments.splice(index, 1);
      console.log('🗑️ File removed:', removedFile.name);
      console.log('📎 Remaining attachments:', this.reportForm.attachments.length);
    }
  }

  getFileSize(file: File): string {
    const sizeInKB = Math.round(file.size / 1024);
    if (sizeInKB < 1024) {
      return `${sizeInKB} KB`;
    } else {
      const sizeInMB = Math.round(sizeInKB / 1024 * 10) / 10;
      return `${sizeInMB} MB`;
    }
  }

  submitReport(): void {
    if (!this.validateForm()) {
      return;
    }

    this.loading = true;
    // Clear any existing alerts

    // Ensure required fields are not empty
    if (!this.reportForm.reporterId?.trim()) {
      this.alertService.showWarning('Reporter ID is required');
      this.loading = false;
      return;
    }
    
    if (!this.reportForm.description?.trim() || this.reportForm.description.length < 10) {
      this.alertService.showWarning('Description must be at least 10 characters');
      this.loading = false;
      return;
    }

    const reportData: CreateReportDto = {
      reporterId: this.reportForm.reporterId.trim(),
      workShift: this.reportForm.workShift,
      title: this.reportForm.title.trim(),
      type: this.reportType!,
      zone: this.reportForm.zone,
      incidentDateTime: this.reportForm.incidentDateTime ? new Date(this.reportForm.incidentDateTime).toISOString() : new Date().toISOString(),
      description: this.reportForm.description.trim(),
      immediateActionsTaken: this.reportForm.immediateActionsTaken?.trim() || undefined,
      actionStatus: this.reportForm.actionStatus || 'Non commencé', // Default value from backend
      personInChargeOfActions: this.reportForm.personInChargeOfActions?.trim() || undefined,
      dateActionsCompleted: this.reportForm.dateActionsCompleted ? new Date(this.reportForm.dateActionsCompleted).toISOString() : undefined,
      injuredPersonsCount: this.reportForm.injuredPersonsCount,
      injuredPersons: this.injuredPersons
        .filter(person => person.name?.trim()) // Only include persons with names
        .map(person => ({
          name: person.name.trim(),
          department: person.department?.trim() || undefined,
          zoneOfPerson: person.zoneOfPerson?.trim() || undefined,
          gender: person.gender || undefined,
          selectedBodyPart: person.selectedBodyPart || undefined,
          injuryType: person.injuryType || undefined,
          severity: person.severity || undefined,
          injuryDescription: person.description?.trim() || undefined,
          injuries: person.injuries && person.injuries.length > 0 
            ? person.injuries.map(injury => ({
                bodyPartId: this.getBodyPartId(injury.bodyPart),
                fractureTypeId: this.getFractureTypeId(injury.injuryType), // Use actual injury type selected by user
                severity: injury.severity || 'Minor',
                description: injury.description || 'No description provided',
                bodyPart: injury.bodyPart // For backward compatibility
              }))
            : [] // Empty array if no injuries
        })),
      attachments: this.reportForm.attachments || []
    };

    // Debug logging to see what we're sending
    console.log('🔧 Raw injured persons array:', this.injuredPersons);
    console.log('🔧 Injured persons count from form:', this.reportForm.injuredPersonsCount);
    console.log('🔧 Filtered injured persons:', this.injuredPersons.filter(person => person.name?.trim()));
    console.log('🔧 Report Data being sent:', reportData);
    console.log('🔧 Report Data JSON:', JSON.stringify(reportData, null, 2));

    this.reportService.createReport(reportData).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success) {
          // Show success message without redirect
          this.alertService.showSuccess(`Report submitted successfully! Tracking Number: ${response.trackingNumber || 'N/A'}`);
          this.resetForm(false, false); // Don't show reset message and don't clear alerts after submission
        } else {
          this.alertService.showError(response.message || 'Failed to submit report');
        }
      },
      error: (error) => {
        this.loading = false;
        console.error('🔧 Report submission error:', error);
        console.error('🔧 Error status:', error.status);
        console.error('🔧 Error details:', error.error);
        console.error('🔧 Full error object:', JSON.stringify(error, null, 2));
        
        if (error.status === 500) {
          this.alertService.showError('Server error occurred. Please check if the backend is running and try again.');
        } else {
          this.alertService.showError(error.error?.message || 'Failed to submit report. Please try again.');
        }
      }
    });
  }

  private validateForm(): boolean {
    // Reset all field validation states to true first
    this.resetFieldValidation();

    let isValid = true;
    
    // Check all required fields according to backend validation
    if (!this.reportForm.reporterId?.trim()) {
      this.fieldValidation.reporterId = false;
      this.alertService.showWarning('Reporter ID is required');
      isValid = false;
    }
    if (!this.reportForm.workShift?.trim()) {
      this.fieldValidation.workShift = false;
      this.alertService.showWarning('Work Shift is required');
      isValid = false;
    }
    if (!this.reportForm.title?.trim()) {
      this.fieldValidation.title = false;
      this.alertService.showWarning('Title is required');
      isValid = false;
    }
    if (!this.reportType?.trim()) {
      this.alertService.showWarning('Report Type is required');
      isValid = false;
    }
    if (!this.reportForm.zone?.trim()) {
      this.fieldValidation.zone = false;
      this.alertService.showWarning('Zone is required');
      isValid = false;
    }
    if (!this.reportForm.description?.trim()) {
      this.fieldValidation.description = false;
      this.alertService.showWarning('Description is required');
      isValid = false;
    }
    if (this.reportForm.description?.trim() && this.reportForm.description.trim().length < 10) {
      this.fieldValidation.description = false;
      this.alertService.showWarning('Description must be at least 10 characters');
      isValid = false;
    }
    if (!this.reportForm.incidentDateTime) {
      this.fieldValidation.incidentDateTime = false;
      this.alertService.showWarning('Incident Date and Time is required');
      isValid = false;
    }
    
    // Validate injured persons if any
    for (let i = 0; i < this.injuredPersons.length; i++) {
      const person = this.injuredPersons[i];
      
      // Ensure we have validation state for this person
      if (!this.fieldValidation.injuredPersons[i]) {
        this.fieldValidation.injuredPersons[i] = {
          name: true,
          department: true,
          zoneOfPerson: true,
          gender: true
        };
      }
      
      if (!person.name?.trim()) {
        this.fieldValidation.injuredPersons[i].name = false;
        this.alertService.showWarning(`Name is required for injured person ${i + 1}`);
        isValid = false;
      }
      
      if (!person.department?.trim()) {
        this.fieldValidation.injuredPersons[i].department = false;
        this.alertService.showWarning(`Department is required for injured person ${i + 1}`);
        isValid = false;
      }
      
      if (!person.zoneOfPerson?.trim()) {
        this.fieldValidation.injuredPersons[i].zoneOfPerson = false;
        this.alertService.showWarning(`Zone is required for injured person ${i + 1}`);
        isValid = false;
      }
      
      if (!person.gender?.trim()) {
        this.fieldValidation.injuredPersons[i].gender = false;
        this.alertService.showWarning(`Gender is required for injured person ${i + 1}`);
        isValid = false;
      }
      
      // Check if person has any injuries added
      if (person.injuries.length === 0) {
        this.alertService.showWarning(`At least one injury must be added for injured person ${i + 1}`);
        isValid = false;
      }
    }
    
    return isValid;
  }

  private resetFieldValidation(): void {
    this.fieldValidation = {
      reporterId: true,
      workShift: true,
      title: true,
      zone: true,
      incidentDateTime: true,
      description: true,
      injuredPersons: this.injuredPersons.map(() => ({
        name: true,
        department: true,
        zoneOfPerson: true,
        gender: true
      }))
    };
  }

  // Helper method to get CSS classes for form fields
  getFieldClass(fieldName: string, baseClass: string = 'w-full px-4 py-2 rounded-lg border focus:ring-2 focus:ring-blue-500'): string {
    const isValid = this.getFieldValidation(fieldName);
    if (isValid) {
      return `${baseClass} border-gray-300`;
    } else {
      return `${baseClass} border-red-500 bg-red-50`;
    }
  }


  // Helper method to get validation state for a field
  getFieldValidation(fieldName: string): boolean {
    switch (fieldName) {
      case 'reporterId': return this.fieldValidation.reporterId;
      case 'workShift': return this.fieldValidation.workShift;
      case 'title': return this.fieldValidation.title;
      case 'zone': return this.fieldValidation.zone;
      case 'incidentDateTime': return this.fieldValidation.incidentDateTime;
      case 'description': return this.fieldValidation.description;
      default: return true;
    }
  }

  // Helper method for injured person fields
  getInjuredPersonFieldClass(personIndex: number, fieldName: string, baseClass: string = 'w-full px-4 py-2 rounded-lg border focus:ring-2 focus:ring-blue-500'): string {
    const personValidation = this.fieldValidation.injuredPersons[personIndex];
    if (!personValidation) return `${baseClass} border-gray-300`;
    
    const isValid = personValidation[fieldName as keyof typeof personValidation];
    if (isValid) {
      return `${baseClass} border-gray-300`;
    } else {
      return `${baseClass} border-red-500 bg-red-50`;
    }
  }

  // Method to clear validation error when user starts typing
  onFieldInput(fieldName: string): void {
    switch (fieldName) {
      case 'reporterId': this.fieldValidation.reporterId = true; break;
      case 'workShift': this.fieldValidation.workShift = true; break;
      case 'title': this.fieldValidation.title = true; break;
      case 'zone': this.fieldValidation.zone = true; break;
      case 'incidentDateTime': this.fieldValidation.incidentDateTime = true; break;
      case 'description': this.fieldValidation.description = true; break;
    }
  }

  // Method to clear validation error for injured person fields
  onInjuredPersonFieldInput(personIndex: number, fieldName: string): void {
    if (this.fieldValidation.injuredPersons[personIndex]) {
      (this.fieldValidation.injuredPersons[personIndex] as any)[fieldName] = true;
    }
  }

  // Utility methods for dropdowns
  getReportTypes(): string[] {
    return this.reportService.getReportTypes();
  }

  getWorkShifts(): string[] {
    return this.reportService.getWorkShifts();
  }

  getStatusOptions(): string[] {
    return this.reportService.getStatusOptions();
  }

  getGenderOptions(): string[] {
    return this.reportService.getGenderOptions();
  }

  getBodyParts(): string[] {
    return this.reportService.getBodyParts();
  }

  getInjuryTypes(): string[] {
    return this.reportService.getInjuryTypes();
  }

  getInjurySeverityOptions(): string[] {
    return this.reportService.getInjurySeverityOptions();
  }

  // Helper methods to convert strings to IDs (temporary workaround)
  private getBodyPartId(bodyPart: string): number {
    // Map SVG IDs from body-map component to database IDs (from migration data)
    const bodyPartMap: { [key: string]: number } = {
      // SVG IDs from body-map.component.html -> Database IDs
      'head': 1,           // Head
      'orbit': 2,          // Eyes  
      'face': 3,           // Face
      'neck': 4,           // Neck
      'left-shoulder': 5,  // Left Shoulder
      'right-shoulder': 6, // Right Shoulder
      'left-arm': 7,       // Left Arm
      'right-arm': 8,      // Right Arm
      'left-hand': 9,      // Left Hand
      'right-hand': 10,    // Right Hand
      'chest': 11,         // Chest
      'back': 12,          // Back
      'abdomen': 13,       // Abdomen
      'left-leg': 14,      // Left Leg
      'right-leg': 15,     // Right Leg
      'left-foot': 16,     // Left Foot
      'right-foot': 17,    // Right Foot
      
      // DB Names (with spaces) to IDs - for stored data display
      'Head': 1, 'Eyes': 2, 'Face': 3, 'Neck': 4,
      'Left Shoulder': 5, 'Right Shoulder': 6,
      'Left Arm': 7, 'Right Arm': 8,
      'Left Hand': 9, 'Right Hand': 10,
      'Chest': 11, 'Back': 12, 'Abdomen': 13,
      'Left Leg': 14, 'Right Leg': 15,
      'Left Foot': 16, 'Right Foot': 17
    };
    
    console.log(`🔧 Mapping body part "${bodyPart}" to ID:`, bodyPartMap[bodyPart] || 1);
    return bodyPartMap[bodyPart] || 1; // Default to Head if not found
  }

  // Convert SVG ID to DB name for storage
  private svgIdToDbName(svgId: string): string {
    const mapping: { [key: string]: string } = {
      'head': 'Head',
      'orbit': 'Eyes',
      'face': 'Face',
      'neck': 'Neck',
      'left-shoulder': 'Left Shoulder',
      'right-shoulder': 'Right Shoulder',
      'left-arm': 'Left Arm',
      'right-arm': 'Right Arm',
      'left-hand': 'Left Hand',
      'right-hand': 'Right Hand',
      'chest': 'Chest',
      'back': 'Back',
      'abdomen': 'Abdomen',
      'left-leg': 'Left Leg',
      'right-leg': 'Right Leg',
      'left-foot': 'Left Foot',
      'right-foot': 'Right Foot'
    };
    
    return mapping[svgId] || svgId;
  }

  private getFractureTypeId(injuryTypeName: string): number {
    // Map injury type names to database IDs (from migration data)
    const fractureTypeMap: { [key: string]: number } = {
      'Cut/Laceration': 1,
      'Bruise/Contusion': 2,
      'Burn': 3,
      'Chemical Burn': 4,
      'Simple Fracture': 5,
      'Compound Fracture': 6,
      'Sprain': 7,
      'Strain': 8,
      'Puncture Wound': 9,
      'Abrasion/Scrape': 10
    };
    return fractureTypeMap[injuryTypeName] || 1; // Default to Cut/Laceration (1) if not found
  }

  // Helper method to format date for datetime-local input
  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }


  // Reset form after successful submission
  resetForm(showResetMessage: boolean = true, clearAlerts: boolean = true): void {
    // Reset main form fields
    this.reportForm = {
      reporterId: '',
      workShift: '',
      title: '',
      zone: '',
      incidentDateTime: this.formatDateForInput(new Date()),
      description: '',
      injuredPersonsCount: 0,
      immediateActionsTaken: '',
      actionStatus: 'Non commencé',
      personInChargeOfActions: '',
      dateActionsCompleted: null,
      attachments: []
    };

    // Reset injured persons array
    this.injuredPersons = [];

    // Reset component state (preserve reportType since it comes from route)
    this.reportTitle = '';
    this.loading = false;

    // Reset validation states
    this.companyIdValid = false;
    this.companyIdValidating = false;
    this.reporterInfo = null;
    this.hasUserStartedTyping = false;
    
    // Reset field validation visual states
    this.resetFieldValidation();

    // Clear any existing alerts only if requested (don't clear submit success alerts)
    if (clearAlerts) {
      this.alertService.clearAll();
    }

    // Clear file input elements
    const fileInputs = document.querySelectorAll('input[type="file"]') as NodeListOf<HTMLInputElement>;
    fileInputs.forEach(input => {
      input.value = '';
    });

    console.log('✅ Form reset successfully (including attachments)');
    
    // Only show reset success message if requested (not after submission)
    if (showResetMessage) {
      // Wait for clearAll() to complete before showing new alert
      setTimeout(() => {
        this.alertService.showSuccess('Form has been reset successfully', { autoHideDuration: 4000 });
      }, 400); // Wait longer than clearAll's 300ms timeout
    }
  }

  goHome(): void {
    console.log('🔧 ReportComponent: Going back to home page');
    this.router.navigate(['/']);
  }
}
