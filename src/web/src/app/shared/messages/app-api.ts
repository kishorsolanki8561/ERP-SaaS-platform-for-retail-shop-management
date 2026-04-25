export const ApiEndpoints = {
  auth: {
    login:          '/api/auth/login',
    refresh:        '/api/auth/refresh',
    logout:         '/api/auth/logout',
    forgotPassword: '/api/auth/forgot-password',
    resetPassword:  '/api/auth/reset-password',
  },
  menu: {
    tree: '/api/menu/tree',
  },
  ddl: {
    single: (key: string) => `/api/ddl/${key}`,
    batch:  '/api/ddl/batch',
  },
  bootstrap: {
    status:        '/api/bootstrap/status',
    registerOwner: '/api/bootstrap/register-product-owner',
  },
  admin: {
    users:       '/api/admin/users',
    shopProfile: '/api/admin/shop-profile',
  },
  dashboard: {
    summary: '/api/dashboard/summary',
  },
  crm: {
    customers:       '/api/crm/customers',
    customer:        (id: number | string) => `/api/crm/customers/${id}`,
    groups:          '/api/crm/groups',
  },
  inventory: {
    products:        '/api/inventory/products',
    product:         (id: number | string) => `/api/inventory/products/${id}`,
    warehouses:      '/api/inventory/warehouses',
    stockAdjust:     '/api/inventory/stock/adjust',
  },
  billing: {
    invoices:        '/api/billing/invoices',
    invoice:         (id: number | string) => `/api/billing/invoices/${id}`,
    invoiceLines:    (id: number | string) => `/api/billing/invoices/${id}/lines`,
    finalize:        (id: number | string) => `/api/billing/invoices/${id}/finalize`,
    cancel:          (id: number | string) => `/api/billing/invoices/${id}/cancel`,
  },
  wallet: {
    balance:         (customerId: number | string) => `/api/wallet/balance/${customerId}`,
    transactions:    (customerId: number | string) => `/api/wallet/transactions/${customerId}`,
    credit:          '/api/wallet/credit',
    debit:           '/api/wallet/debit',
  },
  services: '/api/services',
} as const;
