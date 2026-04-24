# Module: {Module Name}

**Plan reference:** `docs/MASTER_PLAN.md` §{X.Y}

One-line purpose of this module.

## Scope

What this module owns (entities, flows, business rules). Three bullets max.

## Database

- **DB tier:** `TenantDB` | `PlatformDB` | `LogDB` | `{ExtractedDB}`
- **SQL schema:** `{schema}` — all entities in this module use `ToTable(..., schema: {Module}Module.Schema)`.
- **Cross-schema dependencies:** FKs to `{other.schema}` only for read (via `I{Other}Service`). No direct writes.

## Entities

| Entity | Plan § | Notes |
|---|---|---|
| `{Entity1}` | §{X.Y} | Brief purpose |
| `{Entity2}` | §{X.Y} | |

## API Endpoints

| Method + Path | Permission | Feature | Purpose |
|---|---|---|---|
| `GET /api/{resource}/{id}` | `{Module}.View` | — | Fetch one |
| `POST /api/{resource}` | `{Module}.Create` | `{Module}.{Feature}` | Create |
| `PATCH /api/{resource}/{id}` | `{Module}.Edit` | — | Update |
| `DELETE /api/{resource}/{id}` | `{Module}.Delete` | — | Soft-delete |

## DDL catalogs consumed

- `{CATALOG_CODE}` — values used in `{Field}`
- `{CATALOG_CODE}` — values used in `{Field}`

## Permissions introduced

- `{Module}.View` — read access
- `{Module}.Create` — create new records
- `{Module}.Edit` — modify existing (excluding finalized)
- `{Module}.Delete` — soft-delete
- `{Module}.{OtherAction}` — other semantic action

## Features introduced

- `{Module}.{Feature}` — gated on {plan tier}
- `{Module}.{Feature2}` — gated on {plan tier}

## Sequence definitions registered

| Code | Default format | Reset rule |
|---|---|---|
| `{CODE}` | `{PREFIX}-{FY}-{SEQ:6}` | FinancialYear |

## External integrations

- `{Provider}` — purpose — routed through `ThirdPartyApiClientBase`

## Reports

- `{ReportCode}` — registered in the reporting catalog (§5.12)

## How to run tests for this module only

```bash
dotnet test src/ErpSaas.Tests.Unit        --filter "FullyQualifiedName~{Module}"
dotnet test src/ErpSaas.Tests.Integration --filter "FullyQualifiedName~{Module}"
dotnet test src/ErpSaas.Tests.Arch        --filter "FullyQualifiedName~{Module}"
```

## Open questions / deferred

- Anything deferred to a later phase
- Anything that needs a product-owner decision before completion

## Change log

| Date | Author | Change |
|---|---|---|
| YYYY-MM-DD | {Who} | Module created |
