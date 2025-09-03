import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule, NgForOf, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BodyMapComponent } from '../../components/body-map/body-map.component';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { CreateReportDto } from '../../models/report.models';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [CommonModule, FormsModule, NgIf, NgForOf, BodyMapComponent],
  templateUrl: './report.component.html',
})
export class ReportComponent implements OnInit {
  reportType: string | null = null;
  reportTitle: string = '';
  loading = false;
  error: string | null = null;

  // Form data
  reportForm = {
    reporterId: '',
    workShift: 'Day',
    title: '',
    zone: '',
    incidentDateTime: new Date(),
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
    injuries: { id: string, bodyPart: string, severity: string, description: string }[];
  }[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private reportService: ReportService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.reportType = params.get('type');
      this.setReportDetails(this.reportType);
    });
    
    // Set current user as reporter
    const currentUser = this.authService.getCurrentUser();
    if (currentUser) {
      this.reportForm.reporterId = currentUser.id;
    }
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

  onInjuredCountChange(count: number): void {
    this.reportForm.injuredPersonsCount = count;
    this.injuredPersons = [];
    for (let i = 0; i < count; i++) {
      this.injuredPersons.push({
        id: i,
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
  }

  addInjury(personIndex: number): void {
    const person = this.injuredPersons[personIndex];
    if (!person.selectedBodyPart || !person.severity || !person.injuryType) {
      alert('Please fill body part, severity and injury type before adding injury');
      return;
    }
    const newInjury = {
      id: Date.now().toString(),
      bodyPart: person.selectedBodyPart,
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
      this.reportForm.attachments = Array.from(files);
    }
  }

  submitReport(): void {
    if (!this.validateForm()) {
      return;
    }

    this.loading = true;
    this.error = null;

    const reportData: CreateReportDto = {
      reporterId: this.reportForm.reporterId,
      workShift: this.reportForm.workShift,
      title: this.reportForm.title,
      type: this.reportType!,
      zone: this.reportForm.zone,
      incidentDateTime: this.reportForm.incidentDateTime,
      description: this.reportForm.description,
      immediateActionsTaken: this.reportForm.immediateActionsTaken,
      actionStatus: this.reportForm.actionStatus,
      personInChargeOfActions: this.reportForm.personInChargeOfActions,
      dateActionsCompleted: this.reportForm.dateActionsCompleted || undefined,
      injuredPersonsCount: this.reportForm.injuredPersonsCount,
      injuredPersons: this.injuredPersons.map(person => ({
        name: person.name,
        department: person.department,
        zoneOfPerson: person.zoneOfPerson,
        gender: person.gender,
        selectedBodyPart: person.selectedBodyPart,
        injuryType: person.injuryType,
        severity: person.severity,
        injuryDescription: person.description,
        injuries: person.injuries.map(injury => ({
          bodyPart: injury.bodyPart,
          severity: injury.severity,
          description: injury.description
        }))
      })),
      attachments: this.reportForm.attachments
    };

    this.reportService.createReport(reportData).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success) {
          alert('Report submitted successfully!');
          this.router.navigate(['/dashboard']);
        } else {
          this.error = response.message || 'Failed to submit report';
        }
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Failed to submit report. Please try again.';
      }
    });
  }

  private validateForm(): boolean {
    if (!this.reportForm.title || !this.reportForm.description || !this.reportForm.zone) {
      this.error = 'Please fill in all required fields';
      return false;
    }
    return true;
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
}
