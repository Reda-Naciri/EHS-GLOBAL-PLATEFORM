import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { RegistrationService } from '../../services/registration.service';
import { CorrectiveActionsService } from '../../services/corrective-actions.service';
import { Subscription, interval } from 'rxjs';
import { startWith, switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent implements OnInit, OnDestroy {
  
  hasPendingUsers = false;
  hasOverdueActions = false;
  isAdmin = false;
  
  private subscriptions: Subscription[] = [];

  constructor(
    private authService: AuthService,
    private registrationService: RegistrationService,
    private correctiveActionsService: CorrectiveActionsService
  ) {}

  ngOnInit(): void {
    // Check if current user is admin
    const currentUser = this.authService.getCurrentUser();
    this.isAdmin = currentUser?.role === 'Admin';

    // Only check for indicators if user is admin or for actions
    if (this.isAdmin) {
      this.checkPendingUsers();
      // Check every 30 seconds for pending users
      const userCheckSubscription = interval(30000).pipe(
        startWith(0),
        switchMap(() => this.registrationService.getPendingRequestsCount())
      ).subscribe({
        next: (response) => {
          this.hasPendingUsers = response.count > 0;
        },
        error: (error) => {
          console.error('Error checking pending users:', error);
          this.hasPendingUsers = false;
        }
      });
      this.subscriptions.push(userCheckSubscription);
    }

    // Check for overdue actions (for all users)
    this.checkOverdueActions();
    // Check every 60 seconds for overdue actions
    const actionCheckSubscription = interval(60000).pipe(
      startWith(0),
      switchMap(() => this.correctiveActionsService.getAllCorrectiveActions())
    ).subscribe({
      next: (actions) => {
        const today = new Date();
        this.hasOverdueActions = actions.some(action => {
          if (action.status === 'Completed' || action.status === 'Canceled' || action.status === 'Aborted') {
            return false;
          }
          
          // Check action overdue
          const actionDueDate = new Date(action.dueDate);
          const actionOverdue = actionDueDate < today;
          
          // Check sub-actions overdue
          const subActionsOverdue = action.subActions?.some(subAction => {
            if (subAction.status === 'Completed' || subAction.status === 'Canceled') {
              return false;
            }
            const subActionDueDate = new Date(subAction.dueDate);
            return subActionDueDate < today;
          }) || false;
          
          return actionOverdue || subActionsOverdue;
        });
      },
      error: (error) => {
        console.error('Error checking overdue actions:', error);
        this.hasOverdueActions = false;
      }
    });
    this.subscriptions.push(actionCheckSubscription);
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  private checkPendingUsers(): void {
    if (!this.isAdmin) return;
    
    this.registrationService.getPendingRequestsCount().subscribe({
      next: (response) => {
        this.hasPendingUsers = response.count > 0;
      },
      error: (error) => {
        console.error('Error checking pending users:', error);
        this.hasPendingUsers = false;
      }
    });
  }

  private checkOverdueActions(): void {
    this.correctiveActionsService.getAllCorrectiveActions().subscribe({
      next: (actions) => {
        const today = new Date();
        this.hasOverdueActions = actions.some(action => {
          if (action.status === 'Completed' || action.status === 'Canceled' || action.status === 'Aborted') {
            return false;
          }
          
          // Check action overdue
          const actionDueDate = new Date(action.dueDate);
          const actionOverdue = actionDueDate < today;
          
          // Check sub-actions overdue
          const subActionsOverdue = action.subActions?.some(subAction => {
            if (subAction.status === 'Completed' || subAction.status === 'Canceled') {
              return false;
            }
            const subActionDueDate = new Date(subAction.dueDate);
            return subActionDueDate < today;
          }) || false;
          
          return actionOverdue || subActionsOverdue;
        });
      },
      error: (error) => {
        console.error('Error checking overdue actions:', error);
        this.hasOverdueActions = false;
      }
    });
  }
}
