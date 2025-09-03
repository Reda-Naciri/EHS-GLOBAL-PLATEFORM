import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmailConfigurationService, NextScheduledEmails, EmailTimerInfo } from '../../services/email-configuration.service';
import { interval, Subscription } from 'rxjs';

@Component({
  selector: 'app-email-timer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './email-timer.component.html',
  styleUrls: ['./email-timer.component.css']
})
export class EmailTimerComponent implements OnInit, OnDestroy {
  
  nextEmails: NextScheduledEmails = {};
  private updateSubscription?: Subscription;
  private timerSubscription?: Subscription;
  private lastUpdateTime?: number;

  constructor(private emailConfigService: EmailConfigurationService) {}

  ngOnInit(): void {
    this.loadNextScheduledEmails();
    this.startTimerUpdates();
  }

  private startTimerUpdates(): void {
    // Update from server every 30 seconds by default
    // But more frequently when emails are about to be sent
    this.scheduleNextServerUpdate();
    
    // Update countdown display every second for real-time countdown
    this.updateSubscription = interval(1000).subscribe(() => {
      this.updateLocalCountdown();
      this.checkIfNeedFasterRefresh();
    });
  }

  private scheduleNextServerUpdate(): void {
    // Clear existing timer subscription
    this.timerSubscription?.unsubscribe();
    
    // Determine refresh interval based on how soon emails are due
    const refreshInterval = this.getOptimalRefreshInterval();
    
    this.timerSubscription = interval(refreshInterval).subscribe(() => {
      this.loadNextScheduledEmails();
      this.scheduleNextServerUpdate(); // Reschedule for next interval
    });
  }

  private getOptimalRefreshInterval(): number {
    let minMinutesUntilNext = 30; // Default to 30 minutes
    
    if (this.nextEmails.hseEmail?.minutesUntilNext) {
      minMinutesUntilNext = Math.min(minMinutesUntilNext, this.nextEmails.hseEmail.minutesUntilNext);
    }
    
    if (this.nextEmails.adminEmail?.minutesUntilNext) {
      minMinutesUntilNext = Math.min(minMinutesUntilNext, this.nextEmails.adminEmail.minutesUntilNext);
    }

    // If email is due within 2 minutes, refresh every 5 seconds
    if (minMinutesUntilNext <= 2) {
      return 5000; // 5 seconds
    }
    // If email is due within 10 minutes, refresh every 15 seconds  
    else if (minMinutesUntilNext <= 10) {
      return 15000; // 15 seconds
    }
    // Otherwise, refresh every 30 seconds
    else {
      return 30000; // 30 seconds
    }
  }

  private checkIfNeedFasterRefresh(): void {
    // Check if any email was just sent (time jumped forward significantly)
    const currentTime = Date.now();
    if (this.lastUpdateTime && (currentTime - this.lastUpdateTime) > 35000) {
      // If more than 35 seconds passed, likely an email was sent, refresh immediately
      this.loadNextScheduledEmails();
    }
    this.lastUpdateTime = currentTime;
  }

  ngOnDestroy(): void {
    this.updateSubscription?.unsubscribe();
    this.timerSubscription?.unsubscribe();
  }

  private loadNextScheduledEmails(): void {
    this.updateSubscription = this.emailConfigService.getNextScheduledEmails().subscribe({
      next: (data) => {
        // Check if timer was reset (email was sent)
        const wasReset = this.detectTimerReset(data);
        
        this.nextEmails = data;
        
        // If timer was reset, reschedule server updates for optimal frequency
        if (wasReset) {
          this.scheduleNextServerUpdate();
        }
      },
      error: (error) => {
        console.error('Error loading next scheduled emails:', error);
      }
    });
  }

  private detectTimerReset(newData: NextScheduledEmails): boolean {
    // Check if HSE timer was reset (much more time remaining than expected)
    if (this.nextEmails.hseEmail && newData.hseEmail) {
      const oldMinutes = this.nextEmails.hseEmail.minutesUntilNext;
      const newMinutes = newData.hseEmail.minutesUntilNext;
      // If new time is significantly more than old time minus 1 minute, timer was reset
      if (newMinutes > oldMinutes + 5) {
        return true;
      }
    }

    // Check if Admin timer was reset
    if (this.nextEmails.adminEmail && newData.adminEmail) {
      const oldMinutes = this.nextEmails.adminEmail.minutesUntilNext;
      const newMinutes = newData.adminEmail.minutesUntilNext;
      // If new time is significantly more than old time minus 1 minute, timer was reset
      if (newMinutes > oldMinutes + 5) {
        return true;
      }
    }

    return false;
  }

  getProgressPercentage(timerInfo: EmailTimerInfo): number {
    if (!timerInfo || timerInfo.minutesUntilNext <= 0) {
      return 100; // Complete circle when time is up
    }
    
    const totalMinutes = timerInfo.intervalMinutes;
    const elapsedMinutes = totalMinutes - timerInfo.minutesUntilNext;
    return Math.max(0, Math.min(100, (elapsedMinutes / totalMinutes) * 100));
  }

  getTimeDisplay(timerInfo: EmailTimerInfo): string {
    if (!timerInfo) return '--';
    
    const minutes = Math.floor(timerInfo.minutesUntilNext);
    
    if (minutes <= 0) {
      return 'Due now';
    } else if (minutes < 60) {
      return `${minutes}m`;
    } else if (minutes < 1440) { // Less than 24 hours
      const hours = Math.floor(minutes / 60);
      const remainingMinutes = minutes % 60;
      return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
    } else { // 24 hours or more
      const days = Math.floor(minutes / 1440);
      const remainingHours = Math.floor((minutes % 1440) / 60);
      return remainingHours > 0 ? `${days}d ${remainingHours}h` : `${days}d`;
    }
  }

  getIntervalDisplay(timerInfo: EmailTimerInfo): string {
    if (!timerInfo) return '';
    
    const minutes = timerInfo.intervalMinutes;
    
    if (minutes < 60) {
      return `Every ${minutes}m`;
    } else if (minutes < 1440) {
      const hours = minutes / 60;
      return `Every ${hours}h`;
    } else {
      const days = minutes / 1440;
      return `Every ${days}d`;
    }
  }

  refreshTimers(): void {
    this.loadNextScheduledEmails();
  }

  private updateLocalCountdown(): void {
    // Update countdown for HSE email
    if (this.nextEmails.hseEmail) {
      const now = new Date();
      const nextTime = new Date(this.nextEmails.hseEmail.nextScheduledTime);
      const minutesUntilNext = (nextTime.getTime() - now.getTime()) / (1000 * 60);
      this.nextEmails.hseEmail.minutesUntilNext = Math.max(0, minutesUntilNext);
    }

    // Update countdown for Admin email
    if (this.nextEmails.adminEmail) {
      const now = new Date();
      const nextTime = new Date(this.nextEmails.adminEmail.nextScheduledTime);
      const minutesUntilNext = (nextTime.getTime() - now.getTime()) / (1000 * 60);
      this.nextEmails.adminEmail.minutesUntilNext = Math.max(0, minutesUntilNext);
    }
  }
}