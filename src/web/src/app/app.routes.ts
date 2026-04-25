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

      // Dashboard
      {
        path: AppRoutes.dashboard,
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },

      // Admin
      {
        path: AppRoutes.admin.users,
        loadComponent: () =>
          import('./features/admin/users/users.component').then(m => m.UsersComponent),
        canActivate: [permissionGuard(Permissions.users.view)],
      },
      {
        path: AppRoutes.admin.shopProfile,
        loadComponent: () =>
          import('./features/admin/shop-profile/shop-profile.component').then(m => m.ShopProfileComponent),
        canActivate: [permissionGuard(Permissions.shopProfile.view)],
      },
      {
        path: AppRoutes.admin.roles,
        loadComponent: () =>
          import('./features/admin/roles/roles.component').then(m => m.RolesComponent),
        canActivate: [permissionGuard(Permissions.users.view)],
      },

      // CRM
      {
        path: AppRoutes.crm.customers,
        loadComponent: () =>
          import('./features/crm/customers/customers.component').then(m => m.CustomersComponent),
        canActivate: [permissionGuard(Permissions.crm.view)],
      },

      // Inventory
      {
        path: AppRoutes.inventory.products,
        loadComponent: () =>
          import('./features/inventory/products/products.component').then(m => m.ProductsComponent),
        canActivate: [permissionGuard(Permissions.inventory.view)],
      },

      // Billing
      {
        path: AppRoutes.billing.invoices,
        loadComponent: () =>
          import('./features/billing/invoices/invoices.component').then(m => m.InvoicesComponent),
        canActivate: [permissionGuard(Permissions.billing.view)],
      },
      {
        path: 'billing/invoices/:id',
        loadComponent: () =>
          import('./features/billing/invoices/invoice-detail.component').then(m => m.InvoiceDetailComponent),
        canActivate: [permissionGuard(Permissions.billing.view)],
      },

      // Wallet
      {
        path: AppRoutes.wallet.balances,
        loadComponent: () =>
          import('./features/wallet/balances/wallet-balances.component').then(m => m.WalletBalancesComponent),
        canActivate: [permissionGuard(Permissions.wallet.view)],
      },
      {
        path: AppRoutes.wallet.transactions,
        loadComponent: () =>
          import('./features/wallet/transactions/wallet-transactions.component').then(m => m.WalletTransactionsComponent),
        canActivate: [permissionGuard(Permissions.wallet.view)],
      },

      // POS / Shifts
      {
        path: AppRoutes.pos.shifts,
        loadComponent: () =>
          import('./features/pos/shifts/shifts.component').then(m => m.ShiftsComponent),
        canActivate: [permissionGuard(Permissions.shift.view)],
      },
      {
        path: AppRoutes.pos.openShift,
        loadComponent: () =>
          import('./features/pos/open-shift/open-shift.component').then(m => m.OpenShiftComponent),
        canActivate: [permissionGuard(Permissions.shift.open)],
      },
      {
        path: AppRoutes.pos.terminal,
        loadComponent: () =>
          import('./features/pos/terminal/pos-terminal.component').then(m => m.PosTerminalComponent),
        canActivate: [permissionGuard(Permissions.shift.view)],
      },
    ],
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent),
    children: [
      {
        path: AppRoutes.login,
        loadComponent: () =>
          import('./features/auth/login/login.component').then(m => m.LoginComponent),
      },
      {
        path: AppRoutes.forgotPassword,
        loadComponent: () =>
          import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
      },
      {
        path: AppRoutes.resetPassword,
        loadComponent: () =>
          import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
      },
      {
        path: AppRoutes.acceptInvite,
        loadComponent: () =>
          import('./features/auth/accept-invite/accept-invite.component').then(m => m.AcceptInviteComponent),
      },
    ],
  },
  { path: AppRoutes.unauthorized,       loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: AppRoutes.featureUnavailable, loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: '**', redirectTo: '' },
];
