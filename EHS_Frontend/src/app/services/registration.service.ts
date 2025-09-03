import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { RegisterRequestDto, RegisterRequestResponse } from '../models/registration.models';

@Injectable({
  providedIn: 'root'
})
export class RegistrationService {
  private apiUrl = `${environment.apiUrl}/register-request`;
  private pendingUsersUrl = `${environment.apiUrl}/pending-users`;

  constructor(private http: HttpClient) {}

  submitRegistrationRequest(request: RegisterRequestDto): Observable<RegisterRequestResponse> {
    return this.http.post<RegisterRequestResponse>(this.apiUrl, request);
  }

  getPendingRequests(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  getPendingRequestsCount(): Observable<{count: number}> {
    return this.http.get<{count: number}>(`${this.apiUrl}/count`);
  }

  approveRequest(requestId: string): Observable<any> {
    const url = `${this.apiUrl}/${requestId}/approve`;
    console.log(`🚀 Service: Making PUT request to: ${url}`);
    console.log(`📦 Service: Request body: {}`);
    return this.http.put<any>(url, {});
  }

  rejectRequest(requestId: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${requestId}/reject`, {});
  }
}