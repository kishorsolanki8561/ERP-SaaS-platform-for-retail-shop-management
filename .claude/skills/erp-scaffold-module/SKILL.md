---
name: erp-scaffold-module
description: Scaffold a complete new business module for the ERP + SaaS platform — producing the module csproj, DI registration, entity stubs, service + controller + validator, six required test classes, EF configuration, module seeder (DDL + permissions + features + menu items + sequence definitions), schema registration, ServiceDescriptor, and README. Triggers whenever the user says "create module", "scaffold module", "new module", "add module for X", or when starting any new section of the plan under §6.x or §7.x. Also triggers when the user is mid-way through a phase and about to begin a previously-unbuilt module. Use this every time a new module is started — skipping it produces inconsistent module shapes that fail arch tests and doubles rework later.
---

# ERP Module Scaffold

Produces the complete file skeleton a new module needs per `docs/MASTER_PLAN.md` §5 + §4.6. Run this **every time** you start a new module — doing it by hand misses one of the 14 required pieces and the arch tests will fail later.

## Inputs to collect before scaffolding

1. **Module code** — short PascalCase identifier used in namespaces, folder names, service catalog, menu seeds. Examples: `Billing`, `Inventory`, `Wallet`, `CustomerPortal`.
2. **Module schema** — lowercase single word for SQL schema. Must match `docs/MASTER_PLAN.md` §4.6 table. Examples: `sales`, `inventory`, `wallet`.
3. **DB tier** — `Tenant` (default) / `Platform` / `Analytics` / `Log` / one of the three day-1 extractions. Business modules should almost always be `Tenant`.
4. **Primary entities** — list of 3-6 entity names (don't try to scaffold 20 at once; add rest after). Example: `Invoice`, `InvoiceLine`, `InvoicePayment`, `Customer`.
5. **Module's § reference** — which section of `MASTER_PLAN.md` this implements. Example: `§6.2 Module: Billing & Invoicing`.

If any of these is unclear, **stop and ask the human** — don't guess.

## Files to create

Create these **in this order** (later files import from earlier ones):

### A. Project & DI

**1.** `src/ErpSaas.Modules.{Module}/ErpSaas.Modules.{Module}.csproj` — standard module project. Copy from `src/ErpSaas.Modules.Billing.csproj` (the reference module) and rename. Dependencies: `ErpSaas.Core`, `ErpSaas.Shared`, `ErpSaas.Infrastructure`. Nothing else unless justified in PR description.

**2.** `src/ErpSaas.Modules.{Module}/DependencyInjection.cs` — use `templates/DependencyInjection.cs.tpl` verbatim, substitute `{Module}`, `{Schema}`, `{Tier}`, and the ServiceDescriptor fields.

**3.** Add one line to `src/ErpSaas.Api/Program.cs`:
```csharp
builder.Services.Add{Module}Module(builder.Configuration);
```

### B. Entities & EF configuration

**4.** `src/ErpSaas.Modules.{Module}/Entities/{EntityName}.cs` — one file per entity. Use `templates/Entity.cs.tpl` for `TenantEntity`-based entities. For every entity:
- Decide: does it extend `BaseEntity` (platform-level) or `TenantEntity` (shop-scoped)? Business entities are almost always `TenantEntity`.
- Include audit columns (inherited from BaseEntity — do not redeclare).
- Add snapshot columns where appropriate (§1 data-richness principle): `ProductNameSnapshot`, `UnitPriceSnapshot`, `ConversionFactorSnapshot` — anything that's a foreign-key value AND could change in the source table while a historical transaction should still display the original.
- **If the entity has a quantity**, add: `ProductUnitId`, `UnitCodeSnapshot`, `ConversionFactorSnapshot`, `QuantityInBilledUnit`, `QuantityInBaseUnit` (§6.3.c).
- Mark with `[Auditable]` if any state change should land in `AuditLog`.

**5.** `src/ErpSaas.Modules.{Module}/EntityConfigurations/{EntityName}Configuration.cs` — use `templates/EntityConfiguration.cs.tpl`. Every configuration class **must** include:
```csharp
builder.ToTable("{EntityName}", schema: {Module}Module.Schema);   // REQUIRED — arch test enforces
```
Plus: unique indexes on (ShopId + natural key), query indexes for common filters, FK declarations, max-length on strings.

**6.** `src/ErpSaas.Modules.{Module}/{Module}DbContext.cs` — **only if** the module needs its own `DbContext` (typically only for day-1 extractions: `NotificationsDbContext`, `MarketplaceEventsDbContext`, `SyncDbContext`). Otherwise the module's entities register into the shared `TenantDbContext` via `OnModelCreating` partial extensions.

### C. Service layer

**7.** `src/ErpSaas.Modules.{Module}/Services/I{Module}Service.cs` — primary public interface. Methods should match the "Services" list in the plan's §X.Y for this module.

**8.** `src/ErpSaas.Modules.{Module}/Services/{Module}Service.cs` — implementation. Use `templates/Service.cs.tpl`. Every public method:
- Wraps in `_baseService.ExecuteAsync("{Module}.{Method}", async () => { ... }, ct, useTransaction: bool)`.
- Calls `_sequence.NextAsync("{SEQUENCE_CODE}", ...)` for any document number.
- Calls `_audit.LogAsync(...)` for semantic events (login, role change) — declarative `[Auditable]` handles entity mutations automatically.
- Returns `Result<T>` — never raw values, never throws for expected failures.

**9.** `src/ErpSaas.Modules.{Module}/Services/I{Module}ReportQueries.cs` + `{Module}ReportQueries.cs` — **only if** the module has reports. Dapper-only (§5.15).

### D. Validation + controllers

**10.** `src/ErpSaas.Modules.{Module}/Validators/{EntityDto}Validator.cs` — FluentValidation for every DTO coming in. 100% coverage required per §5.13.

**11.** `src/ErpSaas.Modules.{Module}/Controllers/{Module}Controller.cs` — use `templates/Controller.cs.tpl`. Every action:
- `[ApiController]`, `[Route("api/{resource}")]`, `[Authorize]`.
- `[RequirePermission("{X}.{Y}")]` — from the module's permission catalog.
- `[RequireFeature("{Module}.{FeatureCode}")]` if feature-gated.
- `[RequireCaptcha]` **if** public (login, signup, forgot-password, OTP request, invite-accept).
- Thin — delegates to the service and translates `Result<T>` → `IActionResult`.

### E. Seeds

**12.** `src/ErpSaas.Modules.{Module}/Seeds/{Module}SystemSeeder.cs` — runs on every deploy, idempotent (check by code before insert). Seeds:
- **Permissions** (Appendix A-style): `{Module}.View`, `{Module}.Create`, etc.
- **Features** (Appendix B-style): `{Module}.{Feature}` entries in `SubscriptionPlanFeature`.
- **DDL catalogs + items** introduced by this module.
- **Menu items**: at least one `MenuItem` per public page, with `Kind`, `ParentId`, `RequiredPermissionCode`, `RequiredFeatureCode`, `SortOrder`, stable `Code`.
- **Sequence definitions**: any `SequenceDefinition` this module needs (§5.16 seed table).
- **Role-to-permission grants**: update seeded roles (Appendix C) if this module's permissions fit an existing role.

**13.** `src/ErpSaas.Modules.{Module}/Seeds/{Module}TenantSeeder.cs` — **only if** the module needs per-shop tenant-scoped seed data (e.g., default COA for Accounting, default WalletConfig for Wallet). Called during shop onboarding.

### F. Tests (all six are mandatory — §5.13)

**14.** `src/ErpSaas.Tests.Unit/Modules/{Module}/{Module}ServiceTests.cs` — **every public method, happy + every validation failure + every error branch + concurrency + cancellation.** Mock `_db`, real `_baseService` with in-mem `_audit` / `_error` stubs.

**15.** `src/ErpSaas.Tests.Integration/Modules/{Module}/{Module}ControllerTests.cs` — Testcontainers SQL Server + WebApplicationFactory. Every endpoint: auth required, correct permission, correct feature gate, correct validation errors, happy path, 404, 409.

**16.** `src/ErpSaas.Tests.Integration/Modules/{Module}/{Module}TenantIsolationTests.cs` — seed data into Shop A + Shop B. Every read endpoint called as Shop A asserts zero rows from Shop B. Every write endpoint as Shop A → confirm Shop B cannot read/update/delete that row.

**17.** `src/ErpSaas.Tests.Integration/Modules/{Module}/{Module}SubscriptionGateTests.cs` — feature off → endpoint returns 402 Payment Required; feature on → 200. Menu-tree endpoint hides the related item when feature is off.

**18.** `src/ErpSaas.Tests.Integration/Modules/{Module}/{Module}AuditTrailTests.cs` — for every mutating endpoint, after the call assert the correct `AuditLog` row exists with actor, entity id, old/new values.

**19.** `src/ErpSaas.Tests.Arch/Modules/{Module}ArchTests.cs` — NetArchTest rules. Minimum:
- `Schema_Ownership_Matches_Module` — every entity has `ToTable(..., schema: {Module}Module.Schema)`.
- `No_SaveChangesAsync_Outside_BaseService` — covered by global arch test, but add module-specific rule if entity has unusual patterns.
- `No_Raw_SQL` — module code has no `FromSqlRaw` or string-concat SQL.
- If module uses Dapper, `Dapper_Uses_IDapperContext` — never `SqlConnection` directly.

### G. Documentation + registration

**20.** `src/ErpSaas.Modules.{Module}/README.md` — use `templates/ModuleReadme.md.tpl`. Must contain:
- Module name + plan § reference
- DB schema + DB tier
- Entity list
- Endpoint list (URL + permission + feature)
- DDL keys used
- Permissions & features introduced
- Sequence definitions registered
- Integrations (external APIs called)
- Reports wired
- How to run this module's tests only

**21.** Add entry to top-level `docs/module-index.md` — one-line summary linking to the module README.

## Final step — verify

Before declaring the scaffold done:

```bash
# Build
dotnet build src/ErpSaas.sln

# Arch tests (these will catch scaffolding mistakes)
dotnet test src/ErpSaas.Tests.Arch

# New module's empty tests compile + run
dotnet test src/ErpSaas.Tests.Unit --filter "FullyQualifiedName~{Module}"

# Verify the module shows up in service catalog
curl http://localhost:5000/api/services | jq '.[] | select(.code=="{MODULE_CODE}")'

# Verify the menu items were seeded
curl -H "Authorization: Bearer {token}" http://localhost:5000/api/menu/tree | jq
```

If any step fails, **fix before moving on**. A half-scaffolded module is worse than an unstarted one — it hides missing pieces under plausible-looking code.

## Anti-patterns to reject

- Creating a module without its schema declared in an `EntityTypeConfiguration`
- Writing services that don't go through `BaseService.ExecuteAsync`
- Skipping any of the six test classes because "it's obvious" or "we'll add later"
- Seeding permissions in the wrong seeder (tenant vs system)
- Forgetting to update `docs/module-index.md`
- Registering a ServiceDescriptor with a category that doesn't match `ServiceCategory` enum
- Adding entities to a module whose schema is already owned by a different module (if Billing owns `sales`, don't let Inventory put `Product` there)

## When in doubt

- Read the module's `§X.Y` in `docs/MASTER_PLAN.md` once more — every field of every entity is listed there.
- Copy from `src/ErpSaas.Modules.Billing/` — it's the reference module; every pattern is there.
- Write to `docs/open-questions.md` and ask the human.
