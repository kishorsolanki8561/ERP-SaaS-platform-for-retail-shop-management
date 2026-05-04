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

      // ── Sales / Quotations workflow ──────────────────────────────────────────
      {
        path: AppRoutes.sales.quotations,
        loadComponent: () =>
          import('./features/sales/quotations/quotations.component').then(m => m.QuotationsComponent),
        canActivate: [permissionGuard(Permissions.quotations.view)],
      },
      {
        path: AppRoutes.sales.salesOrders,
        loadComponent: () =>
          import('./features/sales/orders/sales-orders.component').then(m => m.SalesOrdersComponent),
        canActivate: [permissionGuard(Permissions.quotations.view)],
      },
      {
        path: AppRoutes.sales.deliveryChallans,
        loadComponent: () =>
          import('./features/sales/delivery-challans/delivery-challans.component').then(m => m.DeliveryChallansComponent),
        canActivate: [permissionGuard(Permissions.quotations.view)],
      },

      // ── Purchasing ────────────────────────────────────────────────────────────
      {
        path: AppRoutes.purchasing.suppliers,
        loadComponent: () =>
          import('./features/purchasing/suppliers/suppliers.component').then(m => m.SuppliersComponent),
        canActivate: [permissionGuard(Permissions.purchasing.view)],
      },
      {
        path: AppRoutes.purchasing.orders,
        loadComponent: () =>
          import('./features/purchasing/orders/purchase-orders.component').then(m => m.PurchaseOrdersComponent),
        canActivate: [permissionGuard(Permissions.purchasing.view)],
      },

      // ── HR ────────────────────────────────────────────────────────────────────
      {
        path: AppRoutes.hr.employees,
        loadComponent: () =>
          import('./features/hr/employees/employees.component').then(m => m.EmployeesComponent),
        canActivate: [permissionGuard(Permissions.hr.view)],
      },
      {
        path: AppRoutes.hr.attendance,
        loadComponent: () =>
          import('./features/hr/attendance/attendance.component').then(m => m.AttendanceComponent),
        canActivate: [permissionGuard(Permissions.hr.view)],
      },
      {
        path: AppRoutes.hr.payroll,
        loadComponent: () =>
          import('./features/hr/payroll/payroll.component').then(m => m.PayrollComponent),
        canActivate: [permissionGuard(Permissions.hr.payroll)],
      },

      // ── Marketplace ───────────────────────────────────────────────────────────
      {
        path: AppRoutes.marketplace.accounts,
        loadComponent: () =>
          import('./features/marketplace/accounts/marketplace-accounts.component').then(m => m.MarketplaceAccountsComponent),
        canActivate: [permissionGuard(Permissions.marketplace.view)],
      },
      {
        path: AppRoutes.marketplace.orders,
        loadComponent: () =>
          import('./features/marketplace/orders/marketplace-orders.component').then(m => m.MarketplaceOrdersComponent),
        canActivate: [permissionGuard(Permissions.marketplace.view)],
      },

      // ── Sales Returns ─────────────────────────────────────────────────────────
      {
        path: AppRoutes.sales.salesReturns,
        loadComponent: () =>
          import('./features/sales/returns/sales-returns.component').then(m => m.SalesReturnsComponent),
        canActivate: [permissionGuard(Permissions.salesReturns.view)],
      },

      // ── Warranty ──────────────────────────────────────────────────────────────
      {
        path: AppRoutes.warranty.registrations,
        loadComponent: () =>
          import('./features/warranty/registrations/warranty-registrations.component').then(m => m.WarrantyRegistrationsComponent),
        canActivate: [permissionGuard(Permissions.warranty.view)],
      },
      {
        path: AppRoutes.warranty.claims,
        loadComponent: () =>
          import('./features/warranty/claims/warranty-claims.component').then(m => m.WarrantyClaimsComponent),
        canActivate: [permissionGuard(Permissions.warranty.view)],
      },

      // ── Pricing ───────────────────────────────────────────────────────────────
      {
        path: AppRoutes.pricing.rules,
        loadComponent: () =>
          import('./features/pricing/rules/pricing-rules.component').then(m => m.PricingRulesComponent),
        canActivate: [permissionGuard(Permissions.pricing.view)],
      },

      // ── Transport ─────────────────────────────────────────────────────────────
      {
        path: AppRoutes.transport.providers,
        loadComponent: () =>
          import('./features/transport/providers/transport-providers.component').then(m => m.TransportProvidersComponent),
        canActivate: [permissionGuard(Permissions.transport.view)],
      },
      {
        path: AppRoutes.transport.deliveries,
        loadComponent: () =>
          import('./features/transport/deliveries/transport-deliveries.component').then(m => m.TransportDeliveriesComponent),
        canActivate: [permissionGuard(Permissions.transport.view)],
      },

      // ── Accounting ────────────────────────────────────────────────────────────
      {
        path: AppRoutes.accounting.accounts,
        loadComponent: () =>
          import('./features/accounting/accounts/accounting-accounts.component').then(m => m.AccountingAccountsComponent),
        canActivate: [permissionGuard(Permissions.accounting.view)],
      },
      {
        path: AppRoutes.accounting.vouchers,
        loadComponent: () =>
          import('./features/accounting/vouchers/accounting-vouchers.component').then(m => m.AccountingVouchersComponent),
        canActivate: [permissionGuard(Permissions.accounting.view)],
      },

      // ── Reports ───────────────────────────────────────────────────────────────
      {
        path: AppRoutes.accounting.reports,
        loadComponent: () =>
          import('./features/reports/reports.component').then(m => m.ReportsComponent),
        canActivate: [permissionGuard(Permissions.reports.viewAccounting)],
      },

      // ── Subscription ──────────────────────────────────────────────────────────
      {
        path: AppRoutes.admin2.subscription,
        loadComponent: () =>
          import('./features/admin/subscription/admin-subscription.component').then(m => m.AdminSubscriptionComponent),
        canActivate: [permissionGuard(Permissions.subscription.view)],
      },

      // ── Audit Logs ────────────────────────────────────────────────────────────
      {
        path: AppRoutes.admin2.auditLogs,
        loadComponent: () =>
          import('./features/admin/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent),
        canActivate: [permissionGuard(Permissions.auditLog.view)],
      },

      // ── Usage & Quotas ────────────────────────────────────────────────────────
      {
        path: AppRoutes.admin2.usage,
        loadComponent: () =>
          import('./features/admin/usage/usage.component').then(m => m.UsageComponent),
        canActivate: [permissionGuard(Permissions.usage.view)],
      },

      // ── Sync — Devices & Exceptions ───────────────────────────────────────────
      {
        path: AppRoutes.admin2.syncDevices,
        loadComponent: () =>
          import('./features/admin/sync/devices/sync-devices.component').then(m => m.SyncDevicesComponent),
        canActivate: [permissionGuard(Permissions.sync.manageDevices)],
      },
      {
        path: AppRoutes.admin2.syncExceptions,
        loadComponent: () =>
          import('./features/admin/sync/exceptions/sync-exceptions.component').then(m => m.SyncExceptionsComponent),
        canActivate: [permissionGuard(Permissions.sync.resolveException)],
      },

      // ── On-Prem Deployments ───────────────────────────────────────────────────
      {
        path: AppRoutes.admin2.onPrem,
        loadComponent: () =>
          import('./features/admin/on-prem/on-prem.component').then(m => m.OnPremComponent),
        canActivate: [permissionGuard(Permissions.onPrem.view)],
      },

      // ── Platform Admin ────────────────────────────────────────────────────────
      {
        path: AppRoutes.platform.shops,
        loadComponent: () =>
          import('./features/platform/shops/platform-shops.component').then(m => m.PlatformShopsComponent),
        canActivate: [permissionGuard(Permissions.platform.shopsView)],
      },
      {
        path: AppRoutes.platform.leads,
        loadComponent: () =>
          import('./features/platform/leads/platform-leads.component').then(m => m.PlatformLeadsComponent),
        canActivate: [permissionGuard(Permissions.lead.view)],
      },
      {
        path: AppRoutes.platform.subscriptionDashboard,
        loadComponent: () =>
          import('./features/platform/subscription-dashboard/platform-subscription-dashboard.component')
            .then(m => m.PlatformSubscriptionDashboardComponent),
        canActivate: [permissionGuard(Permissions.platform.shopsView)],
      },
      {
        path: AppRoutes.platform.systemHealth,
        loadComponent: () =>
          import('./features/platform/system-health/platform-system-health.component')
            .then(m => m.PlatformSystemHealthComponent),
        canActivate: [permissionGuard(Permissions.platform.shopsView)],
      },
      {
        path: AppRoutes.platform.plans,
        loadComponent: () =>
          import('./features/platform/plans/platform-plans.component').then(m => m.PlatformPlansComponent),
        canActivate: [permissionGuard(Permissions.platform.shopsManage)],
      },

      // ── Payment ──────────────────────────────────────────────────────────────
      {
        path: AppRoutes.payment.gateways,
        loadComponent: () =>
          import('./features/payment/gateways/payment-gateways.component').then(m => m.PaymentGatewaysComponent),
        canActivate: [permissionGuard(Permissions.payment.configure)],
      },
      {
        path: AppRoutes.payment.transactions,
        loadComponent: () =>
          import('./features/payment/transactions/payment-transactions.component').then(m => m.PaymentTransactionsComponent),
        canActivate: [permissionGuard(Permissions.payment.view)],
      },
      {
        path: AppRoutes.payment.exceptions,
        loadComponent: () =>
          import('./features/payment/exceptions/payment-exceptions.component').then(m => m.PaymentExceptionsComponent),
        canActivate: [permissionGuard(Permissions.payment.reconcile)],
      },

      // ── Integrations ─────────────────────────────────────────────────────────
      {
        path: AppRoutes.integration.apiKeys,
        loadComponent: () =>
          import('./features/integrations/api-keys/api-keys.component').then(m => m.ApiKeysComponent),
        canActivate: [permissionGuard(Permissions.integration.manageApiKeys)],
      },
      {
        path: AppRoutes.integration.webhooks,
        loadComponent: () =>
          import('./features/integrations/webhooks/webhooks.component').then(m => m.WebhooksComponent),
        canActivate: [permissionGuard(Permissions.integration.manageWebhooks)],
      },

      // ── Service Jobs ─────────────────────────────────────────────────────────
      {
        path: AppRoutes.serviceJobs.list,
        loadComponent: () =>
          import('./features/service-jobs/service-jobs.component').then(m => m.ServiceJobsComponent),
        canActivate: [permissionGuard(Permissions.serviceJobs.view)],
      },

      // ── Medical — Drug Batches ─────────────────────────────────────────────
      {
        path: AppRoutes.medical.batches,
        loadComponent: () =>
          import('./features/medical/medical-batches.component').then(m => m.MedicalBatchesComponent),
        canActivate: [permissionGuard(Permissions.medical.view)],
      },

      // ── Grocery — Loyalty Programme ───────────────────────────────────────
      {
        path: AppRoutes.loyalty.program,
        loadComponent: () =>
          import('./features/loyalty/loyalty-program.component').then(m => m.LoyaltyProgramComponent),
        canActivate: [permissionGuard(Permissions.loyalty.view)],
      },

      // ── Shop Vertical Picker ──────────────────────────────────────────────
      {
        path: AppRoutes.verticals.picker,
        loadComponent: () =>
          import('./features/admin/vertical/admin-vertical.component').then(m => m.AdminVerticalComponent),
        canActivate: [permissionGuard(Permissions.verticals.view)],
      },

      // ── Platform — Vertical Packs ─────────────────────────────────────────
      {
        path: AppRoutes.verticals.platform,
        loadComponent: () =>
          import('./features/platform/verticals/platform-verticals.component').then(m => m.PlatformVerticalsComponent),
        canActivate: [permissionGuard(Permissions.platform.shopsView)],
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
