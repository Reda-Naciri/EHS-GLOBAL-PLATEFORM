import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AlertService } from '../../services/alert.service';
import { ReportService } from '../../services/report.service';

interface ReportSummary {
  id: number;
  title: string;
  type: string;
  status: string;
  createdAt: string;
  trackingNumber: string;
  zone?: string;
}

@Component({
  selector: 'app-follow-up',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './follow-up.component.html',
  styleUrls: ['./follow-up.component.css']
})
export class FollowUpComponent implements OnInit {
  loading = false;
  searchMode = true;
  companyId = '';
  reports: ReportSummary[] = [];
  
  // Company user information from validation
  companyUser: any = null;
  
  // Selected report for follow-up view
  selectedReport: any = null;
  viewMode: 'search' | 'reports' | 'details' = 'search';

  constructor(
    private alertService: AlertService,
    private router: Router,
    private reportService: ReportService
  ) {}

  ngOnInit(): void {
    // No authentication required - users can access follow-up with just their Company ID
    console.log('FollowUpComponent initialized, loading state:', this.loading);
  }

  searchReports(): void {
    if (!this.companyId.trim()) {
      this.alertService.showError('Please enter a valid Company ID');
      return;
    }

    console.log('Setting loading to true for company search');
    this.loading = true;
    
    // Validate company ID exists in the system
    this.validateCompanyExists(this.companyId);
  }

  validateCompanyExists(companyId: string): void {
    console.log('Validating company ID:', companyId);
    
    // Use the report service validation endpoint
    this.reportService.validateCompanyId(companyId).subscribe({
      next: (response: any) => {
        console.log('Company validation response:', response);
        
        if (!response.isValid) {
          console.log('Company ID does not exist:', companyId);
          this.alertService.showError(response.message || `Company ID "${companyId}" does not exist.`);
          console.log('Setting loading to false - invalid company');
          this.loading = false;
          return;
        }
        
        // Store company user information
        this.companyUser = {
          userId: response.userId,
          fullName: response.reporterName,
          companyId: response.companyId,
          department: response.department,
          position: response.position
        };
        
        console.log('Company validated, loading reports for:', this.companyUser);
        
        // Company exists, proceed to load reports
        this.loadReportsByCompany();
      },
      error: (error) => {
        console.error('Error validating company:', error);
        if (error.status === 404) {
          this.alertService.showError(`Company ID "${companyId}" does not exist.`);
        } else {
          this.alertService.showError('Failed to validate company ID. Please try again.');
        }
        console.log('Setting loading to false - validation error');
        this.loading = false;
      }
    });
  }

  loadReportsByCompany(): void {
    console.log('Loading reports for company:', this.companyId);
    
    // Need to create backend endpoint to get reports by company ID
    this.reportService.getReportsByCompanyId(this.companyId).subscribe({
      next: (reports) => {
        console.log('Reports loaded:', reports);
        this.reports = reports;
        this.viewMode = 'reports';
        console.log('Setting loading to false - reports loaded');
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading reports:', error);
        this.alertService.showError('Failed to load reports. Please try again.');
        console.log('Setting loading to false - reports error');
        this.loading = false;
      }
    });
  }

  selectReport(report: ReportSummary): void {
    console.log('Selected report:', report);
    this.loading = true;
    
    // Load report details using the secure follow-up endpoint
    this.reportService.getReportForFollowUp(report.trackingNumber).subscribe({
      next: (reportDetails) => {
        console.log('Report details loaded:', reportDetails);
        this.selectedReport = reportDetails;
        this.viewMode = 'details';
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading report details:', error);
        this.alertService.showError('Failed to load report details. Please try again.');
        this.loading = false;
      }
    });
  }

  backToReports(): void {
    this.viewMode = 'reports';
    this.selectedReport = null;
  }

  resetSearch(): void {
    this.viewMode = 'search';
    this.companyId = '';
    this.companyUser = null;
    this.reports = [];
    this.selectedReport = null;
  }

