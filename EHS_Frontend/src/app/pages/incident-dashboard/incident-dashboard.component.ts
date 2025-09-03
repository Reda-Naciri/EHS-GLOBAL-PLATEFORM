import { Component, OnInit, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Chart } from 'chart.js/auto';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { AlertService } from '../../services/alert.service';
import { ReportSummaryDto, ReportDetailDto } from '../../models/report.models';
import { UserDto } from '../../models/auth.models';
import { Subscription, forkJoin } from 'rxjs';

type SeverityLevel = 'Minor' | 'Moderate' | 'Severe';


@Component({
  selector: 'app-incident-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './incident-dashboard.component.html',
})


export class IncidentDashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  incidentReports: ReportSummaryDto[] = []; // For list display (affected by team view)
  detailedReports: ReportDetailDto[] = []; // For list display (affected by team view)
  
  // HSE personal data (always based on assigned reports for indicators)
  hsePersonalReports: ReportSummaryDto[] = [];
  hsePersonalDetailedReports: ReportDetailDto[] = [];
  
  allIncidentReports: ReportSummaryDto[] = []; // Store all reports for team view
  allDetailedReports: ReportDetailDto[] = []; // Store all detailed reports
  
  hasIncidentData = false;
  loading = true;
  error: string | null = null;
  showTeamView = false; // Toggle between personal and team view - HSE users start with personal view
  
  // Trend chart state
  trendViewMode: 'total' | 'bodyParts' | 'zones' | 'shifts' = 'total';
  
  // Time period navigation for swipeable trends
  currentTimeRange: 'last7Days' | 'last4Weeks' | 'last3Months' | 'last6Months' | 'last12Months' | 'allTime' = 'last3Months';
  availableTimePeriods: string[] = [];
  currentPeriodIndex = 0;
  periodsToShow = 3; // Default to show 3 periods
  
  // Data organized by different time periods
  private dailyData: { [key: string]: { bodyParts: any, zones: any, shifts: any, total: number } } = {};
  private weeklyData: { [key: string]: { bodyParts: any, zones: any, shifts: any, total: number } } = {};
  private monthlyData: { [key: string]: { bodyParts: any, zones: any, shifts: any, total: number } } = {};
  
  // Touch/swipe support
  private touchStartX = 0;
  private touchEndX = 0;
  private minSwipeDistance = 50;
  
  // Responsive display control
  isMobile = false;
  isTablet = false;
  isDesktop = false;
  
  // Status counts for KPI cards
  statusCounts = {
    unopened: 0,
    opened: 0,
    closed: 0
  };
  
  // Filtering
  filters = {
    status: '',
    zone: '',
    severity: '',
    hseAgent: ''
  };
  
  filteredReports: ReportSummaryDto[] = [];
  availableZones: string[] = [];
  availableHSEAgents: string[] = [];
  
  // HSE assignment properties
  hseUsers: UserDto[] = [];
  showingAssignmentDropdown: { [key: number]: boolean } = {};
  selectedHSEAgent: { [key: number]: string } = {};
  
  private subscription?: Subscription;

  kpiCards = [
    {
      label: 'Total Incidents',
      value: 0,
      trend: '',
      trendIcon: '',
      trendClass: '',
      icon: 'fa-ambulance',
      iconBg: 'bg-blue-100',
      iconColor: '#2563eb'
    },
    {
      label: 'Severity Index',
      value: '–',
      trend: '',
      trendIcon: '',
      trendClass: '',
      icon: 'fa-exclamation-triangle',
      iconBg: 'bg-red-100',
      iconColor: '#dc2626'
    },
    {
      label: 'Top Affected Body Part',
      value: '–',
      trend: '',
      trendIcon: '',
      trendClass: '',
      icon: 'fa-hand-paper',
      iconBg: 'bg-yellow-100',
      iconColor: '#ca8a04'
    },
    {
      label: 'High Risk Zone',
      value: '–',
      trend: '',
      trendIcon: '',
      trendClass: '',
      icon: 'fa-map-marker-alt',
      iconBg: 'bg-purple-100',
      iconColor: '#7c3aed'
    }
  ];

  constructor(
    private reportService: ReportService,
    private authService: AuthService,
    private alertService: AlertService
  ) {}

  isHSEUser(): boolean {
    const user = this.authService.getCurrentUser();
    const isHSE = user?.role === 'HSE' || user?.role === 'HSE Agent';
    console.log('🔍 User role check:', { role: user?.role, isHSE: isHSE });
    return isHSE;
  }

  getCurrentUserName(): string {
    const user = this.authService.getCurrentUser();
    console.log('🔍 Current user object:', user);
    
    // Try multiple name formats to find the best match
    let name = '';
    if (user?.fullName) {
      name = user.fullName;
    } else if (user?.firstName && user?.lastName) {
      name = `${user.firstName} ${user.lastName}`;
    } else if (user?.['username']) {
      name = user['username'];
    } else if (user?.email) {
      name = user.email.split('@')[0]; // fallback to email username
    }
    
    console.log('🔍 Resolved user name:', name);
    console.log('🔍 User properties available:', Object.keys(user || {}));
    return name.trim();
  }

  ngOnInit(): void {
    // Ensure HSE users start with personal view
    this.showTeamView = false;
    this.checkScreenSize();
    this.loadIncidentReports();
    this.loadHSEUsers();
    
    // Listen for window resize
    window.addEventListener('resize', () => this.checkScreenSize());
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    window.removeEventListener('resize', () => this.checkScreenSize());
  }
  
  checkScreenSize(): void {
    const width = window.innerWidth;
    this.isMobile = width <= 768;
    this.isTablet = width > 768 && width <= 1024;
    this.isDesktop = width > 1024;
  }

  // Responsive grid classes - completely restructured for tablet
  getKpiGridClass(): string {
    if (this.isMobile) return 'grid grid-cols-1 gap-3 mb-4';
    if (this.isTablet) return 'grid grid-cols-2 gap-3 mb-4 max-w-full';
    return 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8';
  }

  getChartsGridClass(): string {
    if (this.isMobile) return 'grid grid-cols-1 gap-3 mb-4';
    if (this.isTablet) return 'grid grid-cols-1 gap-3 mb-4 max-w-full';
    return 'grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8';
  }

  getStatusGridClass(): string {
    if (this.isMobile) return 'grid grid-cols-1 gap-3 mb-4';
    if (this.isTablet) return 'grid grid-cols-1 gap-2 mb-4';
    return 'grid grid-cols-1 md:grid-cols-3 gap-6 mb-8';
  }

  getContainerClass(): string {
    if (this.isMobile) return 'w-full px-3 py-3 mt-16 box-border';
    if (this.isTablet) return 'w-full px-1 py-2 mt-16 box-border overflow-hidden';
    return 'container mx-auto px-4 py-8 mt-15';
  }

  getHeaderClass(): string {
    if (this.isMobile) return 'mb-4';
    if (this.isTablet) return 'mb-3';
    return 'mb-8';
  }

  getCardClass(): string {
    if (this.isMobile) return 'bg-white rounded-md shadow-sm p-3 animated-card fade-in';
    if (this.isTablet) return 'bg-white rounded-md shadow-sm p-4 animated-card fade-in max-w-full';
    return 'bg-white rounded-lg shadow-md p-6 animated-card fade-in';
  }

  getTrendCardClass(): string {
    if (this.isMobile) return 'bg-white rounded-md shadow-sm p-3 mb-4 animated-card';
    if (this.isTablet) return 'bg-white rounded-md shadow-sm p-2 mb-3 animated-card';
    return 'bg-white rounded-lg shadow-md p-6 mb-8 animated-card';
  }

  getChartCardClass(): string {
    if (this.isMobile) return 'bg-white rounded-md shadow-sm p-3 animated-card';
    if (this.isTablet) return 'bg-white rounded-md shadow-sm p-2 animated-card';
    return 'bg-white rounded-lg shadow-md p-6 col-span-1 animated-card';
  }

  getChartHeight(): string {
    if (this.isMobile) return 'h-[200px]';
    if (this.isTablet) return 'h-[180px]';
    return 'h-[300px]';
  }

  getTitleClass(): string {
    if (this.isMobile) return 'text-lg font-semibold text-gray-800 mb-2';
    if (this.isTablet) return 'text-base font-semibold text-gray-800 mb-2';
    return 'text-xl font-semibold text-gray-800 mb-4';
  }

  getTableCardClass(): string {
    if (this.isMobile) return 'bg-white rounded-md shadow-sm p-3 animated-card';
    if (this.isTablet) return 'bg-white rounded-md shadow-sm p-2 animated-card overflow-x-auto';
    return 'bg-white rounded-lg shadow-md p-6 animated-card';
  }

  loadIncidentReports(): void {
    this.loading = true;
    this.error = null;
    
    console.log('🔄 Loading incident reports...');
    console.log('🔍 Initial state:', { 
      isHSEUser: this.isHSEUser(), 
      showTeamView: this.showTeamView,
      currentUser: this.authService.getCurrentUser()
    });
    
    // Load only Incident-Management type reports
    this.subscription = this.reportService.getReports('Incident-Management').subscribe({
      next: (reports) => {
        console.log('📊 Loaded incident reports:', reports);
        
        // Store all reports for potential team view
        this.allIncidentReports = reports;
        
        if (this.isHSEUser()) {
          // For HSE users, filter personal reports based on assignment
          const currentUserName = this.getCurrentUserName();
          console.log('🔍 Current HSE user name:', currentUserName);
          console.log('🔍 Available assignedHSE values:', [...new Set(reports.map(r => r.assignedHSE))]);
          
          // Filter reports assigned to this HSE user using same logic as reports-list
          this.hsePersonalReports = reports.filter(report => {
            const currentUser = this.authService.getCurrentUser();
            if (!currentUser) return false;
            
            // Primary matching: Check if assignedHSE matches user's full name
            const userFullName = `${currentUser.firstName || ''} ${currentUser.lastName || ''}`.trim();
            const isAssignedByName = report.assignedHSE === userFullName;
            
            // Fallback matching: Check ID and company ID (for backwards compatibility)
            const isAssignedById = report.assignedHSE === currentUser.id;
            const isAssignedByCompanyId = report.assignedHSE === currentUser.companyId;
            
            const isAssigned = isAssignedByName || isAssignedById || isAssignedByCompanyId;
            
            console.log(`🔧 Incident Assignment Check - Report ${report.id} (${report.title}):`, {
              assignedHSE: report.assignedHSE,
              userFullName: userFullName,
              userID: currentUser.id,
              companyID: currentUser.companyId,
              isAssignedByName,
              isAssignedById,
              isAssignedByCompanyId,
              finalMatch: isAssigned
            });
            
            return isAssigned;
          });
          
          console.log('📊 HSE personal reports for indicators:', this.hsePersonalReports.length, 'out of', reports.length);
          console.log('📊 Sample assigned HSE values:', reports.slice(0, 3).map(r => ({ id: r.id, assignedHSE: r.assignedHSE })));
          
          // Additional debugging for matching
          if (this.hsePersonalReports.length === 0) {
            console.warn('⚠️ No HSE personal reports found! Debugging info:');
            console.log('🔍 Current user name variations tried:');
            const user = this.authService.getCurrentUser();
            if (user) {
              console.log('  - fullName:', user.fullName);
              console.log('  - firstName + lastName:', user.firstName && user.lastName ? `${user.firstName} ${user.lastName}` : 'N/A');
              console.log('  - userID:', user.id);
              console.log('  - companyID:', user.companyId);
              console.log('  - email:', user.email);
            }
            console.log('🔍 All unique assignedHSE values in data:');
            const uniqueAssigned = [...new Set(reports.map(r => r.assignedHSE))];
            uniqueAssigned.forEach(name => console.log(`  - "${name}"`));
          }
          
          // HSE users start with personal view by default, can toggle to team view
          this.incidentReports = this.showTeamView ? reports : this.hsePersonalReports;
        } else {
          // For admin users, show all reports
          this.incidentReports = reports;
          this.hsePersonalReports = reports; // Admin sees everything for calculation purposes
        }
        
        this.hasIncidentData = this.incidentReports.length > 0;
        
        // Load detailed reports for both display and HSE personal data
        this.loadDetailedReports();
      },
      error: (error) => {
        console.error('❌ Error loading incident reports:', error);
        this.error = 'Failed to load incident reports';
        this.loading = false;
        this.hasIncidentData = false;
      }
    });
  }

  private loadDetailedReports(): void {
    const allDetailRequests = this.allIncidentReports.map(report => 
      this.reportService.getReportById(report.id)
    );
    
    forkJoin(allDetailRequests).subscribe({
      next: (allDetailedReports) => {
        console.log('📊 Loaded all detailed reports:', allDetailedReports);
        this.allDetailedReports = allDetailedReports;
        
        // Set detailed reports for display based on current view
        this.detailedReports = this.incidentReports.map(report => 
          allDetailedReports.find(detailed => detailed.id === report.id)!
        ).filter(Boolean);
        
        // Set HSE personal detailed reports for indicators
        if (this.isHSEUser()) {
          this.hsePersonalDetailedReports = this.hsePersonalReports.map(report => 
            allDetailedReports.find(detailed => detailed.id === report.id)!
          ).filter(Boolean);
        } else {
          this.hsePersonalDetailedReports = allDetailedReports;
        }
        
        this.updateKPICards();
        this.loading = false;
        
        // Render charts after detailed data is loaded
        setTimeout(() => this.renderCharts(), 100);
      },
      error: (error) => {
        console.error('❌ Error loading detailed reports:', error);
        // Fallback to summary data
        this.detailedReports = [];
        this.hsePersonalDetailedReports = [];
        this.updateKPICards();
        this.loading = false;
        setTimeout(() => this.renderCharts(), 100);
      }
    });
  }

  ngAfterViewInit(): void {
    // Charts will be rendered after data loads
  }

  private updateKPICards(): void {
    // IMPORTANT: KPI calculations are ALWAYS based on personal data for HSE users
    // This ensures indicators reflect only their assigned incidents, regardless of team view toggle
    const reportsForKPI = this.isHSEUser() ? this.hsePersonalReports : this.incidentReports;
    const detailedForKPI = this.isHSEUser() ? this.hsePersonalDetailedReports : this.detailedReports;
    
    console.log('📊 Updating KPI cards with:', { 
      isHSE: this.isHSEUser(), 
      reportsCount: reportsForKPI.length, 
      detailedCount: detailedForKPI.length,
      showTeamView: this.showTeamView 
    });
    
    // Update Total Incidents (always based on personal data for HSE)
    this.kpiCards[0].value = reportsForKPI.length;
    
    // Update Severity Index (always based on personal data for HSE)
    const severityIndex = this.calculateSeverityIndex(detailedForKPI);
    this.kpiCards[1].value = severityIndex > 0 ? severityIndex.toFixed(1) : '–';
    
    // Update Top Affected Body Part with icon (always based on personal data for HSE)
    const topBodyPart = this.getMostAffectedBodyPart(detailedForKPI);
    this.kpiCards[2].value = topBodyPart;
    this.kpiCards[2].icon = this.getBodyPartIcon(topBodyPart);
    
    // Update High Risk Zone (always based on personal data for HSE)
    this.kpiCards[3].value = this.getTopIncidentZoneForKPI(reportsForKPI);
    
    // Update Status Counts (this one changes with team view)
    this.updateStatusCounts();
    
    // Initialize filters
    this.initializeFilterOptions();
    this.applyFilters();
  }

  private updateStatusCounts(): void {
    this.statusCounts = {
      unopened: 0,
      opened: 0,
      closed: 0
    };

    this.incidentReports.forEach(report => {
      const status = report.status?.toLowerCase() || '';
      
      if (status === 'unopened') {
        this.statusCounts.unopened++;
      } else if (status === 'opened' || status === 'open') {
        this.statusCounts.opened++;
      } else if (status === 'closed') {
        this.statusCounts.closed++;
      }
    });

    console.log('📊 Status counts:', this.statusCounts);
  }

  getStatusPercentage(status: 'unopened' | 'opened' | 'closed'): number {
    const total = this.incidentReports.length;
    if (total === 0) return 0;
    
    const count = this.statusCounts[status];
    return Math.round((count / total) * 100);
  }

  getStatusClass(status: string): string {
    const normalizedStatus = status?.toLowerCase() || '';
    
    if (normalizedStatus === 'open' || normalizedStatus === 'opened') {
      return 'bg-blue-100 text-blue-700 border border-blue-200';
    } else if (normalizedStatus === 'unopened') {
      return 'bg-red-100 text-red-700 border border-red-200';
    } else if (normalizedStatus === 'closed') {
      return 'bg-green-100 text-green-700 border border-green-200';
    } else if (normalizedStatus === 'in progress' || normalizedStatus === 'pending') {
      return 'bg-yellow-100 text-yellow-700 border border-yellow-200';
    } else if (normalizedStatus === 'cancelled') {
      return 'bg-gray-100 text-gray-700 border border-gray-200';
    }
    
    return 'bg-gray-100 text-gray-700 border border-gray-200';
  }

  getStatusIcon(status: string): string {
    const normalizedStatus = status?.toLowerCase() || '';
    
    if (normalizedStatus === 'open' || normalizedStatus === 'opened') {
      return 'fas fa-folder-open';
    } else if (normalizedStatus === 'unopened') {
      return 'fas fa-envelope';
    } else if (normalizedStatus === 'closed') {
      return 'fas fa-check-circle';
    } else if (normalizedStatus === 'in progress' || normalizedStatus === 'pending') {
      return 'fas fa-hourglass-half';
    } else if (normalizedStatus === 'cancelled') {
      return 'fas fa-ban';
    }
    
    return 'fas fa-question-circle';
  }

  getSeverityClass(severity?: string): string {
    switch (severity) {
      case 'Severe': return 'bg-red-100 text-red-800';
      case 'Moderate': return 'bg-yellow-100 text-yellow-800';
      case 'Minor': return 'bg-green-100 text-green-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  private calculateSeverityIndex(detailedReports?: ReportDetailDto[]): number {
    // Use provided reports or default to component's detailed reports
    const reportsToUse = detailedReports || this.detailedReports;
    
    // Always calculate from detailed reports if available
    if (reportsToUse.length > 0) {
      let totalWeight = 0;
      let injuryCount = 0;
      
      reportsToUse.forEach(report => {
        if (report.injuredPersons && report.injuredPersons.length > 0) {
          report.injuredPersons.forEach(person => {
            if (person.injuries && person.injuries.length > 0) {
              person.injuries.forEach(injury => {
                if (injury.severity) {
                  injuryCount++;
                  switch (injury.severity.toLowerCase()) {
                    case 'high': 
                    case 'severe': 
                      totalWeight += 3; 
                      break;
                    case 'medium': 
                    case 'moderate': 
                      totalWeight += 2; 
                      break;
                    case 'low': 
                    case 'minor': 
                      totalWeight += 1; 
                      break;
                    default: 
                      totalWeight += 1; 
                      break; // Default to Minor
                  }
                }
              });
            }
          });
        }
      });
      
      console.log('📊 Severity calculation:', { 
        totalWeight, 
        injuryCount, 
        average: injuryCount > 0 ? totalWeight / injuryCount : 0,
        detailedReports: reportsToUse.length,
        sampleInjuries: reportsToUse.slice(0, 2).map(r => r.injuredPersons?.map(p => p.injuries?.map(i => ({ severity: i.severity, bodyPart: i.bodyPart }))))
      });
      return injuryCount > 0 ? totalWeight / injuryCount : 0;
    }
    
    // Fallback to summary data if no detailed reports
    if (this.incidentReports.length === 0) return 0;
    
    let totalWeight = 0;
    let reportCount = 0;
    
    this.incidentReports.forEach(report => {
      if (report.injurySeverity) {
        reportCount++;
        switch (report.injurySeverity.toLowerCase()) {
          case 'high': 
          case 'severe': 
            totalWeight += 3; 
            break;
          case 'medium': 
          case 'moderate': 
            totalWeight += 2; 
            break;
          case 'low': 
          case 'minor': 
            totalWeight += 1; 
            break;
          default: 
            totalWeight += 1; 
            break;
        }
      }
    });
    
    console.log('📊 Severity fallback calculation:', { totalWeight, reportCount, average: reportCount > 0 ? totalWeight / reportCount : 0 });
    return reportCount > 0 ? totalWeight / reportCount : 0;
  }

  private getMostAffectedBodyPart(detailedReports?: ReportDetailDto[]): string {
    const reportsToUse = detailedReports || this.detailedReports;
    if (reportsToUse.length === 0) return '–';
    
    // Count injuries by body part from actual injury data
    const bodyPartCount: Record<string, number> = {};
    
    reportsToUse.forEach(report => {
      if (report.injuredPersons && report.injuredPersons.length > 0) {
        report.injuredPersons.forEach(person => {
          if (person.injuries && person.injuries.length > 0) {
            person.injuries.forEach(injury => {
              const bodyPart = injury.bodyPart || 'Unknown';
              bodyPartCount[bodyPart] = (bodyPartCount[bodyPart] || 0) + 1;
            });
          }
        });
      }
    });
    
    // Find body part with most injuries
    let topBodyPart = '–';
    let maxCount = 0;
    Object.entries(bodyPartCount).forEach(([bodyPart, count]) => {
      if (count > maxCount) {
        maxCount = count;
        topBodyPart = bodyPart;
      }
    });
    
    return topBodyPart;
  }

  private getBodyPartIcon(bodyPart: string): string {
    const iconMap: Record<string, string> = {
      // Tête et visage
      'Head': 'fa-head-side-virus',        // Icône tête de profil
      'Eyes': 'fa-eye',                    // Icône œil
      'Face': 'fa-head-side-mask',         // Icône visage
      'Neck': 'fa-head-side-cough',        // Icône cou/gorge
      
      // Épaules
      'Left Shoulder': 'fa-user-injured',   // Icône épaule/blessure
      'Right Shoulder': 'fa-user-injured',  // Icône épaule/blessure
      
      // Bras
      'Left Arm': 'fa-hand-fist',          // Icône bras/force
      'Right Arm': 'fa-hand-fist',         // Icône bras/force
      
      // Mains
      'Left Hand': 'fa-hand-paper',        // Icône main ouverte
      'Right Hand': 'fa-hand-paper',       // Icône main ouverte
      
      // Tronc
      'Chest': 'fa-lungs',                 // Icône poumons/poitrine
      'Back': 'fa-user-shield',            // Icône dos/protection
      'Abdomen': 'fa-circle-dot',          // Icône abdomen/centre
      
      // Jambes
      'Left Leg': 'fa-person-walking',     // Icône jambe/marche
      'Right Leg': 'fa-person-walking',    // Icône jambe/marche
      
      // Pieds
      'Left Foot': 'fa-shoe-prints',       // Icône empreinte pied
      'Right Foot': 'fa-shoe-prints'       // Icône empreinte pied
    };
    
    return iconMap[bodyPart] || 'fa-user-injured';
  }

  private normalizeShiftName(shift: string): string {
    const normalized = shift.toLowerCase().trim();
    
    // Normalize to match DB values exactly
    if (normalized === 'day' || normalized === 'day shift' || normalized === 'morning' || normalized === 'matin') {
      return 'Day Shift';
    } else if (normalized === 'afternoon' || normalized === 'afternoon shift' || normalized === 'après-midi') {
      return 'Afternoon Shift';
    } else if (normalized === 'night' || normalized === 'night shift' || normalized === 'nuit') {
      return 'Night Shift';
    } else if (normalized === 'office' || normalized === 'office hours' || normalized === 'bureau') {
      return 'Office Hours';
    }
    
    // If it already matches exactly, return as is
    if (shift === 'Day Shift' || shift === 'Afternoon Shift' || shift === 'Night Shift' || shift === 'Office Hours') {
      return shift;
    }
    
    // Default to Day Shift if unknown
    return 'Day Shift';
  }

  private getTopIncidentZone(): string {
    if (this.incidentReports.length === 0) return '–';
    
    // Count incidents by zone
    const zoneCount: Record<string, number> = {};
    this.incidentReports.forEach(report => {
      const zone = report.zone || 'Unknown';
      zoneCount[zone] = (zoneCount[zone] || 0) + 1;
    });
    
    // Find zone with most incidents
    let topZone = '–';
    let maxCount = 0;
    Object.entries(zoneCount).forEach(([zone, count]) => {
      if (count > maxCount) {
        maxCount = count;
        topZone = zone;
      }
    });
    
    return topZone;
  }

  private getTopIncidentZoneForKPI(reports: ReportSummaryDto[]): string {
    if (reports.length === 0) return '–';
    
    // Count incidents by zone
    const zoneCount: Record<string, number> = {};
    reports.forEach(report => {
      const zone = report.zone || 'Unknown';
      zoneCount[zone] = (zoneCount[zone] || 0) + 1;
    });
    
    // Find zone with most incidents
    let topZone = '–';
    let maxCount = 0;
    Object.entries(zoneCount).forEach(([zone, count]) => {
      if (count > maxCount) {
        maxCount = count;
        topZone = zone;
      }
    });
    
    return topZone;
  }

  private renderCharts(): void {
    // Charts show data based on current view for HSE users
    // Personal view = personal data, Team view = all data
    const chartsDetailedReports = this.isHSEUser() && !this.showTeamView 
      ? this.hsePersonalDetailedReports 
      : this.detailedReports;
    const chartsSummaryReports = this.isHSEUser() && !this.showTeamView 
      ? this.hsePersonalReports 
      : this.incidentReports;
    
    console.log('📊 Rendering charts with:', { 
      isHSE: this.isHSEUser(), 
      showTeamView: this.showTeamView,
      detailedCount: chartsDetailedReports.length,
      summaryCount: chartsSummaryReports.length 
    });
    
    // All body parts from the system
    const parts = ['Head', 'Eyes', 'Face', 'Neck', 'Left Shoulder', 'Right Shoulder', 
                   'Left Arm', 'Right Arm', 'Left Hand', 'Right Hand', 'Chest', 
                   'Back', 'Abdomen', 'Left Leg', 'Right Leg', 'Left Foot', 'Right Foot'];
    
    const partCount: Record<string, number> = {};
    parts.forEach(part => partCount[part] = 0);
    
    const zoneCount: Record<string, number> = {};
    
    // Initialize the 4 shifts from DB
    const allShifts = ['Day Shift', 'Afternoon Shift', 'Night Shift', 'Office Hours'];
    const shiftCount: Record<string, number> = {};
    allShifts.forEach(shift => shiftCount[shift] = 0);

    // Count injuries by body part from actual injury data (use filtered detailed reports)
    if (chartsDetailedReports.length > 0) {
      for (const report of chartsDetailedReports) {
        if (report.injuredPersons && report.injuredPersons.length > 0) {
          report.injuredPersons.forEach(person => {
            if (person.injuries && person.injuries.length > 0) {
              person.injuries.forEach(injury => {
                const bodyPart = injury.bodyPart || 'Unknown';
                // Count exact body part names
                if (partCount[bodyPart] !== undefined) {
                  partCount[bodyPart]++;
                } else {
                  // If body part not in predefined list, add it
                  partCount[bodyPart] = (partCount[bodyPart] || 0) + 1;
                }
              });
            }
          });
        }
        
        // Count zones and shifts using real data
        const zone = report.zone || 'Unknown';
        zoneCount[zone] = (zoneCount[zone] || 0) + 1;

        // Use workShift from report data and normalize it
        const rawShift = report.workShift || 'Unknown';
        const normalizedShift = this.normalizeShiftName(rawShift);
        if (shiftCount[normalizedShift] !== undefined) {
          shiftCount[normalizedShift]++;
        } else {
          // If it's an unknown shift, still count it
          shiftCount[normalizedShift] = (shiftCount[normalizedShift] || 0) + 1;
        }
      }
    } else {
      // Fallback: analyze summary data
      for (const report of chartsSummaryReports) {
        // Basic mapping for summary data
        const title = report.title.toLowerCase();
        let mappedPart = 'Head'; // default
        
        if (title.includes('hand')) mappedPart = 'Left Hand';
        else if (title.includes('leg')) mappedPart = 'Left Leg';
        else if (title.includes('back')) mappedPart = 'Back';
        else if (title.includes('head')) mappedPart = 'Head';
        else if (title.includes('eye')) mappedPart = 'Eyes';
        else if (title.includes('foot')) mappedPart = 'Left Foot';
        else if (title.includes('arm')) mappedPart = 'Left Arm';
        
        partCount[mappedPart] = (partCount[mappedPart] || 0) + 1;

        const zone = report.zone || 'Unknown';
        zoneCount[zone] = (zoneCount[zone] || 0) + 1;

        // For summary data, we don't have shift info, so skip or use default
      }
    }

    // Filter out parts with zero count for cleaner chart
    const activeParts = Object.keys(partCount).filter(part => partCount[part] > 0);
    const colors = [
      '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899', 
      '#06B6D4', '#84CC16', '#F97316', '#EF4444', '#A855F7', '#14B8A6',
      '#F59E0B', '#6366F1', '#22C55E', '#F97316', '#8B5CF6'
    ];

    new Chart('bodyPartChart', {
      type: 'doughnut',
      data: {
        labels: activeParts,
        datasets: [{
          data: activeParts.map(p => partCount[p]),
          backgroundColor: colors.slice(0, activeParts.length),
          borderWidth: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '70%',
        plugins: { 
          legend: { 
            display: true,
            position: 'bottom',
            labels: {
              usePointStyle: true,
              padding: 15
            }
          }
        }
      }
    });

    const zoneLabels = Object.keys(zoneCount);
    const zoneValues = Object.values(zoneCount);

    new Chart('zoneChart', {
      type: 'bar',
      data: {
        labels: zoneLabels,
        datasets: [{
          data: zoneValues,
          backgroundColor: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444'],
          borderRadius: 4,
          borderWidth: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          y: { beginAtZero: true, grid: { display: false }, ticks: { stepSize: 1 } },
          x: { grid: { display: false } }
        }
      }
    });

    // Use actual shift data - show all shifts including zeros for completeness
    const shiftLabels = Object.keys(shiftCount);
    const shiftValues = shiftLabels.map(shift => shiftCount[shift]);
    const maxShiftValue = Math.max(...shiftValues, 5); // Minimum scale of 5
    
    console.log('📊 Shift data:', { shiftLabels, shiftValues, shiftCount });

    new Chart('shiftChart', {
      type: 'radar',
      data: {
        labels: shiftLabels,
        datasets: [{
          data: shiftValues,
          backgroundColor: 'rgba(59, 130, 246, 0.2)',
          borderColor: '#3B82F6',
          borderWidth: 2,
          pointBackgroundColor: '#3B82F6',
          pointRadius: 4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          r: {
            angleLines: { display: false },
            suggestedMin: 0,
            suggestedMax: maxShiftValue,
            ticks: { stepSize: Math.max(1, Math.ceil(maxShiftValue / 5)) }
          }
        },
        plugins: { 
          legend: { display: false },
          title: {
            display: true,
            text: 'Incidents by Work Shift'
          }
        }
      }
    });

    // Monthly Trend Chart
    this.renderTrendChart();
  }

  private renderTrendChart(): void {
    // Use same filtered data as other charts
    const trendDetailedReports = this.isHSEUser() && !this.showTeamView 
      ? this.hsePersonalDetailedReports 
      : this.detailedReports;
      
    if (trendDetailedReports.length === 0) return;

    // Initialize all data structures
    this.dailyData = {};
    this.weeklyData = {};
    this.monthlyData = {};
    
    trendDetailedReports.forEach(report => {
      const date = new Date(report.createdAt);
      
      // Daily data (YYYY-MM-DD format)
      const dayKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
      
      // Weekly data (YYYY-WW format)
      const weekNumber = this.getWeekNumber(date);
      const weekKey = `${date.getFullYear()}-W${String(weekNumber).padStart(2, '0')}`;
      
      // Monthly data (YYYY-MM format)
      const monthKey = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
      
      // Initialize data structures if they don't exist
      [
        { data: this.dailyData, key: dayKey },
        { data: this.weeklyData, key: weekKey },
        { data: this.monthlyData, key: monthKey }
      ].forEach(({ data, key }) => {
        if (!data[key]) {
          data[key] = {
            bodyParts: {},
            zones: {},
            shifts: {},
            total: 0
          };
        }
        data[key].total++;
        
        // Count zones
        const zone = report.zone || 'Unknown';
        data[key].zones[zone] = (data[key].zones[zone] || 0) + 1;
        
        // Count shifts  
        const shift = this.normalizeShiftName(report.workShift || 'Unknown');
        data[key].shifts[shift] = (data[key].shifts[shift] || 0) + 1;
        
        // Count body parts from injuries
        if (report.injuredPersons) {
          report.injuredPersons.forEach(person => {
            if (person.injuries) {
              person.injuries.forEach(injury => {
                const bodyPart = injury.bodyPart || 'Unknown';
                data[key].bodyParts[bodyPart] = (data[key].bodyParts[bodyPart] || 0) + 1;
              });
            }
          });
        }
      });
    });

    // Generate available time periods for navigation
    this.updateAvailableTimePeriods();
    this.updateTimeRangeSettings();
    this.updateTrendChart();
  }

  updateTrendChart(): void {
    const canvas = document.getElementById('trendChart') as HTMLCanvasElement;
    if (!canvas) return;

    // Destroy existing chart
    const existingChart = Chart.getChart(canvas);
    if (existingChart) {
      existingChart.destroy();
    }

    // Get periods to display based on current time range and navigation
    const periodsToDisplay = this.getPeriodsToDisplay();
    const currentData = this.getCurrentDataSet();
    const chartData = this.getChartDataForTrendView(periodsToDisplay, currentData);

    new Chart('trendChart', {
      type: 'line',
      data: {
        labels: periodsToDisplay.map(period => this.formatPeriodLabel(period)),
        datasets: chartData.datasets
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: chartData.datasets.length > 1,
            position: 'top' as const,
            labels: {
              usePointStyle: true,
              padding: 20,
              boxWidth: 12
            }
          },
          title: {
            display: true,
            text: `${this.getTimeRangeTitle()} - ${this.getTrendViewTitle()}`
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            grid: {
              display: true,
              color: 'rgba(0, 0, 0, 0.1)'
            },
            ticks: {
              stepSize: 1
            }
          },
          x: {
            grid: {
              display: false
            }
          }
        },
        elements: {
          point: {
            radius: 6,
            backgroundColor: '#3B82F6',
            borderColor: '#ffffff',
            borderWidth: 2
          }
        }
      }
    });
  }

  switchTrendView(viewMode: 'total' | 'bodyParts' | 'zones' | 'shifts' | string): void {
    this.trendViewMode = viewMode as 'total' | 'bodyParts' | 'zones' | 'shifts';
    this.updateTrendChart();
  }

  getTrendViewTitle(): string {
    switch (this.trendViewMode) {
      case 'total':
        return 'Total Incidents';
      case 'bodyParts':
        return 'Body Parts Analysis';
      case 'zones':
        return 'Zones Analysis';
      case 'shifts':
        return 'Shifts Analysis';
      default:
        return 'Total Incidents';
    }
  }

  // Helper method to get week number
  private getWeekNumber(date: Date): number {
    const firstDayOfYear = new Date(date.getFullYear(), 0, 1);
    const pastDaysOfYear = (date.getTime() - firstDayOfYear.getTime()) / 86400000;
    return Math.ceil((pastDaysOfYear + firstDayOfYear.getDay() + 1) / 7);
  }

  // Time navigation methods for swipeable trends
  updateTimeRangeSettings(): void {
    switch (this.currentTimeRange) {
      case 'last7Days':
        this.periodsToShow = 7;
        break;
      case 'last4Weeks':
        this.periodsToShow = 4;
        break;
      case 'last3Months':
        this.periodsToShow = 3;
        break;
      case 'last6Months':
        this.periodsToShow = 6;
        break;
      case 'last12Months':
        this.periodsToShow = 12;
        break;
      case 'allTime':
        this.periodsToShow = this.availableTimePeriods.length;
        break;
    }
    
    // Reset to most recent period when changing time range
    this.currentPeriodIndex = Math.max(0, this.availableTimePeriods.length - this.periodsToShow);
  }

  updateAvailableTimePeriods(): void {
    switch (this.currentTimeRange) {
      case 'last7Days':
        this.availableTimePeriods = Object.keys(this.dailyData).sort();
        break;
      case 'last4Weeks':
        this.availableTimePeriods = Object.keys(this.weeklyData).sort();
        break;
      case 'last3Months':
      case 'last6Months':
      case 'last12Months':
      case 'allTime':
        this.availableTimePeriods = Object.keys(this.monthlyData).sort();
        break;
    }
  }

  getCurrentDataSet(): { [key: string]: { bodyParts: any, zones: any, shifts: any, total: number } } {
    switch (this.currentTimeRange) {
      case 'last7Days':
        return this.dailyData;
      case 'last4Weeks':
        return this.weeklyData;
      case 'last3Months':
      case 'last6Months':
      case 'last12Months':
      case 'allTime':
        return this.monthlyData;
      default:
        return this.monthlyData;
    }
  }

  getChartDataForTrendView(periodsToDisplay: string[], currentData: any): { datasets: any[] } {
    const colors = [
      '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899', 
      '#06B6D4', '#84CC16', '#F97316', '#A855F7', '#14B8A6', '#F59E0B',
      '#6366F1', '#22C55E', '#F472B6', '#FB923C', '#A78BFA', '#34D399',
      '#FBBF24', '#F87171', '#60A5FA', '#4ADE80', '#E879F9', '#FACC15'
    ];

    switch (this.trendViewMode) {
      case 'total':
        // Show total incidents over time (single line)
        return {
          datasets: [{
            label: 'Total Incidents',
            data: periodsToDisplay.map(period => currentData[period]?.total || 0),
            borderColor: '#3B82F6',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            borderWidth: 3,
            fill: true,
            tension: 0.4
          }]
        };

      case 'bodyParts':
        // Get all unique body parts across all periods
        const allBodyParts = new Set<string>();
        periodsToDisplay.forEach(period => {
          if (currentData[period]?.bodyParts) {
            Object.keys(currentData[period].bodyParts).forEach(part => allBodyParts.add(part));
          }
        });

        const bodyPartsArray = Array.from(allBodyParts).sort(); // Show ALL body parts, sorted alphabetically
        return {
          datasets: bodyPartsArray.map((bodyPart, index) => ({
            label: bodyPart,
            data: periodsToDisplay.map(period => currentData[period]?.bodyParts[bodyPart] || 0),
            borderColor: colors[index % colors.length],
            backgroundColor: colors[index % colors.length] + '20',
            borderWidth: 2,
            fill: false,
            tension: 0.4
          }))
        };

      case 'zones':
        // Get all unique zones across all periods
        const allZones = new Set<string>();
        periodsToDisplay.forEach(period => {
          if (currentData[period]?.zones) {
            Object.keys(currentData[period].zones).forEach(zone => allZones.add(zone));
          }
        });

        const zonesArray = Array.from(allZones).sort(); // Show ALL zones, sorted alphabetically
        return {
          datasets: zonesArray.map((zone, index) => ({
            label: zone,
            data: periodsToDisplay.map(period => currentData[period]?.zones[zone] || 0),
            borderColor: colors[index % colors.length],
            backgroundColor: colors[index % colors.length] + '20',
            borderWidth: 2,
            fill: false,
            tension: 0.4
          }))
        };

      case 'shifts':
        // Get all unique shifts across all periods
        const allShifts = new Set<string>();
        periodsToDisplay.forEach(period => {
          if (currentData[period]?.shifts) {
            Object.keys(currentData[period].shifts).forEach(shift => allShifts.add(shift));
          }
        });

        const shiftsArray = Array.from(allShifts).sort(); // Show ALL shifts, sorted alphabetically
        return {
          datasets: shiftsArray.map((shift, index) => ({
            label: shift,
            data: periodsToDisplay.map(period => currentData[period]?.shifts[shift] || 0),
            borderColor: colors[index % colors.length],
            backgroundColor: colors[index % colors.length] + '20',
            borderWidth: 2,
            fill: false,
            tension: 0.4
          }))
        };

      default:
        // Fallback to total incidents
        return {
          datasets: [{
            label: 'Total Incidents',
            data: periodsToDisplay.map(period => currentData[period]?.total || 0),
            borderColor: '#3B82F6',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            borderWidth: 3,
            fill: true,
            tension: 0.4
          }]
        };
    }
  }

  getPeriodsToDisplay(): string[] {
    if (this.availableTimePeriods.length === 0) return [];
    
    if (this.currentTimeRange === 'allTime') {
      return this.availableTimePeriods;
    }
    
    // Calculate the slice of periods to display
    const startIndex = this.currentPeriodIndex;
    const endIndex = Math.min(startIndex + this.periodsToShow, this.availableTimePeriods.length);
    
    return this.availableTimePeriods.slice(startIndex, endIndex);
  }

  formatPeriodLabel(period: string): string {
    switch (this.currentTimeRange) {
      case 'last7Days':
        // Daily format: YYYY-MM-DD -> "Jan 15"
        const [year, month, day] = period.split('-');
        return new Date(parseInt(year), parseInt(month) - 1, parseInt(day)).toLocaleDateString('fr-FR', { 
          month: 'short', 
          day: 'numeric' 
        });
      
      case 'last4Weeks':
        // Weekly format: YYYY-WW -> "W01 2024"
        const [weekYear, weekNum] = period.split('-W');
        return `W${weekNum} ${weekYear}`;
      
      case 'last3Months':
      case 'last6Months':
      case 'last12Months':
      case 'allTime':
        // Monthly format: YYYY-MM -> "Jan 2024"
        const [monthYear, monthNum] = period.split('-');
        return new Date(parseInt(monthYear), parseInt(monthNum) - 1).toLocaleDateString('fr-FR', { 
          year: 'numeric', 
          month: 'short' 
        });
      
      default:
        return period;
    }
  }

  getTimeRangeTitle(): string {
    const periodsToDisplay = this.getPeriodsToDisplay();
    if (periodsToDisplay.length === 0) return 'No Data';
    
    if (this.currentTimeRange === 'allTime') {
      return 'All Time';
    }
    
    const firstPeriod = periodsToDisplay[0];
    const lastPeriod = periodsToDisplay[periodsToDisplay.length - 1];
    
    if (firstPeriod === lastPeriod) {
      return this.formatPeriodLabel(firstPeriod);
    }
    
    return `${this.formatPeriodLabel(firstPeriod)} - ${this.formatPeriodLabel(lastPeriod)}`;
  }

  changeTimeRange(range: 'last7Days' | 'last4Weeks' | 'last3Months' | 'last6Months' | 'last12Months' | 'allTime' | string): void {
    this.currentTimeRange = range as 'last7Days' | 'last4Weeks' | 'last3Months' | 'last6Months' | 'last12Months' | 'allTime';
    this.updateAvailableTimePeriods();
    this.updateTimeRangeSettings();
    this.updateTrendChart();
  }

  navigatePrevious(): void {
    if (this.currentTimeRange === 'allTime') return;
    
    const newIndex = this.currentPeriodIndex - 1;
    if (newIndex >= 0) {
      this.currentPeriodIndex = newIndex;
      this.updateTrendChart();
    }
  }

  navigateNext(): void {
    if (this.currentTimeRange === 'allTime') return;
    
    const newIndex = this.currentPeriodIndex + 1;
    const maxIndex = Math.max(0, this.availableTimePeriods.length - this.periodsToShow);
    
    if (newIndex <= maxIndex) {
      this.currentPeriodIndex = newIndex;
      this.updateTrendChart();
    }
  }

  canNavigatePrevious(): boolean {
    return this.currentTimeRange !== 'allTime' && this.currentPeriodIndex > 0;
  }

  canNavigateNext(): boolean {
    if (this.currentTimeRange === 'allTime') return false;
    const maxIndex = Math.max(0, this.availableTimePeriods.length - this.periodsToShow);
    return this.currentPeriodIndex < maxIndex;
  }

  // Touch/Swipe event handlers for mobile support
  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.touches[0].clientX;
  }

  onTouchEnd(event: TouchEvent): void {
    this.touchEndX = event.changedTouches[0].clientX;
    this.handleSwipe();
  }

  private handleSwipe(): void {
    const swipeDistance = this.touchEndX - this.touchStartX;
    
    if (Math.abs(swipeDistance) >= this.minSwipeDistance) {
      if (swipeDistance > 0) {
        // Swipe right - go to previous period
        this.navigatePrevious();
      } else {
        // Swipe left - go to next period
        this.navigateNext();
      }
    }
  }

  toggleTeamView(): void {
    console.log('🔄 toggleTeamView called');
    console.log('🔍 isHSEUser():', this.isHSEUser());
    console.log('🔍 Current showTeamView:', this.showTeamView);
    
    if (!this.isHSEUser()) {
      console.log('❌ User is not HSE, cannot toggle team view');
      return; // Only HSE users can toggle team view
    }
    
    this.showTeamView = !this.showTeamView;
    console.log('🔄 New showTeamView:', this.showTeamView);
    
    if (this.showTeamView) {
      // Switch to team view - show all reports in list
      console.log('🔄 Switching to TEAM view');
      console.log('🔍 allIncidentReports count:', this.allIncidentReports.length);
      console.log('🔍 allDetailedReports count:', this.allDetailedReports.length);
      this.incidentReports = this.allIncidentReports;
      this.detailedReports = this.allDetailedReports;
    } else {
      // Switch to personal view - show only assigned reports in list
      console.log('🔄 Switching to PERSONAL view');
      console.log('🔍 hsePersonalReports count:', this.hsePersonalReports.length);
      console.log('🔍 hsePersonalDetailedReports count:', this.hsePersonalDetailedReports.length);
      this.incidentReports = this.hsePersonalReports;
      this.detailedReports = this.hsePersonalDetailedReports;
    }
    
    this.hasIncidentData = this.incidentReports.length > 0;
    
    // Update only status counts (other KPIs remain based on personal data)
    this.updateStatusCounts();
    this.initializeFilterOptions();
    this.applyFilters();
    
    // Re-render charts based on current view
    setTimeout(() => this.renderCharts(), 100);
    
    console.log(`📊 Switched to ${this.showTeamView ? 'team' : 'personal'} view. Reports count:`, this.incidentReports.length);
  }

  initializeFilterOptions(): void {
    // Extract unique zones from incident reports
    const zones = this.incidentReports.map(report => report.zone).filter((zone): zone is string => !!zone);
    this.availableZones = [...new Set(zones)].sort();
    
    // Extract unique HSE agents from incident reports (check assignedHSE field)
    const hseAgents = this.incidentReports.map(report => report.assignedHSE).filter((agent): agent is string => !!agent);
    this.availableHSEAgents = [...new Set(hseAgents)].sort();
    
    console.log('📊 Filter options initialized:', { zones: this.availableZones, hseAgents: this.availableHSEAgents });
  }

  applyFilters(): void {
    let filtered = [...this.incidentReports];
    
    // Filter by status
    if (this.filters.status) {
      filtered = filtered.filter(report => {
        const status = report.status?.toLowerCase() || '';
        const filterStatus = this.filters.status.toLowerCase();
        
        if (filterStatus === 'opened') {
          return status === 'opened' || status === 'open';
        }
        return status === filterStatus;
      });
    }
    
    // Filter by zone
    if (this.filters.zone) {
      filtered = filtered.filter(report => report.zone === this.filters.zone);
    }
    
    // Filter by calculated severity
    if (this.filters.severity) {
      filtered = filtered.filter(report => {
        const calculatedSeverity = this.getCalculatedSeverity(report.id).toLowerCase();
        return calculatedSeverity === this.filters.severity.toLowerCase();
      });
    }
    
    // Filter by HSE agent
    if (this.filters.hseAgent) {
      filtered = filtered.filter(report => report.assignedHSE === this.filters.hseAgent);
    }
    
    this.filteredReports = filtered;
    console.log('📊 Filters applied:', this.filters, 'Results:', filtered.length);
  }

  clearFilters(): void {
    this.filters = {
      status: '',
      zone: '',
      severity: '',
      hseAgent: ''
    };
    this.applyFilters();
  }

  getCalculatedSeverity(reportId: number): string {
    // Find the detailed report for this ID
    const detailedReport = this.detailedReports.find(report => report.id === reportId);
    
    if (!detailedReport || !detailedReport.injuredPersons || detailedReport.injuredPersons.length === 0) {
      return 'Unknown';
    }
    
    let totalWeight = 0;
    let injuryCount = 0;
    
    // Calculate weighted severity from all injuries
    detailedReport.injuredPersons.forEach(person => {
      if (person.injuries && person.injuries.length > 0) {
        person.injuries.forEach(injury => {
          if (injury.severity) {
            injuryCount++;
            switch (injury.severity.toLowerCase()) {
              case 'high': 
              case 'severe': 
                totalWeight += 3; 
                break;
              case 'medium': 
              case 'moderate': 
                totalWeight += 2; 
                break;
              case 'low': 
              case 'minor': 
                totalWeight += 1; 
                break;
              default: 
                totalWeight += 1; 
                break;
            }
          }
        });
      }
    });
    
    if (injuryCount === 0) return 'Unknown';
    
    const averageSeverity = totalWeight / injuryCount;
    
    // Convert back to severity level
    if (averageSeverity >= 2.5) return 'High';
    if (averageSeverity >= 1.5) return 'Medium';
    return 'Low';
  }

  getCalculatedSeverityClass(reportId: number): string {
    const severity = this.getCalculatedSeverity(reportId);
    switch (severity) {
      case 'High': return 'bg-red-100 text-red-800';
      case 'Medium': return 'bg-yellow-100 text-yellow-800';
      case 'Low': return 'bg-green-100 text-green-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  getInjuryCount(reportId: number): number {
    const detailedReport = this.detailedReports.find(report => report.id === reportId);
    
    if (!detailedReport || !detailedReport.injuredPersons) {
      return 0;
    }
    
    let totalInjuries = 0;
    detailedReport.injuredPersons.forEach(person => {
      if (person.injuries) {
        totalInjuries += person.injuries.length;
      }
    });
    
    return totalInjuries;
  }

  // HSE assignment methods
  private loadHSEUsers(): void {
    console.log('🔄 Incident Dashboard: Starting to load HSE users...');
    this.reportService.getHSEUsersFromAssignment().subscribe({
      next: (users) => {
        this.hseUsers = users;
        console.log('✅ Incident Dashboard: Loaded HSE users:', users.length);
        console.log('✅ Incident Dashboard: HSE users data:', users);
      },
      error: (error) => {
        console.error('❌ Incident Dashboard: Failed to load HSE users:', error);
        console.error('❌ Incident Dashboard: HSE users error details:', error);
        this.hseUsers = []; // Ensure empty array on error
      }
    });
  }

  toggleAssignmentDropdown(reportId: number): void {
    console.log('🔄 Incident Dashboard: Toggle assignment dropdown for report:', reportId);
    console.log('🔄 Current showingAssignmentDropdown state:', this.showingAssignmentDropdown);
    console.log('🔄 HSE Users available:', this.hseUsers.length);
    this.showingAssignmentDropdown[reportId] = !this.showingAssignmentDropdown[reportId];
    console.log('🔄 New showingAssignmentDropdown state:', this.showingAssignmentDropdown);
  }

  assignHSEAgent(reportId: number): void {
    const selectedUserId = this.selectedHSEAgent[reportId];
    if (!selectedUserId) {
      this.alertService.showError('Please select an HSE agent', {
        autoHide: true,
        autoHideDuration: 3000,
        position: 'top-right'
      });
      return;
    }

    const selectedUser = this.hseUsers.find(u => u.id === selectedUserId);
    if (!selectedUser) {
      this.alertService.showError('Selected HSE agent not found', {
        autoHide: true,
        autoHideDuration: 3000,
        position: 'top-right'
      });
      return;
    }

    this.reportService.updateAssignedHSE(reportId, selectedUserId).subscribe({
      next: (response) => {
        console.log('✅ Incident assigned successfully:', response);
        
        // Update the report in the list
        const report = this.filteredReports.find(r => r.id === reportId);
        if (report) {
          report.assignedHSE = `${selectedUser.firstName} ${selectedUser.lastName}`;
        }
        
        // Also update in main arrays
        const summaryReport = this.incidentReports.find(r => r.id === reportId);
        if (summaryReport) {
          summaryReport.assignedHSE = `${selectedUser.firstName} ${selectedUser.lastName}`;
        }
        
        // Hide dropdown and reset selection
        this.showingAssignmentDropdown[reportId] = false;
        this.selectedHSEAgent[reportId] = '';
        
        // Show success message with delay to ensure DOM updates complete
        const hseUserName = `${selectedUser.firstName} ${selectedUser.lastName}`;
        setTimeout(() => {
          this.alertService.showSuccess(`✅ Incident successfully assigned to ${hseUserName}`, {
            autoHide: true,
            autoHideDuration: 4000,
            position: 'top-right'
          });
        }, 300);
      },
      error: (error) => {
        console.error('❌ Failed to assign incident:', error);
        this.alertService.showError('Failed to assign incident. Please try again.', {
          autoHide: true,
          autoHideDuration: 4000,
          position: 'top-right'
        });
      }
    });
  }

  cancelAssignment(reportId: number): void {
    this.showingAssignmentDropdown[reportId] = false;
    this.selectedHSEAgent[reportId] = '';
  }
}
