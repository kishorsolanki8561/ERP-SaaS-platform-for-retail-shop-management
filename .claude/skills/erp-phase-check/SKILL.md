---
name: erp-phase-check
description: Verify the exit-gate criteria for a phase of the ERP + SaaS platform before declaring it done. Triggers whenever the user says "phase complete", "phase done", "ready for phase N", "mark phase X done", "check phase X", or asks about progress against the phased roadmap. Also triggers when the user is contemplating moving from one phase to the next. Use this to avoid the common failure mode of declaring a phase done when 3-4 items of the exit gate were skipped — which causes compounding gaps in later phases. Before this skill lets you pass a phase, every checklist item must be green, every test suite must pass, and the Definition of Done in `docs/MASTER_PLAN.md` §12 must be satisfied for every module shipped in the phase.
---

# Phase Exit-Gate Check

Running this before declaring a phase done is the single most valuable rule in the project. Skipping it is how good projects accumulate technical debt faster than features.

## How to use this skill

1. Identify which phase is being closed (0–7).
2. Read the corresponding `references/phase-{N}.md` for that phase's exit criteria.
3. Walk every item — **actually run the commands**, don't just nod. If a check fails, the phase is not done. Fix it before moving on.
4. Update `CLAUDE.md` § 13 current-phase tracker only after every item is green.

## The 12 checks that apply to **every phase**

These are the floor. No phase closes without all 12 green.

### 1. Build is green
```bash
dotnet build src/ErpSaas.sln --configuration Release
cd src/web && npm run build
cd src/web-portal && npm run build
```
Zero warnings-as-errors. If CI rejects it, fix it.

### 2. All tests pass
```bash
dotnet test src/ErpSaas.sln --configuration Release --verbosity minimal
cd src/web && npm test -- --ci --coverage
cd src/web && npm run e2e -- --project=chromium
```
No skipped tests except ones with a `TODO: reason` comment explaining the deferral.

### 3. Architecture tests green
```bash
dotnet test src/ErpSaas.Tests.Arch --verbosity detailed
```
Minimum rules every phase must pass:
- `No_SaveChangesAsync_Outside_BaseService`
- `Schema_Ownership_Matches_Module`
- `NoCrossModuleDbContextAccess`
- `EveryPublicAuthEndpoint_HasCaptchaGuard`
- `NoRawHttpClient_OutsideThirdPartyApiClientBase`
- `NoRawSql_InBusinessServices`
- `EveryEntity_HasSchemaDeclared`
- `EveryTenantEntity_HasGlobalQueryFilter`

### 4. Coverage thresholds met
```bash
dotnet test src/ErpSaas.sln --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html
```
- Services ≥ 80% line coverage
- Pricing engine / tax / wallet math = 100%
- Overall repo ≥ 75%
Coverage regression > 2 percentage points vs previous phase closure = phase not done.

### 5. Migrations apply cleanly from a fresh DB
```bash
docker compose -f ops/docker-compose.yml down -v
docker compose -f ops/docker-compose.yml up -d sqlserver
sleep 15
dotnet ef database update --project src/ErpSaas.Api --context PlatformDbContext
dotnet ef database update --project src/ErpSaas.Api --context TenantDbContext
dotnet ef database update --project src/ErpSaas.Api --context LogDbContext
# ... (run for every DbContext in use at this phase)
```
All migrations succeed. Test `MigrationUpDownTests` passes.

### 6. Seeds run idempotently
```bash
dotnet run --project src/ErpSaas.Api -- --seed-and-exit
dotnet run --project src/ErpSaas.Api -- --seed-and-exit   # second run
dotnet test --filter "FullyQualifiedName~SeederIdempotencyTests"
```
Second run must be a no-op (zero inserts, zero updates). Row counts identical to first run.

### 7. Stored procedures deploy idempotently
```bash
dotnet test --filter "FullyQualifiedName~SqlObjectMigratorTests"
```
Every `.sql` file deploys cleanly, hash tracked. Re-deploy is a no-op.

