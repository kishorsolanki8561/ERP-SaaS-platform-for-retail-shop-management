# Module Index

One-line summary of every business module. See the linked README for full details.

| Module | Schema | Plan § | README |
|---|---|---|---|
| **Billing** | `sales` | §6.1 | [src/ErpSaas.Modules.Billing/README.md](../src/ErpSaas.Modules.Billing/README.md) |
| **Inventory** | `inventory` | §6.3 | [src/ErpSaas.Modules.Inventory/README.md](../src/ErpSaas.Modules.Inventory/README.md) |
| **CRM** | `crm` | §6.5 | [src/ErpSaas.Modules.Crm/README.md](../src/ErpSaas.Modules.Crm/README.md) |
| **Identity** | `identity` | §6.6 | [src/ErpSaas.Modules.Identity/README.md](../src/ErpSaas.Modules.Identity/README.md) |
| **Wallet** | `wallet` | §6.7 | [src/ErpSaas.Modules.Wallet/README.md](../src/ErpSaas.Modules.Wallet/README.md) |
| **Shift** | `shift` | §6.8 | [src/ErpSaas.Modules.Shift/README.md](../src/ErpSaas.Modules.Shift/README.md) |
| **Masters** | `masters` | §6.9 | [src/ErpSaas.Modules.Masters/README.md](../src/ErpSaas.Modules.Masters/README.md) |
| **Accounting** | `accounting` | §6.4 | [src/ErpSaas.Modules.Accounting/README.md](../src/ErpSaas.Modules.Accounting/README.md) — COA, double-entry vouchers, expenses, bank accounts, financial years |
| **Purchasing** | `purchasing` | §6.3 | [src/ErpSaas.Modules.Purchasing/README.md](../src/ErpSaas.Modules.Purchasing/README.md) — Suppliers, purchase orders, goods receipt, vendor bills and payments |
| **SalesReturns** | `sales` | §6.2 | [src/ErpSaas.Modules.SalesReturns/README.md](../src/ErpSaas.Modules.SalesReturns/README.md) — Sales returns, credit notes, refund processing (wallet/cash/bank) |
| **Reports** | (no tables) | §6.11 | [src/ErpSaas.Modules.Reports/README.md](../src/ErpSaas.Modules.Reports/README.md) — Trial Balance, P&L, Balance Sheet, Day Book, Ledger, GSTR-1 B2B, HSN Summary; PDF/XLSX/CSV export |
| **Warranty** | `warranty` | §6.5 | [src/ErpSaas.Modules.Warranty/README.md](../src/ErpSaas.Modules.Warranty/README.md) — Warranty registrations (by serial), claim lifecycle, expiry alerts |
| **Pricing** | `pricing` | §6.6 | [src/ErpSaas.Modules.Pricing/README.md](../src/ErpSaas.Modules.Pricing/README.md) — Discount rules, extra charges, offer/combo engine, real-time cart pricing calculation |
| **Transport** | `transport` | §6.7 | [src/ErpSaas.Modules.Transport/README.md](../src/ErpSaas.Modules.Transport/README.md) — Transport providers, vehicle fleet, delivery scheduling and status tracking with full log history |
| **Quotations** | `sales` | §6.17 | [src/ErpSaas.Modules.Quotations/README.md](../src/ErpSaas.Modules.Quotations/README.md) — Quotations → Sales Orders → Delivery Challans; full convert-and-dispatch workflow |
| **CustomerPortal** | `portal` | §6.27 | [src/ErpSaas.Modules.CustomerPortal/README.md](../src/ErpSaas.Modules.CustomerPortal/README.md) — Customer self-service portal: OTP auth, cross-shop purchase history, online orders, support inquiries |
| **ApiAccess** | `integration` | §6.20 | [src/ErpSaas.Modules.ApiAccess/README.md](../src/ErpSaas.Modules.ApiAccess/README.md) — Shop API key management + outbound webhook dispatch with HMAC-SHA256 signing and exponential-backoff retry |
| **Verticals** | `verticals` | §7.1 | src/ErpSaas.Modules.Verticals — Vertical pack registry (ELECTRICAL/MEDICAL/GROCERY) and per-shop vertical installation; gates all vertical-specific features |
| **ServiceJobs** | `service` | §6.24 | src/ErpSaas.Modules.ServiceJobs — Service/repair job tracking with full state machine (Received→Diagnosed→Approved→InProgress→Ready→Delivered), parts and labour costing, anonymous job-status lookup |
| **Medical** | `verticals_medical` | §7.2 | src/ErpSaas.Modules.Verticals.Medical — Drug batch tracking with schedule-H/X enforcement, near-expiry alerts, and prescription record linkage |
| **Grocery** | `verticals_grocery` | §7.3 | src/ErpSaas.Modules.Verticals.Grocery — Customer loyalty points programme (Earn/Redeem/Expire), configurable points-per-rupee and redemption caps |
