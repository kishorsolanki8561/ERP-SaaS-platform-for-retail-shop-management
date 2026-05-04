using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Payment.Connectors;
using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Payment.Services;

public sealed class PaymentGatewayService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    IGatewayConnectorRegistry connectorRegistry)
    : BaseService<TenantDbContext>(db, errorLogger), IPaymentGatewayService
{
    public async Task<Result<long>> InitiateAsync(InitiatePaymentDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.Initiate", async () =>
        {
            var account = await _db.Set<PaymentGatewayAccount>()
                .FirstOrDefaultAsync(a => a.GatewayCode == dto.GatewayCode && a.IsActive, ct);
            if (account is null)
                return Result<long>.NotFound(Errors.Payment.GatewayAccountNotFound);

            var refNumber = $"TXN-{tenant.ShopId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

            var txn = new PaymentGatewayTransaction
            {
                ShopId = tenant.ShopId,
                GatewayCode = dto.GatewayCode,
                GatewayTxnId = string.Empty,
                OurReferenceNumber = refNumber,
                Purpose = dto.Purpose,
                SourceInvoiceId = dto.SourceInvoiceId,
                SourceWalletTopUpId = dto.SourceWalletTopUpId,
                SourceSubscriptionInvoiceId = dto.SourceSubscriptionInvoiceId,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Status = PaymentGatewayStatus.Initiated,
                InitiatedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<PaymentGatewayTransaction>().Add(txn);
            await _db.SaveChangesAsync(ct);

            // Call connector after saving the local row so we always have a record
            var connector = await connectorRegistry.ResolveAsync(dto.GatewayCode, ct);
            var initiateReq = new GatewayInitiateRequest(
                refNumber, dto.Amount, dto.Currency,
                $"Payment for {dto.Purpose}", null, null, null);

            var result = await connector.InitiateAsync(initiateReq, ct);
            if (!result.IsSuccess)
            {
                txn.Status = PaymentGatewayStatus.Failed;
                txn.FailureCode = result.FailureCode;
                txn.FailureMessage = result.FailureMessage;
                txn.CompletedAtUtc = DateTime.UtcNow;
                txn.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                txn.GatewayTxnId = result.GatewayTxnId;
                txn.PaymentUrl = result.PaymentUrl;
                txn.Status = PaymentGatewayStatus.Pending;
                txn.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(txn.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ConfirmAsync(long transactionId, ConfirmPaymentDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.Confirm", async () =>
        {
            var txn = await _db.Set<PaymentGatewayTransaction>()
                .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
            if (txn is null) return Result<bool>.NotFound(Errors.Payment.TransactionNotFound);
            if (txn.Status is PaymentGatewayStatus.Success or PaymentGatewayStatus.Failed or PaymentGatewayStatus.Cancelled)
                return Result<bool>.Conflict(Errors.Payment.TransactionAlreadyFinal);

            txn.GatewayTxnId = dto.GatewayTxnId;
            txn.Method = dto.Method;
            txn.Vpa = dto.Vpa;
            txn.CardLast4 = dto.CardLast4;
            txn.GatewayFee = dto.GatewayFee;
            txn.GatewayGst = dto.GatewayGst;
            txn.NetSettled = dto.NetSettled;
            txn.Status = PaymentGatewayStatus.Success;
            txn.CompletedAtUtc = DateTime.UtcNow;
            txn.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> FailAsync(long transactionId, FailPaymentDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.Fail", async () =>
        {
            var txn = await _db.Set<PaymentGatewayTransaction>()
                .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
            if (txn is null) return Result<bool>.NotFound(Errors.Payment.TransactionNotFound);
            if (txn.Status is PaymentGatewayStatus.Success or PaymentGatewayStatus.Failed or PaymentGatewayStatus.Cancelled)
                return Result<bool>.Conflict(Errors.Payment.TransactionAlreadyFinal);

            if (!string.IsNullOrEmpty(dto.GatewayTxnId))
                txn.GatewayTxnId = dto.GatewayTxnId;
            txn.FailureCode = dto.FailureCode;
            txn.FailureMessage = dto.FailureMessage;
            txn.Status = PaymentGatewayStatus.Failed;
            txn.CompletedAtUtc = DateTime.UtcNow;
            txn.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> RefundAsync(long transactionId, RefundPaymentDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.Refund", async () =>
        {
            var txn = await _db.Set<PaymentGatewayTransaction>()
                .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
            if (txn is null) return Result<bool>.NotFound(Errors.Payment.TransactionNotFound);
            if (txn.Status != PaymentGatewayStatus.Success)
                return Result<bool>.Conflict(Errors.Payment.RefundRequiresSuccess);
            if (dto.RefundAmount > txn.Amount)
                return Result<bool>.Conflict(Errors.Payment.RefundExceedsAmount);

            // Call the real gateway refund API before touching local status
            var connector = await connectorRegistry.ResolveAsync(txn.GatewayCode, ct);
            var refundReq = new GatewayRefundRequest(
                txn.GatewayTxnId, txn.OurReferenceNumber, dto.RefundAmount, dto.Reason);

            var result = await connector.RefundAsync(refundReq, ct);
            if (!result.IsSuccess)
                return Result<bool>.Failure($"{result.FailureCode}: {result.FailureMessage}");

            txn.RefundGatewayTxnId = result.GatewayRefundId;
            txn.Status = dto.RefundAmount == txn.Amount
                ? PaymentGatewayStatus.Refunded
                : PaymentGatewayStatus.PartiallyRefunded;
            txn.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CancelAsync(long transactionId, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.Cancel", async () =>
        {
            var txn = await _db.Set<PaymentGatewayTransaction>()
                .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
            if (txn is null) return Result<bool>.NotFound(Errors.Payment.TransactionNotFound);
            if (txn.Status is PaymentGatewayStatus.Success or PaymentGatewayStatus.Failed or PaymentGatewayStatus.Cancelled)
                return Result<bool>.Conflict(Errors.Payment.TransactionAlreadyFinal);

            txn.Status = PaymentGatewayStatus.Cancelled;
            txn.CompletedAtUtc = DateTime.UtcNow;
            txn.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<PaymentGatewayTransactionDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var txn = await _db.Set<PaymentGatewayTransaction>()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        return txn is null ? null : Map(txn);
    }

    public async Task<IReadOnlyList<PaymentGatewayTransactionDto>> ListAsync(PaymentTransactionListFilter filter, CancellationToken ct = default)
    {
        var q = _db.Set<PaymentGatewayTransaction>().AsQueryable();
        if (filter.Status.HasValue) q = q.Where(t => t.Status == filter.Status.Value);
        if (filter.GatewayCode is not null) q = q.Where(t => t.GatewayCode == filter.GatewayCode);
        if (filter.Purpose.HasValue) q = q.Where(t => t.Purpose == filter.Purpose.Value);
        if (filter.FromDate.HasValue) q = q.Where(t => t.InitiatedAtUtc >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(t => t.InitiatedAtUtc <= filter.ToDate.Value);

        return await q
            .OrderByDescending(t => t.InitiatedAtUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => Map(t))
            .ToListAsync(ct);
    }

    public async Task<Result<bool>> HandleWebhookAsync(string gatewayCode, string rawPayload, string signature, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.HandleWebhook", async () =>
        {
            var account = await _db.Set<PaymentGatewayAccount>()
                .FirstOrDefaultAsync(a => a.GatewayCode == gatewayCode && a.IsActive, ct);
            if (account is null)
                return Result<bool>.NotFound(Errors.Payment.GatewayAccountNotFound);

            var connector = await connectorRegistry.ResolveAsync(gatewayCode, ct);
            var secret = account.WebhookSecretEncrypted ?? string.Empty;
            if (!connector.VerifyWebhookSignature(rawPayload, signature, secret))
                return Result<bool>.Failure("INVALID_SIGNATURE: Webhook signature verification failed.");

            var evt = connector.ParseWebhookEvent(rawPayload);
            if (evt is null)
                return Result<bool>.Success(true); // Unknown/unactionable event — ack and ignore

            var txn = await _db.Set<PaymentGatewayTransaction>()
                .FirstOrDefaultAsync(t => t.GatewayTxnId == evt.GatewayTxnId, ct);
            if (txn is null)
                return Result<bool>.Success(true); // Not our transaction — ack

            switch (evt.EventType)
            {
                case "payment.captured":
                    if (txn.Status is not PaymentGatewayStatus.Success)
                    {
                        txn.Status = PaymentGatewayStatus.Success;
                        txn.CompletedAtUtc = DateTime.UtcNow;
                        txn.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    break;

                case "payment.failed":
                    if (txn.Status is not PaymentGatewayStatus.Failed)
                    {
                        txn.Status = PaymentGatewayStatus.Failed;
                        txn.FailureCode = evt.FailureCode;
                        txn.FailureMessage = evt.FailureMessage;
                        txn.CompletedAtUtc = DateTime.UtcNow;
                        txn.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    break;

                case "refund.processed":
                    if (!string.IsNullOrEmpty(evt.GatewayRefundId))
                    {
                        txn.RefundGatewayTxnId = evt.GatewayRefundId;
                        txn.UpdatedAtUtc = DateTime.UtcNow;
                    }
                    break;
            }

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<IReadOnlyList<PaymentGatewayAccountDto>> ListAccountsAsync(CancellationToken ct = default)
    {
        return await _db.Set<PaymentGatewayAccount>()
            .Select(a => new PaymentGatewayAccountDto(a.Id, a.GatewayCode, a.IsActive, a.IsDefault))
            .ToListAsync(ct);
    }

    public async Task<Result<long>> UpsertAccountAsync(UpsertGatewayAccountDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.UpsertAccount", async () =>
        {
            var existing = await _db.Set<PaymentGatewayAccount>()
                .FirstOrDefaultAsync(a => a.GatewayCode == dto.GatewayCode, ct);

            if (dto.IsDefault)
            {
                await _db.Set<PaymentGatewayAccount>()
                    .Where(a => a.IsDefault && a.GatewayCode != dto.GatewayCode)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);
            }

            if (existing is null)
            {
                existing = new PaymentGatewayAccount
                {
                    ShopId = tenant.ShopId,
                    GatewayCode = dto.GatewayCode,
                    CredentialsJsonEncrypted = dto.CredentialsJson,
                    WebhookSecretEncrypted = dto.WebhookSecret,
                    IsActive = dto.IsActive,
                    IsDefault = dto.IsDefault,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                _db.Set<PaymentGatewayAccount>().Add(existing);
            }
            else
            {
                existing.CredentialsJsonEncrypted = dto.CredentialsJson;
                existing.WebhookSecretEncrypted = dto.WebhookSecret;
                existing.IsActive = dto.IsActive;
                existing.IsDefault = dto.IsDefault;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(existing.Id);
        }, ct, useTransaction: true);
    }

    private static PaymentGatewayTransactionDto Map(PaymentGatewayTransaction t) => new(
        t.Id, t.GatewayCode, t.GatewayTxnId, t.OurReferenceNumber,
        t.Purpose, t.Amount, t.Currency, t.Method, t.Vpa, t.CardLast4,
        t.Status, t.FailureCode, t.FailureMessage,
        t.InitiatedAtUtc, t.CompletedAtUtc,
        t.GatewayFee, t.GatewayGst, t.NetSettled,
        t.SettledAtUtc, t.SettlementReference,
        t.RefundGatewayTxnId, t.PaymentUrl);
}
