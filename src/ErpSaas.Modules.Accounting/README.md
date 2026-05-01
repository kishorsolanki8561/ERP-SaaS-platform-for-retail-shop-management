# Accounting Module

**Plan reference:** `docs/MASTER_PLAN.md` §6.4 — Accounting & Finance

---

## Overview

Double-entry accounting engine for retail shops. Covers chart of accounts, journal vouchers, expenses, bank accounts, and financial year management. Integrates with Billing (auto-post sale vouchers), Wallet (auto-post payment vouchers), and Shift (variance vouchers) via the `IAutoVoucherService` cross-module contract.

---

## Database

| Property | Value |
|---|---|
| **DB tier** | TenantDB |
| **Schema** | `accounting` |
| **Context** | `TenantDbContext` (shared, registered via `IEntityModelConfigurator`) |

---

## Entities

| Entity | Description |
|---|---|
| `AccountGroup` | Ledger group (Assets, Liabilities, Income, Expense, Equity); self-referential parent |
| `Account` | Individual ledger account; belongs to one AccountGroup |
| `Voucher` | Journal / Payment / Receipt / Contra / Sale / Purchase voucher header |
| `VoucherEntry` | Debit or credit line on a Voucher |
| `Expense` | Expense record linked to an Account and optionally to a Voucher |
| `BankAccount` | Bank account details linked to a ledger Account |
| `FinancialYear` | Indian FY (April–March); tracks open/closed state |

---

## Endpoints

| Method | URL | Permission | Feature |
|---|---|---|---|
| GET | `/api/accounting/account-groups` | `Accounting.View` | — |
| GET | `/api/accounting/accounts` | `Accounting.View` | — |
| POST | `/api/accounting/accounts` | `Accounting.ManageAccounts` | — |
| PUT | `/api/accounting/accounts/{id}` | `Accounting.ManageAccounts` | — |
| DELETE | `/api/accounting/accounts/{id}` | `Accounting.ManageAccounts` | — |
| GET | `/api/accounting/vouchers` | `Accounting.View` | — |
| POST | `/api/accounting/vouchers` | `Accounting.CreateVoucher` | — |
| POST | `/api/accounting/vouchers/{id}/post` | `Accounting.PostVoucher` | — |
| POST | `/api/accounting/vouchers/{id}/reverse` | `Accounting.PostVoucher` | — |
| GET | `/api/accounting/expenses` | `Accounting.View` | — |
| POST | `/api/accounting/expenses` | `Accounting.ManageExpenses` | — |
| GET | `/api/accounting/bank-accounts` | `Accounting.View` | — |
| POST | `/api/accounting/bank-accounts` | `Accounting.ManageAccounts` | — |
| GET | `/api/accounting/financial-years` | `Accounting.View` | — |
| POST | `/api/accounting/financial-years` | `Accounting.ManageAccounts` | — |
| POST | `/api/accounting/financial-years/{id}/close` | `Accounting.CloseFinancialYear` | `Accounting.Basic` |

---

## DDL Catalogs

| Key | Description | Items |
|---|---|---|
| `VOUCHER_TYPE` | Voucher types | JOURNAL, PAYMENT, RECEIPT, CONTRA, SALE, PURCHASE |
| `GST_SLAB` | GST tax slabs | GST_0, GST_5, GST_12, GST_18, GST_28 |

---

## Permissions

| Code | Description |
|---|---|
| `Accounting.View` | View accounts, vouchers and reports |
| `Accounting.ManageAccounts` | Create and edit ledger accounts |
| `Accounting.CreateVoucher` | Create journal / payment / receipt vouchers |
| `Accounting.PostVoucher` | Post and reverse vouchers |
| `Accounting.ManageExpenses` | Record and manage expenses |
| `Accounting.CloseFinancialYear` | Close a financial year |

---

## Features (Subscription-gated)

| Code | Plans | Description |
|---|---|---|
| `Accounting.Basic` | Growth, Enterprise | Core accounting — COA, vouchers, expenses, bank accounts |
| `Accounting.Advanced` | Growth, Enterprise | Bank reconciliation, fixed assets, depreciation |
| `Accounting.ITRFiling` | Growth, Enterprise | ITR-3/4 export and tax-saving summary |
| `Accounting.GstReturns` | Growth, Enterprise | GSTR-1 and GSTR-3B generation |
| `Accounting.MultiCurrency` | Growth, Enterprise | Multi-currency transactions and forex gain/loss |

---

## Sequence Definitions

| Code | Prefix | Example |
|---|---|---|
| `VOUCHER_JOURNAL` | `VJ` | VJ-2526-000001 |
| `VOUCHER_PAYMENT` | `VP` | VP-2526-000001 |
| `VOUCHER_RECEIPT` | `VR` | VR-2526-000001 |
| `VOUCHER_CONTRA` | `VC` | VC-2526-000001 |

---

## Menu Items

| Code | Label | Route | Permission | Feature |
|---|---|---|---|---|
| `accounting` | Accounting | — (group) | — | `Accounting.Basic` |
| `accounting.accounts` | Accounts | `/accounting/accounts` | `Accounting.View` | — |
| `accounting.vouchers` | Vouchers | `/accounting/vouchers` | `Accounting.View` | — |
| `accounting.expenses` | Expenses | `/accounting/expenses` | `Accounting.ManageExpenses` | — |
| `accounting.bank-accounts` | Bank Accounts | `/accounting/bank-accounts` | `Accounting.View` | — |
| `accounting.reports` | Reports | `/accounting/reports/trial-balance` | `Accounting.View` | — |
| `accounting.gst` | GST Returns | `/accounting/gst/gstr1` | `Accounting.View` | `Accounting.GstReturns` |
| `accounting.year-end` | Year End | `/accounting/year-end/close` | `Accounting.CloseFinancialYear` | `Accounting.Basic` |

---

## Cross-module Integrations

| Contract | Implemented by | Used by |
|---|---|---|
| `IAutoVoucherService` | `AccountingService` (in `ErpSaas.Shared`) | `BillingService`, `WalletService`, `ShiftService` |

---

## Tenant Seeder

`AccountingTenantSeeder` runs during shop onboarding and seeds a standard Indian Chart of Accounts:

- 5 AccountGroups: Assets, Liabilities, Income, Expense, Equity
- 24 pre-built Accounts: Cash, Bank, AR, Inventory, Input/Output GST, AP, Sales, COGS, Salaries, Rent, etc.

---

## Running this module's tests

```bash
# Unit tests only (fast, SQLite in-memory)
dotnet test src/ErpSaas.Tests.Unit --filter "FullyQualifiedName~Accounting"

# Arch tests
dotnet test src/ErpSaas.Tests.Arch --filter "FullyQualifiedName~Accounting"

# All tests for this module (unit + integration + arch)
dotnet test src/ErpSaas.sln --filter "FullyQualifiedName~Accounting"
```
