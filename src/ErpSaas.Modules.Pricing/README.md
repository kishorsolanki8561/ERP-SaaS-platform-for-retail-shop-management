# ErpSaas.Modules.Pricing

Schema: `pricing` | Phase: 3 | Plan: §6.11

## Entities
- `DiscountRule` — percentage or flat discount applied by product/category/customer-type or date range
- `ExtraChargeRule` — surcharge or levy added to invoice total (e.g., packaging, handling)
- `Offer` — bundled promotional offer (buy-X-get-Y, minimum-order-discount)

## Services
- `IPricingEngine` (singleton, pure) — `CalculateAsync(PricingContext)` → `PricingResult`; no DB writes
- `IPricingManagementService` — CRUD for DiscountRule, ExtraChargeRule, Offer

## Endpoints (8)
| Method | Route | Permission |
|--------|-------|-----------|
| POST | `/api/pricing/discount-rules` | Pricing.Manage |
| GET | `/api/pricing/discount-rules` | Pricing.View |
| PUT | `/api/pricing/discount-rules/{id}` | Pricing.Manage |
| DELETE | `/api/pricing/discount-rules/{id}` | Pricing.Manage |
| POST | `/api/pricing/extra-charge-rules` | Pricing.Manage |
| GET | `/api/pricing/extra-charge-rules` | Pricing.View |
| POST | `/api/pricing/offers` | Pricing.Manage |
| GET | `/api/pricing/offers` | Pricing.View |

## Permissions
`Pricing.View`, `Pricing.Manage`

## Engine contract
`IPricingEngine.CalculateAsync` is injected into `BillingService` to compute live invoice totals. The engine is pure — 100% unit-test covered. Cross-category conversions are not supported.
