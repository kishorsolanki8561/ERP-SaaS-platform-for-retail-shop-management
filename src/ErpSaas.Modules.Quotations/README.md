# ErpSaas.Modules.Quotations

Schema: `quotations` | Phase: 3 | Plan: §6.7

## Entities
- `Quotation` + `QuotationLine` — customer price estimate; status: Draft → Sent → Accepted / Rejected / Expired
- `SalesOrder` + `SalesOrderLine` — confirmed order converted from accepted quotation
- `DeliveryChallan` + `DeliveryChallanLine` — dispatch document against a sales order

## Workflow
`Quotation (Accepted) → SalesOrder → DeliveryChallan → Invoice (via Billing module)`

## Services
- `IQuotationsService` — CreateQuotation, SendQuotation, AcceptQuotation, RejectQuotation, ConvertToSalesOrder, CreateDeliveryChallan, ListQuotations, ListSalesOrders, ListDeliveryChallans

## Endpoints (12)
| Method | Route | Permission |
|--------|-------|-----------|
| POST | `/api/quotations` | Quotation.Create |
| GET | `/api/quotations` | Quotation.View |
| GET | `/api/quotations/{id}` | Quotation.View |
| PUT | `/api/quotations/{id}/send` | Quotation.Manage |
| PUT | `/api/quotations/{id}/accept` | Quotation.Manage |
| PUT | `/api/quotations/{id}/reject` | Quotation.Manage |
| POST | `/api/quotations/{id}/convert` | Quotation.Manage |
| GET | `/api/sales-orders` | Quotation.View |
| GET | `/api/sales-orders/{id}` | Quotation.View |
| POST | `/api/delivery-challans` | Quotation.Dispatch |
| GET | `/api/delivery-challans` | Quotation.View |
| GET | `/api/delivery-challans/{id}` | Quotation.View |

## Permissions
`Quotation.View`, `Quotation.Create`, `Quotation.Manage`, `Quotation.Dispatch`

## Document numbers
Quotation, SalesOrder, and DeliveryChallan numbers are all issued via `ISequenceService`.
