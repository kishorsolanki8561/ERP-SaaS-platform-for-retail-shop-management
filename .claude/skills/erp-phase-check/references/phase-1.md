# Phase 1 — Core Retail Loop — Exit-Gate Extras

> **Goal:** A cashier can walk in, open a shift, scan products, create an invoice, accept multi-mode payment, print a receipt, and close their shift — with every operation gated by permissions, subscription features, and tenant isolation.
> **Duration:** 4–6 weeks
> **Plan references:** §6.2 (Inventory), §6.3 (Billing), §6.4 (Wallet), §6.6 (Shifts/POS), §6.7 (Notifications), §7.1 (Dashboard), §5.16 (Sequences), §6.14 (Masters — tenant tier).

---

## Modules that must be complete at end of Phase 1

Every module listed here must pass its own **six test class** requirement (§6 of CLAUDE.md):
`ServiceTests` + `ControllerTests` + `TenantIsolationTests` + `SubscriptionGateTests` + `AuditTrailTests` + `ArchTests`.

### 1. Identity — Branch Management
- [ ] `Branch` entity (`identity` schema, PlatformDB) with `ShopId`, `Name`, address fields, `IsHeadOffice`, `IsActive`
- [ ] `GET /api/admin/branches` — list active branches (permission: `ShopProfile.View`)
- [ ] `POST /api/admin/branches` — create branch (permission: `ShopProfile.Edit`)
- [ ] `PUT /api/admin/branches/{id}` — update branch
- [ ] `DELETE /api/admin/branches/{id}` — deactivate branch
- [ ] Branch seed: at least one Head Office branch created during shop onboarding
- [ ] Angular: `BranchStore` persists selection in `localStorage`; `<app-branch-selector>` in topbar
- [ ] `X-Branch-Id` header forwarded on every API request via `tenantInterceptor`

### 2. Identity — Role Management
- [ ] `Role` + `RolePermission` entities (PlatformDB)
- [ ] `GET /api/admin/roles` — list roles
- [ ] `POST /api/admin/roles` — create custom role
- [ ] `PUT /api/admin/roles/{id}/permissions` — assign permissions to role
- [ ] `DELETE /api/admin/roles/{id}` — deactivate role
- [ ] `POST /api/admin/users/{id}/roles/{roleId}` — assign role to user
- [ ] `DELETE /api/admin/users/{id}/roles/{roleId}` — remove role from user
- [ ] Angular: `admin/roles` page with permission assignment matrix

### 3. Inventory
- [ ] `Product` entity (`inventory` schema): `Name`, `SKU`, `BarcodeEan`, `HsnCode`, `GstRate`, `SalePrice`, `PurchasePrice`, `Mrp`, `BaseUnitId`, `MinStockLevel`, `IsActive`
- [ ] `ProductUnit` + `UnitConversion` entities
- [ ] `Warehouse` entity with `StorageBin` hierarchy (Zone/Rack/Shelf/Bin) — §3.10
- [ ] `StockLevel` entity per product/warehouse/bin
- [ ] `StockMovement` entity with `MovementType` enum (Purchase, Sale, Adjustment, Transfer, Return)
- [ ] All quantity-bearing rows carry `ProductUnitId`, `UnitCodeSnapshot`, `ConversionFactorSnapshot`, `QuantityInBaseUnit` — §3.7
- [ ] `GET /api/inventory/products` (paged + barcode search)
- [ ] `POST /api/inventory/products`
- [ ] `PUT /api/inventory/products/{id}`
- [ ] `GET /api/inventory/warehouses`
- [ ] `POST /api/inventory/stock/adjust` — manual stock adjustment
- [ ] Angular: products page, stock adjustment dialog
- [ ] Arch test: `EveryStockBearingEntity_HasUnitFields`

### 4. CRM — Customers
- [ ] `Customer` entity (`crm` schema): `DisplayName`, `Type` (Retail/Wholesale enum), `Phone`, `Email`, `GstNumber`, `CreditLimit`, `GroupId`, `IsActive`
- [ ] `CustomerGroup` entity
- [ ] Full CRUD: `GET/POST /api/crm/customers`, `PUT/DELETE /api/crm/customers/{id}`
- [ ] `GET /api/crm/groups`
- [ ] Angular: customers page with group filter

