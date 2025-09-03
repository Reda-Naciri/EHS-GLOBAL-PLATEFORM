import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ApplicationsService, Application } from '../../services/applications.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  expandedSection: 'apps' | 'reports' | 'assignments' | null = null;
  
  // Applications data
  applications: Application[] = [];
  loading = false;

  constructor(
    private applicationsService: ApplicationsService
  ) {}

  ngOnInit(): void {
    this.loadActiveApplications();
  }

  loadActiveApplications(): void {
    this.loading = true;
    this.applicationsService.getActiveApplications().subscribe({
      next: (applications) => {
        this.applications = applications.sort((a, b) => a.order - b.order);
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading applications:', error);
        // Fallback to default applications if service fails
        this.applications = this.getDefaultApplications();
        this.loading = false;
      }
    });
  }

  private getDefaultApplications(): Application[] {
    return [
      {
        id: 1,
        title: 'Chemical Product',
        icon: '🧪',
        redirectUrl: 'http://162.109.85.69:778/app/products',
        isActive: true,
        order: 1
      },
      {
        id: 2,
        title: 'App 2',
        icon: '🖥️',
        redirectUrl: '#',
        isActive: true,
        order: 2
      },
      {
        id: 3,
        title: 'App 3',
        icon: '💼',
        redirectUrl: '#',
        isActive: true,
        order: 3
      },
      {
        id: 4,
        title: 'App 4',
        icon: '📝',
        redirectUrl: '#',
        isActive: true,
        order: 4
      }
    ];
  }

  toggleSection(section: 'apps' | 'reports' | 'assignments') {
    if (this.expandedSection === section) {
      this.expandedSection = null; // Collapse if clicked again  
    } else {
      this.expandedSection = section; // Expand the clicked section
    }
  }

  // Utility methods
  getIconDisplay(icon: string): string {
    if (icon.includes('fa-')) {
      return `<i class="${icon}"></i>`;
    }
    return icon;
  }

  isExternalUrl(url: string): boolean {
    return url.startsWith('http://') || url.startsWith('https://');
  }

  navigateToApplication(application: Application): void {
    if (this.isExternalUrl(application.redirectUrl)) {
      window.open(application.redirectUrl, '_blank');
    } else {
      // Handle internal routing if needed
      window.location.href = application.redirectUrl;
    }
  }
}
