import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Chart } from 'chart.js/auto';
import { REPORTS, Report, INJURYCARDS } from '../../data/demo-data';

type SeverityLevel = 'Minor' | 'Moderate' | 'Severe';


@Component({
  selector: 'app-incident-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './incident-dashboard.component.html',
})


export class IncidentDashboardComponent implements OnInit, AfterViewInit {
  incidentReports: (Report & { severity?: SeverityLevel })[] = [];
  hasIncidentData = false;

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

  ngOnInit(): void {
    this.incidentReports = REPORTS
      .filter(r => r.type === 'Incident-Management')
      .map(report => {
        const cards = INJURYCARDS.filter(card => report.injuryCardIds?.includes(card.id));
        const firstSeverity = cards[0]?.injuries?.[0]?.severity;
        return { ...report, severity: firstSeverity };
      });

    this.hasIncidentData = this.incidentReports.length > 0;

    if (this.hasIncidentData) {
      this.kpiCards[0].value = this.incidentReports.length;
      this.kpiCards[1].value = this.calculateSeverityIndex().toFixed(1);
      this.kpiCards[2].value = this.getMostAffectedBodyPart();
      this.kpiCards[3].value = this.getTopIncidentZone();
    }
  }

  ngAfterViewInit(): void {
    if (this.hasIncidentData) {
      this.renderCharts();
    }
  }

  private calculateSeverityIndex(): number {
    const weights: Record<SeverityLevel, number> = {
      Minor: 1,
      Moderate: 2,
      Severe: 3
    };

    const relevantReports = this.incidentReports.filter(r => r.severity) as (Report & { severity: SeverityLevel })[];

    const total = relevantReports.reduce((acc, report) => {
      return acc + weights[report.severity];
    }, 0);

    return relevantReports.length ? total / relevantReports.length : 0;
  }

  private getMostAffectedBodyPart(): string {
    const countMap: Record<string, number> = {};
    for (const report of this.incidentReports) {
      const title = report.title?.toLowerCase() || '';
      const part = title.includes('hand') ? 'Hands' :
        title.includes('back') ? 'Back' :
          title.includes('leg') ? 'Legs' :
            title.includes('eye') ? 'Eyes' :
              'Other';
      countMap[part] = (countMap[part] || 0) + 1;
    }
    const sorted = Object.entries(countMap).sort((a, b) => b[1] - a[1]);
    return sorted[0]?.[0] || '–';
  }

  private getTopIncidentZone(): string {
    const countMap: Record<string, number> = {};
    for (const report of this.incidentReports) {
      const zone = report.zone || 'Unknown';
      countMap[zone] = (countMap[zone] || 0) + 1;
    }
    const sorted = Object.entries(countMap).sort((a, b) => b[1] - a[1]);
    return sorted[0]?.[0] || '–';
  }

  private renderCharts(): void {
    const parts = ['Hands', 'Legs', 'Back', 'Head', 'Eyes', 'Other'];
    const partCount: Record<string, number> = { Hands: 0, Legs: 0, Back: 0, Head: 0, Eyes: 0, Other: 0 };
    const zoneCount: Record<string, number> = {};
    const shiftCount: Record<string, number> = { Morning: 0, Afternoon: 0, Night: 0 };

    for (const report of this.incidentReports) {
      const title = report.title.toLowerCase();
      if (title.includes('hand')) partCount['Hands']++;
      else if (title.includes('leg')) partCount['Legs']++;
      else if (title.includes('back')) partCount['Back']++;
      else if (title.includes('head')) partCount['Head']++;
      else if (title.includes('eye')) partCount['Eyes']++;
      else partCount['Other']++;

      const zone = report.zone || 'Unknown';
      zoneCount[zone] = (zoneCount[zone] || 0) + 1;

      const shift = (report as any).shift || 'Unknown';
      if (shiftCount[shift] !== undefined) {
        shiftCount[shift]++;
      }
    }

    new Chart('bodyPartChart', {
      type: 'doughnut',
      data: {
        labels: parts,
        datasets: [{
          data: parts.map(p => partCount[p]),
          backgroundColor: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'],
          borderWidth: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '70%',
        plugins: { legend: { display: false } }
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

    new Chart('shiftChart', {
      type: 'radar',
      data: {
        labels: Object.keys(shiftCount),
        datasets: [{
          data: Object.values(shiftCount),
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
            suggestedMax: 10,
            ticks: { stepSize: 1 }
          }
        },
        plugins: { legend: { display: false } }
      }
    });
  }
}
