import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BackendTestService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {
    // Removed console logging to reduce noise
  }

  testBackendConnection(): Observable<any> {
    // Simple test without extra logging
    return this.http.get(`${this.apiUrl}/reports`);
  }

  testAuthEndpoint(): Observable<any> {
    return this.http.get(`${this.apiUrl}/auth/test`);
  }

  testHealthEndpoint(): Observable<any> {
    return this.http.get(`${this.apiUrl}/health`);
  }

  testPingEndpoint(): Observable<any> {
    return this.http.get(`${this.apiUrl}/health/ping`);
  }

  async checkBackendStatus(): Promise<boolean> {
    try {
      const response = await this.testHealthEndpoint().toPromise();
      console.log('✅ Backend health check successful:', response);
      return true;
    } catch (error) {
      console.warn('⚠️ Backend health check failed:', error);
      return false;
    }
  }
}