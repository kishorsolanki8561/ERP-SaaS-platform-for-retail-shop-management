using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Wallet.Entities;
using ErpSaas.Modules.Wallet.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Wallet.Services;

public sealed class WalletTopUpService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    IWalletService walletService)
    : BaseService<TenantDbContext>(db, errorLogger), IWalletTopUpService
{
    public async Task<Result<long>> InitiateAsync(InitiateTopUpDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Wallet.TopUp.Initiate", async () =>
        {
            if (dto.Amount <= 0)
                return Result<long>.Failure(Errors.Wallet.InvalidAmount);

            var topUp = new WalletTopUp
            {
                ShopId = tenant.ShopId,
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = dto.CustomerName,
                Amount = dto.Amount,
                PaymentModeCode = dto.PaymentModeCode,
                Status = WalletTopUpStatus.Pending,
                Notes = dto.Notes,
                InitiatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
            };

            _db.Set<WalletTopUp>().Add(topUp);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(topUp.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CompleteAsync(long topUpId, CompleteTopUpDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Wallet.TopUp.Complete", async () =>
        {
            var topUp = await _db.Set<WalletTopUp>()
                .FirstOrDefaultAsync(t => t.Id == topUpId, ct);

            if (topUp is null)
                return Result<bool>.NotFound(Errors.Wallet.TopUpNotFound);
            if (topUp.Status != WalletTopUpStatus.Pending)
                return Result<bool>.Conflict(Errors.Wallet.TopUpNotPending);

            var creditResult = await walletService.CreditAsync(new WalletCreditDto(
                topUp.CustomerId,
                topUp.CustomerNameSnapshot,
                topUp.Amount,
                "TOP_UP",
                topUpId,
                null,
                topUp.Notes), ct);

            if (!creditResult.IsSuccess)
                return Result<bool>.Failure(creditResult.Errors.FirstOrDefault() ?? "WALLET_ERR");

            topUp.Status = WalletTopUpStatus.Success;
            topUp.ReceiptNumber = creditResult.Value!.ReceiptNumber;
            topUp.PaymentGatewayTransactionId = dto.PaymentGatewayTransactionId;
            topUp.CompletedAtUtc = DateTime.UtcNow;
            topUp.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> FailAsync(long topUpId, string reason, CancellationToken ct = default)
    {
        return await ExecuteAsync("Wallet.TopUp.Fail", async () =>
        {
            var topUp = await _db.Set<WalletTopUp>()
                .FirstOrDefaultAsync(t => t.Id == topUpId, ct);

            if (topUp is null)
                return Result<bool>.NotFound(Errors.Wallet.TopUpNotFound);
            if (topUp.Status != WalletTopUpStatus.Pending)
                return Result<bool>.Conflict(Errors.Wallet.TopUpNotPending);

            topUp.Status = WalletTopUpStatus.Failed;
            topUp.FailureReason = reason;
            topUp.CompletedAtUtc = DateTime.UtcNow;
            topUp.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<WalletTopUpDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var t = await _db.Set<WalletTopUp>().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? null : Map(t);
    }

    public async Task<IReadOnlyList<WalletTopUpDto>> ListAsync(
        long customerId, int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Set<WalletTopUp>()
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.InitiatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => Map(t))
            .ToListAsync(ct);
    }

    private static WalletTopUpDto Map(WalletTopUp t) => new(
        t.Id, t.CustomerId, t.CustomerNameSnapshot, t.Amount,
        t.PaymentModeCode, t.Status, t.ReceiptNumber, t.Notes,
        t.InitiatedAtUtc, t.CompletedAtUtc, t.FailureReason);
}
