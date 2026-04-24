export const Permissions = {
  users: {
    view:     'Users.View',
    manage:   'Users.Manage',
    invite:   'Users.Invite',
    deactivate: 'Users.Deactivate',
  },
  shopProfile: {
    view:   'ShopProfile.View',
    edit:   'ShopProfile.Edit',
  },
  dashboard: {
    view:   'Dashboard.View',
  },
  invoice: {
    view:     'Invoice.View',
    create:   'Invoice.Create',
    edit:     'Invoice.Edit',
    cancel:   'Invoice.Cancel',
    finalize: 'Invoice.Finalize',
  },
  product: {
    view:   'Product.View',
    manage: 'Product.Manage',
  },
  customer: {
    view:   'Customer.View',
    manage: 'Customer.Manage',
  },
} as const;
