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
} as const;
