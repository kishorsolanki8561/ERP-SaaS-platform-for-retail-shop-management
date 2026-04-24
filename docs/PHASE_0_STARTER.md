# PHASE 0 STARTER — Where to put your hands on Day 1

> **Goal:** take the 7,000-line plan and turn it into committed code in a working repo, on a schedule you can actually keep. This file is the concrete "what do I do Monday morning" guide.

---

## Before you write any code — the eight decisions

The plan (§13.1) lists 17 decisions to make before first commit. These eight **cannot be deferred** — every later decision depends on them.

| # | Decision | My choice |
|---|---|---|
| 1 | Cloud host | ___ (Azure recommended for .NET + SQL Server fit) |
| 2 | Primary domain | ___ (e.g., `mycompany.com`) |
| 3 | Subdomain strategy | `app.` + `api.` + `portal.` + `docs.` (recommended) |
| 4 | SQL Server edition for prod | ___ (Azure SQL Managed Instance recommended) |
| 5 | Payment gateway (India) | ___ (Razorpay recommended) |
| 6 | SMS provider (India, DLT) | ___ (MSG91 recommended) |
| 7 | Email provider | ___ (Postmark or AWS SES recommended) |
| 8 | Product Owner email (you) | ___ (this seeds the first account) |

Write your answers in `docs/decisions/0001-infrastructure.md`. Commit that file before writing any code.

---

## Week 1 — Scaffold the repo and the cross-cutting spine

The first week is all scaffolding. You will not ship a feature yet — that's Phase 1. The goal this week is: **a fresh machine can clone the repo, run `docker compose up`, hit a URL, and see the ERP's empty landing page with arch tests passing.**

### Monday — Project setup (8 hours)

```bash
# 1. Create the repo
mkdir erp-saas && cd erp-saas
git init
git branch -M main

# 2. Drop in the reference files
cp {downloaded}/CLAUDE.md .
mkdir -p docs && cp {downloaded}/MASTER_PLAN.md docs/
cp -r {downloaded}/.claude .
cp {downloaded}/PHASE_0_STARTER.md docs/

# 3. Create the .NET solution skeleton
dotnet new sln -n ErpSaas
mkdir -p src
cd src

dotnet new webapi      -n ErpSaas.Api           --framework net8.0 --use-program-main false --use-controllers
dotnet new classlib    -n ErpSaas.Core          --framework net8.0
dotnet new classlib    -n ErpSaas.Shared        --framework net8.0
dotnet new classlib    -n ErpSaas.Infrastructure --framework net8.0
dotnet new classlib    -n ErpSaas.Modules.Identity   --framework net8.0
dotnet new classlib    -n ErpSaas.Modules.Masters    --framework net8.0
dotnet new xunit       -n ErpSaas.Tests.Unit   --framework net8.0
dotnet new xunit       -n ErpSaas.Tests.Integration --framework net8.0
dotnet new xunit       -n ErpSaas.Tests.Arch   --framework net8.0

cd ..
dotnet sln add src/**/*.csproj

# Reference graph
cd src
dotnet add ErpSaas.Shared/ErpSaas.Shared.csproj reference ErpSaas.Core/ErpSaas.Core.csproj
dotnet add ErpSaas.Infrastructure/ErpSaas.Infrastructure.csproj reference ErpSaas.Shared/ErpSaas.Shared.csproj
dotnet add ErpSaas.Modules.Identity/ErpSaas.Modules.Identity.csproj reference ErpSaas.Infrastructure/ErpSaas.Infrastructure.csproj
dotnet add ErpSaas.Modules.Masters/ErpSaas.Modules.Masters.csproj  reference ErpSaas.Infrastructure/ErpSaas.Infrastructure.csproj
dotnet add ErpSaas.Api/ErpSaas.Api.csproj reference ErpSaas.Modules.Identity/ErpSaas.Modules.Identity.csproj
dotnet add ErpSaas.Api/ErpSaas.Api.csproj reference ErpSaas.Modules.Masters/ErpSaas.Modules.Masters.csproj

# 4. First commit
cd ..
git add . && git commit -m "chore: initial solution skeleton per MASTER_PLAN §2"
```

