export const AppRoutes = {
  dashboard:          'dashboard',
  login:              'login',
  forgotPassword:     'forgot-password',
  resetPassword:      'reset-password',
  acceptInvite:       'accept-invite',
  unauthorized:       'unauthorized',
  featureUnavailable: 'feature-unavailable',
  admin: {
    users:        'admin/users',
    roles:        'admin/roles',
    shopProfile:  'admin/shop-profile',
  },
  crm: {
    customers:    'crm/customers',
  },
  inventory: {
    products:     'inventory/products',
  },
  billing: {
    invoices:     'billing/invoices',
    invoiceDetail: (id: number | string) => `billing/invoices/${id}`,
  },
  wallet: {
    balances:     'wallet/balances',
    transactions: 'wallet/transactions',
  },
  pos: {
    shifts:    'pos/shifts',
    openShift: 'pos/open-shift',
    terminal:  'pos/terminal',
  },
  sales: {
    quotations:       'sales/quotations',
    salesOrders:      'sales/orders',
    deliveryChallans: 'sales/delivery-challans',
    salesReturns:     'sales/returns',
  },
  purchasing: {
    suppliers: 'purchasing/suppliers',
    orders:    'purchasing/orders',
  },
  hr: {
    employees:  'hr/employees',
    attendance: 'hr/attendance',
    payroll:    'hr/payroll',
  },
  marketplace: {
    accounts: 'marketplace/accounts',
    orders:   'marketplace/orders',
  },
  accounting: {
    accounts:  'accounting/accounts',
    vouchers:  'accounting/vouchers',
    reports:   'accounting/reports',
  },
  warranty: {
    registrations: 'warranty/registrations',
    claims:        'warranty/claims',
  },
  pricing: {
    rules: 'pricing/rules',
  },
  transport: {
    providers: 'transport/providers',
    deliveries:'transport/deliveries',
  },
  admin2: {
    subscription:  'admin/subscription',
    auditLogs:     'admin/audit-logs',
    usage:         'admin/usage',
    syncDevices:   'admin/sync/devices',
    syncExceptions:'admin/sync/exceptions',
    onPrem:        'admin/on-prem',
  },
  platform: {
    shops:                  'platform/shops',
    leads:                  'platform/leads',
    subscriptionDashboard:  'platform/subscription-dashboard',
    systemHealth:           'platform/system-health',
    plans:                  'platform/plans',
  },
  integration: {
    apiKeys:  'integrations/api-keys',
    webhooks: 'integrations/webhooks',
  },
  payment: {
    gateways:     'payment/gateways',
    transactions: 'payment/transactions',
    exceptions:   'payment/exceptions',
  },
  serviceJobs: {
    list:   'service/jobs',
    track:  'service/track',
  },
  medical: {
    batches:  'medical/batches',
    expiring: 'medical/expiring',
  },
  loyalty: {
    program:   'loyalty/program',
    customers: 'loyalty/customers',
  },
  verticals: {
    picker:   'admin/vertical',
    platform: 'platform/verticals',
  },
} as const;

