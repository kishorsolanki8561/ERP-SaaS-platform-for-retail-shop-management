export const Permissions = {
  users: {
    view:       'Users.View',
    manage:     'Users.Manage',
    invite:     'Users.Invite',
    deactivate: 'Users.Deactivate',
  },
  shopProfile: {
    view:   'ShopProfile.View',
    edit:   'ShopProfile.Edit',
  },
  dashboard: {
    view:   'Dashboard.View',
  },
  // Billing — must match [RequirePermission] on BillingController
  billing: {
    view:   'Billing.View',
    create: 'Billing.Create',
    edit:   'Billing.Edit',
    cancel: 'Billing.Cancel',
  },
  // Inventory — must match [RequirePermission] on InventoryController
  inventory: {
    view:   'Inventory.View',
    manage: 'Inventory.Manage',
  },
  // CRM — must match [RequirePermission] on CrmController
  crm: {
    view:   'Crm.View',
    create: 'Crm.Create',
    edit:   'Crm.Edit',
    manage: 'Crm.Manage',
  },
  // Wallet
  wallet: {
    view:   'Wallet.View',
    credit: 'Wallet.Credit',
    debit:  'Wallet.Debit',
  },
  // POS / Shift — must match [RequirePermission] on ShiftController
  shift: {
    view:        'Shift.View',
    open:        'Shift.Open',
    close:       'Shift.Close',
    forceClose:  'Shift.ForceClose',
    cashMovement:'Shift.CashMovement',
  },
  // Hardware — must match [RequirePermission] on HardwareController
  hardware: {
    cashDrawer: 'Hardware.CashDrawer',
  },
  // Quotations / Sales workflow
  quotations: {
    view:    'Quotation.View',
    create:  'Quotation.Create',
    send:    'Quotation.Send',
    revise:  'Quotation.Revise',
    accept:  'Quotation.Accept',
    convert: 'Quotation.Convert',
    delete:  'Quotation.Delete',
  },
  // Purchasing
  purchasing: {
    view:                   'Purchasing.View',
    manageSuppliers:        'Purchasing.ManageSuppliers',
    createPurchaseOrder:    'Purchasing.CreatePurchaseOrder',
    receiveGoods:           'Purchasing.ReceiveGoods',
    manageBills:            'Purchasing.ManageBills',
    managePurchaseReturns:  'Purchasing.ManagePurchaseReturns',
  },
  // Sales Returns
  salesReturns: {
    view:    'SalesReturns.View',
    create:  'SalesReturns.Create',
    approve: 'SalesReturns.Approve',
  },
  // Warranty
  warranty: {
    view:         'Warranty.View',
    manage:       'Warranty.Manage',
    manageClaims: 'Warranty.ManageClaims',
  },
  // Pricing
  pricing: {
    view:   'Pricing.View',
    manage: 'Pricing.Manage',
  },
  // Transport
  transport: {
    view:   'Transport.View',
    manage: 'Transport.Manage',
  },
  // HR
  hr: {
    view:       'HR.View',
    manage:     'HR.Manage',
    attendance: 'HR.Attendance',
    payroll:    'HR.Payroll',
  },
  // Marketplace
  marketplace: {
    view:         'Marketplace.View',
    manage:       'Marketplace.Manage',
    sync:         'Marketplace.Sync',
    convertOrder: 'Marketplace.ConvertOrder',
  },
  // Reports
  reports: {
    viewAccounting: 'Reports.ViewAccounting',
    viewGst:        'Reports.ViewGst',
    export:         'Reports.Export',
  },
  // Subscription
  subscription: {
    view:   'Subscription.View',
    manage: 'Subscription.Manage',
  },
  // Audit logs
  auditLog: {
    view: 'Admin.AuditLog.View',
  },
  // Usage metering
  usage: {
    view:        'Usage.View',
    viewHistory: 'Usage.ViewHistory',
  },
  // Lead management (platform admin)
  lead: {
    view:    'Lead.View',
    edit:    'Lead.Edit',
    assign:  'Lead.Assign',
    convert: 'Lead.Convert',
  },
  // Marketing CMS (platform admin)
  marketing: {
    edit:    'Marketing.Edit',
    blogEdit:'Blog.Edit',
    publish: 'Blog.Publish',
  },
  // Offline sync
  sync: {
    registerDevice: 'Device.Register',
    manageDevices:  'Device.Manage',
    viewQueue:      'Sync.View',
    resolveException: 'Sync.ResolveException',
  },
  // On-prem replication
  onPrem: {
    view:   'OnPrem.View',
    manage: 'OnPrem.Manage',
  },
  // Platform admin (platform owner only)
  platform: {
    shopsView:   'Platform.Shops.View',
    shopsManage: 'Platform.Shops.Manage',
  },
  // Integration — API Keys + Webhooks
  integration: {
    manageApiKeys:    'Integration.ManageApiKeys',
    manageWebhooks:   'Integration.ManageWebhooks',
    viewDeliveries:   'Integration.ViewDeliveries',
  },
  // Payment gateway
  payment: {
    view:       'Payment.View',
    configure:  'Payment.Configure',
    initiate:   'Payment.Initiate',
    manage:     'Payment.Manage',
    refund:     'Payment.Refund',
    reconcile:  'Payment.Reconcile',
  },
  // Reports — payment
  reportsPayment: {
    view:   'Reports.ViewPayment',
  },
  // Accounting
  accounting: {
    view:           'Accounting.View',
    manage:         'Accounting.ManageAccounts',
    createVoucher:  'Accounting.CreateVoucher',
    postVoucher:    'Accounting.PostVoucher',
    manageExpenses: 'Accounting.ManageExpenses',
    manageCheques:  'Accounting.ManageCheques',
  },
} as const;
