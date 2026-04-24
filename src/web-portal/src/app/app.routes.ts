import { Routes } from '@angular/router';
import { portalAuthGuard } from './core/auth/portal-auth.guard';

export const routes: Routes = [
  {
    path: '',
    canActivate: [portalAuthGuard],
    loadComponent: () =>
      import('./features/dashboard/portal-dashboard.component').then(m => m.PortalDashboardComponent)
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/otp-login.component').then(m => m.OtpLoginComponent)
  },
  { path: '**', redirectTo: '' }
];
