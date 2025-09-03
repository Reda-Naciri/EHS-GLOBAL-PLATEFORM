export interface DashboardStatsDto {
  reports: ReportStats;
  actions: ActionStats;
  users: UserStats;
  trends: TrendData;
}

export interface ReportStats {
  totalReports: number;
  openReports: number; // Note: This represents "Unopened" reports in the new status workflow
  inProgressReports: number; // This represents "Opened" reports
  closedReports: number;
  reportsThisMonth: number;
  reportsLastMonth: number;
  monthlyGrowthRate: number;
  reportsByType: { [key: string]: number };
  reportsByZone: { [key: string]: number };
  reportsByStatus: { [key: string]: number };
}

export interface ActionStats {
  totalActions: number;
  completedActions: number;
  inProgressActions: number;
  notStartedActions: number;
  overdueActions: number;
  completionRate: number;
  actionsByHierarchy: { [key: string]: number };
  actionsByStatus: { [key: string]: number };
}

export interface UserStats {
  totalUsers: number;
  activeUsers: number;
  pendingUsers: number;
  usersByRole: { [key: string]: number };
  usersByDepartment: { [key: string]: number };
}

export interface TrendData {
  reportTrends: MonthlyData[];
  actionTrends: MonthlyData[];
  recentActivity: DailyData[];
}

export interface MonthlyData {
  month: string;
  count: number;
  label: string;
}

export interface DailyData {
  date: Date;
  reports: number;
  actions: number;
  comments: number;
}

export interface ChartDataDto {
  type: string;
  title: string;
  data: { [key: string]: any };
}

export interface RecentActivityDto {
  type: string;
  title: string;
  description: string;
  userName: string;
  timestamp: Date;
  status: string;
  relatedId?: number;
}

export interface PerformanceMetricsDto {
  averageResponseTime: number;
  averageResolutionTime: number;
  totalIncidents: number;
  preventedIncidents: number;
  safetyScore: number;
  kpis: KPIDto[];
}

export interface KPIDto {
  name: string;
  value: number;
  unit: string;
  target: string;
  status: string;
  percentageChange: number;
}