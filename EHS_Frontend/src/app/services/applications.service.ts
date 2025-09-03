import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Application {
  id?: number;
  title: string;
  icon: string; // emoji or icon class
  redirectUrl: string;
  isActive: boolean;
  order: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateApplicationDto {
  title: string;
  icon: string;
  redirectUrl: string;
  isActive: boolean;
  order: number;
}

export interface UpdateApplicationDto {
  title?: string;
  icon?: string;
  redirectUrl?: string;
  isActive?: boolean;
  order?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApplicationsService {
  private baseUrl = environment.apiUrl + environment.endpoints.applications;
  private applicationsSubject = new BehaviorSubject<Application[]>([]);
  public applications$ = this.applicationsSubject.asObservable();

  constructor(private http: HttpClient) {
    console.log('🔧 ApplicationsService: Base URL:', this.baseUrl);
  }

  // Get all applications
  getApplications(): Observable<Application[]> {
    return this.http.get<Application[]>(this.baseUrl);
  }

  // Get applications for public display (active only)
  getActiveApplications(): Observable<Application[]> {
    return this.http.get<Application[]>(`${this.baseUrl}/active`);
  }

  // Create new application
  createApplication(createDto: CreateApplicationDto): Observable<Application> {
    return this.http.post<Application>(this.baseUrl, createDto);
  }

  // Update application
  updateApplication(id: number, updateDto: UpdateApplicationDto): Observable<Application> {
    return this.http.put<Application>(`${this.baseUrl}/${id}`, updateDto);
  }

  // Delete application
  deleteApplication(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // Toggle application status
  toggleApplicationStatus(id: number): Observable<Application> {
    return this.http.patch<Application>(`${this.baseUrl}/${id}/toggle-status`, {});
  }

  // Reorder applications
  reorderApplications(applications: { id: number; order: number }[]): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/reorder`, { applications });
  }

  // Update applications cache
  updateApplicationsCache(applications: Application[]): void {
    this.applicationsSubject.next(applications);
  }

  // Get current applications from cache
  getCurrentApplications(): Application[] {
    return this.applicationsSubject.value;
  }
}