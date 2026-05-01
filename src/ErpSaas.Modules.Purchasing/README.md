# Purchasing Module

**Plan reference:** `docs/MASTER_PLAN.md` §6.5 — Purchasing & Payables

---

## Overview

Full purchase-side workflow for retail/wholesale shops: supplier master, purchase orders, goods receipt, vendor bills and payments, purchase returns, and debit notes. Integrates with Accounting via `IAutoVoucherService` (`PostPurchaseBillVoucherAsync` fires on bill approval) and with Inventory (goods receipt triggers stock movement — Phase 3 wiring).

---

## Database

| Property | Value |
|---|---|
| **DB tier** | TenantDB |
| **Schema** | `purchasing` |
| **Context** | `TenantDbContext` (shared, registered via `IEntityModelConfigurator`) |

---

## Entities

| Entity | Description |
|---|---|
| `Supplier` | Vendor master — name, GST, PAN, contact, opening balance |
| `PurchaseOrder` | PO header; transitions Draft → Sent → Received / PartiallyReceived / Cancelled |
| `PurchaseOrderLine` | PO line with unit snapshots and quantity tracking |
| `Bill` | Vendor bill; transitions Draft → Approved → PartiallyPaid → Paid / Cancelled |
| `BillPayment` | Individual payment against a Bill |
| `PurchaseReturn` | Return header; transitions Draft → Approved / Cancelled |
| `PurchaseReturnLine` | Return line with unit snapshots |
| `DebitNote` | Debit note issued on an approved PurchaseReturn |

---

## Enums (stored as string per §3.9)

| Enum | Values |
|---|---|
| `PurchaseOrderStatus` | `Draft`, `Sent`, `PartiallyReceived`, `Received`, `Cancelled` |
| `BillStatus` | `Draft`, `Approved`, `PartiallyPaid`, `Paid`, `Cancelled` |
| `PurchaseReturnStatus` | `Draft`, `Approved`, `Cancelled` |
| `DebitNoteStatus` | `Issued`, `PartiallyUsed`, `Used`, `Expired` |

---

## Endpoints

| Method | URL | Permission | Feature |
|---|---|---|---|
| GET | `/api/purchasing/suppliers` | `Purchasing.View` | — |
| POST | `/api/purchasing/suppliers` | `Purchasing.ManageSuppliers` | — |
| PUT | `/api/purchasing/suppliers/{id}` | `Purchasing.ManageSuppliers` | — |
| DELETE | `/api/purchasing/suppliers/{id}` | `Purchasing.ManageSuppliers` | — |
| POST | `/api/purchasing/purchase-orders` | `Purchasing.CreatePurchaseOrder` | — |
| POST | `/api/purchasing/purchase-orders/{id}/send` | `Purchasing.CreatePurchaseOrder` | — |
| POST | `/api/purchasing/purchase-orders/{id}/receive` | `Purchasing.ReceiveGoods` | — |
| POST | `/api/purchasing/purchase-orders/{id}/cancel` | `Purchasing.CreatePurchaseOrder` | — |
| POST | `/api/purchasing/bills` | `Purchasing.ManageBills` | — |
| POST | `/api/purchasing/bills/{id}/approve` | `Purchasing.ManageBills` | — |
| POST | `/api/purchasing/bills/{id}/pay` | `Purchasing.ManageBills` | — |
| POST | `/api/purchasing/bills/{id}/cancel` | `Purchasing.ManageBills` | — |
| POST | `/api/purchasing/purchase-returns` | `Purchasing.ManagePurchaseReturns` | — |
| POST | `/api/purchasing/purchase-returns/{id}/approve` | `Purchasing.ManagePurchaseReturns` | — |
| POST | `/api/purchasing/purchase-returns/{id}/cancel` | `Purchasing.ManagePurchaseReturns` | — |
| POST | `/api/purchasing/purchase-returns/{id}/debit-note` | `Purchasing.ManagePurchaseReturns` | — |

---

## Permissions seeded

| Code | Label |
|---|---|
| `Purchasing.View` | View suppliers, purchase orders and bills |
| `Purchasing.ManageSuppliers` | Create and edit suppliers |
| `Purchasing.CreatePurchaseOrder` | Create and send purchase orders |
| `Purchasing.ReceiveGoods` | Receive goods against a purchase order |
| `Purchasing.ManageBills` | Create, approve and pay vendor bills |
| `Purchasing.ManagePurchaseReturns` | Create and manage purchase returns and debit notes |

---

## Sequences

| Code | Prefix | Example |
|---|---|---|
| `PURCHASE_ORDER` | `PO-` | `PO-000001` |
| `BILL` | `BILL-` | `BILL-000001` |
| `PURCHASE_RETURN` | `PR-` | `PR-000001` |
| `DEBIT_NOTE` | `DN-` | `DN-000001` |

---

## Menu items seeded

Group: **Purchasing** (`pi pi-shopping-cart`, sort 40)

| Code | Label | Route | Required Permission |
|---|---|---|---|
| `purchasing.suppliers` | Suppliers | `/purchasing/suppliers` | `Purchasing.View` |
| `purchasing.orders` | Purchase Orders | `/purchasing/orders` | `Purchasing.CreatePurchaseOrder` |
| `purchasing.receive` | Receive Goods | `/purchasing/receive` | `Purchasing.ReceiveGoods` |
| `purchasing.bills` | Bills | `/purchasing/bills` | `Purchasing.ManageBills` |
| `purchasing.returns` | Purchase Returns | `/purchasing/returns` | `Purchasing.ManagePurchaseReturns` |

---

## Cross-module integrations

| Direction | Module | Contract | Trigger |
|---|---|---|---|
| → Accounting | `IAutoVoucherService` | `PostPurchaseBillVoucherAsync` | Bill approved |
| → Inventory | (Phase 3) | Stock movement on goods receipt | PO received |

---

## Tests

| Class | Type | Location |
|---|---|---|
| `PurchasingServiceTests` | Unit | `ErpSaas.Tests.Unit/Modules/Purchasing/` |
| `PurchasingControllerTests` | Integration | `ErpSaas.Tests.Integration/Modules/Purchasing/` |
| `PurchasingTenantIsolationTests` | Integration | `ErpSaas.Tests.Integration/Modules/Purchasing/` |
| `PurchasingSubscriptionGateTests` | Integration | `ErpSaas.Tests.Integration/Modules/Purchasing/` |
| `PurchasingAuditTrailTests` | Integration | `ErpSaas.Tests.Integration/Modules/Purchasing/` |
| `PurchasingArchTests` | Architecture | `ErpSaas.Tests.Arch/Modules/` |
