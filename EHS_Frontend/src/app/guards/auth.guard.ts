import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = (route: any, state: any) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    const requiredRoles = route.data?.['roles'] as string[];
    
    if (requiredRoles && requiredRoles.length > 0) {
      const hasRequiredRole = authService.hasAnyRole(requiredRoles);
      if (!hasRequiredRole) {
        return router.createUrlTree(['/unauthorized']);
      }
    }
    
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const noAuthGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};