### 5. Billing
- [ ] `Invoice` entity (`sales` schema) with state machine: `Draft → Finalized → Paid | Cancelled`
- [ ] `InvoiceLine` entity with full unit fields (§3.7)
- [ ] `InvoicePayment` entity: split-tender allocations per invoice
- [ ] `PaymentTerms` + `DueDate` + `PaidAmount` + `OutstandingAmount` on Invoice
- [ ] Sequences: invoice numbers via `ISequenceService` (`INVOICE_RETAIL`)
- [ ] `POST /api/billing/invoices` — create draft
- [ ] `POST /api/billing/invoices/{id}/lines` — add line
- [ ] `POST /api/billing/invoices/{id}/finalize` — transition to Finalized; SMS hook
- [ ] `POST /api/billing/invoices/{id}/pay` — split-tender payment; wallet debit via `IWalletDebit`
- [ ] `POST /api/billing/invoices/{id}/payment-terms` — set due date
- [ ] `POST /api/billing/invoices/{id}/cancel` — cancel with reason
- [ ] `GET /api/billing/invoices/{id}/pdf?format=A4|Thermal80mm` — PDF via QuestPDF
- [ ] `IInvoicePdfGenerator` (note: NOT `IInvoicePdfService` — suffix "Generator" to avoid arch-test false positive)
- [ ] Angular: invoices list page, invoice detail page, pay dialog

### 6. Wallet
- [ ] `WalletBalance` + `WalletTransaction` entities (`wallet` schema)
- [ ] `WalletTransactionType` enum: Credit, Debit
- [ ] `IWalletDebit` cross-module interface in `ErpSaas.Shared.Services`
- [ ] `GET /api/wallet/balances` (paged)
- [ ] `GET /api/wallet/balance/{customerId}`
- [ ] `GET /api/wallet/transactions/{customerId}` (paged)
- [ ] `POST /api/wallet/credit`
- [ ] `POST /api/wallet/debit`
- [ ] Wallet credit SMS notification via `INotificationService`
- [ ] Angular: wallet balances page, wallet transactions page, credit/debit dialogs

### 7. Shift (POS)
- [ ] `Shift` entity (`shift` schema) with state machine: `Open → Closed | ForceClosed`
- [ ] `ShiftCashMovement` entity (CashIn / CashOut)
- [ ] `ShiftDenominationCount` entity
- [ ] Shift close SMS notification via `INotificationService`
- [ ] `IShiftLookup` cross-module interface: `GetActiveShiftIdAsync(shopId)`
- [ ] `GET /api/shifts`, `GET /api/shifts/current`
- [ ] `POST /api/shifts/open`, `POST /api/shifts/{id}/close`, `POST /api/shifts/{id}/force-close`
- [ ] `POST /api/shifts/{id}/cash-in`, `POST /api/shifts/{id}/cash-out`
- [ ] Angular: shifts list, open-shift form, POS terminal (`/pos/terminal`) with barcode listener

### 8. Hardware Integration Layer
- [ ] Angular `BarcodeListenerService`: keyboard-wedge scanner (50ms threshold), `scanned` signal
- [ ] Angular `ThermalPrintService`: fetches PDF blob from API, prints via hidden iframe
- [ ] Angular `CashDrawerService`: calls `POST /api/hardware/cash-drawer/pop`, `popping` signal
- [ ] `POST /api/hardware/cash-drawer/pop` (permission: `Hardware.CashDrawer`)
- [ ] `PosTerminalComponent` at `/pos/terminal` wiring all three services

### 9. Notifications
- [ ] `NotificationTemplate` rows seeded for: `INVOICE_FINALIZED`, `WALLET_CREDITED`, `SHIFT_CLOSED`
- [ ] Hangfire background job draining `NotificationQueue`
- [ ] SMS provider wired (Twilio or MSG91 or mock for testing)
- [ ] `INotificationService.EnqueueAsync` tested with integration test

### 10. Dashboard
- [ ] `GET /api/dashboard/summary` returns: `TodaySalesAmount`, `TodayInvoiceCount`, `TodaySalesTrend`, `TodaySalesTrendUp`, `ActiveProductCount`, `CustomerCount`
- [ ] Invoice filter includes `Finalized` and `Paid` statuses (not just non-Cancelled)
- [ ] Angular: dashboard quick-action buttons wired (New Invoice, New Customer, Open Shift, POS Terminal)
- [ ] Angular: summary cards auto-refresh every 60 seconds

---

## Phase 1 — specific extra checks

These are in addition to the 12 universal checks in `SKILL.md`.

### P1-1. End-to-end retail transaction demo
Run this in a local environment (or staging):

1. Log in as shop admin.
2. Open a shift: `POST /api/shifts/open` with opening cash ₹500.
3. Create an invoice: `POST /api/billing/invoices` for a customer with 2 lines.
4. Add line via barcode: scan EAN on POS terminal → product auto-populated.
5. Finalize invoice: `POST /api/billing/invoices/{id}/finalize` → SMS sent.
6. Pay via split tender (50% cash + 50% wallet): `POST /api/billing/invoices/{id}/pay`.
7. Print receipt: `GET /api/billing/invoices/{id}/pdf?format=Thermal80mm` → prints.
8. Pop cash drawer: `POST /api/hardware/cash-drawer/pop` → 204.
9. Close shift: `POST /api/shifts/{id}/close` → cashier SMS sent.
10. Dashboard summary card reflects today's sale.

All 10 steps must succeed with no 4xx/5xx responses.

