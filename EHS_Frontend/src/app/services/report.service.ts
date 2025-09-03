import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { 
  CreateReportDto, 
  ReportDetailDto, 
  ReportSummaryDto, 
  RecentReportDto,
  ReportSubmissionResponseDto,
  ValidateReporterDto,
  ReporterValidationResponseDto 
} from '../models/report.models';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = environment.apiUrl + environment.endpoints.reports;
  private adminApiUrl = environment.apiUrl + '/admin';

  constructor(private http: HttpClient) {}

  createReport(report: CreateReportDto): Observable<ReportSubmissionResponseDto> {
    // Always use FormData for consistency with backend [FromForm] attribute
    console.log('🔧 Creating FormData for report submission');
    console.log('🔧 Attachments count:', report.attachments?.length || 0);
    
    const formData = new FormData();
    
    // Add all text fields to FormData with exact property names from DTO
    formData.append('ReporterId', report.reporterId);
    formData.append('WorkShift', report.workShift);
    formData.append('Title', report.title);
    formData.append('Type', report.type);
    formData.append('Zone', report.zone);
    formData.append('IncidentDateTime', report.incidentDateTime);
    formData.append('Description', report.description);
    formData.append('InjuredPersonsCount', report.injuredPersonsCount.toString());
    
    // Add optional fields if they exist
    if (report.immediateActionsTaken) {
      formData.append('ImmediateActionsTaken', report.immediateActionsTaken);
    }
    if (report.actionStatus) {
      formData.append('ActionStatus', report.actionStatus);
    }
    if (report.personInChargeOfActions) {
      formData.append('PersonInChargeOfActions', report.personInChargeOfActions);
    }
    if (report.dateActionsCompleted) {
      formData.append('DateActionsCompleted', report.dateActionsCompleted);
    }
    
    // Add injured persons data as JSON string if present
    if (report.injuredPersons && report.injuredPersons.length > 0) {
      const injuredPersonsJson = JSON.stringify(report.injuredPersons);
      console.log('🔧 Serializing injured persons to JSON:', injuredPersonsJson);
      formData.append('InjuredPersonsJson', injuredPersonsJson);
    } else {
      console.log('🔧 No injured persons to serialize');
    }
    
    // Add file attachments if any
    if (report.attachments && report.attachments.length > 0) {
      report.attachments.forEach((file) => {
        formData.append('Attachments', file, file.name);
      });
    }
    
    console.log('🔧 Sending report via FormData with', report.attachments?.length || 0, 'attachments');
    console.log('🔧 FormData keys:', Array.from(formData.keys()));
    
    // Log all FormData entries for debugging
    console.log('🔧 FormData entries:');
    for (const [key, value] of formData.entries()) {
      if (value instanceof File) {
        console.log(`  ${key}: [File: ${value.name}]`);
      } else {
        console.log(`  ${key}: ${value}`);
      }
    }
    
    // Send as multipart/form-data (Angular will set Content-Type automatically)
    return this.http.post<ReportSubmissionResponseDto>(this.apiUrl, formData);
  }

  getReports(type?: string, status?: string, zone?: string): Observable<ReportSummaryDto[]> {
    let params = new HttpParams();
    
    if (type) params = params.set('type', type);
    if (status) params = params.set('status', status);
    if (zone) params = params.set('zone', zone);

    return this.http.get<ReportSummaryDto[]>(this.apiUrl, { params });
  }

  getReportById(id: number): Observable<ReportDetailDto> {
    return this.http.get<ReportDetailDto>(`${this.apiUrl}/${id}`);
  }

  getReportByTrackingNumber(trackingNumber: string): Observable<ReportDetailDto> {
    return this.http.get<ReportDetailDto>(`${this.apiUrl}/tracking/${trackingNumber}`);
  }

  getReportForFollowUp(trackingNumber: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/follow-up/tracking/${trackingNumber}`);
  }

  getReportsByCompanyId(companyId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/by-company/${companyId}`);
  }

  updateReportStatus(id: number, status: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/status`, { newStatus: status });
  }

  openReport(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${id}/open`, {});
  }

  getRecentReports(limit: number = 10): Observable<RecentReportDto[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<RecentReportDto[]>(`${this.apiUrl}/recent`, { params });
  }

  addComment(reportId: number, content: string, author: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${reportId}/comments`, {
      reportId,
      content,
      author
    });
  }

  validateReporter(reporterId: string): Observable<ReporterValidationResponseDto> {
    return this.http.post<ReporterValidationResponseDto>(`${this.apiUrl}/validate-reporter`, {
      reporterId
    });
  }

  // Get reports with HSE filtering
  getReportsByHSE(hseAgent: string): Observable<ReportSummaryDto[]> {
    const params = new HttpParams().set('assignedHSE', hseAgent);
    return this.http.get<ReportSummaryDto[]>(this.apiUrl, { params });
  }

  // Get all HSE agents
  getHSEAgents(): Observable<string[]> {
    return this.http.get<string[]>(`${this.adminApiUrl}/hse-agents`);
  }

  // Update report status
  updateReportStatusNew(id: number, status: 'Unopened' | 'Opened' | 'Closed'): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/status`, { newStatus: status });
  }

  // ===== BACKEND INTEGRATION METHODS =====
  
  // Validate Company ID
  validateCompanyId(companyId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/validate-company/${companyId}`);
  }

  // Get Departments from backend (public endpoint)
  getDepartments(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/departments/public`);
  }

  // Get Zones from backend
  getZones(): Observable<any[]> {
    return this.http.get<any[]>(`${this.adminApiUrl}/zones`);
  }

  // Get Shifts from backend
  getShifts(): Observable<any[]> {
    return this.http.get<any[]>(`${this.adminApiUrl}/shifts`);
  }

  // Get Fracture/Injury Types from backend
  getFractureTypes(): Observable<any[]> {
    return this.http.get<any[]>(`${this.adminApiUrl}/fracture-types`);
  }

  // ===== FRONTEND UTILITY METHODS =====
  getReportTypes(): string[] {
    return [
      'Hasard',
      'Nearhit', 
      'Enviroment-aspect',
      'Improvement-Idea',
      'Incident-Management'
    ];
  }

  getWorkShifts(): string[] {
    return ['Day Shift', 'Afternoon Shift', 'Night Shift', 'Office Hours'];
  }

  getStatusOptions(): string[] {
    return ['Unopened', 'Opened', 'Closed'];
  }

  getInjurySeverityOptions(): string[] {
    return ['Minor', 'Moderate', 'Severe'];
  }

  getGenderOptions(): string[] {
    return ['Male', 'Female', 'Other', 'Prefer not to say'];
  }

  getBodyParts(): string[] {
    return [
      'Head', 'Face', 'Eye', 'Neck', 'Shoulder', 'Arm', 'Elbow', 'Wrist', 'Hand', 'Finger',
      'Chest', 'Back', 'Abdomen', 'Hip', 'Thigh', 'Knee', 'Leg', 'Ankle', 'Foot', 'Toe'
    ];
  }

  getInjuryTypes(): string[] {
    // This method kept for backward compatibility but should use getFractureTypes() from backend
    return [
      'Cut/Laceration', 'Bruise/Contusion', 'Burn', 'Chemical Burn', 'Simple Fracture', 
      'Compound Fracture', 'Sprain', 'Strain', 'Puncture Wound', 'Abrasion/Scrape'
    ];
  }

  // Download attachment
  downloadAttachment(reportId: number, attachmentId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${reportId}/attachments/${attachmentId}`, {
      responseType: 'blob'
    });
  }

  // Download attachment for follow-up (anonymous access)
  downloadFollowUpAttachment(reportId: number, attachmentId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/follow-up/${reportId}/attachments/${attachmentId}`, {
      responseType: 'blob'
    });
  }

  // ===== ADMIN HSE ASSIGNMENT METHODS =====
  // Get all HSE users for assignment dropdown
  getHSEUsers(): Observable<any[]> {
    const url = `${this.apiUrl}/hse-users`;
    console.log('🔧 ReportService: getHSEUsers called');
    console.log('🔧 URL:', url);
    return this.http.get<any[]>(url);
  }

  // Update assigned HSE agent for a report
  updateAssignedHSE(reportId: number, assignedHSEId: string | null): Observable<any> {
    const url = `${this.apiUrl}/${reportId}/assign-hse`;
    const body = { assignedHSEId: assignedHSEId };
    
    console.log('🔧 ReportService: updateAssignedHSE called');
    console.log('🔧 URL:', url);
    console.log('🔧 Body:', body);
    
    return this.http.put(url, body);
  }

  // Get HSE users from ReportAssignment endpoint
  getHSEUsersFromAssignment(): Observable<any[]> {
    const url = `${environment.apiUrl}/reportassignment/available-hse-users`;
    console.log('🔧 ReportService: getHSEUsersFromAssignment called');
    console.log('🔧 URL:', url);
    return this.http.get<any[]>(url);
  }
}