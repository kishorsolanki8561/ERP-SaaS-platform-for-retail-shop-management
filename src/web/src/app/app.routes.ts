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
    ],
  },
  { path: AppRoutes.unauthorized,       loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: AppRoutes.featureUnavailable, loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: '**', redirectTo: '' },
];
