import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { REPORTS, USERS } from '../../data/demo-data';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './reports-list.component.html',
  styleUrls: ['./reports-list.component.css']
})
export class ReportsListComponent {
  reports = REPORTS.map(report => {
    const author = USERS.find(user => user.id === report.reporterId)?.fullName || 'Unknown';
    return {
      ...report,
      icon: this.getIcon(report.type),
      author,
      expanded: false
    };
  });

  searchQuery: string = '';
  selectedType: string | null = null;
  selectedStatus: string | null = null;

  toggle(report: any) {
    report.expanded = !report.expanded;
  }

  toggleTypeFilter(type: string) {
    this.selectedType = this.selectedType === type ? null : type;
  }

  toggleStatusFilter(status: string) {
    this.selectedStatus = this.selectedStatus === status ? null : status;
  }

  get filteredReports() {
    return this.reports.filter(report => {
      const matchesSearch =
        report.title.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.description.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.zone.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.type.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        report.status.toLowerCase().includes(this.searchQuery.toLowerCase());

      const matchesType = !this.selectedType || report.type === this.selectedType;
      const matchesStatus = !this.selectedStatus || report.status === this.selectedStatus;
      const notIncident = report.type !== 'Incident-Management';

      return matchesSearch && matchesType && matchesStatus && notIncident;
    });
  }

  getIcon(type: string): string {
    switch (type) {
      case 'Hasard': return '📄';
      case 'Nearhit': return '⚠️';
      case 'Incident-Management': return '🚨';
      case 'Enviroment-aspect': return '🏭';
      case 'Improvement-Idea': return '🛠️';
      default: return '📝';
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Not Started': return 'not-started';
      case 'In Progress': return 'in-progress';
      case 'Completed': return 'completed';
      default: return 'unknown';
    }
  }

  getTypeCount(type: string): number {
    return this.reports.filter(report => report.type !== 'Incident-Management' && report.type === type).length;
  }

  getTypePercentage(type: string): number {
    const total = this.reports.filter(report => report.type !== 'Incident-Management').length;
    const count = this.getTypeCount(type);
    return total > 0 ? (count / total) * 100 : 0;
  }

  getStatusCount(status: string): number {
    return this.reports.filter(report => report.type !== 'Incident-Management' && report.status === status).length;
  }

  getTypeClass(type: string): string {
    switch (type) {
      case 'Hasard': return 'type-hazard';
      case 'Nearhit': return 'type-nearhit';
      case 'Enviroment-aspect': return 'type-environment';
      case 'Improvement-Idea': return 'type-improvement';
      default: return '';
    }
  }
}
