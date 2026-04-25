export const AppRoutes = {
  dashboard:          'dashboard',
  login:              'login',
  forgotPassword:     'forgot-password',
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
} as const;

export const AppRoutePaths = {
  dashboard:          `/${AppRoutes.dashboard}`,
  login:              `/${AppRoutes.login}`,
  forgotPassword:     `/${AppRoutes.forgotPassword}`,
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
} as const;
