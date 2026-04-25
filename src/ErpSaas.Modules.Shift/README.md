# ErpSaas.Modules.Shift

POS shift management — open, close, and cash-movement tracking for retail cashiers.

## Domain model

| Entity | Table | Schema |
|---|---|---|
| `Shift` | `Shift` | `shift` |
| `ShiftCashMovement` | `ShiftCashMovement` | `shift` |
| `ShiftDenominationCount` | `ShiftDenominationCount` | `shift` |

## Shift lifecycle

```
Open → Closed
Open → ForceClosed
```

`ShiftStatus` is a C# enum stored as `nvarchar(20)` (`CLAUDE.md §3.9`).

## Cross-module interface

Other modules (e.g., Billing) can query the active shift without coupling to
`ShiftService` directly via `IShiftLookup` (defined in `ErpSaas.Shared.Services`):

```csharp
Task<long?> GetActiveShiftIdAsync(long shopId, CancellationToken ct);
```

## Permissions

| Code | Purpose |
|---|---|
| `Shift.View` | List shifts and view current shift |
| `Shift.Open` | Open a new shift |
| `Shift.Close` | Close or force-close a shift |
| `Shift.CashMovement` | Record cash-in / cash-out movements |

## Notifications

A `SHIFT_CLOSED` SMS is sent via `INotificationService.EnqueueAsync` when a
shift transitions to `Closed` or `ForceClosed`.

## Wiring

1. Call `services.AddShiftModule()` after `AddInfrastructure()`.
2. `ShiftModelConfiguration.Configure(modelBuilder)` is applied inside `TenantDbContext`.
3. Add a project reference from `ErpSaas.Api` to `ErpSaas.Modules.Shift`.
