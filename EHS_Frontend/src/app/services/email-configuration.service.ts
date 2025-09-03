import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export interface EmailConfiguration {
  id?: number;
  isEmailingEnabled: boolean;
  sendProfileAssignmentEmails: boolean;
  sendHSEUpdateEmails: boolean;
  hseUpdateIntervalMinutes: number;
  sendHSEInstantReportEmails: boolean;
  sendAdminOverviewEmails: boolean;
  adminOverviewIntervalMinutes: number;
  superAdminUserIds?: string;
  createdAt?: string;
  updatedAt?: string;
  updatedByUser?: {
    id: string;
    fullName: string;
    email: string;
  };
}

export interface EmailTemplate {
  id: number;
  templateName: string;
  subject: string;
  htmlContent: string;
  plainTextContent: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  updatedByUser?: {
    id: string;
    fullName: string;
    email: string;
  };
}

export interface UpdateEmailConfigurationDto {
  isEmailingEnabled: boolean;
  sendProfileAssignmentEmails: boolean;
  sendHSEUpdateEmails: boolean;
  HSEUpdateIntervalMinutes: number;
  sendHSEInstantReportEmails: boolean;
  sendAdminOverviewEmails: boolean;
  AdminOverviewIntervalMinutes: number;
  superAdminUserIds?: string;
}

export interface UpdateEmailTemplateDto {
  subject: string;
  htmlContent: string;
  plainTextContent: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class EmailConfigurationService {
  private apiUrl = `${environment.apiUrl}/emailconfiguration`;

  constructor(private http: HttpClient) {}

  /**
   * Get current email configuration
   */
  getEmailConfiguration(): Observable<EmailConfiguration> {
    console.log('📧 EmailConfigurationService: Making request to:', this.apiUrl);
    console.log('📧 Full URL being called:', this.apiUrl);
    
    // Try the lowercase version first, then fallback to camelCase if needed
    return this.http.get<EmailConfiguration>(this.apiUrl).pipe(
      catchError(error => {
        if (error.status === 404) {
          console.log('📧 Trying fallback URL: EmailConfiguration (camelCase)');
          const fallbackUrl = `${environment.apiUrl}/EmailConfiguration`;
          return this.http.get<EmailConfiguration>(fallbackUrl);
        }
        return throwError(() => error);
      })
    );
  }

  /**
   * Update email configuration
   */
  updateEmailConfiguration(config: UpdateEmailConfigurationDto): Observable<any> {
    return this.http.put(this.apiUrl, config).pipe(
      catchError(error => {
        if (error.status === 404) {
          console.log('📧 Trying fallback URL for PUT: EmailConfiguration (camelCase)');
          const fallbackUrl = `${environment.apiUrl}/EmailConfiguration`;
          return this.http.put(fallbackUrl, config);
        }
        return throwError(() => error);
      })
    );
  }

  /**
   * Get email templates
   */
  getEmailTemplates(): Observable<EmailTemplate[]> {
    return this.http.get<EmailTemplate[]>(`${this.apiUrl}/templates`);
  }

  /**
   * Update email template
   */
  updateEmailTemplate(templateName: string, template: UpdateEmailTemplateDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/templates/${templateName}`, template);
  }

  /**
   * Test HSE update emails
   */
  testHSEUpdateEmails(): Observable<any> {
    return this.http.post(`${this.apiUrl}/test/hse-updates`, {}).pipe(
      catchError(error => {
        if (error.status === 404) {
          console.log('📧 Trying fallback URL for HSE test: EmailConfiguration (camelCase)');
          const fallbackUrl = `${environment.apiUrl}/EmailConfiguration/test/hse-updates`;
          return this.http.post(fallbackUrl, {});
        }
        return throwError(() => error);
      })
    );
  }

  /**
   * Test admin overview emails
   */
  testAdminOverviewEmails(): Observable<any> {
    return this.http.post(`${this.apiUrl}/test/admin-overview`, {}).pipe(
      catchError(error => {
        if (error.status === 404) {
          console.log('📧 Trying fallback URL for admin test: EmailConfiguration (camelCase)');
          const fallbackUrl = `${environment.apiUrl}/EmailConfiguration/test/admin-overview`;
          return this.http.post(fallbackUrl, {});
        }
        return throwError(() => error);
      })
    );
  }

  /**
   * Reset email scheduling timers
   */
  resetEmailTimers(): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-timers`, {}).pipe(
      catchError(error => {
        if (error.status === 404) {
          console.log('📧 Trying fallback URL for timer reset: EmailConfiguration (camelCase)');
          const fallbackUrl = `${environment.apiUrl}/EmailConfiguration/reset-timers`;
          return this.http.post(fallbackUrl, {});
        }
        return throwError(() => error);
      })
    );
  }

  /**
   * Get next scheduled email times
   */
  getNextScheduledEmails(): Observable<NextScheduledEmails> {
    return this.http.get<NextScheduledEmails>(`${this.apiUrl}/next-scheduled`).pipe(
      catchError(error => {
        console.error('Error getting next scheduled emails:', error);
        return throwError(() => error);
      })
    );
  }
}

// Interfaces for email timer data
export interface NextScheduledEmails {
  hseEmail?: EmailTimerInfo;
  adminEmail?: EmailTimerInfo;
}

export interface EmailTimerInfo {
  isEnabled: boolean;
  nextScheduledTime: string;
  minutesUntilNext: number;
  intervalMinutes: number;
  lastSentTime?: string;
}