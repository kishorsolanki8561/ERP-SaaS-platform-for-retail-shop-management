# CLAUDE.md — ERP + SaaS Platform Project Rules

> This file is read by Claude Code on every session. It captures the **non-negotiable** rules distilled from `docs/MASTER_PLAN.md`. If anything here conflicts with the plan, the plan wins — but read the plan section referenced, then update this file.

---

## 1. Project at a glance

Multi-tenant ERP + SaaS for retail and wholesale shops (starting with electrical / electronics / power-tools, built to resell to any retail+wholesale business via vertical packs). Monolithic-modular architecture — one deployable API + one Angular app internally split into 28 business modules + 7 platform modules + 16 cross-cutting frameworks, all documented in `docs/MASTER_PLAN.md`. Multi-DB (7 physical databases, 17+ schemas), multi-platform (web + Electron desktop + Ionic Capacitor mobile from one Angular codebase), with offline-first POS, dual-hosting (cloud + self-hosted on-prem), customer portal with cross-shop analytics, and a CA portal for tax filing.

**Always read `docs/MASTER_PLAN.md` § references before writing code.** The plan is the spec; this file is the shortlist of rules I will forget if you don't enforce them.

---

## 2. Tech stack (fixed — do not substitute)

- **Backend:** .NET 8 Web API, C# 12, EF Core 8 (code-first), **Dapper** for reports / hot queries / sprocs, Hangfire for jobs, SignalR for real-time, FluentValidation, Serilog, xUnit + Testcontainers + NetArchTest + Playwright
- **Database:** SQL Server (Express/Developer locally, Azure SQL Managed Instance in prod)
- **Frontend:** Angular 18+, standalone components, **signals** (not RxJS for UI state), **OnPush** change detection everywhere, PrimeNG for UI, Jest + Playwright for tests
- **Infra:** Docker Compose local, Azure App Service + Azure SQL for staging/prod, GitHub Actions CI/CD, Redis for caching, Cloudflare Turnstile for CAPTCHA
- **No substitutions without a written decision in `docs/decisions/`.**

---

## 3. The Eight Non-Negotiables

These rules apply to **every line of code** in this repo. CI will reject PRs that violate them.

### 3.1 Every service method goes through `BaseService.ExecuteAsync`
Plan § 5.1. No service writes to the DB without this wrapper — it provides try/catch, optional transaction, CancellationToken, error logging, audit logging, and returns `Result<T>`. An arch test fails the build if `SaveChangesAsync` appears outside `BaseService.ExecuteAsync`.

```csharp
public async Task<Result<long>> CreateInvoiceAsync(InvoiceDto dto, CancellationToken ct)
{
    return await _baseService.ExecuteAsync("Billing.CreateInvoice", async () =>
    {
        // work here
        return Result<long>.Success(id);
    }, ct, useTransaction: true);
}
```

### 3.2 Every dropdown is a DDLKey
Plan § 5.5. Frontend never hardcodes dropdown options — ever. Even `["Active","Inactive"]` must come from a `DdlCatalog` row. If you catch yourself typing a `<p-dropdown [options]="[...]">` with a literal array, stop and add a DDL catalog instead.

### 3.3 Every external call is logged
Plan § 5.3. SMS, email, payment gateway, GST portal, Amazon, Flipkart — routed through `ThirdPartyApiClientBase` which writes a `ThirdPartyApiLog` row (request, response, status, duration) for every call. Never use raw `HttpClient` for external calls.

### 3.4 Every mutation is audited
Plan § 5.4. Entity changes captured by `AuditSaveChangesInterceptor` + `[Auditable]` attribute for declarative coverage. Semantic events (login, role change, data export) use `AuditLogger.LogAsync(...)` explicitly.

### 3.5 Every endpoint is permission-gated + subscription-gated
Plan § 5.7. Every controller action carries `[RequirePermission("X.Y")]` and, where applicable, `[RequireFeature("Module.Feature")]`. Three-layer defense: menu tree filter + Angular route guard + API attribute — all three required for every new page (§ 5.8).

