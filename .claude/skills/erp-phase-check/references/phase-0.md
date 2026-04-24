# Phase 0 — Foundation — Exit-Gate Extras

> **Goal:** Build the cross-cutting spine. Every later module assumes these pieces already work.
> **Duration:** 3–4 weeks (plan §11 Phase 0 — slightly expanded for testing + multi-DB + masters)
> **Plan references:** §4, §5.1–5.16, §6.1, §6.15, §6.26, §7.5, §13.

## What should exist at end of Phase 0

### Projects & structure
- [ ] Solution `src/ErpSaas.sln` with all 10+ projects wired (Api, Core, Shared, Infrastructure, Modules.Billing, Modules.Identity, Modules.Masters, Tests.Unit, Tests.Integration, Tests.Arch)
- [ ] `docs/MASTER_PLAN.md` committed at repo root
- [ ] `CLAUDE.md` at repo root
- [ ] `.claude/skills/` present
- [ ] GitHub repo configured: branch protection on `main`, PR review required, CI status required

### Cross-cutting framework (§5.1–5.16)
- [ ] `BaseService.ExecuteAsync` implemented + unit-tested (every branch: Success, OperationCanceled, DbUpdateConcurrency, Validation, Generic exception)
- [ ] `Result<T>` + `.ToActionResult()` extension + controller → HTTP mapping
- [ ] `ErrorLog` table in LogDB, populated by any thrown exception via middleware
- [ ] `AuditLog` table in LogDB, `[Auditable]` attribute + `AuditSaveChangesInterceptor` running
- [ ] `ThirdPartyApiLog` table in LogDB, `ThirdPartyApiClientBase` + `DelegatingHandler` logging every call
- [ ] DDL engine: `DdlCatalog`, `DdlItem`, `DdlItemTenant` tables in PlatformDB+TenantDB; `GET /api/ddl/{key}` + `GET /api/ddl/batch?keys=` endpoints; Angular DdlKeyStore signal store; `<app-ddl-dropdown>` component
- [ ] RBAC + subscription gate: `[RequirePermission]` and `[RequireFeature]` attributes working; `*hasPermission` and `*hasFeature` Angular directives
- [ ] Dynamic menu engine: `MenuItem` + `MenuItemTenantOverride` tables; `GET /api/menu/tree` returns permission+feature-filtered tree
- [ ] `ISequenceService` + `usp_AllocateSequenceNumber` stored procedure deployed; concurrency test (100 parallel) passes
- [ ] `IDapperContext` registered with all 4 tier accessors; `DapperLoggingInterceptor` writes to `SlowQueryLog`
- [ ] `ISqlObjectMigrator` deploys `.sql` files from `/src/ErpSaas.Infrastructure/Sql/`, tracks SHA-256 per file
- [ ] File upload engine: `FileUploadConfig` + `UploadedFile` tables, `IFileStorage` abstraction (local dev, Azure Blob prod)
- [ ] Messaging config: `MessagingConfig` + `NotificationTemplate` + `NotificationQueue` tables; Hangfire worker drains queue
- [ ] Testing framework: xUnit + Testcontainers + WAF + Respawn + Bogus wired; one sample of each test layer passing (unit, integration, arch, tenant-isolation, E2E)
- [ ] Service catalog: `IServiceCatalog` registry populated by every module's DI registration; `/` HTML landing page + `/api/services` JSON endpoint + `/api/health` + `/api/version` all return 200

### Database tier (§4.5 + §4.6)
- [ ] 7 physical DBs provisioned locally (docker-compose):
  - [ ] PlatformDB
  - [ ] TenantDB (with default standard-tier connection)
  - [ ] AnalyticsDB
  - [ ] LogDB
  - [ ] NotificationsDB
  - [ ] MarketplaceEventsDB
  - [ ] SyncDB
- [ ] All 17+ schemas created in TenantDB (empty schemas for modules not yet built are fine): `sales`, `inventory`, `accounting`, `wallet`, `warranty`, `pricing`, `transport`, `marketplace`, `hr`, `payment`, `reporting`, `branding`, `hardware`, `masters`, `ca`, `service`, `sequence`, plus any others per §4.6
- [ ] `DbContextRegistry` routes contexts per `ITenantContext.ShopId`
- [ ] `IShopConnectionResolver` returns default standard-tier connection for all shops (premium tier comes in Phase 5)
- [ ] Arch test `Schema_Ownership_Matches_Module` passes

### Identity + Shops + Subscriptions (§6.1) — minimum viable
- [ ] Multi-identifier login (username / email / phone)
- [ ] First-time invite flow (separate from forgot-password)
- [ ] Self-service profile + password change + email change with re-verification
- [ ] Admin user management (invite, deactivate, force-reset, unlock)
- [ ] JWT access (15 min) + refresh (30 days rotating) + optional TOTP 2FA
- [ ] `Shop` + `User` + `UserShop` + `UserSecurityToken` entities
- [ ] `SubscriptionPlan` + `SubscriptionPlanFeature` + `ShopSubscription` seeded with at least Starter / Growth / Enterprise
- [ ] Shop onboarding service: creates shop + admin user + trial subscription + tenant seeds in one transaction

