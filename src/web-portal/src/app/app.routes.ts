import { Routes } from '@angular/router';
import { portalAuthGuard } from './core/auth/portal-auth.guard';
import { PortalShellComponent } from './features/shell/portal-shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/otp-login.component').then(m => m.OtpLoginComponent)
  },
  {
    path: '',
    component: PortalShellComponent,
    canActivate: [portalAuthGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/dashboard/portal-dashboard.component').then(m => m.PortalDashboardComponent)
      },
      {
        path: 'purchases',
        loadComponent: () =>
          import('./features/purchases/purchases.component').then(m => m.PurchasesComponent)
      },
      {
        path: 'purchases/:id',
        loadComponent: () =>
          import('./features/purchases/purchase-detail.component').then(m => m.PurchaseDetailComponent)
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./features/orders/orders.component').then(m => m.OrdersComponent)
      },
      {
        path: 'shops',
        loadComponent: () =>
          import('./features/shops/shops.component').then(m => m.ShopsComponent)
      },
      {
        path: 'shops/:shopId/catalog',
        loadComponent: () =>
          import('./features/shops/shop-catalog.component').then(m => m.ShopCatalogComponent)
      },
      {
        path: 'inquiries',
        loadComponent: () =>
          import('./features/inquiries/inquiries.component').then(m => m.InquiriesComponent)
      },
      {
        path: 'insights',
        loadComponent: () =>
          import('./features/insights/insights.component').then(m => m.InsightsComponent)
      },
      {
        path: 'wallet',
        loadComponent: () =>
          import('./features/wallet/wallet.component').then(m => m.WalletComponent)
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/profile/profile.component').then(m => m.ProfileComponent)
      },
    ]
  },
  { path: '**', redirectTo: '' }
];
