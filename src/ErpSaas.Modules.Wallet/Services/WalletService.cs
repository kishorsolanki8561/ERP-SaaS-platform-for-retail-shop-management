#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Wallet.Entities;
using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Wallet.Services;

public sealed class WalletService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    INotificationService? notifications = null,
    ILogger<WalletService>? logger = null)
    : BaseService<TenantDbContext>(db, errorLogger), IWalletService, IWalletDebit
{
    public async Task<PagedResult<WalletBalanceDto>> ListBalancesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default)
    {
        var query = db.Set<WalletBalance>().Where(w => !w.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.CustomerNameSnapshot.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(w => w.Balance)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WalletBalanceDto(
                w.CustomerId,
                w.CustomerNameSnapshot,
                w.Balance,
                w.LastTransactionAtUtc))
            .ToListAsync(ct);

        return new PagedResult<WalletBalanceDto>(items, total, page, pageSize);
    }

    public Task<WalletBalanceDto?> GetBalanceAsync(long customerId, CancellationToken ct = default)
        => db.Set<WalletBalance>()
            .Where(w => w.CustomerId == customerId && !w.IsDeleted)
            .Select(w => (WalletBalanceDto?)new WalletBalanceDto(
                w.CustomerId,
                w.CustomerNameSnapshot,
                w.Balance,
                w.LastTransactionAtUtc))
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<WalletTransactionDto>> ListTransactionsAsync(
        long customerId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Set<WalletTransaction>()
            .Where(t => t.CustomerId == customerId && !t.IsDeleted);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new WalletTransactionDto(
                t.Id,
                t.TransactionType,
                t.Amount,
                t.BalanceBefore,
                t.BalanceAfter,
                t.ReferenceType,
                t.ReferenceNumber,
                t.ReceiptNumber,
                t.Notes,
                t.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<WalletTransactionDto>(items, total, page, pageSize);
    }

    public async Task<Result<WalletCreditResultDto>> CreditAsync(
        WalletCreditDto dto,
        CancellationToken ct = default)
    {
        var result = await ExecuteAsync<WalletCreditResultDto>("Wallet.Credit", async () =>
        {
            if (dto.Amount <= 0)
                return Result<WalletCreditResultDto>.Failure(Errors.Wallet.InvalidAmount);

            var balance = await GetOrCreateBalanceAsync(dto.CustomerId, dto.CustomerName, ct);
            var balanceBefore = balance.Balance;
            var balanceAfter = balanceBefore + dto.Amount;

            var receiptNumber = await sequence.NextAsync(
                Constants.SequenceCodes.PaymentReceipt, tenant.ShopId, ct);

            db.Set<WalletTransaction>().Add(new WalletTransaction
            {
                ShopId = tenant.ShopId,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = dto.CustomerName,
                TransactionType = WalletTransactionType.Credit,
                Amount = dto.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                ReferenceType = dto.ReferenceType ?? "Manual",
                ReferenceId = dto.ReferenceId,
                ReferenceNumber = dto.ReferenceNumber,
                ReceiptNumber = receiptNumber,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            });

            balance.Balance = balanceAfter;
            balance.CustomerNameSnapshot = dto.CustomerName;
            balance.LastTransactionAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Result<WalletCreditResultDto>.Success(new WalletCreditResultDto(receiptNumber, balanceAfter));
        }, ct, useTransaction: true);

        if (result.IsSuccess && dto.CustomerPhone is not null)
            await TrySendCreditNotificationAsync(dto, result.Value!, ct);

        return result;
    }

    private async Task TrySendCreditNotificationAsync(
        WalletCreditDto dto, WalletCreditResultDto credited, CancellationToken ct)
    {
        if (notifications is null) return;
        try
        {
            await notifications.EnqueueAsync(
                tenant.ShopId,
                NotificationChannel.Sms,
                dto.CustomerPhone!,
                Constants.NotificationCodes.WalletCredited,
                new Dictionary<string, string>
                {
                    { "CustomerName",   dto.CustomerName },
                    { "Amount",         dto.Amount.ToString("F2") },
                    { "Balance",        credited.NewBalance.ToString("F2") },
                    { "ReceiptNumber",  credited.ReceiptNumber },
                },
                correlationId: $"WalletCredit:{dto.CustomerId}",
                ct);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Non-critical: failed to enqueue wallet credit SMS for customer {Id}", dto.CustomerId);
        }
    }

    public async Task<Result<bool>> DebitAsync(
        WalletDebitDto dto,
        CancellationToken ct = default)
        => await ExecuteAsync<bool>("Wallet.Debit", async () =>
        {
            if (dto.Amount <= 0)
                return Result<bool>.Failure(Errors.Wallet.InvalidAmount);

            var balance = await db.Set<WalletBalance>()
                .FirstOrDefaultAsync(w => w.CustomerId == dto.CustomerId && !w.IsDeleted, ct);

            if (balance is null || balance.Balance < dto.Amount)
                return Result<bool>.Conflict(Errors.Wallet.InsufficientBalance);

            var balanceBefore = balance.Balance;
            var balanceAfter = balanceBefore - dto.Amount;

            db.Set<WalletTransaction>().Add(new WalletTransaction
            {
                ShopId = tenant.ShopId,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = balance.CustomerNameSnapshot,
                TransactionType = WalletTransactionType.Debit,
                Amount = dto.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                ReferenceType = dto.ReferenceType ?? "Manual",
                ReferenceId = dto.ReferenceId,
                ReferenceNumber = dto.ReferenceNumber,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            });

            balance.Balance = balanceAfter;
            balance.LastTransactionAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> DebitForInvoiceAsync(
        long customerId,
        long invoiceId,
        string invoiceNumber,
        decimal amount,
        CancellationToken ct = default)
        => await DebitAsync(new WalletDebitDto(
            customerId,
            amount,
            "Invoice",
            invoiceId,
            invoiceNumber,
            $"Payment against invoice {invoiceNumber}"), ct);

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<WalletBalance> GetOrCreateBalanceAsync(
        long customerId,
        string customerName,
        CancellationToken ct)
    {
        var existing = await db.Set<WalletBalance>()
            .FirstOrDefaultAsync(w => w.CustomerId == customerId && !w.IsDeleted, ct);

        if (existing is not null)
            return existing;

        var newBalance = new WalletBalance
        {
            ShopId = tenant.ShopId,
            CustomerId = customerId,
            CustomerNameSnapshot = customerName,
            Balance = 0m,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Set<WalletBalance>().Add(newBalance);
        return newBalance;
    }
}
