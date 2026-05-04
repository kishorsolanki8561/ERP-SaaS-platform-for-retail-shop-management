# ErpSaas.Modules.Payment

Online Payment Tracking & Reconciliation (§6.13) + Payment Integration (§7.2).

## Schema
`payment` in TenantDB.

## Entities
| Entity | Purpose |
|---|---|
| `PaymentGatewayTransaction` | Every online payment attempt; source of truth for gateway activity |
| `PaymentGatewayAccount` | Per-shop gateway credentials (Razorpay, Stripe, PhonePe, Paytm) |
| `ReconciliationException` | Settlement mismatches flagged by the daily reconciliation job |

## Services
- `IPaymentGatewayService` — initiate, confirm, fail, cancel, refund, webhook intake, account management
- `IPaymentReconciliationService` — daily reconciliation, exception listing, exception resolution

## Job
`DailyReconciliationJob` runs at 02:00 UTC via Hangfire, detects unsettled transactions, and creates `ReconciliationException` rows.

## Permissions
`Payment.View` · `Payment.Initiate` · `Payment.Manage` · `Payment.Refund` · `Payment.Configure` · `Payment.Reconcile`

## Feature Flags
`Payment.OnlineGateway` · `Payment.UpiLinks`

## DDL Keys
`PAYMENT_GATEWAY` (Razorpay, Stripe, Paytm, PhonePe)
