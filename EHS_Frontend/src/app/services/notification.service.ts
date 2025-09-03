import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, interval } from 'rxjs';
import { environment } from '../../environments/environment';
import { 
  NotificationDto, 
  CreateNotificationDto, 
  NotificationSummaryDto, 
  MarkAsReadDto,
  NotificationPreferencesDto 
} from '../models/notification.models';

/**
 * Notification Service - Handles all notification-related API calls
 * 
 * Backend API Requirements:
 * - GET /api/notifications - Get user notifications with pagination
 * - PUT /api/notifications/mark-read - Mark specific notifications as read
 * - PUT /api/notifications/mark-all-read - Mark all notifications as read
 * - DELETE /api/notifications/{id} - Delete notification
 * - POST /api/notifications - Create notification (admin only)
 * - GET /api/notifications/preferences - Get user notification preferences
 * - PUT /api/notifications/preferences - Update user notification preferences
 */
@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = environment.apiUrl + environment.endpoints.notifications;
  
  // Reactive state for notifications
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  private unreadCountSubject = new BehaviorSubject<number>(0);
  
  public notifications$ = this.notificationsSubject.asObservable();
  public unreadCount$ = this.unreadCountSubject.asObservable();
  
  // Auto-refresh interval (30 seconds)
  private refreshInterval = 30000;
  private autoRefresh$ = interval(this.refreshInterval);

  constructor(private http: HttpClient) {
    // Start auto-refresh for notifications
    this.startAutoRefresh();
  }

  // Get notifications for current user
  getNotifications(page: number = 1, pageSize: number = 20, unreadOnly: boolean = false): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (unreadOnly) {
      params = params.set('unreadOnly', 'true');
    }

    return this.http.get<any>(this.apiUrl, { params });
  }

  // Get notification by ID
  getNotificationById(id: string): Observable<NotificationDto> {
    return this.http.get<NotificationDto>(`${this.apiUrl}/${id}`);
  }

  // Mark notifications as read
  markAsRead(notificationIds: string[]): Observable<{ message: string }> {
    const payload: MarkAsReadDto = { notificationIds };
    return this.http.put<{ message: string }>(`${this.apiUrl}/mark-read`, payload);
  }

  // Mark all notifications as read
  markAllAsRead(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/mark-all-read`, {});
  }

  // Delete notification
  deleteNotification(id: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  // Create notification (admin only)
  createNotification(notification: CreateNotificationDto): Observable<{ id: string; message: string }> {
    return this.http.post<{ id: string; message: string }>(this.apiUrl, notification);
  }

  // Get notification preferences
  getPreferences(): Observable<NotificationPreferencesDto> {
    return this.http.get<NotificationPreferencesDto>(`${this.apiUrl}/preferences`);
  }

  // Update notification preferences
  updatePreferences(preferences: NotificationPreferencesDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/preferences`, preferences);
  }

  // Load and update local state
  refreshNotifications(): void {
    this.getNotifications(1, 20).subscribe({
      next: (response: any) => {
        console.log('📡 Notification API response:', response);
        // Handle the backend response structure which includes totalCount, unreadCount, and notifications
        if (response && response.notifications) {
          console.log('🔔 NOTIFICATION DEBUG: Received notifications:', response.notifications);
          response.notifications.forEach((notif: any, index: number) => {
            console.log(`🔔 NOTIFICATION ${index + 1}: title="${notif.title}", type="${notif.type}", color="${this.getTypeColor(notif.type)}"`);
          });
          this.notificationsSubject.next(response.notifications);
          this.unreadCountSubject.next(response.unreadCount || 0);
        } else {
          console.warn('❌ Unexpected notification API response format:', response);
          this.notificationsSubject.next([]);
          this.unreadCountSubject.next(0);
        }
      },
      error: (error) => {
        console.error('❌ Failed to refresh notifications:', error);
        if (error.status === 401) {
          console.warn('🔐 Notification API requires authentication - user may need to log in');
        } else {
          console.warn('🚫 Backend notification API not available. Please ensure the API server is running and the /notifications endpoint is implemented.');
        }
        // Reset to empty state when backend is not available
        this.notificationsSubject.next([]);
        this.unreadCountSubject.next(0);
      }
    });
  }

  // Start auto-refresh
  private startAutoRefresh(): void {
    // Initial load
    this.refreshNotifications();
    
    // Auto-refresh every 30 seconds
    this.autoRefresh$.subscribe(() => {
      this.refreshNotifications();
    });
  }

  // Local state management
  getCurrentNotifications(): NotificationDto[] {
    return this.notificationsSubject.value;
  }

  getCurrentUnreadCount(): number {
    return this.unreadCountSubject.value;
  }

  // Mark notification as read locally (optimistic update)
  markAsReadLocal(notificationId: string): void {
    const currentNotifications = this.notificationsSubject.value;
    const updatedNotifications = currentNotifications.map(notification => 
      notification.id === notificationId 
        ? { ...notification, isRead: true }
        : notification
    );
    
    this.notificationsSubject.next(updatedNotifications);
    
    // Update unread count
    const unreadCount = updatedNotifications.filter(n => !n.isRead).length;
    this.unreadCountSubject.next(unreadCount);
    
    // Sync with backend using the correct endpoint format
    this.http.post(`${this.apiUrl}/${notificationId}/mark-read`, {}).subscribe({
      next: (response) => {
        console.log('✅ Notification marked as read:', response);
      },
      error: (error) => {
        console.error('❌ Failed to mark notification as read:', error);
        // Revert on error
        this.refreshNotifications();
      }
    });
  }

  // Get notification icon based on type
  getNotificationIcon(type: string): any {
    const iconMappings = {
      // Information notifications (blue)
      'SubActionUpdatedByProfile': { 'fas': true, 'fa-sync': true, 'text-blue-500': true },
      'ActionCreatedByAdmin': { 'fas': true, 'fa-plus-circle': true, 'text-blue-500': true },
      'ReportSubmitted': { 'fas': true, 'fa-file-alt': true, 'text-blue-500': true },
      'ReportAssigned': { 'fas': true, 'fa-user-check': true, 'text-blue-500': true },
      'CommentAdded': { 'fas': true, 'fa-comment': true, 'text-blue-500': true },
      'ActionAdded': { 'fas': true, 'fa-tasks': true, 'text-blue-500': true },
      'DailyUpdate': { 'fas': true, 'fa-calendar-day': true, 'text-blue-500': true },
      
      // Warning/Reminder notifications (yellow/orange)
      'CorrectiveActionOverdue': { 'fas': true, 'fa-exclamation-triangle': true, 'text-yellow-500': true },
      'SubActionOverdue': { 'fas': true, 'fa-clock': true, 'text-yellow-500': true },
      'DeadlineApproaching': { 'fas': true, 'fa-hourglass-half': true, 'text-yellow-500': true },
      'OverdueAlert': { 'fas': true, 'fa-exclamation-triangle': true, 'text-yellow-500': true },
      'RegistrationRequest': { 'fas': true, 'fa-user-plus': true, 'text-yellow-500': true },
      
      // Alert/Error notifications (red)
      'ActionAbortedByAdmin': { 'fas': true, 'fa-ban': true, 'text-red-500': true },
      'SubActionCancelledByAdmin': { 'fas': true, 'fa-times-circle': true, 'text-red-500': true },
      'ActionCancelled': { 'fas': true, 'fa-times-circle': true, 'text-red-500': true },
      'SystemError': { 'fas': true, 'fa-exclamation-circle': true, 'text-red-500': true },
      'CriticalAlert': { 'fas': true, 'fa-radiation': true, 'text-red-500': true },
      
      // Fallback icons
      'info': { 'fas': true, 'fa-info-circle': true, 'text-blue-500': true },
      'warning': { 'fas': true, 'fa-exclamation-triangle': true, 'text-yellow-500': true },
      'success': { 'fas': true, 'fa-check-circle': true, 'text-green-500': true },
      'error': { 'fas': true, 'fa-times-circle': true, 'text-red-500': true }
    };
    return iconMappings[type as keyof typeof iconMappings] || iconMappings.info;
  }

  // Get semantic color for notification type (red=alert, yellow=reminder, blue=info)
  getTypeColor(type: string): any {
    const colorMappings = {
      // Information notifications (blue)
      'SubActionUpdatedByProfile': { 'bg-blue-100': true, 'text-blue-800': true },
      'ActionCreatedByAdmin': { 'bg-blue-100': true, 'text-blue-800': true },
      'ReportSubmitted': { 'bg-blue-100': true, 'text-blue-800': true },
      'ReportAssigned': { 'bg-blue-100': true, 'text-blue-800': true },
      'CommentAdded': { 'bg-blue-100': true, 'text-blue-800': true },
      'ActionAdded': { 'bg-blue-100': true, 'text-blue-800': true },
      'DailyUpdate': { 'bg-blue-100': true, 'text-blue-800': true },
      
      // Warning/Reminder notifications (yellow)
      'CorrectiveActionOverdue': { 'bg-yellow-100': true, 'text-yellow-800': true },
      'SubActionOverdue': { 'bg-yellow-100': true, 'text-yellow-800': true },
      'DeadlineApproaching': { 'bg-yellow-100': true, 'text-yellow-800': true },
      'OverdueAlert': { 'bg-yellow-100': true, 'text-yellow-800': true },
      'RegistrationRequest': { 'bg-yellow-100': true, 'text-yellow-800': true },
      
      // Alert/Error notifications (red)
      'ActionAbortedByAdmin': { 'bg-red-100': true, 'text-red-800': true },
      'SubActionCancelledByAdmin': { 'bg-red-100': true, 'text-red-800': true },
      'ActionCancelled': { 'bg-red-100': true, 'text-red-800': true },
      'SystemError': { 'bg-red-100': true, 'text-red-800': true },
      'CriticalAlert': { 'bg-red-100': true, 'text-red-800': true }
    };
    return colorMappings[type as keyof typeof colorMappings] || { 'bg-gray-100': true, 'text-gray-800': true };
  }

  // Format relative time
  getRelativeTime(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - new Date(date).getTime();
    const diffMinutes = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMinutes < 1) return 'Just now';
    if (diffMinutes < 60) return `${diffMinutes}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    
    return new Date(date).toLocaleDateString();
  }

}