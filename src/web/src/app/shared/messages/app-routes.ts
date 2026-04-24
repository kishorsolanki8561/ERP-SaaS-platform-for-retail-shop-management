export const AppRoutes = {
  dashboard:        'dashboard',
  login:            'login',
  forgotPassword:   'forgot-password',
  unauthorized:     'unauthorized',
  featureUnavailable: 'feature-unavailable',
  admin: {
    users:        'admin/users',
    shopProfile:  'admin/shop-profile',
  },
} as const;

export const AppRoutePaths = {
  dashboard:        `/${AppRoutes.dashboard}`,
  login:            `/${AppRoutes.login}`,
  forgotPassword:   `/${AppRoutes.forgotPassword}`,
  unauthorized:     `/${AppRoutes.unauthorized}`,
  featureUnavailable: `/${AppRoutes.featureUnavailable}`,
  admin: {
    users:        `/${AppRoutes.admin.users}`,
    shopProfile:  `/${AppRoutes.admin.shopProfile}`,
  },
} as const;
