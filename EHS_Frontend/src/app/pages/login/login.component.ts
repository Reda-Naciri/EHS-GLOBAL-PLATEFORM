import { Component, AfterViewInit, ElementRef, Renderer2, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { RegistrationService } from '../../services/registration.service';
import { ReportService } from '../../services/report.service';
import { AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements AfterViewInit, OnDestroy {
  loginForm!: FormGroup;
  registerForm!: FormGroup;
  loading = false;
  registerLoading = false;
  error: string | null = null;
  registerError: string | null = null;
  registerSuccess: string | null = null;
  showDebugInfo = false;
  departments: any[] = [];
  

  constructor(
    private el: ElementRef, 
    private renderer: Renderer2, 
    private router: Router,
    private fb: FormBuilder,
    private authService: AuthService,
    private registrationService: RegistrationService,
    private reportService: ReportService,
    private alertService: AlertService
  ) {
    console.log('🔧 LoginComponent: Constructor called');
    
    this.loginForm = this.fb.group({
      email: ['reda.naciri@te.com', [Validators.required, Validators.email]],
      password: ['Admin123!', [Validators.required, Validators.minLength(6)]]
    });

    this.registerForm = this.fb.group({
      companyId: ['', [Validators.required]],
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      department: ['', [Validators.required]],
      position: ['', [Validators.required]]
    });

    console.log('🔧 LoginComponent: Forms created', {
      loginForm: this.loginForm.value,
      registerForm: this.registerForm.value
    });
  }

  ngAfterViewInit() {
    console.log('🔧 LoginComponent: ngAfterViewInit called');
    
    // Add login-page class to body for styling
    document.body.classList.add('login-page');
    
    // Load departments for registration form
    this.loadDepartments();
    
    const container = this.el.nativeElement.querySelector('#container');
    const registerBtn = this.el.nativeElement.querySelector('#register');
    const loginBtn = this.el.nativeElement.querySelector('#login');
    const mobileSwitchBtn = this.el.nativeElement.querySelector('#mobileSwitch');

    console.log('🔧 LoginComponent: DOM elements found', {
      container: !!container,
      registerBtn: !!registerBtn,
      loginBtn: !!loginBtn,
      mobileSwitchBtn: !!mobileSwitchBtn
    });

    if (registerBtn && loginBtn && container && mobileSwitchBtn) {
      // Desktop Toggle
      this.renderer.listen(registerBtn, 'click', () => {
        console.log('🔧 LoginComponent: Register button clicked');
        container.classList.add("active");
      });

      this.renderer.listen(loginBtn, 'click', () => {
        console.log('🔧 LoginComponent: Login button clicked');
        container.classList.remove("active");
      });

      // Mobile Toggle
      this.renderer.listen(mobileSwitchBtn, 'click', () => {
        console.log('🔧 LoginComponent: Mobile switch button clicked');
        container.classList.toggle("active");
      });
    } else {
      console.error("🔧 LoginComponent: One or more elements not found.");
    }
  }

  onSubmit() {
    console.log('🔧 LoginComponent: onSubmit called');
    console.log('🔧 LoginComponent: Form valid:', this.loginForm.valid);
    console.log('🔧 LoginComponent: Form values:', this.loginForm.value);
    console.log('🔧 LoginComponent: Form errors:', this.loginForm.errors);

    if (this.loginForm.valid) {
      this.loading = true;
      this.error = null;

      const credentials = {
        email: this.loginForm.get('email')?.value,
        password: this.loginForm.get('password')?.value
      };

      console.log('🔧 LoginComponent: Credentials to send:', credentials);

      this.authService.login(credentials).subscribe({
        next: (response) => {
          console.log('🔧 LoginComponent: Login response received:', response);
          this.loading = false;
          if (response.success) {
            console.log('🔧 LoginComponent: Login successful, navigating to dashboard');
            this.router.navigate(['/dashboard']);
          } else {
            console.log('🔧 LoginComponent: Login failed:', response.message);
            this.alertService.showError(response.message || 'Login failed');
          }
        },
        error: (error) => {
          console.error('🔧 LoginComponent: Login error:', error);
          this.loading = false;
          this.alertService.showError(error.error?.message || 'Login failed. Please try again.');
        }
      });
    } else {
      console.log('🔧 LoginComponent: Form is invalid');
      this.alertService.showWarning('Please fill in all required fields correctly');
    }
  }

  testLogin() {
    console.log('🔧 LoginComponent: Test login called');
    this.loginForm.patchValue({
      email: 'reda.naciri@te.com',
      password: 'Admin123!'
    });
    this.onSubmit();
  }

  toggleDebug() {
    this.showDebugInfo = !this.showDebugInfo;
    console.log('🔧 LoginComponent: Debug info toggled:', this.showDebugInfo);
  }

  navigateToDashboard() {
    console.log('🔧 LoginComponent: Direct navigation to dashboard');
    this.router.navigate(['/dashboard']);
  }

  initializeTestDepartments() {
    // Immediately set test departments for visual testing
    this.departments = [
      { name: 'Engineering' },
      { name: 'Production' },
      { name: 'Quality Assurance' },
      { name: 'Quality Control' },
      { name: 'Maintenance' },
      { name: 'Health, Safety & Environment (HSE)' },
      { name: 'Manufacturing' },
      { name: 'Research & Development' },
      { name: 'Operations' },
      { name: 'Supply Chain' },
      { name: 'Logistics' },
      { name: 'Procurement' },
      { name: 'Finance' },
      { name: 'Human Resources' },
      { name: 'Information Technology' },
      { name: 'Administration' },
      { name: 'Sales' },
      { name: 'Marketing' },
      { name: 'Customer Service' },
      { name: 'Legal & Compliance' },
      { name: 'Project Management' },
      { name: 'Facilities' },
      { name: 'Security' },
      { name: 'Training & Development' }
    ];
    console.log('🔧 LoginComponent: Test departments initialized:', this.departments.length);
  }

  loadDepartments() {
    this.reportService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
        console.log('🔧 LoginComponent: Departments loaded:', departments);
      },
      error: (error) => {
        console.error('🔧 LoginComponent: Error loading departments:', error);
        // Fallback to comprehensive department list for testing
        this.departments = [
          { name: 'Engineering' },
          { name: 'Production' },
          { name: 'Quality Assurance' },
          { name: 'Quality Control' },
          { name: 'Maintenance' },
          { name: 'Health, Safety & Environment (HSE)' },
          { name: 'Manufacturing' },
          { name: 'Research & Development' },
          { name: 'Operations' },
          { name: 'Supply Chain' },
          { name: 'Logistics' },
          { name: 'Procurement' },
          { name: 'Finance' },
          { name: 'Human Resources' },
          { name: 'Information Technology' },
          { name: 'Administration' },
          { name: 'Sales' },
          { name: 'Marketing' },
          { name: 'Customer Service' },
          { name: 'Legal & Compliance' },
          { name: 'Project Management' },
          { name: 'Facilities' },
          { name: 'Security' },
          { name: 'Training & Development' }
        ];
      }
    });
  }

  onRegisterSubmit() {
    console.log('🔧 LoginComponent: onRegisterSubmit called');
    console.log('🔧 LoginComponent: Register form valid:', this.registerForm.valid);
    console.log('🔧 LoginComponent: Register form values:', this.registerForm.value);

    if (this.registerForm.valid) {
      this.registerLoading = true;
      this.registerError = null;
      this.registerSuccess = null;

      const requestData = {
        companyId: this.registerForm.get('companyId')?.value,
        fullName: this.registerForm.get('fullName')?.value,
        email: this.registerForm.get('email')?.value,
        department: this.registerForm.get('department')?.value,
        position: this.registerForm.get('position')?.value
      };

      console.log('🔧 LoginComponent: Registration request data:', requestData);

      this.registrationService.submitRegistrationRequest(requestData).subscribe({
        next: (response) => {
          console.log('🔧 LoginComponent: Registration response received:', response);
          this.registerLoading = false;
          this.alertService.showSuccess(response.message || 'Registration request submitted successfully! You will receive an email confirmation.');
          this.registerForm.reset();
        },
        error: (error) => {
          console.error('🔧 LoginComponent: Registration error:', error);
          this.registerLoading = false;
          this.alertService.showError(error.error?.message || 'Registration request failed. Please try again.');
        }
      });
    } else {
      console.log('🔧 LoginComponent: Register form is invalid');
      this.alertService.showWarning('Please fill in all required fields correctly');
    }
  }

  goHome() {
    console.log('🔧 LoginComponent: Going back to home page');
    this.router.navigate(['/']);
  }

  ngOnDestroy() {
    console.log('🔧 LoginComponent: ngOnDestroy called');
    // Remove login-page class from body when leaving the component
    document.body.classList.remove('login-page');
  }

}
