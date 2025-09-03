import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { INJURYCARDS, REPORTS, USERS } from '../../data/demo-data';
import { Report, User, InjuryCard } from '../../data/demo-data';
import { BodyMapComponent } from '../../components/body-map/body-map.component';

@Component({
  selector: 'app-report-details',
  standalone: true,
  imports: [CommonModule, FormsModule, BodyMapComponent],
  templateUrl: './report-details.component.html',
})
export class ReportDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);

  injuryCards: InjuryCard[] = [];
  isDescriptionExpanded = false;

  reportId = this.route.snapshot.paramMap.get('id');
  report: Report | undefined;
  reporter: User | undefined;
  lastUpdatedBy: User | undefined;

  newComment = {
    author: 'Current User', // à remplacer avec utilisateur connecté
    message: ''
  };

  ngOnInit() {
    console.log('✅ ngOnInit executed');
    this.report = REPORTS.find(r => r.id === this.reportId);

    if (this.report) {
      this.reporter = USERS.find(u => u.id === this.report?.reporterId);
      this.lastUpdatedBy = USERS.find(u =>
        this.report?.comments?.length
          ? u.id === this.report.comments[0].author
          : false
      );
      this.injuryCards = INJURYCARDS.filter(card =>
        this.report?.injuryCardIds?.includes(card.id)
      );

      console.log('🧠 Loaded report:', this.report);
      console.log('💥 Injury cards found:', this.injuryCards);
    } else {
      console.warn('❌ Report not found with ID:', this.reportId);
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Completed':
        return 'status-badge status-completed';
      case 'In Progress':
        return 'status-badge status-in-progress';
      case 'Not Started':
        return 'status-badge status-not-started';
      default:
        return 'status-badge';
    }
  }

  getReportProgress(report: Report): number {
    let total = 0;
    let progress = 0;

    for (const action of report.actions) {
      total++;
      if (action.status === 'Completed') {
        progress += 1;
      } else if (action.status === 'In Progress') {
        progress += 0.5;
      }

      for (const sub of action.subActions) {
        total++;
        if (sub.status === 'Completed') {
          progress += 1;
        } else if (sub.status === 'In Progress') {
          progress += 0.5;
        }
      }
    }

    return total === 0 ? 0 : Math.round((progress / total) * 100);
  }

  getProgressBadgeClass(report: Report): string {
    const progress = this.getReportProgress(report);
    if (progress === 100) return 'status-completed';
    if (progress >= 50) return 'status-in-progress';
    return 'status-not-started';
  }

  getProgressBarColorClass(report: Report): string {
    const progress = this.getReportProgress(report);
    if (progress === 100) return 'bg-green-500';
    if (progress > 50) return 'bg-blue-400';
    if (progress > 25) return 'bg-yellow-400';
    return 'bg-red-500';
  }

  getProgress(status: 'Completed' | 'In Progress' | 'Not Started'): number {
    switch (status) {
      case 'Completed':
        return 100;
      case 'In Progress':
        return 60;
      case 'Not Started':
        return 0;
      default:
        return 0;
    }
  }

  getUserName(userId: string): string {
    const user = USERS.find(u => u.id === userId);
    return user ? `${user.firstName} ${user.lastName}` : 'Unknown';
  }

  getUserAvatar(author: string): string {
    const user = USERS.find(u => u.id === author);
    return user?.avatar || 'https://randomuser.me/api/portraits/lego/1.jpg';
  }

  getCurrentUserAvatar(): string {
    return 'https://randomuser.me/api/portraits/men/1.jpg';
  }

  submitComment() {
    if (!this.newComment.message.trim() || !this.report) return;

    const newEntry = {
      ...this.newComment,
      id: Date.now().toString(),
      timestamp: new Date().toLocaleString()
    };

    this.report.comments.push(newEntry);
    this.newComment.message = '';
  }
}