  goHome(): void {
    console.log('🔧 FollowUpComponent: Going back to home page');
    this.router.navigate(['/']);
  }

  // Utility methods
  formatDate(date: string | Date | undefined): string {
    if (!date) return 'Not set';
    return new Date(date).toLocaleDateString();
  }

  formatDateTime(date: string | Date | undefined): string {
    if (!date) return 'Not set';
    
    let dateString = date.toString();
    if (!dateString.endsWith('Z') && !dateString.includes('+') && !dateString.includes('T')) {
      dateString += 'Z';
    } else if (dateString.includes('T') && !dateString.endsWith('Z') && !dateString.includes('+')) {
      dateString += 'Z';
    }
    
    const dateObj = new Date(dateString);
    return dateObj.toLocaleString();
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Closed':
      case 'Completed':
        return 'bg-green-100 text-green-800 border-green-300';
      case 'In Progress':
        return 'bg-blue-100 text-blue-800 border-blue-300';
      case 'Open':
      case 'Pending':
        return 'bg-yellow-100 text-yellow-800 border-yellow-300';
      case 'Draft':
        return 'bg-gray-100 text-gray-800 border-gray-300';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-300';
    }
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case 'Incident-Management':
        return '⚠️';
      case 'Hasard':
        return '📄';
      case 'Nearhit':
        return '🚨';
      case 'Enviroment-aspect':
        return '🏭';
      case 'Improvement-Idea':
        return '🛠️';
      default:
        return '📊';
    }
  }

  trackByReportId(index: number, report: ReportSummary): number {
    return report.id;
  }

  getOpenReportsCount(): number {
    return this.reports.filter(r => r.status !== 'Closed' && r.status !== 'Completed').length;
  }

  getClosedReportsCount(): number {
    return this.reports.filter(r => r.status === 'Closed' || r.status === 'Completed').length;
  }

  formatFileSize(bytes: number): string {
    if (!bytes) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  downloadAttachment(attachment: any): void {
    if (!this.selectedReport || !attachment) {
      this.alertService.showError('Unable to download attachment');
      return;
    }

    console.log('Downloading attachment:', attachment.fileName);
    
    this.reportService.downloadFollowUpAttachment(this.selectedReport.id, attachment.id).subscribe({
      next: (blob: Blob) => {
        // Create download link
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = attachment.fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        
        this.alertService.showSuccess(`Downloaded: ${attachment.fileName}`);
      },
      error: (error) => {
        console.error('Error downloading attachment:', error);
        this.alertService.showError('Failed to download attachment');
      }
    });
  }

  viewAttachment(attachment: any): void {
    if (!this.selectedReport || !attachment) {
      this.alertService.showError('Unable to view attachment');
      return;
    }

    console.log('Viewing attachment:', attachment.fileName);
    
    this.reportService.downloadFollowUpAttachment(this.selectedReport.id, attachment.id).subscribe({
      next: (blob: Blob) => {
        // Create blob URL for viewing
        const url = window.URL.createObjectURL(blob);
        
        // Check if it's an image or PDF for inline viewing
        const fileType = attachment.fileName.toLowerCase();
        if (fileType.endsWith('.pdf') || 
            fileType.endsWith('.jpg') || fileType.endsWith('.jpeg') || 
            fileType.endsWith('.png') || fileType.endsWith('.gif')) {
          // Open in new tab for viewing
          window.open(url, '_blank');
        } else {
          // For other files, download instead
          const link = document.createElement('a');
          link.href = url;
          link.download = attachment.fileName;
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
        }
        
        // Clean up URL after a delay to allow viewing
        setTimeout(() => {
          window.URL.revokeObjectURL(url);
        }, 60000); // 1 minute
      },
      error: (error) => {
        console.error('Error viewing attachment:', error);
        this.alertService.showError('Failed to view attachment');
      }
    });
  }
}
