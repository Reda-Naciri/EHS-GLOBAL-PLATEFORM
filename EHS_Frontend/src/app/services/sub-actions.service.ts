import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SubActionDetailDto, CreateSubActionDto, UpdateSubActionDto } from '../models/report.models';

@Injectable({
  providedIn: 'root'
})
export class SubActionsService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ===== CORRECTIVE ACTION SUB-ACTIONS =====
  
  getCorrectiveActionSubActions(correctiveActionId: number): Observable<SubActionDetailDto[]> {
    return this.http.get<SubActionDetailDto[]>(`${this.apiUrl}/corrective-actions/${correctiveActionId}/sub-actions`);
  }

  createCorrectiveActionSubAction(correctiveActionId: number, dto: CreateSubActionDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/corrective-actions/${correctiveActionId}/sub-actions`, dto);
  }

  // ===== REGULAR ACTION SUB-ACTIONS =====
  
  getActionSubActions(actionId: number): Observable<SubActionDetailDto[]> {
    return this.http.get<SubActionDetailDto[]>(`${this.apiUrl}/actions/${actionId}/sub-actions`);
  }

  createActionSubAction(actionId: number, dto: CreateSubActionDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/actions/${actionId}/sub-actions`, dto);
  }

  // ===== UPDATE METHODS =====
  
  updateSubAction(subActionId: number, dto: UpdateSubActionDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/sub-actions/${subActionId}`, dto);
  }

  updateSubActionStatus(subActionId: number, status: string, userId?: string): Observable<any> {
    const body = userId ? { status, userId } : { status };
    return this.http.put(`${this.apiUrl}/sub-actions/${subActionId}/status`, body);
  }

  // Get sub-actions assigned to a specific user
  getSubActionsByAssignedUser(userId: string): Observable<SubActionDetailDto[]> {
    return this.http.get<SubActionDetailDto[]>(`${this.apiUrl}/sub-actions/assigned-to/${userId}`);
  }

  // ===== HELPER METHODS =====
  
  getDefaultCreateDto(): CreateSubActionDto {
    return {
      title: '',
      description: '',
      dueDate: undefined,
      assignedToId: undefined
    };
  }

  getStatusOptions(): string[] {
    return ['Not Started', 'In Progress', 'Completed', 'Cancelled'];
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'bg-green-100 text-green-800 border-green-300';
      case 'In Progress':
        return 'bg-blue-100 text-blue-800 border-blue-300';
      case 'Not Started':
        return 'bg-gray-100 text-gray-800 border-gray-300';
      case 'Cancelled':
        return 'bg-red-100 text-red-800 border-red-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  // ===== PROGRESS CALCULATION =====
  
  /**
   * Calculate progress value for a single sub-action
   * Not Started = 0, In Progress = 0.5, Completed = 1, Cancelled = 0
   */
  getSubActionProgressValue(status: string): number {
    switch (status) {
      case 'Completed':
        return 1;
      case 'In Progress':
        return 0.5;
      case 'Not Started':
      case 'Cancelled':
      default:
        return 0;
    }
  }

  /**
   * Calculate total progress percentage for a list of sub-actions
   */
  calculateProgressPercentage(subActions: SubActionDetailDto[]): number {
    if (!subActions || subActions.length === 0) return 0;
    
    const totalProgress = subActions.reduce((sum, subAction) => {
      return sum + this.getSubActionProgressValue(subAction.status);
    }, 0);
    
    return Math.round((totalProgress / subActions.length) * 100);
  }

  /**
   * Calculate parent status based on sub-action statuses
   */
  calculateParentStatus(subActions: SubActionDetailDto[]): string {
    if (!subActions || subActions.length === 0) return 'Not Started';

    const activeSubActions = subActions.filter(sa => sa.status !== 'Cancelled');
    if (activeSubActions.length === 0) return 'Cancelled';

    // If all active sub-actions are "Not Started"
    if (activeSubActions.every(sa => sa.status === 'Not Started')) {
      return 'Not Started';
    }

    // If all active sub-actions are "Completed"
    if (activeSubActions.every(sa => sa.status === 'Completed')) {
      return 'Completed';
    }

    // If any sub-action is "In Progress", or mixed statuses
    return 'In Progress';
  }

  /**
   * Get progress bar color class based on percentage
   */
  getProgressBarColorClass(percentage: number): string {
    if (percentage === 100) return 'bg-green-500';
    if (percentage >= 50) return 'bg-blue-400';
    if (percentage > 0) return 'bg-yellow-400';
    return 'bg-gray-300';
  }
}