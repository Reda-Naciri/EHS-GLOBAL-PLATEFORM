import { Component, HostListener, Renderer2, ElementRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardService } from '../../services/dashboard.service';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { DashboardStatsDto, RecentActivityDto } from '../../models/dashboard.models';
import { RecentReportDto } from '../../models/report.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  dropdownOpen = false;
  sidebarOpen = false;
  showAllReports = false;
  loading = true;
  error: string | null = null;

  dashboardStats: DashboardStatsDto | null = null;
  recentReports: RecentReportDto[] = [];
  recentActivity: RecentActivityDto[] = [];

  constructor(
    private renderer: Renderer2, 
    private el: ElementRef,
    private dashboardService: DashboardService,
    private reportService: ReportService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.loadDashboardData();
  }

  toggleDropdown() {
    this.dropdownOpen = !this.dropdownOpen;
  }

  toggleSidebar() {
    this.sidebarOpen = !this.sidebarOpen;
    const sidebar = this.el.nativeElement.querySelector('.sidebar');
    this.sidebarOpen
      ? this.renderer.addClass(sidebar, 'open')
      : this.renderer.removeClass(sidebar, 'open');
  }

  @HostListener('document:click', ['$event'])
  closeSidebar(event: Event) {
    if (this.sidebarOpen) {
      const sidebar = this.el.nativeElement.querySelector('.sidebar');
      const hamburger = this.el.nativeElement.querySelector('.hamburger');
      if (!sidebar.contains(event.target as Node) && !hamburger.contains(event.target as Node)) {
        this.renderer.removeClass(sidebar, 'open');
        this.sidebarOpen = false;
      }
    }
  }

  private loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    // For testing, set sample data and skip API calls
    setTimeout(() => {
      this.dashboardStats = {
        reports: {
          totalReports: 42,
          openReports: 15,
          inProgressReports: 8,
          closedReports: 19,
          reportsThisMonth: 12,
          reportsLastMonth: 8,
          monthlyGrowthRate: 50,
          reportsByType: {
            'Hasard': 18,
            'Nearhit': 12,
            'Incident-Management': 5,
            'Enviroment-aspect': 4,
            'Improvement-Idea': 3
          },
          reportsByZone: {
            'Zone A': 15,
            'Zone B': 12,
            'Zone C': 8,
            'Zone D': 7
          },
          reportsByStatus: {
            'Open': 15,
            'In Progress': 8,
            'Closed': 19
          }
        },
        actions: {
          totalActions: 28,
          completedActions: 15,
          inProgressActions: 8,
          notStartedActions: 5,
          overdueActions: 3,
          completionRate: 53.6,
          actionsByHierarchy: {
            'Elimination': 8,
            'Substitution': 6,
            'Engineering Controls': 7,
            'Administrative Measures': 4,
            'PPE': 3
          },
          actionsByStatus: {
            'Completed': 15,
            'In Progress': 8,
            'Not Started': 5
          }
        },
        users: {
          totalUsers: 156,
          activeUsers: 142,
          pendingUsers: 14,
          usersByRole: {
            'Admin': 5,
            'HSE': 12,
            'Profil': 139
          },
          usersByDepartment: {
            'Production': 45,
            'Quality': 23,
            'Maintenance': 18,
            'Safety': 12,
            'Engineering': 15,
            'Administration': 8,
            'HR': 6,
            'Finance': 5
          }
        },
        trends: {
          reportTrends: [],
          actionTrends: [],
          recentActivity: []
        }
      };

      this.recentReports = [
        {
          id: 1,
          title: 'Chemical Spill in Zone A',
          type: 'Hasard',
          status: 'Open',
          createdAt: new Date('2024-01-15'),
          reporterId: 'user123',
          zone: 'Zone A',
          injurySeverity: 'Minor',
          isUrgent: true
        },
        {
          id: 2,
          title: 'Near Miss - Forklift Operation',
          type: 'Nearhit',
          status: 'In Progress',
          createdAt: new Date('2024-01-14'),
          reporterId: 'user456',
          zone: 'Zone B',
          injurySeverity: undefined,
          isUrgent: false
        }
      ];

      this.loading = false;
    }, 1000);

    /* Original API calls - uncomment when backend is ready
    // Load dashboard stats
    this.dashboardService.getDashboardStats().subscribe({
      next: (stats) => {
        this.dashboardStats = stats;
        this.loading = false;
      },
      error: (error) => {
        this.error = 'Failed to load dashboard stats';
        this.loading = false;
        console.error('Dashboard stats error:', error);
      }
    });

    // Load recent reports
    this.reportService.getRecentReports(10).subscribe({
      next: (reports) => {
        this.recentReports = reports;
      },
      error: (error) => {
        console.error('Recent reports error:', error);
      }
    });

    // Load recent activity
    this.dashboardService.getRecentActivity(10).subscribe({
      next: (activity) => {
        this.recentActivity = activity;
      },
      error: (error) => {
        console.error('Recent activity error:', error);
      }
    });
    */
  }

  get totalReports(): number {
    return this.dashboardStats?.reports?.totalReports || 0;
  }

  get incidentManagementCount(): number {
    return this.dashboardStats?.reports?.reportsByType?.['Incident-Management'] || 0;
  }

  get openReports(): number {
    return this.dashboardStats?.reports?.openReports || 0;
  }

  get closedReports(): number {
    return this.dashboardStats?.reports?.closedReports || 0;
  }

  get totalActions(): number {
    return this.dashboardStats?.actions?.totalActions || 0;
  }

  get completedActions(): number {
    return this.dashboardStats?.actions?.completedActions || 0;
  }

  getUserName(reporterId: string): string {
    return reporterId || 'Unknown';
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

  get displayedReports() {
    const limit = this.showAllReports ? 10 : 5;
    return this.recentReports.slice(0, limit);
  }

  toggleShowMore() {
    this.showAllReports = !this.showAllReports;
  }

  logout() {
    this.authService.logout();
  }
}
