export const AppMessages = {
  auth: {
    loginFailed:        'Login failed. Please check your credentials.',
    sessionExpired:     'Your session has expired. Please sign in again.',
    unauthorized:       'You do not have permission to perform this action.',
    featureUnavailable: 'This feature is not available on your current plan.',
  },
  common: {
    saveSuccess:   'Changes saved successfully.',
    deleteSuccess: 'Record deleted.',
    error:         'An unexpected error occurred. Please try again.',
  },
  toast: {
    errorSummary:   'Error',
    successSummary: 'Success',
    warningSummary: 'Warning',
  },
  validation: {
    required:       'This field is required.',
    emailInvalid:   'Please enter a valid email address.',
    passwordTooShort: 'Password must be at least 6 characters.',
  },
} as const;

export const AppLabels = {
  appName: 'ShopEarth ERP',
  layout: {
    logout: 'Logout',
  },
  auth: {
    loginTitle:            'Sign in to your account',
    forgotPasswordTitle:   'Reset Password',
    forgotPasswordSubtext: 'Enter your email to receive a reset link.',
    identifierLabel:       'Email / Phone / Username',
    identifierPlaceholder: 'Enter identifier',
    emailLabel:            'Email',
    emailPlaceholder:      'you@example.com',
    passwordLabel:         'Password',
    signInButton:          'Sign In',
    sendResetButton:       'Send Reset Link',
    backToLogin:           'Back to Login',
  },
  admin: {
    usersTitle:    'Users',
    usersSubtitle: 'Manage staff accounts for this shop.',
    inviteUser:    'Invite User',
    editUser:      'Edit',
    deactivate:    'Deactivate',
    shopProfileTitle: 'Shop Profile',
    saveChanges:   'Save Changes',
    shopCode:      'Shop Code',
    legalName:     'Legal Name',
    tradeName:     'Trade Name',
    gstNumber:     'GST Number',
    addressLine1:  'Address Line 1',
    addressLine2:  'Address Line 2',
    city:          'City',
    state:         'State',
    pinCode:       'PIN Code',
    currency:      'Currency',
  },
  shared: {
    search:     'Search...',
    selectPlaceholder: 'Select...',
  },
} as const;