export const AppRoutePaths = {
  dashboard:          `/${AppRoutes.dashboard}`,
  login:              `/${AppRoutes.login}`,
  forgotPassword:     `/${AppRoutes.forgotPassword}`,
  resetPassword:      `/${AppRoutes.resetPassword}`,
  acceptInvite:       `/${AppRoutes.acceptInvite}`,
  unauthorized:       `/${AppRoutes.unauthorized}`,
  featureUnavailable: `/${AppRoutes.featureUnavailable}`,
  admin: {
    users:        `/${AppRoutes.admin.users}`,
    roles:        `/${AppRoutes.admin.roles}`,
    shopProfile:  `/${AppRoutes.admin.shopProfile}`,
  },
  crm: {
    customers:    `/${AppRoutes.crm.customers}`,
  },
  inventory: {
    products:     `/${AppRoutes.inventory.products}`,
  },
  billing: {
    invoices:     `/${AppRoutes.billing.invoices}`,
    invoiceDetail: (id: number | string) => `/${AppRoutes.billing.invoiceDetail(id)}`,
  },
  wallet: {
    balances:     `/${AppRoutes.wallet.balances}`,
    transactions: `/${AppRoutes.wallet.transactions}`,
  },
  pos: {
    shifts:    `/${AppRoutes.pos.shifts}`,
    openShift: `/${AppRoutes.pos.openShift}`,
    terminal:  `/${AppRoutes.pos.terminal}`,
  },
  sales: {
    quotations:       `/${AppRoutes.sales.quotations}`,
    salesOrders:      `/${AppRoutes.sales.salesOrders}`,
    deliveryChallans: `/${AppRoutes.sales.deliveryChallans}`,
    salesReturns:     `/${AppRoutes.sales.salesReturns}`,
  },
  purchasing: {
    suppliers: `/${AppRoutes.purchasing.suppliers}`,
    orders:    `/${AppRoutes.purchasing.orders}`,
  },
  hr: {
    employees:  `/${AppRoutes.hr.employees}`,
    attendance: `/${AppRoutes.hr.attendance}`,
    payroll:    `/${AppRoutes.hr.payroll}`,
  },
  marketplace: {
    accounts: `/${AppRoutes.marketplace.accounts}`,
    orders:   `/${AppRoutes.marketplace.orders}`,
  },
  accounting: {
    accounts:  `/${AppRoutes.accounting.accounts}`,
    vouchers:  `/${AppRoutes.accounting.vouchers}`,
    reports:   `/${AppRoutes.accounting.reports}`,
  },
  warranty: {
    registrations: `/${AppRoutes.warranty.registrations}`,
    claims:        `/${AppRoutes.warranty.claims}`,
  },
  pricing: {
    rules: `/${AppRoutes.pricing.rules}`,
  },
  transport: {
    providers:  `/${AppRoutes.transport.providers}`,
    deliveries: `/${AppRoutes.transport.deliveries}`,
  },
  admin2: {
    subscription:  `/${AppRoutes.admin2.subscription}`,
    auditLogs:     `/${AppRoutes.admin2.auditLogs}`,
    usage:         `/${AppRoutes.admin2.usage}`,
    syncDevices:   `/${AppRoutes.admin2.syncDevices}`,
    syncExceptions:`/${AppRoutes.admin2.syncExceptions}`,
    onPrem:        `/${AppRoutes.admin2.onPrem}`,
  },
  platform: {
    shops:                 `/${AppRoutes.platform.shops}`,
    leads:                 `/${AppRoutes.platform.leads}`,
    subscriptionDashboard: `/${AppRoutes.platform.subscriptionDashboard}`,
    systemHealth:          `/${AppRoutes.platform.systemHealth}`,
    plans:                 `/${AppRoutes.platform.plans}`,
  },
  integration: {
    apiKeys:  `/${AppRoutes.integration.apiKeys}`,
    webhooks: `/${AppRoutes.integration.webhooks}`,
  },
  payment: {
    gateways:     `/${AppRoutes.payment.gateways}`,
    transactions: `/${AppRoutes.payment.transactions}`,
    exceptions:   `/${AppRoutes.payment.exceptions}`,
  },
  serviceJobs: {
    list:   `/${AppRoutes.serviceJobs.list}`,
    track:  `/${AppRoutes.serviceJobs.track}`,
  },
  medical: {
    batches:  `/${AppRoutes.medical.batches}`,
    expiring: `/${AppRoutes.medical.expiring}`,
  },
  loyalty: {
    program:   `/${AppRoutes.loyalty.program}`,
    customers: `/${AppRoutes.loyalty.customers}`,
  },
  verticals: {
    picker:   `/${AppRoutes.verticals.picker}`,
    platform: `/${AppRoutes.verticals.platform}`,
  },
} as const;