### 3.6 Every document number comes from `ISequenceService`
Plan § 5.16. Invoice, PO, Quotation, SO, DC, Credit Note, Payment Receipt, Voucher, Warranty Claim, etc. — all numbers from `await _sequence.NextAsync("INVOICE_RETAIL", ...)`. Never write a private sequence table. `usp_AllocateSequenceNumber` handles concurrency via UPDLOCK+HOLDLOCK.

### 3.7 Every quantity-bearing row captures unit + conversion factor
Plan § 6.3.c. `InvoiceLine`, `PurchaseOrderLine`, `DeliveryChallanLine`, `SalesReturnLine`, `StockMovement`, `StockCountLine` — all carry `ProductUnitId`, `UnitCodeSnapshot`, `ConversionFactorSnapshot`, and `QuantityInBaseUnit`. Stock math is always in base unit; UI can render in any unit. Cross-category conversions (KG → M) are forbidden.

### 3.10 Every stock-bearing location has a bin address
Every product stored in a warehouse can optionally carry a `StorageBin` address — a 4-level hierarchy: **Zone → Rack → Shelf → Bin** (all nullable; you can use just Rack+Bin for a simple shop). The `StorageBin` entity lives in the `inventory` schema with `WarehouseId` + unique `BinCode` per warehouse. `StockMovement` and `StockLevel` rows carry an optional `StorageBinId` so you can answer "where exactly in the warehouse is this product?" Stock count sheets, put-away instructions, and pick lists are generated per bin. UI shows a location badge on every product card (e.g., `A-R2-S3-B4` = Zone A, Rack 2, Shelf 3, Bin 4).

### 3.8 Every public auth endpoint is CAPTCHA-gated
Plan § 9.4. Login, forgot-password, signup, OTP request, invite-accept, bootstrap — all carry `[RequireCaptcha]`. Arch test `EveryPublicAuthEndpoint_HasCaptchaGuard` blocks PR if one is missing.

### 3.9 Every status / state-machine field is a C# enum stored as string
Any field that drives business logic transitions (invoice status, movement type, order state, etc.) **must** be a C# enum, never a `string` with magic literals. EF Core must store it as a string in the DB column via `.HasConversion<string>()` in the entity's type configuration. DTOs that expose these fields must carry the enum type, not `string`. Comparing or assigning hardcoded string literals like `"Draft"`, `"Finalized"`, `"Purchase"` instead of the enum member is a build violation.

```csharp
// Entity
public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

// EF config (HasConversion stores "Draft", "Finalized", ... instead of 0, 1, ...)
e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

// Service — always compare with enum member, never a string literal
if (invoice.Status != InvoiceStatus.Draft) return Result<bool>.Conflict(...);
invoice.Status = InvoiceStatus.Finalized;
```

---

## 4. Database rules (§4.5 + §4.6)

### 4.1 Four physical DBs by concern + three day-1 extractions = seven DBs
| DB | Holds | Connection string key |
|---|---|---|
| `PlatformDB` | Plans, permissions, menus, shop registry, platform masters (Country/State/HSN), CA assignments, platform owner actions | `ConnectionStrings:PlatformDb` |
| `TenantDB` | Business data (17+ schemas — see §4.6). Standard-tier shops share; premium shops get their own | `ConnectionStrings:TenantDb` (resolved per-shop via `IShopConnectionResolver`) |
| `AnalyticsDB` | Read-only; facts, dims, marts for dashboards and reports | `ConnectionStrings:AnalyticsDb` |
| `LogDB` | ErrorLog, AuditLog, ThirdPartyApiLog, SequenceAllocation history, SlowQueryLog | `ConnectionStrings:LogDb` |
| `NotificationsDB` | Queue + delivery history (extracted day 1) | `ConnectionStrings:NotificationsDb` |
| `MarketplaceEventsDB` | Raw webhook payloads from Amazon / Flipkart (extracted day 1) | `ConnectionStrings:MarketplaceEventsDb` |
| `SyncDB` | Offline command queue + on-prem replication logs (extracted day 1) | `ConnectionStrings:SyncDb` |

### 4.2 Schema per module inside TenantDB
Every business module owns a SQL schema. Entity configuration **must** declare it: `b.ToTable("Invoice", schema: "sales")`. Arch test `Schema_Ownership_Matches_Module` fails the build if an entity lands in the wrong schema. See plan §4.6 for the full mapping table.

