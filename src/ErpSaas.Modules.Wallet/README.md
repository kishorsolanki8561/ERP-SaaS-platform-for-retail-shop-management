# ErpSaas.Modules.Wallet

Customer wallet ledger — credit, debit, and balance inquiry for retail shops.

## Domain model

| Entity | Table | Schema |
|---|---|---|
| `WalletBalance` | `WalletBalance` | `wallet` |
| `WalletTransaction` | `WalletTransaction` | `wallet` |

## Transaction types

`WalletTransactionType` enum stored as `nvarchar(20)`:

| Value | Triggered by |
|---|---|
| `Credit` | Manual top-up via `POST /api/wallet/credit` |
| `Debit` | Invoice split-tender payment via `IWalletDebit` |

## Cross-module interface

`BillingService` calls wallet deductions through `IWalletDebit` (defined in
`ErpSaas.Shared.Services`) — it never injects `WalletService` directly. This
keeps the Billing and Wallet modules decoupled.

## Permissions

| Code | Purpose |
|---|---|
| `Wallet.View` | Read balances and transaction history |
| `Wallet.Credit` | Top up a customer wallet |
| `Wallet.Debit` | Deduct from a customer wallet |

## Notifications

A `WALLET_CREDITED` SMS is sent via `INotificationService.EnqueueAsync` after
every successful credit operation.

## Wiring

1. Call `services.AddWalletModule()` after `AddInfrastructure()`.
2. `WalletModelConfiguration.Configure(modelBuilder)` is applied inside `TenantDbContext`.
3. Add a project reference from `ErpSaas.Api` to `ErpSaas.Modules.Wallet`.
