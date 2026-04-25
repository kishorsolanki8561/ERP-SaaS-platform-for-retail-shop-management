# ErpSaas.Modules.Masters

Platform-level master data — countries, states, cities, currencies, HSN/SAC codes,
and the dropdown catalog (`DdlCatalog`) used everywhere in the UI.

## Responsibilities

- Serves `GET /api/masters/countries`, `/states`, `/cities`, `/currencies`, `/hsn`
- Serves `GET /api/ddl/{key}` — generic dropdown values consumed by `<app-ddl-dropdown>`
- Seeds all platform-level reference rows (countries, Indian states, common HSN codes)

## Owned tables (PlatformDB)

All master entities live in `PlatformDB`, not `TenantDB`, because they are
shared across all tenants.

| Entity | Schema | Notes |
|---|---|---|
| `Country` | `masters` | Seeded with 250 countries |
| `State` | `masters` | Indian states + UTs seeded |
| `City` | `masters` | Optional; linked to State |
| `Currency` | `masters` | ISO 4217 codes |
| `HsnCode` | `masters` | HSN/SAC with default GST rate |
| `DdlCatalog` | `masters` | All dropdown option sets |

## Dropdown catalog keys (§3.2)

All dropdown keys follow `SCREAMING_SNAKE_CASE`. Examples:

| Key | Used by |
|---|---|
| `PAYMENT_MODE` | Billing, Wallet |
| `INVOICE_STATUS` | Billing |
| `SHIFT_STATUS` | Shift |
| `MOVEMENT_TYPE` | Inventory |
| `CUSTOMER_TYPE` | CRM |
| `UNIT_TYPE` | Inventory |

## Permissions

Master data endpoints are read-only for all authenticated users. No
`[RequirePermission]` beyond standard auth on read endpoints.

## Wiring

1. Call `services.AddMastersModule()` after `AddInfrastructure()`.
2. `MasterDataSeeder` runs automatically via `DatabaseSeeder` during startup.
3. Add a project reference from `ErpSaas.Api` to `ErpSaas.Modules.Masters`.