### 4.3 Context injection is arch-test-gated
Business modules inject `TenantDbContext` only — never `PlatformDbContext` / `AnalyticsDbContext` / `LogDbContext` directly. Only these modules may touch those:
- `ErrorLogger`, `AuditLogger`, `ThirdPartyApiLogger` → `LogDbContext`
- Reporting engine + Dashboard → `AnalyticsDbContext`
- Platform admin + Identity + Masters (platform-level) → `PlatformDbContext`

### 4.4 Global query filter = non-negotiable tenant isolation
Every `TenantEntity` has a global query filter on `ShopId = ITenantContext.ShopId`. Never `IgnoreQueryFilters()` in a business service. Integration tests run every endpoint twice (two shops) and assert zero cross-shop leakage.

### 4.5 Cross-DB transactions are forbidden
Cross-DB writes use outbox pattern (local write + outbox row → relay job → consumer). If a single operation writes to Tenant + Platform, route one side through outbox.

---

## 5. Data access rules (§5.15)

### 5.1 EF Core is the default; Dapper is the scalpel
Use EF for CRUD, simple list pages, and anything with tenant-filter dependency. Use Dapper for:
- Reports (anything in §5.12 or §6.11)
- Dashboards (§7.1)
- Queries joining 3+ tables with aggregates / windows / CTEs
- Stored procedure calls
- Bulk operations > 100 rows
- Reads from AnalyticsDB and LogDB

### 5.2 Dapper shares EF's transaction
Inside `BaseService.ExecuteAsync(useTransaction: true)`, Dapper calls must use `_db.Connection` + `_db.CurrentTransaction`. Never open a parallel connection inside the same unit of work.

### 5.3 Stored procedures are code
All `.sql` files live in `/src/ErpSaas.Infrastructure/Sql/`, use `CREATE OR ALTER`, are idempotent, and are deployed by `ISqlObjectMigrator` which tracks SHA-256 per file. Every sproc has at least one integration test against Testcontainers SQL Server.

---

## 6. Testing rules (§5.13)

### 6.1 Six test classes required per module
Every business module ships with:
- `{Module}ServiceTests` — unit (mocked) — every public method, happy path + every failure
- `{Module}ControllerTests` — integration (Testcontainers + WAF) — every endpoint with auth, permission, feature, validation
- `{Module}TenantIsolationTests` — seed two shops, assert no cross-shop reads/writes
- `{Module}SubscriptionGateTests` — feature off → 402, on → 200, menu hidden when off
- `{Module}AuditTrailTests` — after every mutation, assert correct `AuditLog` row exists
- `{Module}ArchTests` — NetArchTest rules specific to the module

### 6.2 Coverage gates
- Services ≥ 80% line coverage
- Pricing engine, tax computation, wallet ledger math = 100%
- Overall repo ≥ 75%
- Coverage drop in a PR = PR rejected

### 6.3 Tests use builders + fixtures, not hand-rolled entities
`InvoiceBuilder`, `ProductBuilder`, `CustomerBuilder` with Bogus-backed defaults + fluent `.WithLine(p, qty).ForCustomer(c)` style. Shared fixtures: `OnboardedShopFixture`, `WholesaleCustomerFixture`.

### 6.4 Tests run against real SQL Server (Testcontainers)
Never use EF in-memory provider for integration tests — its semantics diverge from SQL Server and you will ship bugs you can't reproduce.

### 6.5 Seed deterministically
Bogus seeded with a constant. Same test run → same fake data. No flaky tests.

---

## 7. Frontend rules (§8)

### 7.1 Standalone + OnPush + signals — no exceptions
- `@Component({ standalone: true, changeDetection: ChangeDetectionStrategy.OnPush })`
- State in `signal()` / `computed()` / `effect()`. Use `toSignal()` for streams. Never `BehaviorSubject` for UI state.
- Inputs via `input()` / `input.required()`. Outputs via `output()`. Two-way via `model()`.

### 7.2 No hardcoded anything
- No hardcoded dropdowns (use `<app-ddl-dropdown [dkey]="..." />`)
- No hardcoded permission strings outside the `permissions.ts` generated from the API's permission catalog
- No hardcoded colors/spacing outside the design system

