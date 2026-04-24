import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { permissionGuard } from './core/auth/permission.guard';
import { AppRoutes } from './shared/messages/app-routes';
import { Permissions } from './shared/messages/app-permissions';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layout/app-layout/app-layout.component').then(m => m.AppLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: AppRoutes.dashboard, pathMatch: 'full' },
      {
        path: AppRoutes.dashboard,
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: AppRoutes.admin.users,
        loadComponent: () =>
          import('./features/admin/users/users.component').then(m => m.UsersComponent),
        canActivate: [permissionGuard(Permissions.users.view)]
      },
      {
        path: AppRoutes.admin.shopProfile,
        loadComponent: () =>
          import('./features/admin/shop-profile/shop-profile.component').then(m => m.ShopProfileComponent),
        canActivate: [permissionGuard(Permissions.shopProfile.view)]
      }
    ]
  },
  {
    path: AppRoutes.login,
    loadComponent: () =>
      import('./layout/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent)
  },
  {
    path: AppRoutes.forgotPassword,
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent)
  },
  { path: AppRoutes.unauthorized, loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: AppRoutes.featureUnavailable, loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: '**', redirectTo: '' }
];