### 8. Every shipped module has its complete Definition of Done (§12)
For each module delivered in the phase, tick off every box in `docs/MASTER_PLAN.md` § 12. The critical non-negotiables:
- Schema declared and arch-test enforced
- All six test classes present (ServiceTests, ControllerTests, TenantIsolationTests, SubscriptionGateTests, AuditTrailTests, ArchTests)
- Permission + feature seeded + menu item seeded with all three gate layers (menu hide / route guard / API attribute)
- ServiceDescriptor registered and visible on `/api/services`
- Module README up-to-date in `src/ErpSaas.Modules.{X}/README.md`
- Analytics fields reviewed (§1 data-richness)

### 9. API landing page + service catalog reflects what was shipped
```bash
# Boot the API and hit the catalog
dotnet run --project src/ErpSaas.Api &
sleep 5
curl http://localhost:5000/api/services | jq '[.[] | .code] | sort'
```
Every module shipped this phase appears in the catalog with its tagline, docs URL, and health check.

### 10. Performance baselines held
```bash
dotnet test --filter "FullyQualifiedName~PerformanceBaselineTests"
```
No endpoint regressed > 20% on p95 vs the baseline captured at the previous phase closure.

### 11. Security posture unchanged or improved
- Every new endpoint has `[RequirePermission]` (and `[RequireCaptcha]` if public)
- Every new external API call routes through `ThirdPartyApiClientBase`
- Every new entity with sensitive data uses AES-256 via `ValueConverter`
- No new secret landed in source (run `git secrets --scan` or `trufflehog`)

### 12. `CLAUDE.md` § 13 phase tracker updated
Move the checkmark from current phase to next. Fill in the next phase's "current sprint" goal.

## Phase-specific extras

Each phase has extras on top of the 12. Read the matching file:

- `references/phase-0.md` — Foundation (cross-cutting framework + DB scaffolding + testing harness + Product Owner bootstrap)
- `references/phase-1.md` — Core Retail Loop (Identity, Inventory, Billing, Wallet core, Notifications, basic Dashboard)
- `references/phase-2.md` — Financial Core (Accounting, PO, Expenses, Reports, Reporting engine, Bank reconciliation)
- `references/phase-3.md` — Operations (Warranty, Pricing, Transport, Payment integration, Online wallet, Refund orchestrator)
- `references/phase-4.md` — HR + Marketplace
- `references/phase-5.md` — SaaS Polish (Platform admin, Branding, ITR, Subscription lifecycle, Smart Shopping)
- `references/phase-6.md` — Multi-Platform Shells (Electron, Capacitor, Offline POS)
- `references/phase-7.md` — Vertical Packs

## If a check fails

1. **Do not** argue with the check — it's the spec, you're the implementation.
2. **Do not** disable the failing test or arch rule "for now."
3. **Do not** declare partial phase completion — either it closes or it doesn't.
4. Fix the gap, re-run the relevant check, continue.
5. If the gap reveals a real misunderstanding in the plan, update `docs/MASTER_PLAN.md`, commit, and rerun.

## Output format when running the skill

Report results as a checklist with every check state:

```
Phase 0 exit-gate check — 2026-05-15
────────────────────────────────────────────
✅  1. Build green
✅  2. Tests pass (unit 847/847, integration 212/212, E2E 14/14)
❌  3. Arch tests: Schema_Ownership_Matches_Module failed — 2 entities
       in BillingModule missing ToTable(..., schema: ...) declaration
       → src/ErpSaas.Modules.Billing/Entities/PaymentMode.cs:14
       → src/ErpSaas.Modules.Billing/Entities/CustomerTier.cs:9
       Fix: add schema to EntityTypeConfiguration; re-run check.
⏭️  4. Coverage — blocked on #3
...
Phase 0 status: NOT READY (1 check blocked)
Action: Fix arch-test violations above, re-run `claude skill:erp-phase-check`.
```
