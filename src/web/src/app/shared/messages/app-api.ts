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
    status:          '/api/bootstrap/status',
    registerOwner:   '/api/bootstrap/register-product-owner',
  },
  admin: {
    users:       '/api/admin/users',
    shopProfile: '/api/admin/shop-profile',
  },
  services: '/api/services',
} as const;
