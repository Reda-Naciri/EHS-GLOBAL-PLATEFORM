import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { AlertService, Alert } from '../../services/alert.service';
import { SharedAlertComponent } from '../shared-alert/shared-alert.component';

@Component({
  selector: 'app-alert-container',
  standalone: true,
  imports: [CommonModule, SharedAlertComponent],
  templateUrl: './alert-container.component.html',
  styleUrls: ['./alert-container.component.css']
})
export class AlertContainerComponent implements OnInit, OnDestroy {
  alerts: Alert[] = [];
  private alertSubscription?: Subscription;

  constructor(private alertService: AlertService) {}

  ngOnInit() {
    this.alertSubscription = this.alertService.alerts$.subscribe(alerts => {
      this.alerts = alerts;
    });
  }

  ngOnDestroy() {
    if (this.alertSubscription) {
      this.alertSubscription.unsubscribe();
    }
  }

  onAlertClose(alertId: string) {
    this.alertService.hideAlert(alertId);
  }

  // Group alerts by position for better display
  getAlertsByPosition(position: string): Alert[] {
    return this.alerts.filter(alert => alert.position === position);
  }

  // Calculate top position for stacked alerts
  getTopPosition(alert: Alert, index: number): string {
    const baseTop = alert.position?.includes('top') ? 16 : 'auto'; // 1rem = 16px
    if (typeof baseTop === 'number') {
      return `${baseTop + (index * 80)}px`; // Stack with 80px spacing
    }
    return '1rem';
  }

  // Calculate bottom position for stacked alerts
  getBottomPosition(alert: Alert, index: number): string {
    const baseBottom = alert.position?.includes('bottom') ? 16 : 'auto';
    if (typeof baseBottom === 'number') {
      return `${baseBottom + (index * 80)}px`;
    }
    return '1rem';
  }
}