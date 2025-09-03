import { Component, AfterViewInit, ElementRef, Renderer2, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements AfterViewInit, OnDestroy {
  loginForm: FormGroup;
  loading = false;
  error: string | null = null;
  showDebugInfo = false;

  constructor(
    private el: ElementRef, 
    private renderer: Renderer2, 
    private router: Router,
    private fb: FormBuilder,
    private authService: AuthService
  ) {
    console.log('🔧 LoginComponent: Constructor called');
    
    this.loginForm = this.fb.group({
      email: ['reda.naciri@te.com', [Validators.required, Validators.email]],
      password: ['Admin123!', [Validators.required, Validators.minLength(6)]]
    });

    console.log('🔧 LoginComponent: LoginForm created', this.loginForm.value);
  }

  ngAfterViewInit() {
    console.log('🔧 LoginComponent: ngAfterViewInit called');
    
    // Add login-page class to body for styling
    document.body.classList.add('login-page');
    
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
            this.error = response.message || 'Login failed';
          }
        },
        error: (error) => {
          console.error('🔧 LoginComponent: Login error:', error);
          this.loading = false;
          this.error = error.error?.message || 'Login failed. Please try again.';
        }
      });
    } else {
      console.log('🔧 LoginComponent: Form is invalid');
      this.error = 'Please fill in all required fields correctly';
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

  ngOnDestroy() {
    console.log('🔧 LoginComponent: ngOnDestroy called');
    // Remove login-page class from body when leaving the component
    document.body.classList.remove('login-page');
  }
}
