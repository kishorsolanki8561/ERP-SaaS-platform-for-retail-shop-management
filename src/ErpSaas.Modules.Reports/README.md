# Reports Module

**Plan reference:** `docs/MASTER_PLAN.md` §6.11 — Reporting Engine

---

## Overview

Read-only reporting engine that renders financial and tax reports via Dapper queries against TenantDB. All queries bypass EF and run through `IReportQueryRepository`. Export is handled by `IReportBuilderService` (Excel via EPPlus, PDF via QuestPDF). No entities or migrations — this module owns no schema rows.

---

## Database

| Property | Value |
|---|---|
| **DB tier** | TenantDB (read-only, Dapper only) |
| **Schema** | None — read-only cross-schema queries |
| **Context** | `TenantDbContext` injected for connection/transaction sharing only |

---

## Reports shipped

| Code | Report | Output |
|---|---|---|
| `TRIAL_BALANCE` | Trial Balance | Excel / PDF |
| `PNL` | Profit & Loss Statement | Excel / PDF |
| `BALANCE_SHEET` | Balance Sheet | Excel / PDF |
| `DAY_BOOK` | Day Book (all vouchers by date) | Excel / PDF |
| `LEDGER` | Account Ledger | Excel / PDF |
| `GSTR1_B2B` | GSTR-1 B2B Invoice Summary | Excel / PDF |
| `HSN_SUMMARY` | HSN/SAC Summary | Excel / PDF |
| `GSTR3B` | GSTR-3B Liability Summary | Excel / PDF |
| `CASH_BOOK` | Cash Book | Excel / PDF |
| `BANK_BOOK` | Bank Book | Excel / PDF |
| `WALLET_STATEMENT` | Customer Wallet Statement | Excel / PDF |

---

## Endpoints

| Method | URL | Permission | Feature |
|---|---|---|---|
| GET | `/api/reports/{code}` | `Reports.View` | `Reports.Export` |
| GET | `/api/reports/{code}/export` | `Reports.View` | `Reports.Export` |

---

## Permissions seeded

| Code | Label |
|---|---|
| `Reports.View` | View and run financial reports |

---

## Features seeded

| Code | Label |
|---|---|
| `Reports.Export` | Export reports to Excel and PDF |

---

## Services

| Interface | Implementation | Role |
|---|---|---|
| `IReportBuilderService` | `ReportBuilderService` | Orchestrates report generation and export |
| `IReportQueryRepository` | `ReportQueryRepository` | Dapper queries for each report type |

---

## Design constraints

- **Dapper only** — no EF queries; reports join multiple schemas and use window functions / CTEs that EF can't express cleanly.
- **No tenant filter bypass** — all Dapper queries include `WHERE ShopId = @shopId` explicitly.
- **No write path** — this module never calls `SaveChangesAsync`.
- **Export formats** — Excel via EPPlus; PDF via QuestPDF. Format selected by `ReportFormat` enum (`Excel`, `Pdf`).

---

## Cross-module dependencies (read-only)

Reads from: `accounting.*`, `sales.*`, `purchasing.*`, `inventory.*`, `wallet.*` schemas within TenantDB.

---

## Tests

| Class | Type | Location |
|---|---|---|
| `ReportBuilderServiceTests` | Unit | `ErpSaas.Tests.Unit/Modules/Reports/` |
| `ReportsControllerTests` | Integration | `ErpSaas.Tests.Integration/Modules/Reports/` |
| `ReportsTenantIsolationTests` | Integration | `ErpSaas.Tests.Integration/Modules/Reports/` |
| `ReportsSubscriptionGateTests` | Integration | `ErpSaas.Tests.Integration/Modules/Reports/` |
| `ReportsAuditTrailTests` | Integration | `ErpSaas.Tests.Integration/Modules/Reports/` |
| `ReportsArchTests` | Architecture | `ErpSaas.Tests.Arch/Modules/` |
