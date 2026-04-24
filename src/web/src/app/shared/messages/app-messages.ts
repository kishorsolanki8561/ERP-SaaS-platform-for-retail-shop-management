export const AppMessages = {
  auth: {
    loginFailed: 'Login failed. Please check your credentials.',
    sessionExpired: 'Your session has expired. Please sign in again.',
    unauthorized: 'You do not have permission to perform this action.',
    featureUnavailable: 'This feature is not available on your current plan.',
  },
  common: {
    saveSuccess: 'Changes saved successfully.',
    deleteSuccess: 'Record deleted.',
    error: 'An unexpected error occurred. Please try again.',
  },
  toast: {
    errorSummary: 'Error',
    successSummary: 'Success',
    warningSummary: 'Warning',
  },
} as const;
