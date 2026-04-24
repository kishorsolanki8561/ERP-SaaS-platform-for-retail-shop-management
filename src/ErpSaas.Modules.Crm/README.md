# ErpSaas.Modules.Crm

Customer Relationship Management module — Phase 1, Week 1 delivery.

## Schema

All entities live in the `crm` schema inside `TenantDB`.

## Entities

| Entity | Table | Notes |
|---|---|---|
| `CustomerGroup` | `crm.CustomerGroup` | Shared discount groups per shop |
| `Customer` | `crm.Customer` | Core CRM record; code auto-generated (CUST00001) |
| `CustomerAddress` | `crm.CustomerAddress` | Multiple billing/shipping addresses per customer |

## Registering in TenantDbContext

After calling `AddCrmModule()` in DI, wire the EF configs into `TenantDbContext.OnModelCreating`:

```csharp
using ErpSaas.Modules.Crm.Extensions;
// inside OnModelCreating:
modelBuilder.ApplyCrmEntityConfigurations();
```

## Endpoints

| Method | Route | Permission |
|---|---|---|
| GET | `/api/crm/customers` | `Crm.View` |
| GET | `/api/crm/customers/{id}` | `Crm.View` |
| POST | `/api/crm/customers` | `Crm.Create` |
| PUT | `/api/crm/customers/{id}` | `Crm.Edit` |
| DELETE | `/api/crm/customers/{id}` | `Crm.Edit` |
| GET | `/api/crm/groups` | `Crm.View` |
| POST | `/api/crm/groups` | `Crm.Manage` |

## Permissions

| Code | Description |
|---|---|
| `Crm.View` | Read customers and groups |
| `Crm.Create` | Create new customers |
| `Crm.Edit` | Update / deactivate customers |
| `Crm.Manage` | Manage customer groups and CRM settings |

## DDL Catalog Keys

| Key | Items |
|---|---|
| `CUSTOMER_TYPE` | `Retail`, `Wholesale` |

## Seeder

`CrmSystemSeeder` (Order=40) seeds permissions, DDL catalogs, and menu items into PlatformDB.
It is idempotent — re-running on an already-seeded database is safe.