### P1-2. Tenant isolation verified end-to-end
```bash
dotnet test --filter "Category=TenantIsolation"
```
Every `TenantIsolationTests` class passes. Specifically:
- Invoice of Shop A not visible from Shop B's JWT
- Wallet balance of Shop A customer not readable by Shop B
- Shift of Shop A not closeable by Shop B

### P1-3. Subscription gate verified
```bash
dotnet test --filter "Category=SubscriptionGate"
```
Features `Billing.EInvoice` and `Wallet.AdvancedReports` return 402 on Starter plan, 200 on Growth plan.

### P1-4. No SaveChangesAsync outside ExecuteAsync
```bash
dotnet test --filter "No_SaveChangesAsync_Outside_BaseService"
```
Arch rule passes for all new Phase 1 entities and services.

### P1-5. All status/state fields are C# enums stored as string
Arch test `EveryStateField_IsEnumWithStringConversion` passes.
Specifically: `InvoiceStatus`, `WalletTransactionType`, `ShiftStatus`, `MovementType`, `PaymentMode` — all stored as `nvarchar(20)` in DB.

### P1-6. Sequences are in use
```bash
dotnet test --filter "SequenceService_ConcurrencyTest"
```
100 parallel calls to `ISequenceService.NextAsync("INVOICE_RETAIL", ...)` produce 100 unique, consecutive numbers with no gaps.

### P1-7. PDF generation both formats
```bash
dotnet test --filter "BillingServiceTests.GeneratePdf"
```
Both A4 and Thermal80mm generate non-empty PDF bytes without throwing.

### P1-8. Seeder idempotency for new templates
```bash
dotnet run --project src/ErpSaas.Api -- --seed-and-exit
dotnet run --project src/ErpSaas.Api -- --seed-and-exit
```
Second run inserts 0 rows. Notification templates `INVOICE_FINALIZED`, `WALLET_CREDITED`, `SHIFT_CLOSED` present after first run.

### P1-9. Angular TypeScript compiles clean
```bash
cd src/web && npx tsc --noEmit
```
Exit code 0, zero errors.

### P1-10. Module READMEs present
Every new module has `src/ErpSaas.Modules.{X}/README.md`:
- `ErpSaas.Modules.Billing/README.md`
- `ErpSaas.Modules.Inventory/README.md`
- `ErpSaas.Modules.Crm/README.md`
- `ErpSaas.Modules.Wallet/README.md`
- `ErpSaas.Modules.Shift/README.md`
- `ErpSaas.Modules.Masters/README.md`

---

## Phase 1 exit gate demo

Run live in front of the human product owner (or record it):

1. Cold start: `docker compose up -d`, `dotnet run --project src/ErpSaas.Api`.
2. Admin logs in → branch selector shows in topbar → switches to branch "Warehouse 1".
3. Admin opens a shift via POS terminal → shift appears in shift list.
4. Cashier creates invoice, scans a product barcode → line auto-fills.
5. Cashier finalizes invoice → customer SMS received.
6. Cashier pays via split tender (cash + wallet) → wallet balance updated.
7. Cashier prints 80mm thermal receipt → receipt matches invoice.
8. Cashier pops cash drawer → 204 from API.
9. Admin closes shift → close SMS sent → dashboard updates.
10. Run `dotnet test` — all unit + arch tests green.
11. Run `erp-phase-check` — all 12 universal + all P1 extras green.

Only after all 11 demo steps pass is Phase 1 closed.

---

## Common Phase 1 mistakes to avoid

- **Storing invoice status as a string literal.** Every status transition must use `InvoiceStatus.Finalized`, not `"Finalized"`. The arch test `EveryStateField_IsEnumWithStringConversion` catches this.
- **Forgetting `QuantityInBaseUnit` on line entities.** All invoice lines, stock movements, and PO lines must carry base-unit quantity. Stock math is always in base unit; UI can render in any user-facing unit.
- **Using `IInvoicePdfService` as the class name.** The arch test `BillingService_ExtendsBaseService` checks all `*Service` classes — the PDF generator must be named `InvoicePdfGenerator` / `IInvoicePdfGenerator` to avoid a false positive.
- **Cross-module DB access.** `BillingService` must not inject `PlatformDbContext`. Use `IShopInfoProvider` (a thin contract in `ErpSaas.Shared`) for shop details needed by PDF generation.
- **Calling `IWalletDebit` without the `IWalletDebit` interface.** `BillingService` must reference the cross-module interface, not `WalletService` directly. The arch test `NoCrossModuleDbContextAccess` enforces this boundary.
- **Skipping the subscription gate on new endpoints.** Every new billing/wallet/shift endpoint that is a premium feature must carry `[RequireFeature("Module.Feature")]`.
- **Not wiring `X-Branch-Id` header.** The `tenantInterceptor` must include the active branch ID. Without it, branch-scoped data (stock levels, shifts) returns data for all branches.
