# SalesReturns Module

**Plan reference:** `docs/MASTER_PLAN.md` §6.6 — Sales Returns & Credit Notes

---

## Overview

Handles the full customer-return workflow: create a sales return against an invoice, approve it, issue a credit note, and apply or cancel the credit note. Integrates with Accounting via `IAutoVoucherService` (`PostSalesReturnVoucherAsync` fires on return approval). Refund can be issued as a credit note or wallet top-up (wallet integration wired in Phase 3).

---

## Database

| Property | Value |
|---|---|
| **DB tier** | TenantDB |
| **Schema** | `sales` |
| **Context** | `TenantDbContext` (shared, registered via `IEntityModelConfigurator`) |

---

## Entities

| Entity | Description |
|---|---|
| `SalesReturn` | Return header linked to an original Invoice; transitions Draft → Approved / Cancelled |
| `SalesReturnLine` | Return line with unit snapshots and quantities |
| `CreditNote` | Credit note issued to a customer; tracks used/remaining balance |

---

## Enums (stored as string per §3.9)

| Enum | Values |
|---|---|
| `SalesReturnStatus` | `Draft`, `Approved`, `Cancelled` |
| `CreditNoteStatus` | `Draft`, `Issued`, `Applied`, `Cancelled` |
| `RefundMethod` | `CreditNote`, `WalletCredit`, `BankTransfer`, `Cash` |

---

## Endpoints

| Method | URL | Permission | Feature |
|---|---|---|---|
| POST | `/api/sales-returns` | `SalesReturns.Create` | — |
| POST | `/api/sales-returns/{id}/approve` | `SalesReturns.Approve` | — |
| POST | `/api/sales-returns/{id}/cancel` | `SalesReturns.Approve` | — |
| POST | `/api/sales-returns/credit-notes` | `SalesReturns.Approve` | — |
| POST | `/api/sales-returns/credit-notes/{id}/apply` | `SalesReturns.Approve` | — |
| POST | `/api/sales-returns/credit-notes/{id}/cancel` | `SalesReturns.Approve` | — |
| GET | `/api/sales-returns` | `SalesReturns.View` | — |
| GET | `/api/sales-returns/credit-notes` | `SalesReturns.View` | — |

---

## Permissions seeded

| Code | Label |
|---|---|
| `SalesReturns.View` | View sales returns and credit notes |
| `SalesReturns.Create` | Create sales returns |
| `SalesReturns.Approve` | Approve returns and issue credit notes |

---

## Sequences

| Code | Prefix | Example |
|---|---|---|
| `SALES_RETURN` | `SR-` | `SR-000001` |
| `CREDIT_NOTE` | `CN-` | `CN-000001` |

---

## Menu items seeded

Group: **Returns** (`pi pi-replay`, sort 35)

| Code | Label | Route | Required Permission |
|---|---|---|---|
| `returns.sales-returns` | Sales Returns | `/returns/sales-returns` | `SalesReturns.View` |
| `returns.credit-notes` | Credit Notes | `/returns/credit-notes` | `SalesReturns.View` |

---

## Cross-module integrations

| Direction | Module | Contract | Trigger |
|---|---|---|---|
| → Accounting | `IAutoVoucherService` | `PostSalesReturnVoucherAsync` | Sales return approved |
| → Wallet | (Phase 3) | Wallet top-up | Return with `RefundMethod.WalletCredit` approved |
| → Inventory | (Phase 3) | Stock movement | Return approved — goods back in stock |

---

## Tests

| Class | Type | Location |
|---|---|---|
| `SalesReturnsServiceTests` | Unit | `ErpSaas.Tests.Unit/Modules/SalesReturns/` |
| `SalesReturnsControllerTests` | Integration | `ErpSaas.Tests.Integration/Modules/SalesReturns/` |
| `SalesReturnsTenantIsolationTests` | Integration | `ErpSaas.Tests.Integration/Modules/SalesReturns/` |
| `SalesReturnsSubscriptionGateTests` | Integration | `ErpSaas.Tests.Integration/Modules/SalesReturns/` |
| `SalesReturnsAuditTrailTests` | Integration | `ErpSaas.Tests.Integration/Modules/SalesReturns/` |
| `SalesReturnsArchTests` | Architecture | `ErpSaas.Tests.Arch/Modules/` |