**Monday afternoon:** add NuGet packages via `dotnet add package`:
- EFCore (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`) to Infrastructure
- Dapper to Infrastructure
- FluentValidation.AspNetCore to Core
- Hangfire (+ Hangfire.SqlServer) to Api
- Serilog (+ sinks: Seq, Console) to Api
- Testcontainers.MsSql, Respawn, Bogus, NetArchTest.Rules, FluentAssertions to all three test projects

Commit. End of Monday.

### Tuesday — Docker Compose + databases (8 hours)

Create `ops/docker-compose.yml` that spins up:
- SQL Server 2022 Developer (one container, 7 databases on it)
- Redis
- MailHog (for local email testing)
- Azurite (for local blob storage)
- Seq (for local log aggregation)

Sample shape (full file in `docs/phase-0/docker-compose.reference.yml`):

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${SQLSERVER_SA_PASSWORD}"
    ports: ["1433:1433"]
    volumes: ["mssql-data:/var/opt/mssql"]
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S . -U sa -P $$MSSQL_SA_PASSWORD -Q "SELECT 1"
      interval: 10s
  redis:    { image: redis:7-alpine,        ports: ["6379:6379"] }
  mailhog:  { image: mailhog/mailhog,       ports: ["1025:1025","8025:8025"] }
  azurite:  { image: mcr.microsoft.com/azure-storage/azurite, ports: ["10000:10000"] }
  seq:      { image: datalust/seq,          ports: ["5341:80"], environment: { ACCEPT_EULA: "Y" } }
volumes:
  mssql-data:
```

Create `ops/create-databases.sql` that creates the 7 databases on first SQL Server startup:
```sql
IF DB_ID('PlatformDb') IS NULL CREATE DATABASE PlatformDb;
IF DB_ID('TenantDb') IS NULL CREATE DATABASE TenantDb;
IF DB_ID('AnalyticsDb') IS NULL CREATE DATABASE AnalyticsDb;
IF DB_ID('LogDb') IS NULL CREATE DATABASE LogDb;
IF DB_ID('NotificationsDb') IS NULL CREATE DATABASE NotificationsDb;
IF DB_ID('MarketplaceEventsDb') IS NULL CREATE DATABASE MarketplaceEventsDb;
IF DB_ID('SyncDb') IS NULL CREATE DATABASE SyncDb;
```

Wire `docker-compose` to run this on container start. Commit. End of Tuesday.

### Wednesday — BaseEntity, TenantEntity, and the four DbContexts (8 hours)

Implement in `src/ErpSaas.Shared/Data/`:
- `BaseEntity` (Id, CreatedAtUtc, CreatedByUserId, UpdatedAtUtc, UpdatedByUserId, IsDeleted, RowVersion)
- `TenantEntity : BaseEntity` (adds ShopId)
- `ITenantContext` (ShopId, CurrentUserId, CurrentUserRoles)
- `AuditSaveChangesInterceptor` (stamps audit columns)
- `TenantSaveChangesInterceptor` (stamps ShopId from ITenantContext)

Create the four DbContexts in `src/ErpSaas.Infrastructure/`:
- `PlatformDbContext`, `TenantDbContext`, `AnalyticsDbContext`, `LogDbContext`

Each context registers its entities via `OnModelCreating` with schema declarations per §4.6. Start with empty entity sets — they fill up as modules land.

Apply a global query filter on every `TenantEntity`-derived type in `TenantDbContext.OnModelCreating`:
```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var shopIdProp = Expression.Property(parameter, nameof(TenantEntity.ShopId));
        var ctxShopId = Expression.Property(
            Expression.Constant(tenantContext), nameof(ITenantContext.ShopId));
        var lambda = Expression.Lambda(Expression.Equal(shopIdProp, ctxShopId), parameter);
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
    }
}
```

Commit. End of Wednesday.

### Thursday — BaseService + Result + error/audit/third-party logs (8 hours)

Implement in `src/ErpSaas.Shared/Services/`:
- `Result<T>` + `Result` + static factory methods (Success, NotFound, Conflict, Validation, Forbidden, Cancelled)
- `ResultExtensions.ToActionResult()` for controllers
- `IBaseService` + `BaseService.ExecuteAsync` — the full try/catch/transaction/audit/error flow
- `IErrorLogger` + `ErrorLogger` — writes to `LogDbContext.ErrorLogs` via non-blocking `Channel`
- `IAuditLogger` + `AuditLogger` + `[Auditable]` attribute — writes to `LogDbContext.AuditLogs`
- `ThirdPartyApiClientBase` + `LoggingDelegatingHandler` — every external call logged

Write the first four arch tests in `src/ErpSaas.Tests.Arch/`:
```csharp
[Fact] public void NoSaveChangesAsyncOutsideBaseService() { ... }
[Fact] public void NoRawHttpClientOutsideThirdPartyApiClientBase() { ... }
[Fact] public void EveryTenantEntityHasGlobalQueryFilter() { ... }
[Fact] public void EveryEntityHasSchemaDeclared() { ... }
```

Write the first unit test: `BaseServiceTests` covers Success, OperationCanceled, DbUpdateConcurrency, Validation, Generic exception branches — 5 tests. Run `dotnet test`. All green? Commit. End of Thursday.

### Friday — DDL engine, Sequence service, Service catalog (8 hours)

**Morning (4h):** DDL engine (§5.5)
- `DdlCatalog`, `DdlItem`, `DdlItemTenant` entities in Infrastructure
- `IDdlService` with memory-cached `GetItemsAsync(key, shopId, parentCode?)`
- `/api/ddl/{key}` + `/api/ddl/batch` controller
- Seed 5 basic catalogs: `PAYMENT_MODE`, `INVOICE_STATUS`, `WARRANTY_STATUS`, `STOCK_MOVEMENT_TYPE`, `CUSTOMER_TYPE`

**Afternoon (4h):** Sequence service (§5.16) + Service catalog (§5.14)
- `SequenceDefinition`, `SequenceCounter` tables in `sequence` schema of TenantDB
- `SequenceAllocation` table in LogDB
- `/src/ErpSaas.Infrastructure/Sql/usp_AllocateSequenceNumber.sql` — deploy via `ISqlObjectMigrator`
- `ISequenceService.NextAsync` wrapping the sproc via Dapper
- Write the concurrency test: 100 parallel `NextAsync("TEST", ...)` — assert 100 unique monotonic values
- `IServiceCatalog` + `ServiceDescriptor` + `AddServiceCatalogEntry` extension
- `/api/services` controller — returns the registered catalog
- `/` controller — renders a minimal Razor HTML page listing registered services

Demo: start the API. Open `http://localhost:5000/`. See "ERP SaaS Platform — services: DDL, Sequence, ServiceCatalog." Hit `/api/services` — see the JSON. Hit `/swagger` — see every endpoint.

Commit. Weekly retro. End of Week 1.

---

## Week 2 — Identity + RBAC + Subscription gate + Angular shell

### Monday — Identity entities + auth flow

- `User`, `UserShop`, `UserSecurityToken` entities (PlatformDB)
- `Role`, `Permission`, `RolePermission`, `UserRole` (PlatformDB, §5.7)
- `SubscriptionPlan`, `SubscriptionPlanFeature`, `ShopSubscription` (PlatformDB, §6.1)
- Seed Starter / Growth / Enterprise plans with feature sets
- Password hashing (BCrypt cost 12), JWT issuance + refresh token rotation

### Tuesday — `[RequirePermission]` + `[RequireFeature]` attributes + middleware

- `RequirePermissionAttribute` on controllers — reads permission codes from packed JWT claim
- `RequireFeatureAttribute` — checks the shop's active subscription
- `TenantContextMiddleware` — reads `shop_id` claim, populates `ITenantContext`
- Unit tests for the attributes, integration test stubs

### Wednesday — Dynamic menu engine (§5.8)

- `MenuItem`, `MenuItemTenantOverride` tables
- `GET /api/menu/tree` — returns permission+feature-filtered tree
- Seed the menu for the modules that will exist in Phase 1 (empty Kind=Page items with the right permission codes even though the pages aren't built yet)

### Thursday — Angular shell (§8)

- `ng new src/web --routing --style=scss --standalone`
- Install PrimeNG + PrimeIcons + tailwindcss
- Auth layout + app layout
- `AuthService` + auth interceptor + refresh logic using signals
- `DdlKeyStore` signal store (§5.5)
- `<app-ddl-dropdown>` component
- Login page + forgot-password page (with Cloudflare Turnstile)

### Friday — Angular shell continued + Product Owner bootstrap

- `<app-data-table>` server-driven wrapper
- `<app-form-field>`, `<app-page-header>`, `<app-confirm-dialog>`
- `PermissionGuard` + `FeatureGuard` route guards
- Menu rendering + shell layout
- `PlatformBootstrapSeeder` for Product Owner (§6.26)
- `POST /api/bootstrap/register-product-owner` endpoint
- First end-to-end flow: fresh DB → bootstrap PO → PO logs in → sees empty platform dashboard

---

## Week 3 — Master Data + Customer Portal skeleton + DevOps

### Monday–Tuesday — Master Data Management (§6.14)

- `Country`, `State`, `City`, `Currency`, `HsnSacCode` entities in PlatformDB masters schema
- Seed data — all 195 countries, all 36 Indian states + UTs with GST codes, top 500 Indian cities, top 10 currencies, 200 common HSN codes
- Generic `MasterDataService<T>` + `MasterDefinition` registry
- `/api/masters/{code}` + `/api/masters/{code}/{id}` + import/export endpoints
- `<app-master-page>` generic Angular component that renders form + grid from `MasterDefinition`
- Admin UI at `/admin/masters` + `/admin/masters/countries` etc.

### Wednesday — Customer Portal skeleton (§6.27)

- `src/web-portal` workspace (separate Angular app)
- `PlatformCustomer`, `CustomerLink`, `CustomerLoginSession` entities in PlatformDB
- OTP-based signup + login (Turnstile CAPTCHA gated)
- Portal shell: layout, nav, placeholder `/portal/` home page
- `/api/portal/auth/*` endpoints

### Thursday — CAPTCHA + Security hardening (§9)

- `ICaptchaService` + Cloudflare Turnstile implementation
- `[RequireCaptcha]` attribute
- `EveryPublicAuthEndpoint_HasCaptchaGuard` arch test
- Rate limiting (`AspNetCoreRateLimit`) — auth endpoints 5/min/IP
- Security headers middleware: CSP, HSTS, X-Frame-Options, X-Content-Type-Options
- AES-256 `ValueConverter` for sensitive columns; KEK in local user-secrets for dev

### Friday — DevOps + CI/CD

- GitHub Actions workflow: restore → build → unit → arch → integration (Testcontainers) → coverage report on PR
- PR template
- Branch protection on `main`: require PR, require CI green, require 1 review
- Deploy to staging (Azure App Service + Azure SQL — create resources via Bicep in `ops/azure/`)
- Configure secrets in Azure Key Vault
- Custom domains `staging-api.` + `staging-app.` + `staging-portal.` + HTTPS certs via Let's Encrypt / Azure Front Door
- Smoke test in staging — same bootstrap + login flow

---

## Week 4 (optional buffer) — Catch-up, polish, Phase 0 close

Plan for overruns. Common leaks into Week 4:
- Testcontainers + CI flakes (local Docker differences)
- Azure App Service deployment quirks
- Mail delivery setup with SPF / DKIM / DMARC
- SQL Server schema differences between Developer Edition locally and Azure SQL in prod

Use this week to:
1. Run `erp-phase-check` skill. Walk every item of Phase 0's exit gate.
2. Fix anything yellow.
3. Demo to the Product Owner (yourself, for now).
4. Update `CLAUDE.md` §13 — tick Phase 0 done, set Phase 1 as current sprint.
5. Tag the commit: `git tag phase-0-complete -m "Phase 0 exit gate passed"`.

---

## Rhythm after Phase 0 — how to run Phase 1+

1. **Read the module's §X.Y in `docs/MASTER_PLAN.md`** front to back.
2. **Run the `erp-scaffold-module` skill** to create all 21 files.
3. **Run the `erp-write-service` skill** for every service method.
4. **Add tests as you go.** Not at the end.
5. **Open a PR per module sub-section** — not per module. A module like Billing is 5–8 PRs.
6. **Each PR is mergeable in isolation.** Arch tests green, new tests covering the PR's changes, no other module broken.
7. **End of phase: run `erp-phase-check`.** Every check green or the phase isn't done.

---

## Things that slow down every team at Phase 0 — budget for them

| Surprise | Budget |
|---|---|
| Testcontainers takes ~15s to pull + start SQL Server per test class | Use xUnit collection fixtures, share containers across tests |
| EF migrations conflict when multiple devs generate at the same time | One dev per migration window; always rebase before generating |
| Schema quirks between SQL Server Developer and Azure SQL Managed Instance | Run integration tests against Azure SQL in CI once per week |
| CORS between `app.` and `api.` subdomains | Nail this Day 1 — configuring it later always costs a day |
| Cloudflare Turnstile's localhost testing mode | Use the built-in `XXXX-DUMMY-SITE-KEY` in dev; switch to real key in staging |
| Mail deliverability (SMS + email going to spam) | Set up SPF / DKIM / DMARC for the email provider in Week 2 |
| `appsettings.Production.json` getting a real secret checked in by accident | Use `user-secrets` locally; Azure Key Vault in deployed envs; `git-secrets` pre-commit hook |

---

## If this is taking longer than 3–4 weeks

**Common reasons:**
1. You're scope-creeping into Phase 1 work.
2. You're re-doing things because tests were skipped.
3. You're solo and the decisions pile up.

**Not-reasons:**
1. "The plan is too big." The plan is a reference, not a to-do list.
2. "We need to rethink the architecture." Stop. The plan is correct for this problem shape. Push through.

If genuinely stuck, write the specific stuck point in `docs/open-questions.md`, commit, and reach out.

---

## The Phase 0 acceptance demo — what "done" looks like

Follow `.claude/skills/erp-phase-check/references/phase-0.md` — specifically the 13-step live demo at the bottom. Record it or do it in front of a human reviewer. Only after all 13 steps pass is Phase 0 closed, tagged, and ready for Phase 1.

---

**Good luck. Phase 0 is the hardest because nothing is built yet and every decision matters. Once the spine is in place, Phases 1 through 7 follow a template and move much faster.**
