import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { 
  DashboardStatsDto, 
  ChartDataDto, 
  RecentActivityDto, 
  PerformanceMetricsDto 
} from '../models/dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = environment.apiUrl + environment.endpoints.dashboard;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  getDashboardStats(zone?: string): Observable<DashboardStatsDto> {
    let params = new HttpParams();
    if (zone) {
      params = params.set('zone', zone);
    }
    
    return this.http.get<DashboardStatsDto>(`${this.apiUrl}/stats`, {
      headers: this.authService.getAuthHeaders(),
      params
    });
  }

  getChartData(type: string, zone?: string): Observable<ChartDataDto> {
    let params = new HttpParams();
    if (zone) {
      params = params.set('zone', zone);
    }
    
    return this.http.get<ChartDataDto>(`${this.apiUrl}/charts/${type}`, {
      headers: this.authService.getAuthHeaders(),
      params
    });
  }

  getRecentActivity(limit: number = 20, zone?: string): Observable<RecentActivityDto[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (zone) {
      params = params.set('zone', zone);
    }
    
    return this.http.get<RecentActivityDto[]>(`${this.apiUrl}/recent-activity`, {
      headers: this.authService.getAuthHeaders(),
      params
    });
  }

  getPerformanceMetrics(zone?: string): Observable<PerformanceMetricsDto> {
    let params = new HttpParams();
    if (zone) {
      params = params.set('zone', zone);
    }
    
    return this.http.get<PerformanceMetricsDto>(`${this.apiUrl}/metrics`, {
      headers: this.authService.getAuthHeaders(),
      params
    });
  }

  // Utility methods for chart configurations
  getChartTypes(): string[] {
    return [
      'reports-by-type',
      'reports-by-status',
      'actions-by-hierarchy',
      'monthly-trends'
    ];
  }

  getChartColors(): { [key: string]: string } {
    return {
      'Hasard': '#ff6b6b',
      'Nearhit': '#4ecdc4',
      'Enviroment-aspect': '#45b7d1',
      'Improvement-Idea': '#96ceb4',
      'Incident-Management': '#feca57',
      'Unopened': '#ff9ff3',
      'Opened': '#54a0ff',
      'Closed': '#5f27cd',
      'Elimination': '#00d2d3',
      'Substitution': '#ff9f43',
      'Engineering Controls': '#10ac84',
      'Administrative Measures': '#ee5a24',
      'PPE': '#0984e3'
    };
  }

  formatChartData(data: { [key: string]: any }): any[] {
    return Object.entries(data).map(([key, value]) => ({
      name: key,
      value: value,
      color: this.getChartColors()[key] || '#8884d8'
    }));
  }

  calculatePercentage(value: number, total: number): number {
    return total > 0 ? Math.round((value / total) * 100) : 0;
  }

  formatNumber(num: number): string {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    } else if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  }

  getStatusColor(status: string): string {
    const statusColors: { [key: string]: string } = {
      'good': '#10ac84',
      'warning': '#ff9f43',
      'critical': '#ee5a24',
      'Unopened': '#ff9ff3',
      'Opened': '#54a0ff',
      'Closed': '#5f27cd',
      'Completed': '#10ac84',
      'Not Started': '#c8d6e5'
    };
    return statusColors[status] || '#8884d8';
  }

  getPriorityColor(priority: string): string {
    const priorityColors: { [key: string]: string } = {
      'Low': '#10ac84',
      'Medium': '#ff9f43',
      'High': '#ee5a24',
      'Critical': '#c23616'
    };
    return priorityColors[priority] || '#8884d8';
  }
}