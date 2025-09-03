export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: Date;
  userId: string;
  triggeredByUserId?: string;
  relatedActionId?: number;
  relatedCorrectiveActionId?: number;
  relatedReportId?: number;
  redirectUrl?: string; // URL for navigation when notification is clicked
  readAt?: Date;
  isEmailSent?: boolean;
  emailSentAt?: Date;
}

export interface CreateNotificationDto {
  title: string;
  message: string;
  type: 'info' | 'warning' | 'success' | 'error';
  userId: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  actionUrl?: string;
  priority?: 'low' | 'medium' | 'high' | 'urgent';
}

export interface NotificationSummaryDto {
  totalCount: number;
  unreadCount: number;
  notifications: NotificationDto[];
}

export interface MarkAsReadDto {
  notificationIds: string[];
}

export interface NotificationPreferencesDto {
  emailNotifications: boolean;
  pushNotifications: boolean;
  reportUpdates: boolean;
  actionReminders: boolean;
  systemAnnouncements: boolean;
}