# ErpSaas.Modules.Transport

Schema: `transport` | Phase: 3 | Plan: §6.12

## Entities
- `TransportProvider` — logistics company or in-house fleet record
- `Vehicle` — vehicle belonging to a provider (plate, type, capacity)
- `Delivery` — a single dispatch linked to a delivery challan; tracks status through Scheduled → PickedUp → InTransit → Delivered / Failed
- `DeliveryLog` — timestamped status-change events on a Delivery

## Services
- `ITransportService` — CreateProvider, ListProviders, CreateVehicle, ListVehicles, ScheduleDelivery, UpdateDeliveryStatus, GetDelivery, ListDeliveries

## Endpoints (9)
| Method | Route | Permission |
|--------|-------|-----------|
| POST | `/api/transport/providers` | Transport.Manage |
| GET | `/api/transport/providers` | Transport.View |
| POST | `/api/transport/vehicles` | Transport.Manage |
| GET | `/api/transport/vehicles` | Transport.View |
| POST | `/api/transport/deliveries` | Transport.Dispatch |
| GET | `/api/transport/deliveries` | Transport.View |
| GET | `/api/transport/deliveries/{id}` | Transport.View |
| PUT | `/api/transport/deliveries/{id}/status` | Transport.Dispatch |
| GET | `/api/transport/deliveries/{id}/log` | Transport.View |

## Permissions
`Transport.View`, `Transport.Manage`, `Transport.Dispatch`
