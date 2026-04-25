export const AppConstants = {
  pagination: {
    defaultPageSize: 20,
    pageSizeOptions: [10, 25, 50, 100],
  },
  toast: {
    defaultLife: 5000,
  },
  password: {
    minLength: 6,
  },
  table: {
    actionsColumnWidth: '120px',
    initialSortOrder: 1,
  },
  debounce: {
    searchMs: 300,
  },
  ddlKeys: {
    invoiceStatus:       'INVOICE_STATUS',
    customerType:        'CUSTOMER_TYPE',
    productCategory:     'PRODUCT_CATEGORY',
    paymentMode:         'PAYMENT_MODE',
    indianState:         'INDIAN_STATE',
    currency:            'CURRENCY',
    walletReferenceType: 'WALLET_REFERENCE_TYPE',
  },
} as const;
