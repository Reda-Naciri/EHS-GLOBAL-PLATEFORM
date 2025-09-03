import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateCorrectiveActionDto {
  reportId?: number | null; // Optional for standalone corrective actions
  title: string;
  description: string;
  priority: string;
  hierarchy: string;
  assignedTo?: string;
  dueDate?: Date;
  createdByHSEId: string; // ID of the user who created the action (admin or HSE)
}

export interface UpdateCorrectiveActionDto {
  title?: string;
  description?: string;
  priority?: string;
  hierarchy?: string;
  assignedTo?: string;
  dueDate?: Date;
  status?: string;
}

export interface CorrectiveActionDetailDto {
  id: number;
  title: string;
  description: string;
  status: string;
  priority: string;
  hierarchy: string;
  assignedTo: string;
  dueDate: Date;
  createdAt: Date;
  updatedAt: Date;
  reportId: number;
  reportTitle?: string;
  reportTrackingNumber?: string;
  createdByHSEId: string; // ID of the user who created the action
  createdByName?: string; // Name of the user who created the action (populated from lookup)
  subActionsCount: number;
  subActions: any[];
  attachments: any[];
  overdue?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class CorrectiveActionsService {
  private apiUrl = `${environment.apiUrl}/corrective-actions`;

  constructor(private http: HttpClient) { }

  // Get all corrective actions for a specific report
  getCorrectiveActionsByReport(reportId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/by-report/${reportId}`);
  }

  // Get a specific corrective action by ID
  getCorrectiveAction(id: number): Observable<CorrectiveActionDetailDto> {
    return this.http.get<CorrectiveActionDetailDto>(`${this.apiUrl}/${id}`);
  }

  // Get all corrective actions with optional filtering
  getAllCorrectiveActions(status?: string, priority?: string, assignedTo?: string, reportId?: number): Observable<CorrectiveActionDetailDto[]> {
    let params = new URLSearchParams();
    if (status) params.append('status', status);
    if (priority) params.append('priority', priority);
    if (assignedTo) params.append('assignedTo', assignedTo);
    if (reportId) params.append('reportId', reportId.toString());
    
    const queryString = params.toString();
    const url = queryString ? `${this.apiUrl}?${queryString}` : this.apiUrl;
    return this.http.get<CorrectiveActionDetailDto[]>(url);
  }

  // Create a new corrective action
  createCorrectiveAction(dto: CreateCorrectiveActionDto): Observable<CorrectiveActionDetailDto> {
    return this.http.post<CorrectiveActionDetailDto>(this.apiUrl, dto);
  }

  // Update an existing corrective action
  updateCorrectiveAction(id: number, dto: UpdateCorrectiveActionDto): Observable<CorrectiveActionDetailDto> {
    return this.http.put<CorrectiveActionDetailDto>(`${this.apiUrl}/${id}`, dto);
  }

  // Delete a corrective action
  deleteCorrectiveAction(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Update corrective action status
  updateCorrectiveActionStatus(id: number, status: string): Observable<CorrectiveActionDetailDto> {
    return this.http.put<CorrectiveActionDetailDto>(`${this.apiUrl}/${id}/status`, { status });
  }

  // Abort corrective action with reason tracking
  abortCorrectiveAction(id: number, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/abort`, { reason });
  }

  // Create sub-action for a corrective action
  createSubAction(correctiveActionId: number, subActionData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/${correctiveActionId}/sub-actions`, subActionData);
  }

  // Update sub-action status using the dedicated sub-actions API
  updateSubActionStatus(subActionId: number, status: string): Observable<any> {
    return this.http.put(`${environment.apiUrl}/sub-actions/${subActionId}/status`, { status });
  }

  // Cancel sub-action (update status to Canceled)
  cancelSubAction(subActionId: number): Observable<any> {
    return this.updateSubActionStatus(subActionId, 'Canceled');
  }

  // Get dropdown data
  getPriorityOptions(): string[] {
    return ['Low', 'Medium', 'High', 'Critical'];
  }

  // Get hierarchy options from database
  getHierarchyOptions(): Observable<string[]> {
    return this.http.get<string[]>(`${environment.apiUrl}/hierarchies`);
  }

  // Fallback method for static hierarchies (in case backend is not available)
  getStaticHierarchyOptions(): string[] {
    return ['Elimination', 'Substitution', 'Mesure d\'ingenierie', 'Mesures Administratives', 'EPI'];
  }

  getStatusOptions(): string[] {
    return ['Not Started', 'In Progress', 'Completed'];
  }

  // Admin Hierarchy Management Methods
  createHierarchy(name: string, description?: string, order?: number): Observable<any> {
    return this.http.post(`${environment.apiUrl}/hierarchies`, { 
      name, 
      description, 
      order 
    });
  }

  updateHierarchy(id: number, name: string, description?: string, order?: number): Observable<any> {
    return this.http.put(`${environment.apiUrl}/hierarchies/${id}`, { 
      name, 
      description, 
      order 
    });
  }

  deleteHierarchy(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/hierarchies/${id}`);
  }

  getHierarchyDetails(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/hierarchies/details`);
  }
}