### 7.3 List pages use the shared `<app-data-table>`
Lazy mode, server-driven paging/sort/filter, column config, export button, row-action menu. If you're writing a `<table>` by hand, you're doing it wrong.

### 7.4 Every feature is lazy-loaded
Every module folder's routes in a `*.routes.ts` file, loaded via `loadChildren`. Auth shell stays in the initial bundle; nothing else.

### 7.5 Structural directives guard every permission-sensitive UI element
```html
<button *hasPermission="'Invoice.Cancel'" (click)="cancel()">Cancel</button>
<div *hasFeature="'Billing.EInvoice'">IRN: {{ irn() }}</div>
```

---

## 8. Security non-negotiables (§9)

- **JWT:** 15-min access + 30-day rotating refresh + optional TOTP 2FA. Customer portal JWTs carry `token_scope=customer` and are rejected by staff API.
- **Password hashing:** BCrypt cost 12. Never `MD5` / `SHA-256(password)` / plain text anywhere.
- **Sensitive columns:** `PasswordHash`, `TotpSecretEncrypted`, `AadhaarNumber`, gateway credentials — AES-256 via `ValueConverter`. KEK in Azure Key Vault; DEK rotated quarterly.
- **SQL injection:** Always parameterized. Arch test forbids `FormattableString.Invariant` concat.
- **Secrets in code:** Zero tolerance. `appsettings.Production.json` is empty; env vars / Key Vault only.
- **Rate limiting:** Auth endpoints 5/min/IP. OTP endpoints 3/hour/identifier. Public endpoints 100/min/IP.
- **CAPTCHA:** Every public auth surface (see §3.8 above).

---

## 9. Naming & convention (be boring, be consistent)

- Entities: `Invoice`, not `InvoiceEntity`. Plural DbSets: `Invoices`.
- Service interfaces: `IInvoiceService`. Implementation: `InvoiceService`.
- DTOs: `InvoiceDto` (request) + `InvoiceResponseDto` or just reuse entity for simple cases.
- Methods on services: `CreateAsync` / `GetAsync` / `ListAsync` / `UpdateAsync` / `DeleteAsync` / `FinalizeAsync`. Never `DoInvoice()`.
- Permission codes: `Invoice.Create`, `Invoice.Cancel`, `Product.Manage` — PascalCase, dot-separated, noun.verb.
- Feature codes: `Billing.BarcodePos`, `Inventory.MultiUnit` — PascalCase, dot-separated, module.feature.
- Test names: `MethodName_Scenario_Expected` → `CreateAsync_WhenCustomerInactive_ReturnsFailure`.
- Migration names: `{Timestamp}_{Module}_{Change}` → `20260424_Billing_AddCreditNote`.
- Schema names: lowercase, single word, module-aligned — `sales`, `inventory`, `accounting`, `hr`, `wallet`, `warranty`, etc.
- DDL catalog keys: `SCREAMING_SNAKE_CASE` → `PAYMENT_MODE`, `INVOICE_STATUS`.

---

## 10. Commands I will run a lot

```bash
# Backend
dotnet build src/ErpSaas.sln                            # build all
dotnet test src/ErpSaas.sln                             # run all tests
dotnet test --filter Category=Unit                      # unit only
dotnet test --filter Category=Integration               # integration only
dotnet test --filter FullyQualifiedName~BillingModule   # one module

# Migrations — per module, per DB tier
dotnet ef migrations add {Name} --project src/ErpSaas.Modules.Billing --context TenantDbContext --output-dir Migrations/Tenant
dotnet ef database update --project src/ErpSaas.Modules.Billing --context TenantDbContext

# Frontend
cd src/web && npm install && npm start
cd src/web && npm test                                   # Jest
cd src/web && npm run e2e                                # Playwright

# Docker — local dev stack
docker compose -f ops/docker-compose.yml up -d           # SQL Server, Redis, MailHog, Azurite
docker compose logs -f api                               # tail API logs

# Arch tests (run these locally before pushing)
dotnet test --filter Category=Architecture

# Phase exit-gate check
# (See .claude/skills/erp-phase-check/SKILL.md)
```

---

