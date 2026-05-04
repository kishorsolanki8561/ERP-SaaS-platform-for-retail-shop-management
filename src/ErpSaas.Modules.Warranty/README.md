# ErpSaas.Modules.Warranty

Schema: `warranty` | Phase: 3 | Plan: §6.10

## Entities
- `WarrantyRegistration` — product-level warranty record linked to an invoice line
- `WarrantyClaim` — customer-initiated claim against a registration (status: Open → InProgress → Resolved/Rejected)

## Services
- `IWarrantyService` — Register, GetBySerial, ListRegistrations, SubmitClaim, UpdateClaim, ListClaims

## Endpoints (7)
| Method | Route | Permission |
|--------|-------|-----------|
| POST | `/api/warranty/registrations` | Warranty.Register |
| GET | `/api/warranty/registrations` | Warranty.View |
| GET | `/api/warranty/registrations/{id}` | Warranty.View |
| GET | `/api/warranty/registrations/serial/{serial}` | Warranty.View |
| POST | `/api/warranty/claims` | Warranty.Claim |
| GET | `/api/warranty/claims` | Warranty.View |
| PUT | `/api/warranty/claims/{id}` | Warranty.Manage |

## Permissions
`Warranty.View`, `Warranty.Register`, `Warranty.Claim`, `Warranty.Manage`

## DDL catalogs used
`WARRANTY_STATUS` — Active, Claimed, Expired, Voided

## Jobs
None — warranty expiry transitions handled at query time via `ExpiresAtUtc`.
