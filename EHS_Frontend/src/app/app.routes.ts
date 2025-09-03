import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { ReportComponent } from './pages/report/report.component';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { UsersComponent } from './pages/users/users.component';
import { ReportsListComponent } from './pages/reports-list/reports-list.component';
import { LayoutComponent } from './layout/layout.component';
import { IncidentDashboardComponent } from './pages/incident-dashboard/incident-dashboard.component';
import { ActionsComponent } from './pages/actions/actions.component';
import { AdminParametersComponent } from './pages/admin-parameters/admin-parameters.component';
import { AssignmentsComponent } from './pages/assignments/assignments.component';
import { FollowUpComponent } from './pages/follow-up/follow-up.component';
import { authGuard, noAuthGuard } from './guards/auth.guard';


export const routes: Routes = [
  // 🔓 Pages publiques (sans layout)
  { path: '', component: HomeComponent, data: { title: 'Home' } },
  { path: 'home', component: HomeComponent, data: { title: 'Home' } },
  { path: 'login', component: LoginComponent, canActivate: [noAuthGuard], data: { title: 'Login' } },
  { path: 'report/:type', component: ReportComponent, data: { title: 'Submit Report' } },
  { path: 'assignments', component: AssignmentsComponent, data: { title: 'My Assignments' } },
  { path: 'follow-up', component: FollowUpComponent, data: { title: 'Report Follow-up' } },

  // 🔐 Pages avec layout (sidebar + navbar)
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent, data: { title: 'Dashboard' } },
      { path: 'users', component: UsersComponent, data: { title: 'Users Management', roles: ['Admin'] } },
      { path: 'reports', component: ReportsListComponent, data: { title: 'Reports List' } },
      { path: 'incident-dashboard', component: IncidentDashboardComponent, data: { title: 'Incidents Management' } },
      { path: 'admin-parameters', component: AdminParametersComponent, data: { title: 'System Parameters', roles: ['Admin'] } },
      {
        path: 'reports/:id',
        loadComponent: () => import('./pages/report-details/report-details.component').then(m => m.ReportDetailsComponent),
        data: { title: 'Report Details' }
      },
      {
        path: 'actions',
        loadComponent: () => import('./pages/actions/actions.component').then(m => m.ActionsComponent),
        data: { title: 'Actions Management' }
      },

    ]
  },

  // ❓ Route inconnue
  { path: '**', redirectTo: '' }
];