## 11. Things I must NOT do

- **Do not** write `await _db.SaveChangesAsync()` outside `BaseService.ExecuteAsync`.
- **Do not** declare a status/state field as `string` — use a C# enum with `.HasConversion<string>()` in EF config (§ 3.9).
- **Do not** use EF in-memory provider for integration tests.
- **Do not** hardcode strings for tenant / user IDs, permission codes, feature codes, DDL options.
- **Do not** call `HttpClient` directly — always through `ThirdPartyApiClientBase`.
- **Do not** create a private numbering table — `ISequenceService` only.
- **Do not** add a new page without seeding its MenuItem + permission + feature flag + controller attributes + route guard.
- **Do not** bypass global query filters with `IgnoreQueryFilters()` in business services.
- **Do not** write `<p-dropdown [options]="[...]">` with literal options.
- **Do not** skip or disable a failing arch test. If it fails, the code is wrong.
- **Do not** introduce RxJS for new UI state — use signals.
- **Do not** create a new module without: ServiceDescriptor registration, schema declaration, six test classes, module README.
- **Do not** run destructive SQL against a shared environment — only against the per-PR test DB.
- **Do not** merge my own PR — request review, even if the human approver is AFK.

---

## 12. When I get stuck

1. **Re-read the referenced plan section.** Every rule above has a `§X.Y` — the plan has the design rationale.
2. **Check the arch tests.** They often tell you exactly what pattern the code should follow.
3. **Look for an existing similar module.** Billing is the reference module; most write-heavy flows mirror it.
4. **Check the skills.** `.claude/skills/` has templates for scaffolding, services, and tests — follow them.
5. **If the plan is ambiguous**, don't guess: stop, write the ambiguity down in `docs/open-questions.md`, ask the human.

---

## 13. Current phase tracker

> **Update this section at the end of every session.** It's the one piece of state that survives between sessions.

- [x] **Phase 0 — Foundation** — ✅ DONE (2026-04-24)
- [ ] **Phase 1 — Core Retail Loop** ← current
- [ ] **Phase 2 — Financial Core**
- [ ] **Phase 3 — Operations**
- [ ] **Phase 4 — HR + Marketplace**
- [ ] **Phase 5 — SaaS Polish**
- [ ] **Phase 6 — Multi-Platform Shells**
- [ ] **Phase 7 — Vertical Packs**

**Current sprint:** Phase 1 — Week 1: Customer entity (CRM schema) + Product/Inventory entity (inventory schema) + basic Invoice (sales schema) skeleton
**Blockers:** Staging deployment + full integration test suite + Cloudflare Turnstile CAPTCHA still pending (deferred to Phase 1 kickoff)
**Next action:** `dotnet new classlib -n ErpSaas.Modules.Crm` — scaffold CRM module per `erp-scaffold-module` skill

---

## 14. Where to find things

```
/                               repo root
├── CLAUDE.md                   this file
├── docs/
│   ├── MASTER_PLAN.md          the full spec — ground truth
│   ├── PHASE_0_STARTER.md      Week 1 kickoff
│   ├── decisions/              architecture decision records (ADRs)
│   └── open-questions.md       ambiguities waiting on human
├── .claude/
│   └── skills/                 project-specific Claude skills
│       ├── erp-scaffold-module/
│       ├── erp-write-service/
│       └── erp-phase-check/
├── src/
│   ├── ErpSaas.sln
│   ├── ErpSaas.Api/            host project
│   ├── ErpSaas.Core/           shared domain contracts
│   ├── ErpSaas.Infrastructure/ EF, Dapper, sprocs, seeds
│   ├── ErpSaas.Shared/         cross-cutting (§5 framework)
│   ├── ErpSaas.Modules.*/      one project per module
│   ├── ErpSaas.Tests.Unit/
│   ├── ErpSaas.Tests.Integration/
│   └── ErpSaas.Tests.Arch/
├── src/web/                    Angular staff app
├── src/web-portal/             Angular customer portal (§6.27)
└── ops/
    ├── docker-compose.yml
    └── github-actions/
```

---

**When in doubt — read `docs/MASTER_PLAN.md` § {section number referenced above}. The plan is ~7,000 lines because every line earned its place.**
