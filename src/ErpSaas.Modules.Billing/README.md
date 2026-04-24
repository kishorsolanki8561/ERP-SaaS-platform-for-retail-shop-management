# ErpSaas.Modules.Billing

Handles sales invoicing for retail and wholesale shops.

## Domain model

| Entity | Table | Schema |
|---|---|---|
| `Invoice` | `Invoice` | `sales` |
| `InvoiceLine` | `InvoiceLine` | `sales` |

## Document numbering

All invoice numbers come from `ISequenceService.NextAsync("INVOICE_RETAIL", shopId, ct)`.
The sequence definition is seeded by `BillingSystemSeeder` (Order = 42).

## Permissions

| Code | Purpose |
|---|---|
| `Billing.View` | List and read invoices |
| `Billing.Create` | Create draft invoices and add lines |
| `Billing.Edit` | Finalize invoices |
| `Billing.Cancel` | Cancel invoices |

## Invoice lifecycle

```
Draft → Finalized → (no further transitions)
Draft → Cancelled
Finalized → Cancelled
```

## Unit snapshot fields (CLAUDE.md §3.7)

`InvoiceLine` carries `ProductUnitId`, `UnitCodeSnapshot`, `ConversionFactorSnapshot`,
`QuantityInBilledUnit`, and `QuantityInBaseUnit`.  All stock math downstream uses
`QuantityInBaseUnit`; the UI renders the billed-unit quantity.

## Wiring

1. Call `services.AddBillingModule()` in `Program.cs` after `AddInfrastructure()`.
2. Call `BillingModelConfiguration.Configure(modelBuilder)` inside
   `TenantDbContext.OnModelCreating` so EF discovers `Invoice` and `InvoiceLine`.
3. Add a project reference to `ErpSaas.Modules.Billing` from `ErpSaas.Api`.

## TODOs (deferred to subsequent phases)

- Load `CustomerNameSnapshot` / `CustomerGstSnapshot` from CRM module when
  `ErpSaas.Modules.Crm` is wired to `TenantDbContext`.
- Load `ProductNameSnapshot`, `ProductCodeSnapshot`, `UnitCodeSnapshot`,
  `ConversionFactorSnapshot`, and `GstRate` from Inventory module.
- Replace placeholder `GstRate = 18m` with per-product HSN-based rate.