### Master Data (§6.14) — platform masters only
- [ ] `Country` (all 195 seeded)
- [ ] `State` (Indian states + UTs with GST state codes)
- [ ] `City` (top 500 Indian cities)
- [ ] `Currency` (INR + top 10)
- [ ] `HsnSacCode` (~200 electrical / electronic rows seeded)
- [ ] Generic `MasterDataService<T>` + `<app-master-page>` Angular component
- [ ] CRUD working for Country / State / City via admin UI

### Angular shell
- [ ] `src/web` workspace with auth layout + app layout
- [ ] HTTP interceptors: Auth, Tenant, Error, Loading
- [ ] Signal stores for DDL cache + user profile + menu tree
- [ ] Route guards: `PermissionGuard`, `FeatureGuard`, `AuthGuard`
- [ ] Shared components: `<app-ddl-dropdown>`, `<app-data-table>`, `<app-form-field>`, `<app-page-header>`, `<app-confirm-dialog>`
- [ ] Login + forgot-password + profile + admin/users + admin/shop-profile pages

### Customer Portal (§6.27) — skeleton only
- [ ] Separate `src/web-portal` workspace at `portal.*` subdomain
- [ ] OTP-based signup + login flow with Cloudflare Turnstile CAPTCHA
- [ ] `PlatformCustomer` + `CustomerLink` + `CustomerLoginSession` tables in PlatformDB
- [ ] Customer can log in and see empty-state `/portal/` + `/portal/profile`
- [ ] Purchase history / shop list come in Phase 1; the skeleton is enough for Phase 0

### Product Owner bootstrap (§6.26)
- [ ] `PlatformBootstrapSeeder` runs on first startup; checks for existing Product Owner; seeds from env vars if absent
- [ ] `POST /api/bootstrap/register-product-owner` available only when no owner exists
- [ ] `GET /api/bootstrap/status` returns `hasOwner` boolean
- [ ] Minimal `/platform/overview` page accessible to the Product Owner

### CAPTCHA (§9.4)
- [ ] `ICaptchaService` interface + Cloudflare Turnstile provider implementation
- [ ] `[RequireCaptcha]` attribute working
- [ ] `EveryPublicAuthEndpoint_HasCaptchaGuard` arch test passing

### DevOps
- [ ] `ops/docker-compose.yml` spins the whole local stack in one command: API + all 7 DBs + Redis + MailHog + Azurite
- [ ] GitHub Actions CI: restore → build → unit → arch → integration (Testcontainers) → coverage → lint → E2E
- [ ] PR template + branch-protection rules
- [ ] Deployed to a staging environment (Azure App Service or equivalent) with real domain names for `app.` and `portal.` and `api.`
- [ ] Secrets in Azure Key Vault (no secrets in repo)

## Phase 0 exit gate demo

Before closing Phase 0, do this demo live in front of the human product owner (or record it):

1. `git clone` on a fresh machine.
2. `docker compose up -d` — wait for healthy.
3. `dotnet run --project src/ErpSaas.Api` — API starts, seeds run, shows "Phase 0 ready" log line.
4. Browse to `/` (landing page) — services grid renders, shows all registered modules.
5. Browse to `/swagger` — Swagger UI lists every endpoint.
6. Bootstrap a Product Owner via the one-time form — login works, TOTP challenge prompts.
7. As Product Owner, create a new shop + admin user — onboarding transaction completes, tenant seeds ran.
8. Log in as that shop admin. Sees permission-filtered menu (with empty modules for things not yet built).
9. Create a Country in masters — CRUD page works, AuditLog row created.
10. Log in as a customer via portal subdomain using OTP — lands on portal home with empty state.
11. Run `dotnet test src/ErpSaas.sln` — every test green.
12. Run `dotnet test src/ErpSaas.Tests.Arch` — every arch rule green.
13. Run `erp-phase-check` skill — every of the 12 universal checks + all Phase 0 extras green.

Only after all 13 demo steps pass is Phase 0 closed.

## Common Phase 0 mistakes to avoid

- **Rushing `BaseService` because "it's just a try/catch."** It's the spine; bugs here cascade into every later module. Unit-test it exhaustively.
- **Putting seeders in the wrong class.** System seeders (`ISystemSeeder`) run on every deploy; tenant seeders (`ITenantSeeder`) run once per shop at onboarding. Getting this wrong means new shops have missing defaults or every shop re-inserts platform data.
- **Skipping the arch tests "because we'll add them later."** The arch tests are how you keep the next 28 modules consistent. Writing them in Phase 0 costs a day; retrofitting them later costs weeks.
- **Treating the Service Catalog as optional.** The API landing page and module discovery depend on it. A module without a `ServiceDescriptor` is invisible to the platform admin dashboard and the marketing site.
- **Using EF in-memory provider for integration tests.** Its semantics diverge from SQL Server (no foreign-key enforcement, different query translation). Use Testcontainers.
- **Not setting up staging.** Phase 0 isn't done until something is deployed somewhere real. Local-only Phase 0 hides deployment problems until Phase 3.
