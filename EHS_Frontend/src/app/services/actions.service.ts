import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateActionDto {
  reportId: number;
  title: string;
  description: string;
  hierarchy: string;
  assignedToId: string;
  dueDate: Date;
  createdById: string;
}

export interface UpdateActionDto {
  title?: string;
  description?: string;
  hierarchy?: string;
  assignedToId?: string;
  dueDate?: Date;
  status?: string;
}

export interface AbortActionDto {
  reason: string;
}

export interface ActionDetailDto {
  id: number;
  title: string;
  description: string;
  dueDate?: Date;
  status: string;
  hierarchy: string;
  assignedToId?: string;
  assignedToName?: string;
  createdById: string;
  createdByName?: string;
  createdAt: Date;
  updatedAt?: Date;
  reportId?: number;
  reportTitle?: string;
  reportTrackingNumber?: string;
  subActions: any[];
  attachments: any[];
  overdue?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ActionsService {
  private apiUrl = `${environment.apiUrl}/actions`;

  constructor(private http: HttpClient) {}

  createAction(dto: CreateActionDto): Observable<any> {
    return this.http.post(this.apiUrl, dto);
  }

  updateAction(id: number, dto: UpdateActionDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, dto);
  }

  updateActionStatus(id: number, status: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/status`, { status });
  }

  abortAction(id: number, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/abort`, { reason });
  }

  deleteAction(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getAction(id: number): Observable<ActionDetailDto> {
    return this.http.get<ActionDetailDto>(`${this.apiUrl}/${id}`);
  }

  getAllActions(status?: string, assignedTo?: string, hierarchy?: string, reportId?: number): Observable<ActionDetailDto[]> {
    let params = new URLSearchParams();
    if (status) params.append('status', status);
    if (assignedTo) params.append('assignedTo', assignedTo);
    if (hierarchy) params.append('hierarchy', hierarchy);
    if (reportId) params.append('reportId', reportId.toString());
    
    const queryString = params.toString();
    const url = queryString ? `${this.apiUrl}?${queryString}` : this.apiUrl;
    return this.http.get<ActionDetailDto[]>(url);
  }

  getSubActions(actionId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${actionId}/sub-actions`);
  }

  createSubAction(actionId: number, dto: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/${actionId}/sub-actions`, dto);
  }

  getHierarchyOptions(): Observable<string[]> {
    // Return common action hierarchy options
    return new Observable(observer => {
      observer.next([
        'Elimination',
        'Substitution', 
        'Engineering Controls',
        'Administrative Controls',
        'Personal Protective Equipment (PPE)'
      ]);
      observer.complete();
    });
  }

  getStatusOptions(): string[] {
    return ['Not Started', 'In Progress', 'Completed', 'On Hold', 'Canceled', 'Aborted'];
  }

  getPriorityOptions(): string[] {
    return ['Low', 'Medium', 'High', 'Critical'];
  }
}