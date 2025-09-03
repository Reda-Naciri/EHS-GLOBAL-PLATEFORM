import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { 
  UserDto, 
  CreateUserDto, 
  UpdateUserDto, 
  UpdateUserRoleDto, 
  ChangePasswordDto,
  AdminResetPasswordDto 
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = environment.apiUrl + environment.endpoints.users;

  constructor(private http: HttpClient) {}

  getUsers(
    page: number = 1, 
    pageSize: number = 10, 
    search?: string, 
    role?: string, 
    department?: string, 
    zone?: string
  ): Observable<{ users: UserDto[], pagination: any }> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (search) params = params.set('search', search);
    if (role) params = params.set('role', role);
    if (department) params = params.set('department', department);
    if (zone) params = params.set('zone', zone);

    return this.http.get<{ users: UserDto[], pagination: any }>(this.apiUrl, { params });
  }

  getUserById(id: string): Observable<UserDto> {
    return this.http.get<UserDto>(`${this.apiUrl}/${id}`);
  }

  createUser(user: CreateUserDto): Observable<{ id: string; message: string }> {
    return this.http.post<{ id: string; message: string }>(this.apiUrl, user);
  }

  updateUser(id: string, user: UpdateUserDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}`, user);
  }

  testCompanyIdRouting(companyId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/company/${companyId}/test`);
  }

  updateUserProfile(companyId: string, formData: FormData): Observable<{ message: string }> {
    console.log('updateUserProfile called with URL:', `${this.apiUrl}/company/${companyId}/profile`);
    return this.http.put<{ message: string }>(`${this.apiUrl}/company/${companyId}/profile`, formData);
  }

  deleteUser(companyId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/company/${companyId}`);
  }

  updateUserRole(id: string, role: UpdateUserRoleDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/role`, role);
  }

  changePassword(companyId: string, passwordData: ChangePasswordDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/company/${companyId}/password`, passwordData);
  }

  adminResetPassword(companyId: string, passwordData: AdminResetPasswordDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/company/${companyId}/reset-password`, passwordData);
  }

  getDepartments(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/departments`);
  }

  getZones(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/zones`);
  }

  getRoles(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/roles`);
  }

  // Utility methods
  getDefaultRoles(): string[] {
    return ['Admin', 'HSE', 'Profil'];
  }

  getDefaultDepartments(): string[] {
    return [
      'Production', 
      'Quality', 
      'Maintenance', 
      'Safety', 
      'Engineering', 
      'Administration', 
      'HR', 
      'Finance'
    ];
  }

  getDefaultZones(): string[] {
    return [
      'Zone A', 
      'Zone B', 
      'Zone C', 
      'Zone D', 
      'Office Area', 
      'Warehouse', 
      'Laboratory', 
      'Parking'
    ];
  }

  getUserRoleColor(role: string): string {
    const roleColors: { [key: string]: string } = {
      'Admin': '#e74c3c',
      'HSE': '#f39c12',
      'Profil': '#3498db'
    };
    return roleColors[role] || '#95a5a6';
  }

  getUserStatusColor(isActive: boolean): string {
    return isActive ? '#27ae60' : '#e74c3c';
  }

  formatUserFullName(user: UserDto): string {
    return `${user.firstName} ${user.lastName}`.trim();
  }

  validateEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  validatePassword(password: string): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];
    
    if (password.length < 6) {
      errors.push('Password must be at least 6 characters long');
    }
    
    if (!/[A-Z]/.test(password)) {
      errors.push('Password must contain at least one uppercase letter');
    }
    
    if (!/[a-z]/.test(password)) {
      errors.push('Password must contain at least one lowercase letter');
    }
    
    if (!/\d/.test(password)) {
      errors.push('Password must contain at least one number');
    }
    
    if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
      errors.push('Password must contain at least one special character');
    }
    
    return {
      isValid: errors.length === 0,
      errors
    };
  }

  generateStrongPassword(): string {
    const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const lowercase = 'abcdefghijklmnopqrstuvwxyz';
    const numbers = '0123456789';
    const special = '!@#$%^&*(),.?":{}|<>';
    
    let password = '';
    password += uppercase.charAt(Math.floor(Math.random() * uppercase.length));
    password += lowercase.charAt(Math.floor(Math.random() * lowercase.length));
    password += numbers.charAt(Math.floor(Math.random() * numbers.length));
    password += special.charAt(Math.floor(Math.random() * special.length));
    
    const allChars = uppercase + lowercase + numbers + special;
    for (let i = 4; i < 12; i++) {
      password += allChars.charAt(Math.floor(Math.random() * allChars.length));
    }
    
    return password.split('').sort(() => Math.random() - 0.5).join('');
  }

  // Admin utility to assign company IDs
  assignCompanyIds(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/assign-company-ids`, {});
  }

  // User status management (Admin only)
  activateUser(userId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/UserManagement/${userId}/activate`, {});
  }

  deactivateUser(userId: string, reason?: string): Observable<{ message: string }> {
    const body = reason ? { reason } : {};
    return this.http.post<{ message: string }>(`${environment.apiUrl}/UserManagement/${userId}/deactivate`, body);
  }

  getUserStatus(userId: string): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/UserManagement/${userId}/status`);
  }

  getAllUsersWithStatus(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/UserManagement`);
  }

  getActiveUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/UserManagement/active`);
  }

  getInactiveUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/UserManagement/inactive`);
  }

  getUserStatistics(): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/UserManagement/statistics`);
  }
}