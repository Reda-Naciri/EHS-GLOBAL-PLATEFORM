import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { LoginDto, AuthResponseDto, UserDto } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl + environment.endpoints.auth;
  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    console.log('🔧 AuthService: Constructor called');
    console.log('🔧 AuthService: API URL:', this.apiUrl);
    
    // Check if user is logged in on app start - but handle errors gracefully
    this.checkAuthStatus();
  }

  private checkAuthStatus(): void {
    console.log('🔧 AuthService: checkAuthStatus called');
    const token = this.getToken();
    console.log('🔧 AuthService: Token found:', !!token);
    
    if (token && !this.isTokenExpired(token)) {
      console.log('🔧 AuthService: Valid token found, attempting to restore user from storage');
      
      // Use stored user data to avoid circular dependency during initialization
      const storedUser = this.getStoredUserData();
      if (storedUser) {
        console.log('🔧 AuthService: Using stored user data:', storedUser);
        this.currentUserSubject.next(storedUser);
      } else {
        console.log('🔧 AuthService: No stored user data available');
        // Don't make HTTP call during initialization to avoid circular dependency
        // The profile will be fetched when needed by other components
      }
    } else if (token) {
      console.log('🔧 AuthService: Token expired, logging out');
      this.logout();
    }
  }

  login(credentials: LoginDto): Observable<AuthResponseDto> {
    console.log('🔧 AuthService: login called with credentials:', credentials);
    
    // Check if we should use real backend or mock
    const useRealBackend = true; // Backend is running on port 5225
    
    if (useRealBackend) {
      console.log('🔧 AuthService: Using real backend');
      return this.http.post<AuthResponseDto>(`${this.apiUrl}/login`, credentials)
        .pipe(
          tap(response => {
            console.log('🔧 AuthService: Backend response:', response);
            if (response.success && response.token && response.user) {
              localStorage.setItem('token', response.token);
              this.storeUserData(response.user);
              this.currentUserSubject.next(response.user);
            }
          })
        );
    } else {
      console.log('🔧 AuthService: Using mock backend');
      
      // Create a mock response
      const mockResponse: AuthResponseDto = {
        success: true,
        message: 'Login successful',
        token: 'mock-jwt-token-12345',
        user: {
          id: 'user-123',
          email: credentials.email,
          firstName: 'Test',
          lastName: 'User',
          role: 'Admin',
          department: 'IT',
          position: 'Manager',
          isActive: true,
          accountCreatedAt: new Date(),
          lastLoginAt: new Date()
        }
      };

      console.log('🔧 AuthService: Mock response created:', mockResponse);

      // Store token and user
      if (mockResponse.token) {
        localStorage.setItem('token', mockResponse.token);
        console.log('🔧 AuthService: Token stored in localStorage');
      }
      if (mockResponse.user) {
        this.storeUserData(mockResponse.user);
        this.currentUserSubject.next(mockResponse.user);
        console.log('🔧 AuthService: User stored in currentUserSubject and localStorage');
      }

      // Return as observable
      return new Observable(observer => {
        setTimeout(() => {
          console.log('🔧 AuthService: Sending mock response');
          observer.next(mockResponse);
          observer.complete();
        }, 1000); // Simulate network delay
      });
    }
  }

  logout(): void {
    console.log('🔓 Logout called - starting process');
    
    const token = this.getToken();
    if (token) {
      console.log('🔓 Token found, calling backend logout first');
      // Call backend logout first, then local logout
      this.http.post(`${this.apiUrl}/logout`, {}, {
        headers: this.getAuthHeaders()
      }).subscribe({
        next: (response) => {
          console.log('✅ Backend logout successful:', response);
          this.performLogout();
        },
        error: (error) => {
          console.error('❌ Backend logout failed:', error);
          // Still perform local logout even if backend fails
          this.performLogout();
        }
      });
    } else {
      console.log('🔓 No token found, performing local logout only');
      this.performLogout();
    }
  }

  private performLogout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('userData');
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  getProfile(): Observable<{ success: boolean; user?: UserDto }> {
    return this.http.get<{ success: boolean; user?: UserDto }>(`${this.apiUrl}/profile`, {
      headers: this.getAuthHeaders()
    });
  }

  validateToken(): Observable<{ valid: boolean; message: string; userId?: string }> {
    return this.http.get<{ valid: boolean; message: string; userId?: string }>(`${this.apiUrl}/validate`, {
      headers: this.getAuthHeaders()
    });
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    return token !== null && !this.isTokenExpired(token);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  getCurrentUser(): UserDto | null {
    return this.currentUserSubject.value;
  }

  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user ? user.role === role : false;
  }

  hasAnyRole(roles: string[]): boolean {
    const user = this.getCurrentUser();
    return user ? roles.includes(user.role) : false;
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp * 1000; // Convert to milliseconds
      return Date.now() >= exp;
    } catch (error) {
      return true;
    }
  }

  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  private storeUserData(user: UserDto): void {
    try {
      localStorage.setItem('userData', JSON.stringify(user));
      console.log('🔧 AuthService: User data stored in localStorage');
    } catch (error) {
      console.error('🔧 AuthService: Failed to store user data:', error);
    }
  }

  private getStoredUserData(): UserDto | null {
    try {
      const userData = localStorage.getItem('userData');
      if (userData) {
        const user = JSON.parse(userData);
        console.log('🔧 AuthService: Retrieved stored user data:', user);
        return user;
      }
    } catch (error) {
      console.error('🔧 AuthService: Failed to parse stored user data:', error);
    }
    return null;
  }

  // Validate current session and refresh user data (call this after app is initialized)
  validateSession(): void {
    const token = this.getToken();
    if (token && !this.isTokenExpired(token)) {
      console.log('🔧 AuthService: Validating session with backend');
      
      this.getProfile().subscribe({
        next: (profileResponse) => {
          console.log('🔧 AuthService: Profile response:', profileResponse);
          if (profileResponse.success && profileResponse.user) {
            this.storeUserData(profileResponse.user);
            this.currentUserSubject.next(profileResponse.user);
            console.log('🔧 AuthService: User profile refreshed:', profileResponse.user);
          }
        },
        error: (error) => {
          console.warn('🔧 AuthService: Failed to validate session:', error);
          // Keep using stored data if available, don't logout automatically
        }
      });
    }
  }
}