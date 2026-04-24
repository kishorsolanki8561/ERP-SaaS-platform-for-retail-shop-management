import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/app-layout/app-layout.component').then(m => m.AppLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'admin/users',
        loadComponent: () =>
          import('./features/admin/users/users.component').then(m => m.UsersComponent),
        canActivate: [permissionGuard('Users.View')]
      },
      {
        path: 'admin/shop-profile',
        loadComponent: () =>
          import('./features/admin/shop-profile/shop-profile.component').then(m => m.ShopProfileComponent),
        canActivate: [permissionGuard('ShopProfile.View')]
      }
    ]
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./layout/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
  },
  { path: 'unauthorized', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'feature-unavailable', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: '**', redirectTo: '' }
];
