import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface Alert {
  id: string;
  type: 'warning' | 'success' | 'error' | 'info';
  message: string;
  show: boolean;
  autoHide?: boolean;
  autoHideDuration?: number;
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left';
  showProgressBar?: boolean;
  allowClose?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AlertService {
  private alertsSubject = new BehaviorSubject<Alert[]>([]);
  public alerts$ = this.alertsSubject.asObservable();

  private alertIdCounter = 0;

  constructor() {}

  private generateId(): string {
    return `alert-${++this.alertIdCounter}-${Date.now()}`;
  }

  private addAlert(alert: Omit<Alert, 'id' | 'show'>): string {
    const newAlert: Alert = {
      id: this.generateId(),
      show: true,
      autoHide: true,
      autoHideDuration: 5000,
      position: 'top-right',
      showProgressBar: true,
      allowClose: true,
      ...alert
    };

    const currentAlerts = this.alertsSubject.value;
    this.alertsSubject.next([...currentAlerts, newAlert]);

    // Auto-hide if enabled
    if (newAlert.autoHide) {
      setTimeout(() => {
        this.hideAlert(newAlert.id);
      }, newAlert.autoHideDuration);
    }

    return newAlert.id;
  }

  showWarning(message: string, options?: Partial<Omit<Alert, 'id' | 'type' | 'message' | 'show'>>): string {
    return this.addAlert({
      type: 'warning',
      message,
      autoHideDuration: 5000,
      ...options
    });
  }

  showSuccess(message: string, options?: Partial<Omit<Alert, 'id' | 'type' | 'message' | 'show'>>): string {
    return this.addAlert({
      type: 'success',
      message,
      autoHideDuration: 5000,
      ...options
    });
  }

  showError(message: string, options?: Partial<Omit<Alert, 'id' | 'type' | 'message' | 'show'>>): string {
    return this.addAlert({
      type: 'error',
      message,
      autoHideDuration: 7000, // Errors stay longer
      ...options
    });
  }

  showInfo(message: string, options?: Partial<Omit<Alert, 'id' | 'type' | 'message' | 'show'>>): string {
    return this.addAlert({
      type: 'info',
      message,
      autoHideDuration: 4000,
      ...options
    });
  }

  hideAlert(id: string): void {
    const currentAlerts = this.alertsSubject.value;
    const alertIndex = currentAlerts.findIndex(alert => alert.id === id);
    
    if (alertIndex > -1) {
      // First set show to false for animation
      currentAlerts[alertIndex].show = false;
      this.alertsSubject.next([...currentAlerts]);
      
      // Then remove after animation completes
      setTimeout(() => {
        const updatedAlerts = this.alertsSubject.value.filter(alert => alert.id !== id);
        this.alertsSubject.next(updatedAlerts);
      }, 300); // Match CSS transition duration
    }
  }

  clearAll(): void {
    const currentAlerts = this.alertsSubject.value;
    // Set all to hidden first
    const hiddenAlerts = currentAlerts.map(alert => ({ ...alert, show: false }));
    this.alertsSubject.next(hiddenAlerts);
    
    // Then clear after animation
    setTimeout(() => {
      this.alertsSubject.next([]);
    }, 300);
  }

  getAlerts(): Alert[] {
    return this.alertsSubject.value;
  }

  // Utility methods for quick alerts
  quickWarning(message: string): void {
    this.showWarning(message);
  }

  quickSuccess(message: string): void {
    this.showSuccess(message);
  }

  quickError(message: string): void {
    this.showError(message);
  }

  quickInfo(message: string): void {
    this.showInfo(message);
  }
}